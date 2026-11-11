using Game.Core.Story;
using Game.StoryDsl;

namespace Game.Tests;

public sealed class StoryCompilerTests
{
    [Theory]
    [InlineData("掌柜(疲惫)")]
    [InlineData("(旁白)")]
    [InlineData("guard(tired)")]
    [InlineData("O'Neil")]
    public void Compile_PreservesSpeakerNamesContainingParenthesesAndApostrophes(string speaker)
    {
        var script = StoryCompiler.Compile($"# 开始\n{speaker}：你好");

        var dialogue = Assert.IsType<DialogueStep>(Assert.Single(script.Segments[0].Steps));
        Assert.Equal(speaker, dialogue.Speaker);
        Assert.Equal("你好", dialogue.Text);
    }

    [Theory]
    [InlineData("suggest('旁白：你好')")]
    [InlineData("""suggest('some \'quote: text')""")]
    [InlineData("change_silver(true ? 1 : 2)")]
    [InlineData("(change_silver(false ? 1 : 2))")]
    public void Compile_KeepsColonsInsideCallArgumentsInCommands(string source)
    {
        var script = StoryCompiler.Compile("# 开始\n" + source);

        Assert.Equal(source, Assert.IsType<CommandStep>(Assert.Single(script.Segments[0].Steps)).Call.Source);
    }

    [Fact]
    public void Compile_ProducesRuntimeStepsForDialogueVariablesCallsAndBattle()
    {
        var script = StoryCompiler.Compile("""
            // 文件注释
            #   开始
            主角 : 开始冒险
            change_silver(2) // 命令注释
            是否拜师 = true
            声望 += 2
            声望 -= 1
            del 是否拜师
            call 公共片段
            battle 新手战
            - win
              jump 胜利
            - lose
              return
            - timeout
            # 公共片段
            ：公共对白
            return
            # 胜利
            ：
            """, "test.story");

        var segment = script.Segments[0];
        Assert.Equal("开始", segment.Name);
        Assert.Equal("主角", Assert.IsType<DialogueStep>(segment.Steps[0]).Speaker);
        Assert.Equal("change_silver(2)", Assert.IsType<CommandStep>(segment.Steps[1]).Call.Source);
        Assert.Equal("true", Assert.IsType<SetVariableStep>(segment.Steps[2]).Value.Source);
        Assert.Equal("声望 + (2)", Assert.IsType<SetVariableStep>(segment.Steps[3]).Value.Source);
        Assert.Equal("声望 - (1)", Assert.IsType<SetVariableStep>(segment.Steps[4]).Value.Source);
        Assert.Equal("是否拜师", Assert.IsType<DeleteVariableStep>(segment.Steps[5]).Target);
        Assert.Equal("公共片段", Assert.IsType<CallStep>(segment.Steps[6]).Target);
        var battle = Assert.IsType<BattleStep>(segment.Steps[7]);
        Assert.IsType<JumpStep>(Assert.Single(battle.Outcomes[BattleOutcome.Win]));
        Assert.IsType<ReturnStep>(Assert.Single(battle.Outcomes[BattleOutcome.Lose]));
        Assert.Empty(battle.Outcomes[BattleOutcome.Timeout]);
        Assert.Equal("", Assert.IsType<DialogueStep>(Assert.Single(script.Segments[2].Steps)).Text);
    }

    [Fact]
    public void Compile_PreservesOrderedChoiceBlocksAndNestedConditions()
    {
        var script = StoryCompiler.Compile("""
            # 商店
            掌柜：[# style = bold ] 客官需要什么？
            - 离开
              return
            - [color=red]特殊[/color] if has_var('license')
              change_silver(1)
            if shop_open and silver > 0
              - 购买 if silver >= 100
                if has_var('discount')
                  change_silver(-50)
                elif silver > 200
                  change_silver(-100)
                else
                  change_silver(-80)
              - 出售
            elif silver <= 0
              - 赊账
            else
              - 询问行情
            - 告别
            if has_var('secret_shop')
              - 隐藏路线
            """);

        var choice = Assert.IsType<ChoiceStep>(Assert.Single(script.Segments[0].Steps));
        Assert.Equal(ChoiceStyle.Bold, choice.Style);
        Assert.Equal("客官需要什么？", choice.Prompt.Text);
        Assert.Equal(4, choice.Blocks.Count);
        var options = Assert.IsType<ChoiceOptionsBlock>(choice.Blocks[0]).Options;
        Assert.Equal(2, options.Count);
        Assert.Null(options[0].When);
        Assert.Equal("has_var('license')", options[1].When!.Source);
        var conditional = Assert.IsType<ChoiceBranchBlock>(choice.Blocks[1]);
        Assert.Equal(2, conditional.Cases.Count);
        Assert.Single(conditional.Fallback!);
        var purchase = conditional.Cases[0].Options[0];
        Assert.Equal("silver >= 100", purchase.When!.Source);
        var nested = Assert.IsType<BranchStep>(Assert.Single(purchase.Steps));
        Assert.Equal(2, nested.Cases.Count);
        Assert.Single(nested.Fallback!);
        Assert.IsType<ChoiceOptionsBlock>(choice.Blocks[2]);
        Assert.Null(Assert.IsType<ChoiceBranchBlock>(choice.Blocks[3]).Fallback);
    }

    [Fact]
    public void Compile_DistinguishesRegularBranchesFromConditionalChoiceOptions()
    {
        var script = StoryCompiler.Compile("""
            # 开始
            旁白：普通对白
            if silver >= 10
              change_silver(-10)
            if true
              旁白：分支内选项
              - 继续
                battle 试炼
            """);

        Assert.IsType<DialogueStep>(script.Segments[0].Steps[0]);
        Assert.IsType<BranchStep>(script.Segments[0].Steps[1]);
        var branch = Assert.IsType<BranchStep>(script.Segments[0].Steps[2]);
        var choice = Assert.IsType<ChoiceStep>(Assert.Single(branch.Cases[0].Steps));
        var option = Assert.Single(Assert.IsType<ChoiceOptionsBlock>(choice.Blocks[0]).Options);
        Assert.Empty(Assert.IsType<BattleStep>(Assert.Single(option.Steps)).Outcomes);
    }

    [Fact]
    public void Compile_PreservesCommentMarkersAndConditionalDelimitersInsideStringsAndRichText()
    {
        var script = StoryCompiler.Compile("""
            # 开始
            suggest('https://example.test/a\'b') // 尾注释
            旁白：'https://example.test'
            - 'what if true'
            - [url=some if target]链接[/url]
            - 继续 if 'if' in ['if', 'else']
            """);

        Assert.Equal("suggest('https://example.test/a\\'b')",
            Assert.IsType<CommandStep>(script.Segments[0].Steps[0]).Call.Source);
        var choice = Assert.IsType<ChoiceStep>(script.Segments[0].Steps[1]);
        var options = Assert.IsType<ChoiceOptionsBlock>(Assert.Single(choice.Blocks)).Options;
        Assert.Null(options[0].When);
        Assert.Null(options[1].When);
        Assert.Equal("'if' in ['if', 'else']", options[2].When!.Source);
    }

    [Theory]
    [InlineData("maxlevel('独孤九剑', 1)", "开始_独孤九剑")]
    [InlineData("(maxlevel('独孤九剑', 1))", "开始_独孤九剑")]
    [InlineData("((maxlevel('剑)法', (1 + 2))))", "开始_剑)法")]
    [InlineData("maxlevel('独孤九剑', 1, 'explicit')", "explicit")]
    public void Compile_PreservesRewardKeys(string command, string expectedKey)
    {
        var script = StoryCompiler.Compile("# 开始\n" + command);
        var call = Assert.IsType<CommandStep>(Assert.Single(script.Segments[0].Steps)).Call;
        Assert.Equal(command, call.Source);
        var literal = Assert.IsType<Game.Expressions.LiteralExpressionSyntax>(call.Root.Arguments[2]);
        Assert.Equal(expectedKey, literal.Value.Text);
    }

    [Fact]
    public void Compile_PreservesAutomaticallyGeneratedRewardKeyCharacters()
    {
        var script = StoryCompiler.Compile("""
            # 侠客'传\开始
            maxlevel('剑\'法', 1)
            """);
        var call = Assert.IsType<CommandStep>(Assert.Single(script.Segments[0].Steps)).Call;
        Assert.Equal("侠客'传\\开始_剑'法",
            Assert.IsType<Game.Expressions.LiteralExpressionSyntax>(call.Root.Arguments[2]).Value.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("// 仅注释\r\n")]
    public void Compile_AcceptsEmptyDocuments(string source)
    {
        Assert.Empty(StoryCompiler.Compile(source).Segments);
    }

    [Fact]
    public void Compile_UsesSharedExpressionGrammarForValuesAndCommandArguments()
    {
        var script = StoryCompiler.Compile("""
            # 开始
            reward = true ? 1 : 2
            change_silver(false ? 1 : 2)
            """);

        var steps = script.Segments[0].Steps;
        Assert.IsType<Game.Expressions.ConditionalExpressionSyntax>(
            Assert.IsType<SetVariableStep>(steps[0]).Value.Root);
        Assert.IsType<Game.Expressions.ConditionalExpressionSyntax>(
            Assert.IsType<CommandStep>(steps[1]).Call.Root.Arguments[0]);
    }

    [Fact]
    public void Compile_AcceptsBomAndReportsPhysicalCrLfLineAndExpressionColumn()
    {
        var exception = Assert.Throws<StoryCompilationException>(() =>
            StoryCompiler.Compile("\uFEFF# 开始\r\n\r\nif true\r\n  change_silver(@)\r\n", "scripts/test.story"));

        Assert.Equal("scripts/test.story", exception.SourceName);
        Assert.Equal("开始", exception.SegmentName);
        Assert.Equal(4, exception.Line);
        Assert.Equal(16, exception.Column);
        Assert.Contains("scripts/test.story(4,16)", exception.Message);
    }

    [Theory]
    [InlineData("旁白：无段头")]
    [InlineData("# ")]
    [InlineData("# 开始\n#   开始  ")]
    [InlineData("# 开始\n change_silver(1)")]
    [InlineData("# 开始\n\tchange_silver(1)")]
    [InlineData("# 开始\n    change_silver(1)")]
    [InlineData("# 开始\n- 无提示选项")]
    [InlineData("# 开始\nelif true")]
    [InlineData("# 开始\nelse")]
    [InlineData("# 开始\nif true\nelse\nelse")]
    [InlineData("# 开始\nif true\nelse false")]
    [InlineData("# 开始\nbattle")]
    [InlineData("# 开始\nbattle 试炼\n- draw")]
    [InlineData("# 开始\nbattle 试炼\n- win\n- win")]
    [InlineData("# 开始\njump")]
    [InlineData("# 开始\ncall")]
    [InlineData("# 开始\nreturn 1")]
    [InlineData("# 开始\njump 结束\nchange_silver(1)")]
    [InlineData("# 开始\nreturn\nchange_silver(1)")]
    [InlineData("# 开始\ndel")]
    [InlineData("# 开始\ndel two names")]
    [InlineData("# 开始\ncount =")]
    [InlineData("# 开始\ncount += @")]
    [InlineData("# 开始\nchange_silver 1")]
    [InlineData("# 开始\nwin()")]
    [InlineData("# 开始\nmaxlevel(skill_id, 1)")]
    [InlineData("# 开始\n旁白：[#style=bold]正文\n- 选项 if")]
    [InlineData("# 开始\n旁白：正文\n- [#style=bold]选项")]
    [InlineData("# 开始\n旁白：正文[#style=bold]")]
    [InlineData("# 开始\n旁白：[#style=bold")]
    [InlineData("# 开始\n旁白：[#other=test]正文")]
    [InlineData("# 开始\n旁白：[#style=bold][#style=regular]正文")]
    [InlineData("# 开始\n旁白：[#style=unknown]正文\n- 选项")]
    [InlineData("# 开始\n旁白：[#style=bold]正文")]
    [InlineData("# 开始\n旁白：正文\nif true\n  - 选项\nelse\n  change_silver(1)")]
    public void Compile_RejectsMalformedSource(string source)
    {
        var exception = Assert.Throws<StoryCompilationException>(() => StoryCompiler.Compile(source, "invalid.story"));
        Assert.Equal("invalid.story", exception.SourceName);
        Assert.True(exception.Line > 0);
        Assert.True(exception.Column > 0);
    }
}
