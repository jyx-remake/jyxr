using Game.Core.Definitions;

namespace Game.Core.Battle;

public sealed class BattleBuffInstance
{
    public const int TimelineTicksPerRound = 50;

    public BattleBuffInstance(
        BuffDefinition definition,
        int level,
        int remainingTurns,
        string sourceUnitId,
        long appliedAtActionSerial)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfLessThan(remainingTurns, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUnitId);

        Definition = definition;
        Level = level;
        RemainingTurns = remainingTurns;
        SourceUnitId = sourceUnitId;
        AppliedAtActionSerial = appliedAtActionSerial;
        TicksUntilRound = TimelineTicksPerRound;
    }

    public BuffDefinition Definition { get; }

    public int Level { get; private set; }

    public int RemainingTurns { get; private set; }

    public string SourceUnitId { get; }

    public long AppliedAtActionSerial { get; }

    public int TicksUntilRound { get; private set; }

    public bool IsExpired => RemainingTurns <= 0;

    public void Strengthen(int levelDelta, int turnDelta)
    {
        if (levelDelta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(levelDelta));
        }

        if (turnDelta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnDelta));
        }

        Level = checked(Level + levelDelta);
        RemainingTurns = checked(RemainingTurns + turnDelta);
    }

    internal bool AdvanceTimeline()
    {
        TicksUntilRound--;
        if (TicksUntilRound > 0)
        {
            return false;
        }

        TicksUntilRound = TimelineTicksPerRound;
        return true;
    }

    internal void ConsumeRound() => RemainingTurns--;
}
