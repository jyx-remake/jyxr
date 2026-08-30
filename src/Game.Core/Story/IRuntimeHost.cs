namespace Game.Core.Story;

public interface IRuntimeHost
{
	/// <summary>
	/// UI hosts can opt into retaining story flow when an individual command is
	/// not implemented. Headless/application hosts remain strict by default.
	/// </summary>
	bool ContinueOnCommandFailure => false;

    ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken);

    /// <returns>The <see cref="ChoiceOptionView.Index"/> of the selected visible option.</returns>
    ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken);

    ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken);

    ValueTask PlayEffectAsync(string effectId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask GameOverAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>
    /// Reports a command that could not be executed. Hosts may present the
    /// reason to the player; the story runtime will then continue with the
    /// next step. The default keeps non-UI hosts backwards compatible.
    /// </summary>
    ValueTask CommandFailedAsync(
        string commandName,
        string message,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public interface IStoryRuntimeContext : IRuntimeHost
{
    bool IsExecutionActive { get; }

    ExpressionEnvironment ExpressionEnvironment { get; }

    AsyncExpressionCallRegistry<StoryCommandResult> Commands { get; }

    ValueTask AssignVariableAsync(
        string name,
        ExpressionValue value,
        CancellationToken cancellationToken);

    ValueTask<bool> DeleteVariableAsync(
        string name,
        CancellationToken cancellationToken);
}
