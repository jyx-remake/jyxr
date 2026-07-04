using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class CharacterResourceLimitPolicy
{
    private readonly Func<GameConfig> _configProvider;
    private readonly Func<int> _roundProvider;

    public CharacterResourceLimitPolicy(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _configProvider = () => session.Config;
        _roundProvider = () => session.State.Adventure.Round;
    }

    public CharacterResourceLimitPolicy(GameConfig? config = null, int round = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        var resolvedConfig = config ?? new GameConfig();
        _configProvider = () => resolvedConfig;
        _roundProvider = () => round;
    }

    public int GetMaxHpMp()
    {
        var config = _configProvider();
        ArgumentOutOfRangeException.ThrowIfLessThan(config.MaxHpMp, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(config.MaxHpMpPerRound);
        return checked(config.MaxHpMp + (_roundProvider() - 1) * config.MaxHpMpPerRound);
    }

    public bool ClampBaseResourceStats(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return ClampBaseResourceStat(character, StatType.MaxHp) |
               ClampBaseResourceStat(character, StatType.MaxMp);
    }

    public bool ClampBaseResourceStat(CharacterInstance character, StatType statType)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (!IsBaseResourceStat(statType))
        {
            return false;
        }

        var currentValue = character.GetBaseStat(statType);
        var maxValue = GetMaxHpMp();
        if (currentValue <= maxValue)
        {
            return false;
        }

        character.AddBaseStat(statType, maxValue - currentValue);
        return true;
    }

    public static bool IsBaseResourceStat(StatType statType) =>
        statType is StatType.MaxHp or StatType.MaxMp;
}
