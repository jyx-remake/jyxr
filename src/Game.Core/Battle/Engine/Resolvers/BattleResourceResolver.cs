using Game.Core.Affix;

namespace Game.Core.Battle;

internal static class BattleResourceResolver
{
    public static int AddRage(
        BattleState state,
        BattleUnit target,
        int value,
        HookTiming? timing = null,
        string? detailSource = null)
    {
        var before = target.Rage;
        target.AddRage(value);
        var actual = target.Rage - before;
        state.AddMessage(new BattleFact(
            BattleFactKind.RageChanged,
            target.Id,
            timing,
            detail: detailSource is null ? actual.ToString() : $"{detailSource}:{actual}"));
        return actual;
    }

    public static int SetRage(
        BattleState state,
        BattleUnit target,
        int value,
        HookTiming? timing = null,
        string? detailSource = null)
    {
        var before = target.Rage;
        target.SetRage(value);
        var actual = target.Rage - before;
        state.AddMessage(new BattleFact(
            BattleFactKind.RageChanged,
            target.Id,
            timing,
            detail: detailSource is null ? actual.ToString() : $"{detailSource}:{actual}"));
        return actual;
    }

    public static double AddActionGauge(
        BattleState state,
        BattleUnit target,
        int value,
        HookTiming? timing = null)
    {
        var before = target.ActionGauge;
        target.SetActionGauge(Math.Max(0d, before + value));
        var actual = target.ActionGauge - before;
        state.AddMessage(new BattleFact(
            BattleFactKind.ActionGaugeChanged,
            target.Id,
            timing,
            detail: actual.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return actual;
    }

    public static void SetActionGauge(BattleUnit target, int value) => target.SetActionGauge(value);

    public static int DamageMp(BattleState state, BattleUnit target, int value, HookTiming? timing = null, string? detail = null)
    {
        var actual = target.DamageMp(value);
        state.AddMessage(new BattleFact(BattleFactKind.MpDamaged, target.Id, timing, detail: detail ?? actual.ToString()));
        return actual;
    }
}
