namespace Game.Core.Story;

public sealed record PredicateInvocation(
    string Name,
    IReadOnlyList<ExprValue> Arguments);
