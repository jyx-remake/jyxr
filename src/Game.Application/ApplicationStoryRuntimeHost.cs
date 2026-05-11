using Game.Core.Story;

namespace Game.Application;

internal sealed class ApplicationStoryRuntimeHost : IRuntimeHost
{
    private readonly IRuntimeHost _externalHost;
    private readonly StoryConditionEvaluator _conditionEvaluator;
    private readonly StoryCommandDispatcher _commandDispatcher;
    private readonly StoryTextInterpolator _textInterpolator;

    public ApplicationStoryRuntimeHost(
        IRuntimeHost externalHost,
        StoryConditionEvaluator conditionEvaluator,
        StoryCommandDispatcher commandDispatcher,
        StoryTextInterpolator textInterpolator)
    {
        ArgumentNullException.ThrowIfNull(externalHost);
        ArgumentNullException.ThrowIfNull(conditionEvaluator);
        ArgumentNullException.ThrowIfNull(commandDispatcher);
        ArgumentNullException.ThrowIfNull(textInterpolator);

        _externalHost = externalHost;
        _conditionEvaluator = conditionEvaluator;
        _commandDispatcher = commandDispatcher;
        _textInterpolator = textInterpolator;
    }

    public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dialogue);

        return _externalHost.DialogueAsync(
            new DialogueContext(
                _textInterpolator.Interpolate(dialogue.Speaker),
                _textInterpolator.Interpolate(dialogue.Text)),
            cancellationToken);
    }

    public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
        _conditionEvaluator.GetVariableAsync(name, cancellationToken);

    public ValueTask<bool> EvaluatePredicateAsync(
        string name,
        IReadOnlyList<ExprValue> args,
        CancellationToken cancellationToken) =>
        _conditionEvaluator.EvaluatePredicateAsync(name, args, cancellationToken);

    public ValueTask<StoryCommandResult> ExecuteCommandAsync(
        string name,
        IReadOnlyList<ExprValue> args,
        CancellationToken cancellationToken) =>
        _commandDispatcher.ExecuteCommandAsync(name, args, cancellationToken);

    public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(choice);

        var interpolatedChoice = new ChoiceContext(
            _textInterpolator.Interpolate(choice.PromptSpeaker),
            _textInterpolator.Interpolate(choice.PromptText),
            choice.Options
                .Select(option => new ChoiceOptionView(option.Index, _textInterpolator.Interpolate(option.Text)))
                .ToArray());

        return _externalHost.ChooseOptionAsync(interpolatedChoice, cancellationToken);
    }

    public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
        _externalHost.ResolveBattleAsync(battle, cancellationToken);
}
