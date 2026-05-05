using Game.Core.Definitions.Skills;
using Game.Core.Model.Character;

namespace Game.Core.Model.Skills;

public sealed record LegendSkillInstance(
    LegendSkillDefinition Definition,
    SkillInstance Parent) : SkillInstance(Parent.Owner)
{
    public override string Id => Definition.Id;
    public override string Name => Definition.Name;
    public override string Animation => Definition.Animation??Parent.Animation;
    public override string Audio => Parent.Audio;
    public override int Level
    {
        get => 0;
        set => throw new NotImplementedException();
    }

    public override double Power => (Parent.Power + Definition.PowerExtra) * (1 + Bonus);
    public override int MpCost => Parent.MpCost;
    public override int RageCost => Parent.RageCost;
    public override int Cooldown => 0;
    public override int CastSize => Parent.CastSize;
    public override SkillImpactType ImpactType => Parent.ImpactType;
    public override int ImpactSize => Parent.ImpactSize;
    public override IReadOnlyList<SkillBuffDefinition> Buffs => Definition.Buffs;
    public override bool IsHarmony => Parent.IsHarmony;
    public override double Affinity => Parent.Affinity;
    public override bool IsActive => Parent.IsActive;
    public override int CurrentCooldown { get; set; }
    public override SkillKind SkillKind => Parent.SkillKind;
    public override WeaponType WeaponType => Parent.WeaponType;

}