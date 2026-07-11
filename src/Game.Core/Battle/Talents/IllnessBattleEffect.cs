using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record IllnessBattleEffectParameters(
    string? ProtectorCharacterId = null,
    string? FloatText = null,
    string? SkipReason = null);

public sealed class IllnessBattleEffectHandler : CustomBattleEffectHandler<IllnessBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Execute(
        IActionStartEffectContext context,
        IllnessBattleEffectParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.FloatText))
        {
            context.RequestFloatText(
                context.Unit,
                parameters.FloatText,
                BattleFloatTextStyle.Harmful);
        }

        var protectorIsPresent = context.State.GetLivingUnits().Any(unit =>
            unit.Team == context.Unit.Team &&
            string.Equals(
                unit.Character.Definition.Id,
                parameters.ProtectorCharacterId,
                StringComparison.Ordinal));
        if (!protectorIsPresent)
        {
            context.SkipCurrentAction(parameters.SkipReason);
        }
    }
}
