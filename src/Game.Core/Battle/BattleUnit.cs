using Game.Core;
using Game.Core.Affix;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Core.Battle;

public sealed class BattleUnit
{
    public const int MaxRage = 6;

    private readonly List<BattleBuffInstance> _buffs = [];
    private readonly HashSet<string> _disabledSkillIds = new(StringComparer.Ordinal);

    public BattleUnit(
        string id,
        CharacterInstance character,
        int team,
        GridPosition position,
        BattleFacing facing = BattleFacing.Right,
        int? maxHp = null,
        int? maxMp = null,
        int? hp = null,
        int? mp = null,
        int rage = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentOutOfRangeException.ThrowIfNegative(rage);

        Id = id;
        Character = character;
        Team = team;
        Position = position;
        Facing = facing;

        MaxHp = Math.Max(1, maxHp ?? ResolvePositiveStat(character, StatType.MaxHp, 1));
        MaxMp = Math.Max(0, maxMp ?? ResolvePositiveStat(character, StatType.MaxMp, 0));
        Hp = Math.Clamp(hp ?? MaxHp, 0, MaxHp);
        Mp = Math.Clamp(mp ?? MaxMp, 0, MaxMp);
        Rage = Math.Clamp(rage, 0, MaxRage);
    }

    public string Id { get; }

    public CharacterInstance Character { get; }

    public int Team { get; }

    public GridPosition Position { get; internal set; }

    public BattleFacing Facing { get; internal set; }

    public int MaxHp { get; }

    public int Hp { get; private set; }

    public int MaxMp { get; }

    public int Mp { get; private set; }

    public int Rage { get; private set; }

    public double ActionSpeed => GetActionSpeed();

    public int MovePower => GetMovePower();

    public double ActionGauge { get; set; }

    public int ItemCooldown { get; private set; }

    public bool IsAlive => Hp > 0;

    public IReadOnlyList<BattleBuffInstance> Buffs => _buffs;

    public IReadOnlySet<string> DisabledSkillIds => _disabledSkillIds;

    public bool HasTrait(TraitId traitId) =>
        Character.Traits.Contains(traitId)
        || GetActiveBuffs().Any(buff => buff.Definition.Affixes
            .OfType<TraitAffix>()
            .Any(affix => affix.TraitId == traitId));

    public void AddDisabledSkill(string skillId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        _disabledSkillIds.Add(skillId);
    }

    public bool RemoveDisabledSkill(string skillId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        return _disabledSkillIds.Remove(skillId);
    }

    public void SpendMp(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (Mp < amount)
        {
            throw new InvalidOperationException($"Unit '{Id}' does not have enough MP.");
        }

        Mp -= amount;
    }

    public void SpendRage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (Rage < amount)
        {
            throw new InvalidOperationException($"Unit '{Id}' does not have enough rage.");
        }

        Rage -= amount;
    }

    public int RestoreHp(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = Hp;
        Hp = Math.Min(MaxHp, Hp + amount);
        return Hp - before;
    }

    public int RestoreMp(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = Mp;
        Mp = Math.Min(MaxMp, Mp + amount);
        return Mp - before;
    }

    public int DamageMp(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = Mp;
        Mp = Math.Max(0, Mp - amount);
        return before - Mp;
    }

    public void AddRage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Rage = Math.Min(MaxRage, Rage + amount);
    }

    public void AddItemCooldown(int cooldown)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cooldown);
        ItemCooldown = checked(ItemCooldown + cooldown);
    }

    public void RecoverItemCooldown()
    {
        if (ItemCooldown > 0)
        {
            ItemCooldown--;
        }
    }

    public int TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = Hp;
        Hp = Math.Max(0, Hp - amount);
        return before - Hp;
    }

    public void ApplyBuff(BattleBuffInstance buff)
    {
        ArgumentNullException.ThrowIfNull(buff);
        _buffs.Add(buff);
    }

    public IReadOnlyList<BattleBuffInstance> GetActiveBuffs() =>
        _buffs
            .Where(static buff => !buff.IsExpired)
            .GroupBy(static buff => buff.Definition.Id, StringComparer.Ordinal)
            .Select(static group => group
                .OrderByDescending(static buff => buff.Level)
                .ThenByDescending(static buff => buff.RemainingTurns)
                .ThenByDescending(static buff => buff.AppliedAtActionSerial)
                .First())
            .ToList();

    public double GetStat(StatType statType) =>
        GetActiveBuffModifierBucket(
                affix => affix is StatModifierAffix statModifier && statModifier.Stat == statType,
                affix => ((StatModifierAffix)affix).Value)
            .Combine(GetActiveBuffLevelModifierBucket(statType))
            .Evaluate(Character.GetStat(statType));

    public double GetWeaponBonusValue(WeaponType weaponType, double baseValue) =>
        GetActiveBuffModifierBucket(
                affix => affix is WeaponBonusModifierAffix weaponModifier && weaponModifier.WeaponType == weaponType,
                affix => ((WeaponBonusModifierAffix)affix).Value)
            .Evaluate(Character.GetWeaponBonusValue(weaponType, baseValue));

    public double GetActionSpeed()
    {
        if (HasTrait(TraitId.Ghost))
        {
            return 3.5d;
        }

        var speed = GetStat(StatType.Shenfa) / 100d + GetStat(StatType.Gengu) / 130d;
        speed = Math.Clamp(speed, 1d, 2.2d);
        speed += GetStat(StatType.Speed);

        if (TryGetBuff(BattleContentIds.Paralysis) is { Level: > 0 } paralysis)
        {
            speed -= paralysis.Level * 0.2d;
        }

        if (TryGetBuff(BattleContentIds.Swift) is { } swift)
        {
            speed += swift.Level * 0.2d;
        }

        return Math.Clamp(speed, 0.8d, 2.5d);
    }

    public int GetMovePower()
    {
        if (HasBuff(BattleContentIds.Immobilized))
        {
            return 0;
        }

        var movePower = 2;
        var shenfa = GetStat(StatType.Shenfa);
        if (shenfa > 100d)
        {
            movePower++;
        }

        if (shenfa > 180d)
        {
            movePower++;
        }

        if (shenfa > 250d)
        {
            movePower++;
        }

        movePower += (int)Character.GetStat(StatType.Movement);
        if (TryGetBuff(BattleContentIds.Slow) is { } slow)
        {
            movePower -= (int)(slow.Level * 1.5d);
        }

        if (TryGetBuff(BattleContentIds.LightBody) is { } lightBody)
        {
            movePower += lightBody.Level + 1;
        }

        return Math.Clamp(movePower, 1, 5);
    }

    public bool HasBuff(string buffId) => TryGetBuff(buffId) is not null;

    public BattleBuffInstance? TryGetBuff(string buffId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buffId);

        return GetActiveBuffs()
            .FirstOrDefault(buff => string.Equals(buff.Definition.Id, buffId, StringComparison.Ordinal));
    }

    internal IReadOnlyList<BattleBuffInstance> RemoveExpiredBuffs()
    {
        var expired = _buffs.Where(static buff => buff.IsExpired).ToList();
        _buffs.RemoveAll(static buff => buff.IsExpired);
        return expired;
    }

    private ModifierBucket GetActiveBuffModifierBucket(
        Func<AffixDefinition, bool> predicate,
        Func<AffixDefinition, ModifierValue> selector)
    {
        var bucket = ModifierBucket.Empty;
        foreach (var buff in GetActiveBuffs())
        {
            foreach (var affix in buff.Definition.Affixes)
            {
                if (predicate(affix))
                {
                    bucket = bucket.Apply(selector(affix));
                }
            }
        }

        return bucket;
    }

    private ModifierBucket GetActiveBuffLevelModifierBucket(StatType statType)
    {
        var bucket = ModifierBucket.Empty;
        foreach (var buff in GetActiveBuffs())
        {
            foreach (var affix in buff.Definition.Affixes.OfType<BuffLevelStatModifierAffix>())
            {
                if (affix.Stat != statType)
                {
                    continue;
                }

                var add = affix.AddBase + affix.AddPerLevel * buff.Level;
                if (Math.Abs(add) > double.Epsilon)
                {
                    bucket = bucket.Apply(ModifierValue.Add(add));
                }

                var mul = affix.MulPerLevel * buff.Level;
                if (Math.Abs(mul) > double.Epsilon)
                {
                    bucket = bucket.Apply(ModifierValue.Increase(mul));
                }
            }
        }

        return bucket;
    }

    private static int ResolvePositiveStat(CharacterInstance character, StatType statType, int fallback)
    {
        var value = (int)Math.Round(character.GetStat(statType));
        return value > 0 ? value : fallback;
    }
}
