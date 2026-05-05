using Game.Core.Definitions.Skills;
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

public abstract record SkillInstance(CharacterInstance Owner)
{
    public const int DefaultMaxLevel = 20;

    public virtual double Bonus => Owner.GetSkillBonusValue(Id);
    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual string Description => string.Empty;
    public virtual string Icon => string.Empty;
    public abstract string Animation { get; }
    public abstract string Audio { get; }
    public abstract int Level { get; set; }
    public virtual int MaxLevel => DefaultMaxLevel;
    public virtual int Exp
    {
        get => 0;
        set => throw new NotImplementedException();
    }

    public virtual string CooldownKey => Id;
    public abstract int CurrentCooldown { get; set; }
    public abstract SkillKind SkillKind { get; }
    public abstract WeaponType WeaponType { get; }
    public abstract double Power { get; }
    public abstract int MpCost { get; }
    public abstract int RageCost { get; }
    public abstract int Cooldown { get; }
    public abstract int CastSize { get; }
    public abstract SkillImpactType ImpactType { get; }
    public abstract int ImpactSize { get; }
    public abstract IReadOnlyList<SkillBuffDefinition> Buffs { get; }
    public abstract bool IsHarmony { get; }
    public abstract double Affinity { get; }
    public abstract bool IsActive { get; }
}
