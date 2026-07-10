namespace Game.Core.Story;

public interface IRuntimeHost
{
    ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken);

    ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken);

    ValueTask<bool> EvaluatePredicateAsync(string name, IReadOnlyList<ExprValue> args, CancellationToken cancellationToken);

    ValueTask<StoryCommandResult> ExecuteCommandAsync(string name, IReadOnlyList<ExprValue> args, CancellationToken cancellationToken);

    /// <returns>The <see cref="ChoiceOptionView.Index"/> of the selected visible option.</returns>
    ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken);

    ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken);
}
