using Game.Application;
using Game.Core.Model;

namespace Game.Tests;

public sealed class CanonicalDslCatalogTests
{
    [Theory]
    [InlineData("change_item")]
    [InlineData("remove_item")]
    [InlineData("add_random_item")]
    [InlineData("add_random_item_options")]
    [InlineData("change_silver")]
    [InlineData("change_yuanbao")]
    [InlineData("advance_days")]
    [InlineData("advance_time_slots")]
    [InlineData("advance_to_time_slot")]
    [InlineData("show_cloud")]
    [InlineData("set_round")]
    [InlineData("set_difficulty")]
    [InlineData("set_no_regret")]
    [InlineData("set_sect")]
    [InlineData("change_morality")]
    [InlineData("change_favorability")]
    [InlineData("set_rank")]
    [InlineData("journal")]
    [InlineData("set_flag")]
    [InlineData("clear_flag")]
    [InlineData("change_story_number")]
    [InlineData("list_story_numbers")]
    [InlineData("set_time_key")]
    [InlineData("clear_time_key")]
    [InlineData("world_triggers")]
    [InlineData("change_stat")]
    [InlineData("set_character_name")]
    [InlineData("set_growth")]
    [InlineData("scale_stats")]
    [InlineData("grant_points")]
    [InlineData("grant_exp")]
    [InlineData("level_up")]
    [InlineData("upgrade_external")]
    [InlineData("upgrade_internal")]
    [InlineData("upgrade_skill")]
    [InlineData("maxlevel")]
    [InlineData("join")]
    [InlineData("join_random")]
    [InlineData("follow")]
    [InlineData("leave")]
    [InlineData("leave_follower")]
    [InlineData("leave_all")]
    [InlineData("learn_external")]
    [InlineData("learn")]
    [InlineData("learn_internal")]
    [InlineData("learn_special")]
    [InlineData("learn_talent")]
    [InlineData("remove_external")]
    [InlineData("remove")]
    [InlineData("remove_internal")]
    [InlineData("remove_special")]
    [InlineData("remove_talent")]
    [InlineData("unlock_achievement")]
    [InlineData("minigame")]
    [InlineData("refine")]
    [InlineData("tower")]
    [InlineData("huashan")]
    [InlineData("trial")]
    [InlineData("zhenlong")]
    [InlineData("arena")]
    public void BusinessCommandIsExplicitlyRegistered(string name)
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        Assert.True(session.StoryService.CommandDispatcher.Registry.TryGetDescriptor(name, out _));
    }

    [Theory]
    [InlineData("item", "change_item")]
    [InlineData("cost_item", "remove_item")]
    [InlineData("item_random", "add_random_item")]
    [InlineData("get_money", "change_silver")]
    [InlineData("yuanbao", "change_yuanbao")]
    [InlineData("cost_day", "advance_days")]
    [InlineData("cost_hour", "advance_time_slots")]
    [InlineData("to_chinesetime", "advance_to_time_slot")]
    [InlineData("set_game_mode", "set_difficulty")]
    [InlineData("log", "journal")]
    [InlineData("daode", "change_morality")]
    [InlineData("haogan", "change_favorability")]
    [InlineData("menpai", "set_sect")]
    [InlineData("growtemplate", "set_growth")]
    [InlineData("grant_point", "grant_points")]
    [InlineData("get_point", "grant_points")]
    [InlineData("get_exp", "grant_exp")]
    [InlineData("levelup", "level_up")]
    [InlineData("max_skill_level", "maxlevel")]
    [InlineData("nick", "unlock_achievement")]
    [InlineData("leave_follow", "leave_follower")]
    [InlineData("game", "minigame")]
    [InlineData("xilian", "refine")]
    [InlineData("zhenlongqiju", "zhenlong")]
    public void ApprovedBusinessAliasSharesCanonicalDescriptor(string alias, string canonical)
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        Assert.True(session.StoryService.CommandDispatcher.Registry.TryGetDescriptor(alias, out var descriptor));
        Assert.Equal(canonical, descriptor.Name);
    }

    [Fact]
    public void MaxLevelDescriptorPreservesDefaultsAndOnceKey()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        Assert.True(session.StoryService.CommandDispatcher.Registry.TryGetDescriptor("maxlevel", out var descriptor));

        Assert.Collection(
            descriptor.Parameters,
            parameter =>
            {
                Assert.Equal("skillId", parameter.Name);
                Assert.False(parameter.IsOptional);
                Assert.Equal(ExpressionValueKind.String, parameter.Kind);
            },
            parameter =>
            {
                Assert.Equal("levels", parameter.Name);
                Assert.True(parameter.IsOptional);
                Assert.Equal(1, parameter.DefaultValue.AsInt32("test"));
            },
            parameter =>
            {
                Assert.Equal("onceKey", parameter.Name);
                Assert.True(parameter.IsOptional);
                Assert.Equal(string.Empty, parameter.DefaultValue.AsString("test"));
            });
    }

    [Fact]
    public void SetTimeKeyDescriptorMakesTargetStoryOptional()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        Assert.True(session.StoryService.CommandDispatcher.Registry.TryGetDescriptor("set_time_key", out var descriptor));

        var storyId = Assert.Single(descriptor.Parameters, parameter => parameter.Name == "storyId");
        Assert.True(storyId.IsOptional);
        Assert.Equal(string.Empty, storyId.DefaultValue.AsString("test"));
    }

    [Theory]
    [InlineData("item_count")]
    [InlineData("favorability")]
    [InlineData("character_level")]
    [InlineData("character_stat")]
    [InlineData("skill_level")]
    [InlineData("map_event_completed")]
    [InlineData("story_completed")]
    [InlineData("story_completion_count")]
    [InlineData("story_elapsed_days")]
    [InlineData("last_story_is")]
    [InlineData("has_time_key")]
    [InlineData("in_team")]
    [InlineData("character_gender")]
    [InlineData("has_var")]
    [InlineData("story_number")]
    [InlineData("has_flag")]
    [InlineData("contains")]
    [InlineData("chance")]
    public void QueryFunctionIsExplicitlyRegistered(string name)
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        Assert.True(new GameExpressionEnvironment(session).Create().Functions.TryGetDescriptor(name, out _));
    }

    [Theory]
    [InlineData("should_finish", "story_completed")]
    [InlineData("follow_story", "last_story_is")]
    [InlineData("active_party_contains", "in_team")]
    [InlineData("haogan", "favorability")]
    public void ApprovedQueryAliasSharesCanonicalDescriptor(string alias, string canonical)
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        Assert.True(new GameExpressionEnvironment(session).Create().Functions.TryGetDescriptor(alias, out var descriptor));
        Assert.Equal(canonical, descriptor.Name);
    }

    [Theory]
    [InlineData("silver")]
    [InlineData("yuanbao")]
    [InlineData("round")]
    [InlineData("difficulty")]
    [InlineData("sect")]
    [InlineData("morality")]
    [InlineData("daode")]
    [InlineData("rank")]
    [InlineData("elapsed_days")]
    [InlineData("current_map")]
    [InlineData("current_time_slot")]
    [InlineData("current_date")]
    [InlineData("system_date")]
    [InlineData("friend_count")]
    [InlineData("achievement_count")]
    [InlineData("kill_count")]
    public void BuiltInValueIsResolvable(string name)
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        Assert.True(new GameExpressionEnvironment(session).Create().Variables.TryResolve(name, out _));
    }

    [Fact]
    public void BusinessCommandsUseClrParameters()
    {
        var offenders = typeof(GameSession).Assembly.GetTypes()
            .SelectMany(static type => type.GetMethods(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.DeclaredOnly))
            .Where(static method => method.GetCustomAttributes(typeof(StoryCommandAttribute), inherit: false).Length > 0)
            .Where(static method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(ExpressionValue)))
            .Select(static method => $"{method.DeclaringType?.Name}.{method.Name}")
            .ToArray();

        Assert.Empty(offenders);
    }
}
