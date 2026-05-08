using Game.Core.Definitions.Skills;
using Game.Core.Model.Character;

namespace Game.Core.Model.Skills;

public sealed class InternalSkillInstance(
    InternalSkillDefinition definition,
    CharacterInstance owner) : SkillInstance(owner)
{
    public InternalSkillDefinition Definition { get; } = definition ?? throw new ArgumentNullException(nameof(definition));

    public override string Id => Definition.Id;

    public override string Name => Definition.Name;

    public override string Description => Definition.Description;

    public override string Icon => Definition.Icon;

    public override string Animation => "";
    public override string Audio => "";

    public override int CurrentCooldown { get; set; }
    public override SkillKind SkillKind => SkillKind.Internal;
    public override WeaponType WeaponType => WeaponType.InternalSkill;

    public int Yin => Definition.Yin * Level / 10;

    public int Yang => Definition.Yang * Level / 10;

    public double AttackRatio => Level / 10d * Definition.AttackScale * (1 + Bonus);

    public override double Power => AttackRatio * 13d;
    public override int MpCost => (int)Definition.Hard * Level * 4;
    public override int RageCost => 0;
    public override int Cooldown => 0;
    public override int CastSize => 0;
    public override SkillImpactType ImpactType => SkillImpactType.Single;
    public override int ImpactSize => 0;
    public override IReadOnlyList<SkillBuffDefinition> Buffs => [];
    public override bool IsHarmony => false;
    public override double Affinity => 0;

    public bool IsEquipped => string.Equals(Owner.EquippedInternalSkillId, Definition.Id, StringComparison.Ordinal);

    public override bool IsActive => IsEquipped;

    public double DefenceRatio => Level / 10d * Definition.DefenceScale * (1 + Bonus);

    public double CriticalRatio => (Math.Min(Level, 10) / 10d) * Definition.CriticalScale;

    public int LevelUpExp => GetLevelUpExp(Level);

    public IReadOnlyList<FormSkillInstance> GetFormSkills() =>
        Definition.FormSkills.Select(definition => new FormSkillInstance(definition, this)).ToList();

    public int GetLevelUpExp(int currentLevel)
    {
        if (currentLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(currentLevel));
        }

        return (int)(((currentLevel + 4d) / 4d) * ((Definition.Hard + 4d) / 4d) * 40d);
    }

    public void SetState(int level, int exp, int? maxLevel = null)
    {
        MaxLevel = maxLevel ?? DefaultMaxLevel;
        Level = level;
        Exp = exp;
    }

    public SkillLevelChange<InternalSkillInstance> UpgradeLevel(int levels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);

        var oldLevel = Level;
        var targetLevel = Math.Min(checked(oldLevel + levels), MaxLevel);
        if (targetLevel == oldLevel)
        {
            return new SkillLevelChange<InternalSkillInstance>(this, oldLevel, targetLevel, false);
        }

        Level = targetLevel;
        Exp = 0;
        return new SkillLevelChange<InternalSkillInstance>(this, oldLevel, targetLevel, false);
    }
}
