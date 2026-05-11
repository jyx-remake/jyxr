using System.Runtime.CompilerServices;

namespace Game.Core.Story;

internal sealed class StoryRuntimeSession(
    StoryScript script,
    IRuntimeHost host,
    string? startSegment,
    CancellationToken cancellationToken)
{
    private readonly IReadOnlyDictionary<string, Segment> _segments =
        script.Segments.ToDictionary(segment => segment.Name, StringComparer.Ordinal);

    private string _currentSegmentName = startSegment ?? script.Segments.FirstOrDefault()?.Name ?? string.Empty;

    public async IAsyncEnumerable<StoryEvent> RunAsync([EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(host);

        if (script.Segments.Count == 0)
        {
            yield break;
        }

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, enumeratorCancellationToken);
        var ct = linkedCancellation.Token;

        while (TryGetCurrentSegment(out var segment))
        {
            yield return new SegmentStartedEvent(segment.Name);
            string? jumpTarget = null;

            await foreach (var stepResult in ExecuteStepsAsync(segment.Steps, ct))
            {
                if (stepResult.Event is not null)
                {
                    yield return stepResult.Event;
                }

                if (stepResult.JumpTarget is not null)
                {
                    jumpTarget = stepResult.JumpTarget;
                    break;
                }
            }

            yield return new SegmentCompletedEvent(segment.Name);

            if (jumpTarget is null)
            {
                yield break;
            }

            _currentSegmentName = jumpTarget;
        }
    }

    private bool TryGetCurrentSegment(out Segment segment)
    {
        if (_segments.TryGetValue(_currentSegmentName, out segment!))
        {
            return true;
        }

        throw new StoryRuntimeException($"Segment '{_currentSegmentName}' does not exist.");
    }

    private async IAsyncEnumerable<StepResult> ExecuteStepsAsync(
        IReadOnlyList<Step> steps,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();

            await foreach (var result in ExecuteStepAsync(step, ct))
            {
                yield return result;
                if (result.JumpTarget is not null)
                {
                    yield break;
                }
            }
        }
    }

    private async IAsyncEnumerable<StepResult> ExecuteStepAsync(
        Step step,
        [EnumeratorCancellation] CancellationToken ct)
    {
        switch (step)
        {
            case DialogueStep dialogue:
                var context = new DialogueContext(dialogue.Speaker, dialogue.Text);
                yield return StepResult.FromEvent(new DialogueReadyEvent(context));
                await host.DialogueAsync(context, ct);
                yield break;
            case CommandStep command:
            {
                var args = await EvaluateValueArgsAsync(command.Args, ct);
                var result = await host.ExecuteCommandAsync(command.Name, args, ct);
                yield return StepResult.FromEvent(new CommandExecutedEvent(command.Name, args));
                if (result.JumpTarget is not null)
                {
                    yield return StepResult.FromEvent(new JumpEvent(result.JumpTarget));
                    yield return StepResult.FromJump(result.JumpTarget);
                }

                yield break;
            }
            case ChoiceStep choice:
                await foreach (var result in ExecuteChoiceAsync(choice, ct))
                {
                    yield return result;
                }

                yield break;
            case BattleStep battle:
                await foreach (var result in ExecuteBattleAsync(battle, ct))
                {
                    yield return result;
                }

                yield break;
            case BranchStep branch:
                await foreach (var result in ExecuteBranchAsync(branch, ct))
                {
                    yield return result;
                }

                yield break;
            case JumpStep jump:
                yield return StepResult.FromEvent(new JumpEvent(jump.Target));
                yield return StepResult.FromJump(jump.Target);
                yield break;
            default:
                throw new StoryRuntimeException($"Unsupported step type '{step.GetType().Name}'.");
        }
    }

    private async IAsyncEnumerable<StepResult> ExecuteChoiceAsync(
        ChoiceStep choice,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = new ChoiceContext(
            choice.Prompt.Speaker,
            choice.Prompt.Text,
            choice.Options.Select((option, index) => new ChoiceOptionView(index, option.Text)).ToArray());

        yield return StepResult.FromEvent(new ChoiceOfferedEvent(context));

        var selectedIndex = await host.ChooseOptionAsync(context, ct);
        if (selectedIndex < 0 || selectedIndex >= choice.Options.Count)
        {
            throw new StoryRuntimeException(
                $"Choice selection index {selectedIndex} is out of range for {choice.Options.Count} options.");
        }

        yield return StepResult.FromEvent(new ChoiceResolvedEvent(context, selectedIndex));

        await foreach (var result in ExecuteStepsAsync(choice.Options[selectedIndex].Steps, ct))
        {
            yield return result;
            if (result.JumpTarget is not null)
            {
                yield break;
            }
        }
    }

    private async IAsyncEnumerable<StepResult> ExecuteBattleAsync(
        BattleStep battle,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = new BattleContext(battle.BattleId, battle.Outcomes.Keys.ToArray());
        yield return StepResult.FromEvent(new BattleStartedEvent(context));

        var selectedOutcome = await host.ResolveBattleAsync(context, ct);
        if (!battle.Outcomes.TryGetValue(selectedOutcome, out var steps))
        {
            throw new StoryRuntimeException(
                $"Battle '{battle.BattleId}' resolved to '{selectedOutcome}', but the script does not define that outcome.");
        }

        yield return StepResult.FromEvent(new BattleResolvedEvent(context, selectedOutcome));

        await foreach (var result in ExecuteStepsAsync(steps, ct))
        {
            yield return result;
            if (result.JumpTarget is not null)
            {
                yield break;
            }
        }
    }

    private async IAsyncEnumerable<StepResult> ExecuteBranchAsync(
        BranchStep branch,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var branchCase in branch.Cases)
        {
            var result = await ExpressionEvaluator.EvaluateAsync(branchCase.When, host, ct);
            if (!result.AsBoolean("branch condition"))
            {
                continue;
            }

            await foreach (var stepResult in ExecuteStepsAsync(branchCase.Steps, ct))
            {
                yield return stepResult;
                if (stepResult.JumpTarget is not null)
                {
                    yield break;
                }
            }

            yield break;
        }

        if (branch.Fallback is null)
        {
            yield break;
        }

        await foreach (var stepResult in ExecuteStepsAsync(branch.Fallback, ct))
        {
            yield return stepResult;
            if (stepResult.JumpTarget is not null)
            {
                yield break;
            }
        }
    }

    private async Task<IReadOnlyList<ExprValue>> EvaluateValueArgsAsync(
        IReadOnlyList<ExprNode> args,
        CancellationToken ct)
    {
        var values = new List<ExprValue>(args.Count);
        foreach (var arg in args)
        {
            values.Add(await ExpressionEvaluator.EvaluateValueArgAsync(arg, host, ct));
        }

        return values;
    }

    private sealed record StepResult(StoryEvent? Event, string? JumpTarget)
    {
        public static StepResult FromEvent(StoryEvent storyEvent) => new(storyEvent, null);

        public static StepResult FromJump(string jumpTarget) => new(null, jumpTarget);
    }
}
