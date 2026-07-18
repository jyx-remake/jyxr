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

        foreach (var group in choice.Groups)
        {
            var isAvailable = true;
            if (group.When is not null)
            {
                var result = await ExpressionEvaluator.EvaluateAsync(group.When, host, ct);
                isAvailable = result.AsBoolean("choice group condition");
            }

            foreach (var option in group.Options)
            {
                if (isAvailable)
                {
                    availableOptions.Add(sourceIndex, option);
                    optionViews.Add(new ChoiceOptionView(sourceIndex, option.Text));
                }

                sourceIndex += 1;
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

    private async IAsyncEnumerable<StepResult> ExecuteBattleAsync(
        BattleStep battle,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = new BattleContext(battle.BattleId, battle.Outcomes.Keys.ToArray());
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
                var args = Array.Empty<ExprValue>();
                await host.ExecuteCommandAsync(GameOverCommand, args, ct);
                yield return StepResult.FromEvent(new BattleResolvedEvent(context, selectedOutcome));
                yield return StepResult.FromEvent(new CommandExecutedEvent(GameOverCommand, args));
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
            var result = await ExpressionEvaluator.EvaluateAsync(branchCase.When, host, ct);
            if (!result.AsBoolean("branch condition"))
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
