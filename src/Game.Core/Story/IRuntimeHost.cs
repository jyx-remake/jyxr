namespace Game.Core.Story;

public interface IRuntimeHost
{
    ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken);

    /// <returns>The <see cref="ChoiceOptionView.Index"/> of the selected visible option.</returns>
    ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken);

    ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken);

    ValueTask PlayEffectAsync(string effectId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask GameOverAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask GameCompleteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask MainMenuAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public interface IStoryRuntimeContext : IRuntimeHost
{
    bool IsExecutionActive { get; }

    ExpressionEnvironment ExpressionEnvironment { get; }

    ValueTask<StoryCommandResult> ExecuteCommandAsync(
        string name,
        IReadOnlyList<ExpressionValue> args,
        CancellationToken cancellationToken);

    ValueTask AssignVariableAsync(
        string name,
        ExpressionValue value,
        CancellationToken cancellationToken);

    ValueTask<bool> DeleteVariableAsync(
        string name,
        CancellationToken cancellationToken);
}
