using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Core.Model.Skills;

public sealed record ExternalSkillInstance : SkillInstance
{
    private bool _isActive;

    public ExternalSkillInstance(
        ExternalSkillDefinition definition,
        int level,
        int exp,
        bool isActive,
        CharacterInstance owner,
        int? maxLevel = null)
        : base(owner)
    {
        Definition = definition;
        Level = level;
        Exp = exp;
        MaxLevel = maxLevel ?? DefaultMaxLevel;
        _isActive = isActive;
    }

    public ExternalSkillDefinition Definition { get; }
    public override string Id => Definition.Id;
    public override string Name => Definition.Name;
    public override string Description => Definition.Description;
    public override string Icon => Definition.Icon;
    public override string Animation => CurrentLevelOverride?.Animation ?? Definition.Animation;
    public override string Audio => Definition.Audio;
    public override int Level { get; set; }
    public override int MaxLevel { get; }
    public override int Exp { get; set; }
    public override int CurrentCooldown { get; set; }
    public override SkillKind SkillKind => SkillKind.External;
    public override WeaponType WeaponType => Definition.Type;
    public override double Power
    {
        get
        {
            if (Level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(Level));
            }

            if (Definition.LevelOverrides.TryGetValue(Level, out var overrideDefinition)
                && overrideDefinition.PowerOverride is not null)
            {
                return overrideDefinition.PowerOverride.Value * (1 + Bonus);
            }

            return (Definition.PowerBase + (Level - 1) * Definition.PowerStep) * (1 + Bonus);
        }
    }

    public override bool IsActive => _isActive;
    public override int MpCost => Definition.Cost?.Mp ?? SkillHelper.GetMpCost(this);
    public override int RageCost => Definition.Cost.Rage;
    public override int Cooldown => CurrentLevelOverride?.Cooldown ?? Definition.Cooldown;
    public override int CastSize => CurrentTargeting.CastSize ?? SkillHelper.GetCastSize(this);
    public override SkillImpactType ImpactType => CurrentTargeting.ImpactType ?? SkillHelper.GetImpactType(this);
    public override int ImpactSize => CurrentTargeting.ImpactSize ?? SkillHelper.GetImpactSize(this);
    public override IReadOnlyList<SkillBuffDefinition> Buffs => Definition.Buffs;
    public override bool IsHarmony => Definition.IsHarmony;
    public override double Affinity => Definition.Affinity;

    public IReadOnlyList<FormSkillInstance> GetFormSkills() =>
        Definition.FormSkills.Select(definition => new FormSkillInstance(definition, this)).ToList();

    public bool SetActive(bool isActive)
    {
        if (_isActive == isActive)
        {
            return false;
        }

        _isActive = isActive;
        return true;
    }

    public int LevelUpExp => GetLevelUpExp(Level);

    public int GetLevelUpExp(int currentLevel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(currentLevel, 1);
        return (int)((currentLevel / 4f) * ((float)Definition.Hard + 1f) / 4f * 15f * 8f);
    }

    private ExternalSkillLevelDefinition? CurrentLevelOverride =>
        Definition.LevelOverrides.GetValueOrDefault(Level);

    private SkillTargetingDefinition CurrentTargeting =>
        CurrentLevelOverride?.Targeting ?? Definition.Targeting;
}
