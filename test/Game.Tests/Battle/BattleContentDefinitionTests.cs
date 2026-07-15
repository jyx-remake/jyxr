using Game.Content.Loading;
using Game.Core.Affix;

namespace Game.Tests;

public sealed class BattleContentDefinitionTests
{
    private static string RealContentDirectoryPath =>
        Path.Combine(AppContext.BaseDirectory, "mods", "jyxr-base", "data");

    [Fact]
    public void AttackStrengthening_DefinesLegacyCompleteAttackMultiplier()
    {
        var repository = new JsonContentLoader().LoadFromDirectory(RealContentDirectoryPath);
        var buff = repository.GetBuff("攻击强化");

        var hook = Assert.IsType<HookAffix>(Assert.Single(buff.Affixes));
        Assert.Equal(HookTiming.BeforeDamageCalculation, hook.Timing);

        var role = Assert.IsType<ContextUnitRoleBattleHookConditionDefinition>(Assert.Single(hook.Conditions));
        Assert.Equal(BattleHookContextUnitRole.Source, role.Role);

        var effect = Assert.IsType<ModifyDamageContextBattleHookEffectDefinition>(Assert.Single(hook.Effects));
        Assert.Equal(BattleDamageContextField.SourceAttack, effect.Field);
        Assert.Equal(ModifierOp.More, effect.Op);
        Assert.Equal(1d, effect.Delta);
        Assert.Equal(0.1d, effect.DeltaPerBuffLevel);
    }
}
