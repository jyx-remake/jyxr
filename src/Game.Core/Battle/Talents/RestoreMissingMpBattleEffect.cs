using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

public sealed record RestoreMissingMpBattleEffectParameters(
    [property: Probability] double MissingMpFactor,
    string? FloatTextPrefix = null);

internal sealed class RestoreMissingMpBattleEffectHandler
    : CustomBattleEffectHandler<RestoreMissingMpBattleEffectParameters, IActionStartEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.BeforeActionStart };

    public override void Execute(
        IActionStartEffectContext context,
        RestoreMissingMpBattleEffectParameters parameters)
    {
        var missingMp = context.Unit.MaxMp - context.Unit.Mp;
        if (missingMp <= 0)
        {
            return;
        }

        var requested = (int)Math.Ceiling(missingMp * parameters.MissingMpFactor);
        var actual = context.ApplyMpRecovery(context.Unit, requested, "restore_missing_mp");
        if (actual > 0 && !string.IsNullOrWhiteSpace(parameters.FloatTextPrefix))
        {
            context.RequestFloatText(
                context.Unit,
                $"{parameters.FloatTextPrefix}{actual}",
                BattleFloatTextStyle.Recovery);
        }
    }
}
