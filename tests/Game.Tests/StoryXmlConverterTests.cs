using System.Text;
using System.Xml.Linq;
using Game.StoryDsl;
using Game.Tools.StoryXml;

namespace Game.Tests;

public sealed class StoryXmlConverterTests
{
    [Theory]
    [InlineData("ITEM", "小还丹#2", "change_item('小还丹', 2)")]
    [InlineData("COST_MONEY", "100", "change_silver(-100)")]
    [InlineData("COST_MONEY", "-0", "change_silver(0)")]
    [InlineData("COST_MONEY", "-1.5", "change_silver(1.5)")]
    [InlineData("NICK", "初出茅庐", "unlock_achievement('初出茅庐')")]
    [InlineData("HEAD", "头像.少林弟子", "set_portrait('主角', '头像.少林弟子')")]
    [InlineData("HEAD", "郭襄#头像.郭襄", "set_portrait('郭襄', '头像.郭襄')")]
    [InlineData("SET_FLAG", "NO_GLOBAL_EVENT", "world_triggers(false)")]
    [InlineData("CLEAR_FLAG", "NO_GLOBAL_EVENT", "world_triggers(true)")]
    [InlineData("SET_FLAG", "是否拜师", "是否拜师 = true")]
    [InlineData("CLEAR_FLAG", "quest_started", "del quest_started")]
    [InlineData("SET_VAR", "quest_note#ready", "quest_note = 'ready'")]
    [InlineData("CHANGE_VAR", "quest_stage#2", "quest_stage += 2")]
    [InlineData("REMOVE_VAR", "quest_note", "del quest_note")]
    [InlineData("LEARN.SKILL", "主角#伏虎掌#5", "learn_external('主角', '伏虎掌', 5)")]
    [InlineData("LEARN.INTERNALSKILL", "主角#基本内功#5", "learn_internal('主角', '基本内功', 5)")]
    [InlineData("LEARN.SPECIALSKILL", "主角#凌波微步", "learn_special('主角', '凌波微步')")]
    [InlineData("LEARN", "talent#主角#妙手空空", "learn_talent('主角', '妙手空空')")]
    [InlineData("REMOVE.SKILL", "主角#伏虎掌", "remove_external('主角', '伏虎掌')")]
    [InlineData("UPGRADE", "拳掌#主角#3", "change_stat('主角', '拳掌', 3)")]
    [InlineData("UPGRADE.SKILL", "郭襄#峨眉剑法#2", "upgrade_external('郭襄', '峨眉剑法', 2)")]
    [InlineData("UPGRADE.INTERNALSKILL", "主角#基本内功", "upgrade_internal('主角', '基本内功', 1)")]
    [InlineData("UPGRADE.SKILLL", "达尔巴#火焰刀法#5", "upgrade_external('达尔巴', '火焰刀法', 5)")]
    [InlineData("UPGRADE……SKILL", "小龙女#玉女素心剑#5", "upgrade_external('小龙女', '玉女素心剑', 5)")]
    [InlineData("RANDOM_ITEM", "[小还丹, 大还丹]#2", "add_random_item(['小还丹', '大还丹'], 2)")]
    [InlineData("RANDOM_JOIN", "[郭襄, 程英]", "join_random(['郭襄', '程英'])")]
    [InlineData("MINUS_MAXPOINTS", "主角#5", "scale_stats('主角', 0.5)")]
    [InlineData("TOAST", "off", "toast(false)")]
    [InlineData("WORLD_TRIGGER", "on", "world_triggers(true)")]
    [InlineData("SELECT_HEAD", "", "select_portrait('主角')")]
    [InlineData("CHANGE_FEMALE_NAME", "铃兰", "input_name('女主', '铃兰')")]
    [InlineData("MAXLEVEL", "火焰刀法#2", "maxlevel('火焰刀法', 2)")]
    [InlineData("gameFin", "gameFin", "game_complete()")]
    [InlineData("NEWBIE", "ignored", "")]
    [InlineData("TOUCH", "ignored", "")]
    public void Convert_MapsActions(string type, string value, string expected)
    {
        var source = LegacyStoryXmlConverter.Convert(Xml(new XElement("action",
            new XAttribute("type", type), new XAttribute("value", value))));

        Assert.Equal("# 测试\n" + (expected.Length == 0 ? "" : expected + "\n"), source);
    }

    [Theory]
    [InlineData("should_finish", "前段", "story_completed('前段')")]
    [InlineData("should_not_finish", "前段", "not story_completed('前段')")]
    [InlineData("follow_story", "前段", "last_story_is('前段')")]
    [InlineData("have_item", "小还丹", "item_count('小还丹') >= 1")]
    [InlineData("has_item", "小还丹#2", "item_count('小还丹') >= 2")]
    [InlineData("not_have_item", "小还丹", "item_count('小还丹') == 0")]
    [InlineData("have_money", "100", "silver >= 100")]
    [InlineData("have_yuanbao", "5", "yuanbao >= 5")]
    [InlineData("in_round", "2", "round == 2")]
    [InlineData("game_mode", "normal", "difficulty == 'normal'")]
    [InlineData("exceed_day", "10", "elapsed_days > 10")]
    [InlineData("probability", "25", "chance(0.25)")]
    [InlineData("probability", "0", "chance(0)")]
    [InlineData("probability", "100", "chance(1)")]
    [InlineData("key_in_team", "郭襄", "in_team('郭襄')")]
    [InlineData("key_not_in_team", "郭襄", "not in_team('郭襄')")]
    [InlineData("not_has_time_key", "任务", "not has_time_key('任务')")]
    [InlineData("level_greater_than", "主角#20", "character_level('主角') >= 20")]
    [InlineData("level_greater_than", "郭襄#20", "in_team('郭襄') and character_level('郭襄') >= 20")]
    [InlineData("skill_less_than", "郭襄#峨眉剑法#5", "in_team('郭襄') and skill_level('郭襄', '峨眉剑法') < 5")]
    [InlineData("daode_more_than", "50", "morality >= 50")]
    [InlineData("daode_less_than", "50", "morality < 50")]
    [InlineData("haogan_more_than", "100", "favorability('女主') >= 100")]
    [InlineData("haogan_less_than", "郭襄#50", "favorability('郭襄') < 50")]
    [InlineData("rank", "10", "rank != -1 and rank <= 10")]
    [InlineData("quanzhang_more_than", "主角#20", "character_stat('主角', 'quanzhang') >= 20")]
    [InlineData("jianfa_less_than", "郭襄#50", "in_team('郭襄') and character_stat('郭襄', 'jianfa') < 50")]
    [InlineData("custom_query", "任务#状态", "custom_query('任务', '状态')")]
    public void Convert_MapsConditions(string type, string value, string expected)
    {
        var source = LegacyStoryXmlConverter.Convert(Xml(new XElement("result",
            new XAttribute("type", "story"), new XAttribute("value", "结束"),
            new XElement("condition", new XAttribute("type", type), new XAttribute("value", value)))));

        Assert.Equal($"# 测试\nif {expected}\n  jump 结束\n", source);
    }

    [Fact]
    public void Convert_EmitsChoicesAndBothNamedAndNumericBattleOutcomes()
    {
        var source = LegacyStoryXmlConverter.Convert("""
            <root>
              <story name="选择">
                <action type="SELECT" value="主角#去哪里？#出发#结束"/>
                <result type="story" ret="0" value="下一段"/>
                <result type="gameFin" ret="1" value="gameFin"/>
              </story>
              <story name="战斗">
                <action type="BATTLE" value="测试战斗"/>
                <result type="story" ret="win" value="胜利"/>
                <result type="gameOver" ret="1" value="gameOver"/>
                <result type="story" ret="2" value="超时"/>
              </story>
            </root>
            """);

        Assert.Equal("""
            # 选择
            主角：去哪里？
            - 出发
              jump 下一段
            - 结束
              game_complete()

            # 战斗
            battle 测试战斗
            - win
              jump 胜利
            - lose
              game_over()
            - timeout
              jump 超时

            """.ReplaceLineEndings("\n"), source);
    }

    [Fact]
    public void Convert_CombinesConditionsAndCommentsConflictingFallbacks()
    {
        var source = LegacyStoryXmlConverter.Convert("""
            <root><story name="测试">
              <result type="story" ret="0" value="旧默认"/>
              <result type="story" ret="0" value="条件分支">
                <condition type="in_team" value="郭襄"/>
                <condition type="probability" value="25"/>
              </result>
              <result type="story" ret="0" value="默认"/>
            </story></root>
            """);

        Assert.Equal("# 测试\n// jump 旧默认\nif in_team('郭襄') and chance(0.25)\n  jump 条件分支\njump 默认\n", source);
    }

    [Fact]
    public void Convert_PrefersFirstNumericFallbackWithoutConditions()
    {
        var source = LegacyStoryXmlConverter.Convert("""
            <root><story name="测试">
              <result type="story" value="旧默认"/>
              <result type="story" ret="0" value="默认"/>
              <result type="story" ret="1" value="其他"/>
            </story></root>
            """);

        Assert.Equal("# 测试\n// jump 旧默认\njump 默认\n// jump 其他\n", source);
    }

    [Fact]
    public void Convert_PreservesXmlEntitiesColorsNewlinesAndEscapedArguments()
    {
        var source = LegacyStoryXmlConverter.Convert("""
            <root><story name="测试">
              <action type="DiAlOg" value="O&apos;Neil#你好 &amp; [[red:红色]]&#10;下一行"/>
              <action type="LOG" value="路径 C:\story，&apos;引号&apos; &amp; &lt;内容&gt;"/>
            </story></root>
            """);

        Assert.Equal("# 测试\nO'Neil：你好 & [color=red]红色[/color][br]下一行\njournal('路径 C:\\\\story，\\'引号\\' & <内容>')\n", source);
    }

    [Theory]
    [InlineData("<root><story></root>")]
    [InlineData("<!DOCTYPE root [<!ENTITY text 'hello'>]><root/>")]
    [InlineData("<wrong/>")]
    [InlineData("<root><story/></root>")]
    [InlineData("<root><story name='测试'><action type='ITEM' value='药#NaN'/></story></root>")]
    [InlineData("<root><story name='测试'><action type='SET_FLAG' value='Bad Name'/></story></root>")]
    [InlineData("<root><story name='测试'><action type='MINUS_MAXPOINTS' value='郭襄#5'/></story></root>")]
    [InlineData("<root><story name='测试'><result type='story' value='结束'><condition type='probability' value='101'/></result></story></root>")]
    public void Convert_ReportsInvalidXmlAndUnsupportedValuesWithSourceLocation(string xml)
    {
        var exception = Assert.Throws<FormatException>(() => LegacyStoryXmlConverter.Convert(xml, "input.xml"));

        Assert.Contains("input.xml(", exception.Message);
    }

    [Fact]
    public void Convert_ValidatesGeneratedStoryBeforeReturning()
    {
        Assert.Throws<StoryCompilationException>(() => LegacyStoryXmlConverter.Convert("""
            <root><story name="测试">
              <result type="story" value="结束"/>
              <result type="story" value="条件分支"><condition type="in_team" value="郭襄"/></result>
            </story></root>
            """));
    }

    private static string Xml(XElement child) =>
        new XElement("root", new XElement("story", new XAttribute("name", "测试"), child)).ToString();
}

public sealed class StoryXmlConverterCommandTests : IDisposable
{
    private readonly string _directory = Path.Combine(AppContext.BaseDirectory, "TestScratch", "story-xml", Guid.NewGuid().ToString("N"));
    private const string ValidXml = "<root><story name='测试'><action type='DIALOG' value='主角#你好'/></story></root>";

    [Fact]
    public void Run_ProvidesUsage()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, StoryXmlConverterCommand.Run(["--help"], output, error));
        Assert.Contains("input.xml", output.ToString());
        Assert.Equal(1, StoryXmlConverterCommand.Run([], output, error));
        Assert.Contains("input.xml", error.ToString());
    }

    [Fact]
    public void Run_DefaultOutputUsesUniqueNamesAndUtf8WithoutBom()
    {
        var input = Write("input.xml", ValidXml);
        var existing = Write("input.converted.story", "keep");

        Assert.Equal(0, Run(input));
        Assert.Equal(0, Run(input));

        Assert.Equal("keep", File.ReadAllText(existing));
        Assert.Equal(Encoding.UTF8.GetBytes("# 测试\n主角：你好\n"), File.ReadAllBytes(Path.Combine(_directory, "input.converted-2.story")));
        Assert.True(File.Exists(Path.Combine(_directory, "input.converted-3.story")));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public async Task Run_ConcurrentConversionsChooseDistinctDefaultOutputs()
    {
        var input = Write("input.xml", ValidXml);

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => Run(input))));

        Assert.All(results, result => Assert.Equal(0, result));
        Assert.Equal(4, Directory.GetFiles(_directory, "*.story").Length);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Run_ExplicitOutputDoesNotOverwriteExistingFiles()
    {
        var input = Write("input.xml", ValidXml);
        var output = Write("output.story", "keep");

        Assert.Equal(1, Run(input, output));
        Assert.Equal("keep", File.ReadAllText(output));
        Assert.Equal(1, Run(input, input));
        Assert.Equal(ValidXml, File.ReadAllText(input));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Theory]
    [InlineData("<root><story name='测试'><action type='ITEM' value='药#invalid'/></story></root>")]
    [InlineData("<root><story name='测试'><action type='DIALOG' value='主角#[#style=bold]你好'/></story></root>")]
    public void Run_FailedValidationLeavesNoOutput(string xml)
    {
        var input = Write("input.xml", xml);
        var output = Path.Combine(_directory, "output.story");

        Assert.Equal(1, Run(input, output));
        Assert.Equal([input], Directory.GetFiles(_directory));
    }

    [Fact]
    public void Run_DefaultOutputSkipsDirectoryCollisions()
    {
        var input = Write("input.xml", ValidXml);
        Directory.CreateDirectory(Path.Combine(_directory, "input.converted.story"));

        Assert.Equal(0, Run(input));
        Assert.True(File.Exists(Path.Combine(_directory, "input.converted-2.story")));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Run_ReadsUtf8BomAndWritesExplicitOutput()
    {
        Directory.CreateDirectory(_directory);
        var input = Path.Combine(_directory, "input.xml");
        File.WriteAllText(input, ValidXml, new UTF8Encoding(true));
        var output = Path.Combine(_directory, "output.story");

        Assert.Equal(0, Run(input, output));
        Assert.Equal("# 测试\n主角：你好\n", File.ReadAllText(output));
    }

    private string Write(string name, string content)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static int Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        return StoryXmlConverterCommand.Run(args, output, error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
