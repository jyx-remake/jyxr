using Game.Core.Story;

namespace Game.Application;

internal sealed class NullRuntimeHost : IRuntimeHost
{
    public static NullRuntimeHost Instance { get; } = new();

    private NullRuntimeHost()
    {
    }

    public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
        ValueTask.FromException(new InvalidOperationException("Story runtime host is not configured."));

    public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
        ValueTask.FromException<ExprValue>(new InvalidOperationException("Story runtime host is not configured."));

    public ValueTask<bool> EvaluatePredicateAsync(string name, IReadOnlyList<ExprValue> args, CancellationToken cancellationToken) =>
        ValueTask.FromException<bool>(new InvalidOperationException("Story runtime host is not configured."));

    public ValueTask ExecuteCommandAsync(string name, IReadOnlyList<ExprValue> args, CancellationToken cancellationToken) =>
        ValueTask.FromException(new InvalidOperationException("Story runtime host is not configured."));

    public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
        ValueTask.FromException<int>(new InvalidOperationException("Story runtime host is not configured."));

    public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
        ValueTask.FromException<BattleOutcome>(new InvalidOperationException("Story runtime host is not configured."));
}
