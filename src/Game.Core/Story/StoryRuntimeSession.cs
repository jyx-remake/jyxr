using System.Runtime.CompilerServices;

namespace Game.Core.Story;

internal sealed partial class StoryRuntimeSession(
    StoryScript script,
    IRuntimeHost host,
    string? startSegment,
    CancellationToken cancellationToken)
{
    private const string GameOverCommand = "gameover";

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
            string? jumpTarget = null;

            await foreach (var stepResult in ExecuteSegmentAsync(segment, ct))
            {
                if (stepResult.Event is not null)
                {
                    yield return stepResult.Event;
                }

                if (stepResult.Control == StepControl.Jump)
                {
                    jumpTarget = stepResult.Target;
                    break;
                }

                if (stepResult.Control is StepControl.Terminate or StepControl.Return)
                {
                    yield break;
                }
            }

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

    private bool TryGetSegment(string name, out Segment segment)
    {
        if (_segments.TryGetValue(name, out segment!))
        {
            return true;
        }

        throw new StoryRuntimeException($"Segment '{name}' does not exist.");
    }

    private async IAsyncEnumerable<StepResult> ExecuteSegmentAsync(
        Segment segment,
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return StepResult.FromEvent(new SegmentStartedEvent(segment.Name));

        StepResult? control = null;
        await foreach (var stepResult in ExecuteStepsAsync(segment.Steps, ct))
        {
            if (stepResult.Event is not null)
            {
                yield return stepResult;
            }

            if (stepResult.IsControl)
            {
                control = stepResult;
                break;
            }
        }

        if (control?.Control == StepControl.Terminate)
        {
            yield return StepResult.Terminate();
            yield break;
        }

        yield return StepResult.FromEvent(new SegmentCompletedEvent(segment.Name));

        if (control?.Control == StepControl.Jump)
        {
            yield return StepResult.Jump(control.Target!);
            yield break;
        }

        if (control?.Control == StepControl.Return)
        {
            yield return StepResult.Return();
        }
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
                if (result.IsControl)
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
                    yield return StepResult.Jump(result.JumpTarget);
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
                yield return StepResult.Jump(jump.Target);
                yield break;
            case CallStep call:
                if (!TryGetSegment(call.Target, out var segment))
                {
                    yield break;
                }

                await foreach (var result in ExecuteSegmentAsync(segment, ct))
                {
                    if (result.Event is not null)
                    {
                        yield return result;
                    }

                    if (result.Control is StepControl.Terminate or StepControl.Jump)
                    {
                        yield return result;
                        yield break;
                    }

                    if (result.Control == StepControl.Return)
                    {
                        yield break;
                    }
                }

                yield break;
            case ReturnStep:
                yield return StepResult.Return();
                yield break;
            default:
                throw new StoryRuntimeException($"Unsupported step type '{step.GetType().Name}'.");
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

}
