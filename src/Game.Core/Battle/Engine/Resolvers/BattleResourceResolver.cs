using Game.Core.Affix;

namespace Game.Core.Battle;

internal static class BattleResourceResolver
{
    public static void AddRage(BattleState state, BattleUnit target, int value, HookTiming? timing = null)
    {
        target.AddRage(value);
        state.AddMessage(new BattleFact(BattleFactKind.RageChanged, target.Id, timing, detail: value.ToString()));
    }

    public static void SetRage(BattleState state, BattleUnit target, int value, HookTiming? timing = null)
    {
        target.SetRage(value);
        state.AddMessage(new BattleFact(BattleFactKind.RageChanged, target.Id, timing, detail: $"set:{value}"));
    }

    public static void SetActionGauge(BattleUnit target, int value) => target.SetActionGauge(value);

    public static int DamageMp(BattleState state, BattleUnit target, int value, HookTiming? timing = null, string? detail = null)
    {
        var actual = target.DamageMp(value);
        state.AddMessage(new BattleFact(BattleFactKind.MpDamaged, target.Id, timing, detail: detail ?? actual.ToString()));
        return actual;
    }
}
