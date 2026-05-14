using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Core.Model.Skills;

public enum SkillKind
{
    External,
    Internal,
    Form,
    Special,
    Legend,
}

public abstract class SkillInstance(CharacterInstance owner)
{
    public static int DefaultMaxLevel => new GameConfig().MaxExternalSkillLevel;

    private int _level = 1;
    private int _maxLevel = DefaultMaxLevel;
    private int _exp;

    public CharacterInstance Owner { get; } = owner ?? throw new ArgumentNullException(nameof(owner));

    public virtual double Bonus => Owner.GetSkillBonusValue(Id);
    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual string Description => string.Empty;
    public virtual string Icon => string.Empty;
    public abstract string Animation { get; }
    public abstract string Audio { get; }
    public virtual int Level
    {
        get => _level;
        set
        {
            ValidateLevel(value);
            _level = value;
        }
    }

    public virtual int MaxLevel
    {
        get => _maxLevel;
        set
        {
            ValidateMaxLevel(value);
            _maxLevel = value;
        }
    }

    public virtual int Exp
    {
        get => _exp;
        set
        {
            ValidateExperience(value);
            _exp = value;
        }
    }

    public virtual string CooldownKey => Id;
    public abstract int CurrentCooldown { get; set; }
    public abstract SkillKind SkillKind { get; }
    public abstract WeaponType WeaponType { get; }
    public abstract double Power { get; }
    public abstract int MpCost { get; }
    public abstract int RageCost { get; }
    public abstract int Cooldown { get; }
    public abstract bool CanTargetSelf { get; }
    public abstract int CastSize { get; }
    public abstract SkillImpactType ImpactType { get; }
    public abstract int ImpactSize { get; }
    public abstract IReadOnlyList<SkillBuffDefinition> Buffs { get; }
    public abstract bool IsHarmony { get; }
    public abstract double Affinity { get; }
    public abstract bool IsActive { get; }

    protected static void ValidateLevel(int level) => ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);

    protected static void ValidateExperience(int exp) => ArgumentOutOfRangeException.ThrowIfNegative(exp);

    protected static void ValidateMaxLevel(int maxLevel) => ArgumentOutOfRangeException.ThrowIfLessThan(maxLevel, 1);
}
