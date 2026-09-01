using System.Runtime.CompilerServices;

namespace Game.Core.Story;

internal sealed partial class StoryRuntimeSession(
    StoryScript script,
    IStoryRuntimeContext host,
    string? startSegment,
    CancellationToken cancellationToken)
{
    private readonly ExpressionEvaluator _expressionEvaluator = new();

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
            if (!host.IsExecutionActive)
            {
                yield break;
            }

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
            if (!host.IsExecutionActive)
            {
                control = StepResult.Terminate();
                break;
            }

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

        if (!host.IsExecutionActive || control?.Control == StepControl.Terminate)
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
                var context = new DialogueContext(dialogue.Speaker, dialogue.Text, dialogue.Portrait);
                yield return StepResult.FromEvent(new DialogueReadyEvent(context));
                await host.DialogueAsync(context, ct);
                yield break;
            case CommandStep command:
            {
                IReadOnlyList<ExpressionValue> args = [];
                StoryCommandResult? result = null;
                string? failureMessage = null;
                try
                {
                    args = _expressionEvaluator.EvaluateArguments(command.Call, host.ExpressionEnvironment);
                    result = await host.Commands.InvokeAsync(command.Call.Root.Name, args, ct);
                }
                catch (Exception exception) when (
                    host.ContinueOnCommandFailure &&
                    (exception is not OperationCanceledException || !ct.IsCancellationRequested))
                {
                    var contextual = exception is ExpressionException expressionException
                        ? ExpressionException.WithLocation(
                            expressionException,
                            command.Call.SourceName,
                            command.Call.Root.Span)
                        : exception;
                    failureMessage = contextual.Message;
                }

                if (failureMessage is not null)
                {
                    await host.CommandFailedAsync(command.Call.Root.Name, failureMessage, ct);
                    yield return StepResult.FromEvent(new CommandFailedEvent(command.Call.Root.Name, failureMessage));
                    yield break;
                }

                yield return StepResult.FromEvent(new CommandExecutedEvent(command.Call.Root.Name, args));
                if (result is { TerminatesStory: true })
                {
                    yield return StepResult.FromEvent(new StoryTerminatedEvent());
                    yield return StepResult.Terminate();
                    yield break;
                }
                if (result is { } commandResult && commandResult.JumpTarget is not null)
                {
                    yield return StepResult.FromEvent(new JumpEvent(commandResult.JumpTarget));
                    yield return StepResult.Jump(commandResult.JumpTarget);
                }

                yield break;
            }
            case SetVariableStep assignment:
            {
                var value = _expressionEvaluator.Evaluate(assignment.Value, host.ExpressionEnvironment);
                await host.AssignVariableAsync(assignment.Target, value, ct);
                yield return StepResult.FromEvent(new VariableAssignedEvent(assignment.Target, value));
                yield break;
            }
            case DeleteVariableStep deletion:
                if (await host.DeleteVariableAsync(deletion.Target, ct))
                {
                    yield return StepResult.FromEvent(new VariableDeletedEvent(deletion.Target));
                }
                yield break;
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

}
