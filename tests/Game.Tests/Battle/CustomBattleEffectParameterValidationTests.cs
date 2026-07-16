using System.Text.Json;
using Game.Core.Battle;

namespace Game.Tests.Battle;

public sealed class CustomBattleEffectParameterValidationTests
{
    [Theory]
    [InlineData("bright_sacred_fire_formation", "{\"chancePerAlly\":1.1}", "ChancePerAlly")]
    [InlineData("survive_at_one_hp", "{\"abilityId\":\"   \",\"chance\":0.5}", "AbilityId")]
    [InlineData("formless_healing", "{\"baseValue\":-1,\"valuePerLevel\":30}", "BaseValue")]
    [InlineData("qi_shield_defeat_prevention", "{\"abilityId\":\"shield\",\"chance\":0.5,\"mpCostPerDamage\":0}", "MpCostPerDamage")]
    public void Resolve_rejects_parameters_that_fail_default_validation(
        string effectId,
        string parametersJson,
        string memberName)
    {
        var effect = CreateEffect(effectId, parametersJson);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            effect.Resolve(TestContentFactory.CreateRepository()));

        Assert.Contains($"Custom battle effect '{effectId}' parameters failed validation", exception.Message);
        Assert.Contains(memberName, exception.Message);
    }

    [Fact]
    public void Resolve_runs_handler_validation_after_default_validation()
    {
        var effect = CreateEffect(
            "attribute_contest_debuff",
            """
            {
              "sourceStat": "fuyuan",
              "targetStat": "dingli",
              "scale": 0.01,
              "buffId": "封印",
              "level": 0,
              "duration": 2,
              "minimumChance": 0.8,
              "maximumChance": 0.2
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            effect.Resolve(TestContentFactory.CreateRepository()));

        Assert.Contains("Maximum contest chance cannot be lower", exception.Message);
    }

    [Fact]
    public void Resolve_accepts_valid_defaulted_parameters()
    {
        var effect = CreateEffect("bright_sacred_fire_formation", "{}");

        effect.Resolve(TestContentFactory.CreateRepository());
    }

    private static CustomBattleEffectDefinition CreateEffect(string effectId, string parametersJson)
    {
        using var document = JsonDocument.Parse(parametersJson);
        return new CustomBattleEffectDefinition(effectId, document.RootElement.Clone());
    }
}
