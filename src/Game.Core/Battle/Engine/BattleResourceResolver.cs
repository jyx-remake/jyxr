using Game.Core.Affix;

namespace Game.Core.Battle;

internal static class BattleResourceResolver
{
    public static void AddRage(BattleState state, BattleUnit target, int value, HookTiming? timing = null)
    {
        target.AddRage(value);
        state.AddEvent(new BattleEvent(BattleEventKind.RageChanged, target.Id, timing, Detail: value.ToString()));
    }

    public static void SetRage(BattleState state, BattleUnit target, int value, HookTiming? timing = null)
    {
        target.SetRage(value);
        state.AddEvent(new BattleEvent(BattleEventKind.RageChanged, target.Id, timing, Detail: $"set:{value}"));
    }

    public static void SetActionGauge(BattleUnit target, int value) => target.SetActionGauge(value);

    public static int DamageMp(BattleState state, BattleUnit target, int value, HookTiming? timing = null, string? detail = null)
    {
        var actual = target.DamageMp(value);
        state.AddEvent(new BattleEvent(BattleEventKind.MpDamaged, target.Id, timing, Detail: detail ?? actual.ToString()));
        return actual;
    }
}
