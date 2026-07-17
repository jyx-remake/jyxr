using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Core.Model.Skills;

public enum FormSkillInstanceState
{
	Locked,
	Available,
	Disabled,
	SourceNotEquipped,
}

public sealed class FormSkillInstance(
	FormSkillDefinition definition,
	SkillInstance parent) : SkillInstance(parent?.Owner ?? throw new ArgumentNullException(nameof(parent)))
{
	public FormSkillDefinition Definition { get; } = definition ?? throw new ArgumentNullException(nameof(definition));

	public SkillInstance Parent { get; } = parent;

	public override string Id => Definition.Id;

	public override string Name => Definition.Name;

	public override string Description => Definition.Description;

	public override string Icon => Definition.Icon ?? Parent.Icon;

	public override string Animation => Definition.Animation??Parent.Animation;
	public override string Audio => Definition.Audio??Parent.Audio;

	public override int Level
	{
		get => Parent.Level;
		set => Parent.Level = value;
	}

	public override int Exp
	{
		get => Parent.Exp;
		set => Parent.Exp = value;
	}

	public override int CurrentCooldown { get; set; }
	public override SkillKind SkillKind => SkillKind.Form;
	public override WeaponType WeaponType => Parent.WeaponType;

	public override int Cooldown => Definition.Cooldown;
	public override bool CanCastAtSelf => Definition.Targeting is { } targeting
		? targeting.CanCastAtSelf ?? SkillTargetingDefaults.CanCastAtSelf(ImpactType)
		: Parent.CanCastAtSelf;
	public override bool CanImpactSelf => Definition.Targeting is { } targeting
		? targeting.CanImpactSelf ?? SkillTargetingDefaults.CanImpactSelf
		: Parent.CanImpactSelf;
	public override int CastSize => Definition.Targeting?.CastSize??Parent.CastSize;
	public override SkillImpactType ImpactType => Definition.Targeting?.ImpactType??Parent.ImpactType;
	public override int ImpactSize => Definition.Targeting?.ImpactSize??Parent.ImpactSize;
	public override IReadOnlyList<SkillBuffDefinition> Buffs => Definition.Buffs;
	public override bool IsHarmony => Parent.IsHarmony;
	public override double Affinity => Parent.Affinity;

	public override string SourceSkillId => Parent.SourceSkillId;

	public string SourceSkillName => Parent.Name;

	public double SourcePower => Parent.Power;

	public bool IsSourceEquipped => Parent is not InternalSkillInstance internalSkill || internalSkill.IsEquipped;

	public bool IsEnabled => Parent is not IFormSkillSource source || source.IsFormSkillEnabled(Id);

	public FormSkillInstanceState State =>
		Level < Definition.UnlockLevel
			? FormSkillInstanceState.Locked
			: !IsSourceEquipped
				? FormSkillInstanceState.SourceNotEquipped
				: !IsEnabled
					? FormSkillInstanceState.Disabled
					: FormSkillInstanceState.Available;

	public override double Power => (SourcePower + Definition.PowerExtra) * (1 + Bonus);

	public override int MpCost => Definition.Cost.Mp ?? Parent.MpCost;

	public override int RageCost => Definition.Cost.Rage;

	public bool IsUnlocked => State is not FormSkillInstanceState.Locked;

	public override bool IsActive => State is FormSkillInstanceState.Available;
}
