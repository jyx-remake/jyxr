namespace Game.Core.Battle;

public sealed class BattleEventRuntimeException : Exception
{
    public BattleEventRuntimeException(string message) : base(message)
    {
    }

    public BattleEventRuntimeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
