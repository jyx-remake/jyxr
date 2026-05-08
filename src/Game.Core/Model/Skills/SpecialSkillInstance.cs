using Game.Core.Definitions.Skills;
using Game.Core.Model.Character;

namespace Game.Core.Model.Skills;

public sealed class SpecialSkillInstance(
    SpecialSkillDefinition definition,
    CharacterInstance owner,
    bool active) : SkillInstance(owner)
{
    private bool _isActive = active;

    public SpecialSkillDefinition Definition { get; } = definition ?? throw new ArgumentNullException(nameof(definition));

    public override string Id => Definition.Id;
    public override string Name => Definition.Name;
    public override string Description => Definition.Description;
    public override string Icon => Definition.Icon;
    public override string Animation => Definition.Animation;
    public override string Audio => Definition.Audio;
    public override int Cooldown => Definition.Cooldown;
    public override int CastSize => Definition.Targeting?.CastSize ?? 0;
    public override SkillImpactType ImpactType => Definition.Targeting?.ImpactType ?? SkillImpactType.Single;
    public override int ImpactSize => Definition.Targeting?.ImpactSize??0;
    public override IReadOnlyList<SkillBuffDefinition> Buffs => Definition.Buffs;
    public override bool IsHarmony => false;
    public override double Affinity => 0;
    public override int CurrentCooldown { get; set; }
    public override SkillKind SkillKind => SkillKind.Special;
    public override WeaponType WeaponType => WeaponType.Unknown;
    public override double Power => 0;
    public override int MpCost => Definition.Cost.Mp ?? 0;
    public override int RageCost => Definition.Cost.Rage;
    public override bool IsActive => _isActive;

    public bool SetActive(bool isActive)
    {
        if (_isActive == isActive)
        {
            return false;
        }

        _isActive = isActive;
        return true;
    }
}
