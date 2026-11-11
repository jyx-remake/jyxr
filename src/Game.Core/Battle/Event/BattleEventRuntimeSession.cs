using System.Runtime.CompilerServices;
using Game.Core.Story;
namespace Game.Core.Battle;

internal sealed partial class BattleEventRuntimeSession(
    BattleEventScript script,
    BattleMainSegment battleMainSegment,
    BattleState state,
    IStoryRuntimeContext host,
    string? startSegment,
    CancellationToken cancellationToken)
{
    private readonly ExpressionEvaluator _expressionEvaluator = new();

    private readonly IReadOnlyDictionary<string, BattleMainSegment> _segments =
        script.Segments.ToDictionary(segment => segment.Name, StringComparer.Ordinal);

    private string _currentSegmentName = startSegment ?? script.Segments.FirstOrDefault()?.Name ?? string.Empty;

    public async IAsyncEnumerable<BattleEvent> RunAsync([EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
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

            await foreach (var stepResult in ExecuteMainSegmentAsync(battleMainSegment, ct))
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

    private bool TryGetCurrentSegment(out BattleMainSegment segment)
    {
        if (_segments.TryGetValue(_currentSegmentName, out segment!))
        {
            return true;
        }

        throw new StoryRuntimeException($"Segment '{_currentSegmentName}' does not exist.");
    }

    private bool TryGetSegment(string name, out BattleMainSegment segment)
    {
        if (_segments.TryGetValue(name, out segment!))
        {
            return true;
        }

        throw new StoryRuntimeException($"Segment '{name}' does not exist.");
    }

    private async IAsyncEnumerable<StepResult> ExecuteMainSegmentAsync(
        BattleMainSegment segment,
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return StepResult.FromEvent(new SegmentStartedEvent(segment.Name));

        StepResult? control = null;
        await foreach (var stepResult in ExecuteSmallSegmentAsync(segment, ct))
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
    private async IAsyncEnumerable<StepResult> ExecuteSmallSegmentAsync(
        BattleMainSegment mainSegment,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var smallSegments = mainSegment.BattleSmallSegments;
        for (var index = 0; index < smallSegments.Count; index++)
        {
            var smallSegment = smallSegments[index];
            if (smallSegment.MaxExecutions is { } maxExecutions &&
                state.BattleEvents.CountExecutions(mainSegment.Name, index) >= maxExecutions)
            {
                continue;
            }

            state.BattleEvents.RecordExecution(mainSegment.Name, index);

            await foreach (var stepResult in ExecuteBranchAsync(smallSegment, ct))
            {
                yield return stepResult;
            }
        }
    }
    private async IAsyncEnumerable<StepResult> ExecuteBranchAsync(
    BattleSmallSegment battleSmallSegments,
    [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var branchCase in battleSmallSegments.Cases)
        {
            if (branchCase.When !=null&& !_expressionEvaluator.EvaluateBoolean(
                branchCase.When,
                host.ExpressionEnvironment,
                "branch condition"))
            {
                continue;
            }

            await foreach (var stepResult in ExecuteStepsAsync(branchCase.Steps, ct))
            {
                yield return stepResult;
                if (stepResult.IsControl)
                {
                    yield break;
                }
            }

            yield break;
        }

        if (battleSmallSegments.Fallback is null)
        {
            yield break;
        }

        await foreach (var stepResult in ExecuteStepsAsync(battleSmallSegments.Fallback, ct))
        {
            yield return stepResult;
            if (stepResult.IsControl)
            {
                yield break;
            }
        }
    }
    private async IAsyncEnumerable<StepResult> ExecuteStepsAsync(
        IReadOnlyList<BattleStep> steps,
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

    private async IAsyncEnumerable<StepResult> ExecuteChoiceAsync(
        ChoiceStep choice,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var availableOptions = new Dictionary<int, ChoiceOption>();
        var optionViews = new List<ChoiceOptionView>();
        var sourceIndex = 0;

        foreach (var block in choice.Blocks)
        {
            switch (block)
            {
                case ChoiceOptionsBlock optionsBlock:
                    AddChoiceOptions(optionsBlock.Options, true, availableOptions, optionViews, ref sourceIndex);
                    break;
                case ChoiceBranchBlock branchBlock:
                    AddChoiceBranchOptions(branchBlock, availableOptions, optionViews, ref sourceIndex);
                    break;
                default:
                    throw new StoryRuntimeException($"Unsupported choice block type '{block.GetType().Name}'.");
            }
        }

        if (availableOptions.Count == 0)
        {
            throw new StoryRuntimeException(
                $"Choice '{choice.Prompt.Text}' has no available options after evaluating its conditions.");
        }

        var context = new ChoiceContext(
            choice.Prompt.Speaker,
            choice.Prompt.Text,
            optionViews,
            choice.Style);

        yield return StepResult.FromEvent(new ChoiceOfferedEvent(context));

        var selectedIndex = await host.ChooseOptionAsync(context, ct);
        if (!availableOptions.TryGetValue(selectedIndex, out var selectedOption))
        {
            throw new StoryRuntimeException(
                $"Choice selection index {selectedIndex} is not an available option.");
        }

        yield return StepResult.FromEvent(new ChoiceResolvedEvent(context, selectedIndex));

        await foreach (var result in ExecuteStepsAsync(selectedOption.Steps, ct))
        {
            yield return result;
            if (result.IsControl)
            {
                yield break;
            }
        }
    }

    private void AddChoiceBranchOptions(
        ChoiceBranchBlock block,
        IDictionary<int, ChoiceOption> availableOptions,
        ICollection<ChoiceOptionView> optionViews,
        ref int sourceIndex)
    {
        var selectedCaseIndex = -1;
        for (var index = 0; index < block.Cases.Count; index++)
        {
            if (!_expressionEvaluator.EvaluateBoolean(
                    block.Cases[index].When,
                    host.ExpressionEnvironment,
                    "choice branch condition"))
            {
                continue;
            }

            selectedCaseIndex = index;
            break;
        }

        for (var index = 0; index < block.Cases.Count; index++)
        {
            AddChoiceOptions(
                block.Cases[index].Options,
                index == selectedCaseIndex,
                availableOptions,
                optionViews,
                ref sourceIndex);
        }

        if (block.Fallback is not null)
        {
            AddChoiceOptions(
                block.Fallback,
                selectedCaseIndex < 0,
                availableOptions,
                optionViews,
                ref sourceIndex);
        }
    }

    private void AddChoiceOptions(
        IReadOnlyList<ChoiceOption> options,
        bool blockIsActive,
        IDictionary<int, ChoiceOption> availableOptions,
        ICollection<ChoiceOptionView> optionViews,
        ref int sourceIndex)
    {
        foreach (var option in options)
        {
            var isAvailable = blockIsActive;
            if (isAvailable && option.When is not null)
            {
                isAvailable = _expressionEvaluator.EvaluateBoolean(
                    option.When,
                    host.ExpressionEnvironment,
                    "choice option condition");
            }

            if (isAvailable)
            {
                availableOptions.Add(sourceIndex, option);
                optionViews.Add(new ChoiceOptionView(sourceIndex, option.Text));
            }

            sourceIndex += 1;
        }
    }

    private async IAsyncEnumerable<StepResult> ExecuteStepAsync(
        BattleStep step,
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
                    var args = _expressionEvaluator.EvaluateArguments(command.Call, host.ExpressionEnvironment);
                    StoryCommandResult result;
                    try
                    {
                        result = await host.Commands.InvokeAsync(command.Call.Root.Name, args, ct);
                    }
                    catch (ExpressionException exception)
                    {
                        throw ExpressionException.WithLocation(
                            exception,
                            command.Call.SourceName,
                            command.Call.Root.Span);
                    }
                    yield return StepResult.FromEvent(new CommandExecutedEvent(command.Call.Root.Name, args));
                    if (result.JumpTarget is not null)
                    {
                        yield return StepResult.FromEvent(new JumpEvent(result.JumpTarget));
                        yield return StepResult.Jump(result.JumpTarget);
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
            //case BranchStep branch:
            //    await foreach (var result in ExecuteBranchAsync(branch, ct))
            //    {
            //        yield return result;
            //    }

                //yield break;
            case JumpStep jump:
                yield return StepResult.FromEvent(new JumpEvent(jump.Target));
                yield return StepResult.Jump(jump.Target);
                yield break;
            case CallStep call:
                if (!TryGetSegment(call.Target, out var segment))
                {
                    yield break;
                }

                await foreach (var result in ExecuteMainSegmentAsync(segment, ct))
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
