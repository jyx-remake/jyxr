using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Battle.Talents;

public sealed record WeaponEquipmentPenaltyBattleEffectParameters(
    WeaponType ExpectedWeaponType,
    double DamageFactor = 0.9d);

public sealed class WeaponEquipmentPenaltyBattleEffectHandler
    : CustomBattleEffectHandler<WeaponEquipmentPenaltyBattleEffectParameters, IDamageCalculationEffectContext>
{
    private static readonly IReadOnlySet<WeaponType> ExternalWeaponTypes = new HashSet<WeaponType>
    {
        WeaponType.Quanzhang,
        WeaponType.Jianfa,
        WeaponType.Daofa,
        WeaponType.Qimen,
    };

    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeDamageCalculation };

    public override void Validate(WeaponEquipmentPenaltyBattleEffectParameters parameters)
    {
        if (!ExternalWeaponTypes.Contains(parameters.ExpectedWeaponType))
        {
            throw new InvalidOperationException(
                $"Equipment penalty expected weapon type '{parameters.ExpectedWeaponType}' is not an external weapon type.");
        }

        if (parameters.DamageFactor is < 0d or > 1d)
        {
            throw new InvalidOperationException("Equipment penalty damage factor must be between 0 and 1.");
        }
    }

    public override void Execute(
        IDamageCalculationEffectContext context,
        WeaponEquipmentPenaltyBattleEffectParameters parameters)
    {
        var actualWeaponType = context.Skill?.WeaponType ?? WeaponType.Unknown;
        if (!ExternalWeaponTypes.Contains(actualWeaponType) || actualWeaponType == parameters.ExpectedWeaponType)
        {
            return;
        }

        context.DamageCalculation.AddModifier(
            BattleDamageContextField.SourceAttack,
            ModifierOp.More,
            parameters.DamageFactor);
    }
}
