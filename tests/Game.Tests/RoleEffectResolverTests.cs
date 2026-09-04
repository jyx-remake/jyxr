using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Tests;

public sealed class RoleEffectResolverTests
{
    private static SkillAffixDefinition AuraAffix(
        string animationId,
        int minimumLevel = 1,
        bool requiresEquipped = false) =>
        new(new RoleEffectAffix("label", animationId), minimumLevel, requiresEquipped);

    [Fact]
    public void Resolve_ReturnsNullWithoutAuraSources()
    {
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001", TestContentFactory.CreateCharacterDefinition("hero"));

        Assert.Null(RoleEffectResolver.Resolve(character));
    }

    [Fact]
    public void Resolve_PrefersEquippedTitleAura()
    {
        var title = new CharacterTitleDefinition
        {
            Id = "dugu",
            Name = "dugu",
            Affixes = [new RoleEffectAffix("剑气", "gh_jq")],
        };
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001", TestContentFactory.CreateCharacterDefinition("hero"));
        character.AddTitle(title, equipped: true);

        var resolved = RoleEffectResolver.Resolve(character);

        Assert.NotNull(resolved);
        Assert.Equal("gh_jq", resolved.AnimationId);
    }

    [Fact]
    public void Resolve_PrefersEquippedInternalOverTitle()
    {
        var title = new CharacterTitleDefinition
        {
            Id = "dugu",
            Name = "dugu",
            Affixes = [new RoleEffectAffix("剑气", "gh_jq")],
        };
        var inner = TestContentFactory.CreateInternalSkill(
            "neigong",
            affixes: [AuraAffix("gh_zl", minimumLevel: 20, requiresEquipped: true)]);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001", TestContentFactory.CreateCharacterDefinition("hero"));
        character.AddTitle(title, equipped: true);
        character.SetInternalSkillState(inner, 20, 0);
        character.EquipInternalSkill("neigong");

        var resolved = RoleEffectResolver.Resolve(character);

        Assert.NotNull(resolved);
        Assert.Equal("gh_zl", resolved.AnimationId);
    }

    [Fact]
    public void Resolve_RespectsLevelGate()
    {
        var inner = TestContentFactory.CreateInternalSkill(
            "neigong",
            affixes: [AuraAffix("gh_zl", minimumLevel: 20, requiresEquipped: true)]);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001", TestContentFactory.CreateCharacterDefinition("hero"));
        character.SetInternalSkillState(inner, 10, 0);
        character.EquipInternalSkill("neigong");

        Assert.Null(RoleEffectResolver.Resolve(character));
    }
}
