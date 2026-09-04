using Game.Application;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Expressions;

namespace Game.Tests;

public sealed class StoryGenderAliasTests
{
    [Theory]
    [InlineData("male", "$$性别1$$", "少侠")]
    [InlineData("male", "$$性别2$$", "师弟")]
    [InlineData("male", "$$性别3$$", "公子")]
    [InlineData("female", "$$性别1$$", "女侠")]
    [InlineData("female", "$$性别2$$", "师妹")]
    [InlineData("female", "$$性别3$$", "小姐")]
    [InlineData("eunuch", "$$性别1$$", "少侠")]
    [InlineData("animal", "$$性别2$$", "师弟")]
    [InlineData("neutral", "$$性别3$$", "公子")]
    public void Interpolate_DoubledDollarGenderAlias_FollowsHeroGender(string gender, string placeholder, string expected)
    {
        var session = CreateSession(gender);

        Assert.Equal(expected, new StoryTextInterpolator(session).Interpolate(placeholder));
    }

    [Fact]
    public void Interpolate_SingleDollarGenderAlias_ResolvesToo()
    {
        Assert.Equal("少侠", new StoryTextInterpolator(CreateSession("male")).Interpolate("$性别1$"));
        Assert.Equal("女侠", new StoryTextInterpolator(CreateSession("female")).Interpolate("$性别1$"));
    }

    [Fact]
    public void Interpolate_MixedText_LeavesNoDollarResidue()
    {
        var session = CreateSession("female");

        var text = new StoryTextInterpolator(session).Interpolate(
            "只是想到$$性别1$$曾帮助过我，此番便助$$性别1$$一臂之力，$性别2$也来相助");

        Assert.Equal("只是想到女侠曾帮助过我，此番便助女侠一臂之力，师妹也来相助", text);
    }

    [Fact]
    public void Interpolate_UnknownPlaceholder_StaysVerbatim()
    {
        var session = CreateSession("male");
        var interpolator = new StoryTextInterpolator(session);

        Assert.Equal("$未知变量$", interpolator.Interpolate("$未知变量$"));
        Assert.Equal("$$未知$$", interpolator.Interpolate("$$未知$$"));
    }

    [Fact]
    public void Expression_GenderAliasVariable_ComparesAgainstForms()
    {
        var session = CreateSession("male");
        var evaluator = new ExpressionEvaluator();
        var environment = new GameExpressionEnvironment(session).Create();

        Assert.True(evaluator.Evaluate(
            new ExpressionParser().ParseExpression("性别1 == '少侠'"), environment).AsBoolean("test"));
        Assert.False(evaluator.Evaluate(
            new ExpressionParser().ParseExpression("性别1 == '女侠'"), environment).AsBoolean("test"));
    }

    private static GameSession CreateSession(string gender)
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = new GameState();
        // The production hero id is 主角; ResolveHeroGender looks the hero up
        // by that constant, so the test must use the same id.
        var hero = TestContentFactory.CreateCharacterInstance("主角", definition, state.EquipmentInstanceFactory);
        hero.SetGender(Enum.Parse<CharacterGender>(gender, ignoreCase: true));
        state.Party.AddMember(hero);
        return new GameSession(state, TestContentFactory.CreateRepository(characters: [definition]));
    }
}
