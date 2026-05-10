using Game.Core.Definitions.Skills;
using Game.Core.Model.Character;

namespace Game.Core.Model.Skills;

public sealed class LegendSkillInstance(
    LegendSkillDefinition definition,
    SkillInstance parent) : SkillInstance(parent?.Owner ?? throw new ArgumentNullException(nameof(parent)))
{
    public LegendSkillDefinition Definition { get; } = definition ?? throw new ArgumentNullException(nameof(definition));

    public SkillInstance Parent { get; } = parent;

    public override string Id => Definition.Id;
    public override string Name => Definition.Name;
    public override string Description => Parent.Description;
    public override string Icon => Parent.Icon;
    public override string Animation => Parent.Animation;
    public override string Audio => Parent.Audio;
    public string? ScreenEffectAnimation => Definition.Animation;
    public override int Level
    {
        get => 0;
        set => throw new NotSupportedException($"{nameof(LegendSkillInstance)} does not support level changes.");
    }

    public override int MaxLevel
    {
        get => 0;
        set => throw new NotSupportedException($"{nameof(LegendSkillInstance)} does not support max level changes.");
    }

    public override int Exp
    {
        get => 0;
        set => throw new NotSupportedException($"{nameof(LegendSkillInstance)} does not support experience changes.");
    }

    public override double Power => (Parent.Power + Definition.PowerExtra) * (1 + Bonus);
    public override int MpCost => Parent.MpCost;
    public override int RageCost => Parent.RageCost;
    public override int Cooldown => 0;
    public override bool CanTargetSelf => Parent.CanTargetSelf;
    public override int CastSize => Parent.CastSize;
    public override SkillImpactType ImpactType => Parent.ImpactType;
    public override int ImpactSize => Parent.ImpactSize;
    public override IReadOnlyList<SkillBuffDefinition> Buffs => Definition.Buffs;
    public override bool IsHarmony => Parent.IsHarmony;
    public override double Affinity => Parent.Affinity;
    public override bool IsActive => Parent.IsActive;
    public override int CurrentCooldown { get; set; }
    public override SkillKind SkillKind => SkillKind.Legend;
    public override WeaponType WeaponType => Parent.WeaponType;

}
