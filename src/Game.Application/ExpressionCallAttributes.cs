namespace Game.Application;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class StoryCommandAttribute : ExpressionSymbolAttribute
{
    public StoryCommandAttribute(string name, params string[] aliases) : base(name, aliases) { }
}

public sealed class BattleEventCommandAttribute : ExpressionSymbolAttribute
{
    public BattleEventCommandAttribute(string name, params string[] aliases) : base(name, aliases) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class DebugCommandAttribute : ExpressionSymbolAttribute
{
    public DebugCommandAttribute(string name, params string[] aliases) : base(name, aliases) { }
}
