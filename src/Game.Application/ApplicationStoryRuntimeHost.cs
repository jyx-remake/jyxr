using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

internal sealed class ApplicationStoryRuntimeHost : IStoryRuntimeContext
{
    private readonly GameSession _session;
    private readonly GameState _executionState;
    private readonly IRuntimeHost _externalHost;
    private readonly StoryTextInterpolator _textInterpolator;
    private readonly StoryCommandDispatcher _commandDispatcher;

    public ApplicationStoryRuntimeHost(
        GameSession session,
        GameState executionState,
        IRuntimeHost externalHost,
        StoryCommandDispatcher commandDispatcher,
        StoryTextInterpolator textInterpolator,
        ExpressionEnvironment expressionEnvironment)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _executionState = executionState ?? throw new ArgumentNullException(nameof(executionState));
        _externalHost = externalHost ?? throw new ArgumentNullException(nameof(externalHost));
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _textInterpolator = textInterpolator ?? throw new ArgumentNullException(nameof(textInterpolator));
        ExpressionEnvironment = expressionEnvironment ?? throw new ArgumentNullException(nameof(expressionEnvironment));
    }

    public bool IsExecutionActive => ReferenceEquals(_session.State, _executionState) && !_session.Execution.IsTerminal;

    public ExpressionEnvironment ExpressionEnvironment { get; }

    public ValueTask<StoryCommandResult> ExecuteCommandAsync(
        string name,
        IReadOnlyList<ExpressionValue> args,
        CancellationToken cancellationToken) =>
        _commandDispatcher.ExecuteCommandAsync(name, args, cancellationToken);

    public ValueTask AssignVariableAsync(
        string name,
        ExpressionValue value,
        CancellationToken cancellationToken)
    {
        _commandDispatcher.VariableMutations.Assign(name, value);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> DeleteVariableAsync(string name, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_commandDispatcher.VariableMutations.Delete(name, "del"));

    public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
        _externalHost.DialogueAsync(
            new DialogueContext(_textInterpolator.Interpolate(dialogue.Speaker), _textInterpolator.Interpolate(dialogue.Text)),
            cancellationToken);

    public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
        _externalHost.ChooseOptionAsync(
            new ChoiceContext(
                _textInterpolator.Interpolate(choice.PromptSpeaker),
                _textInterpolator.Interpolate(choice.PromptText),
                choice.Options.Select(option => new ChoiceOptionView(option.Index, _textInterpolator.Interpolate(option.Text))).ToArray(),
                choice.Style),
            cancellationToken);

    public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
        _externalHost.ResolveBattleAsync(battle, cancellationToken);

    public ValueTask PlayEffectAsync(string effectId, CancellationToken cancellationToken) =>
        _externalHost.PlayEffectAsync(effectId, cancellationToken);

    public async ValueTask GameOverAsync(CancellationToken cancellationToken) =>
        await _session.SessionFlowService.EndAsync(StoryOutcome.Defeated, _externalHost, cancellationToken);
}
