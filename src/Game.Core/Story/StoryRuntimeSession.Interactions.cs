using System.Runtime.CompilerServices;

namespace Game.Core.Story;

internal sealed partial class StoryRuntimeSession
{
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

    private async IAsyncEnumerable<StepResult> ExecuteBattleAsync(
        BattleStep battle,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = new BattleContext(
            battle.BattleId,
            battle.Outcomes.Keys.ToArray(),
            battle.TotalBattles,
            battle.BattleLevel);
        yield return StepResult.FromEvent(new BattleStartedEvent(context));

        var selectedOutcome = await host.ResolveBattleAsync(context, ct);
        if (!battle.Outcomes.TryGetValue(selectedOutcome, out var steps))
        {
            if (selectedOutcome == BattleOutcome.Win)
            {
                yield return StepResult.FromEvent(new BattleResolvedEvent(context, selectedOutcome));
                yield break;
            }

            if (selectedOutcome == BattleOutcome.Lose)
            {
                await host.GameOverAsync(ct);
                yield return StepResult.FromEvent(new BattleResolvedEvent(context, selectedOutcome));
                yield return StepResult.Terminate();
                yield break;
            }

            throw new StoryRuntimeException(
                $"Battle '{battle.BattleId}' resolved to '{selectedOutcome}', but the script does not define that outcome.");
        }

        yield return StepResult.FromEvent(new BattleResolvedEvent(context, selectedOutcome));

        await foreach (var result in ExecuteStepsAsync(steps, ct))
        {
            yield return result;
            if (result.IsControl)
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
            if (!_expressionEvaluator.EvaluateBoolean(
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

        if (branch.Fallback is null)
        {
            yield break;
        }

        await foreach (var stepResult in ExecuteStepsAsync(branch.Fallback, ct))
        {
            yield return stepResult;
            if (stepResult.IsControl)
            {
                yield break;
            }
        }
    }
}
