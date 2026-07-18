using Game.Core.Story;

namespace Game.Tests;

public sealed class StoryBinderTests
{
    [Fact]
    public async Task StoryCommandBinder_BindsParamsArrayArguments()
    {
        var target = new RecordingTarget();
        var binder = new StoryCommandBinder(target);

        var executed = binder.TryExecute(
            "music",
            [ExprValue.FromString("a"), ExprValue.FromString("b"), ExprValue.FromString("c")],
            CancellationToken.None,
            out var result);

        Assert.True(executed);
        await result;
        Assert.Equal(["a", "b", "c"], target.TrackIds);
    }

    [Fact]
    public async Task StoryCommandBinder_BindsStringListArgument()
    {
        var target = new RecordingTarget();
        var binder = new StoryCommandBinder(target);

        var executed = binder.TryExecute(
            "playlist",
            [ExprValue.FromList([ExprValue.FromString("a"), ExprValue.FromString("b")])],
            CancellationToken.None,
            out var result);

        Assert.True(executed);
        await result;
        Assert.Equal(["a", "b"], target.TrackIds);
    }

    [Fact]
    public void StoryScriptJson_ParsesListValueArguments()
    {
        const string json = """
        {
          "version": 2,
          "segments": [
            {
              "name": "start",
              "steps": [
                {
                  "kind": "command",
                  "name": "random_join",
                  "args": [["list", "胡斐", ["var", "candidateId"]]]
                }
              ]
            }
          ]
        }
        """;

        var script = StoryScriptJson.Parse(json);
        var command = Assert.IsType<CommandStep>(Assert.Single(script.Segments[0].Steps));
        var list = Assert.IsType<ListExprNode>(Assert.Single(command.Args));

        Assert.Collection(
            list.Items,
            item => Assert.Equal("胡斐", Assert.IsType<LiteralExprNode>(item).Value.Text),
            item => Assert.Equal("candidateId", Assert.IsType<VariableExprNode>(item).Name));
    }

    [Fact]
    public void StoryScriptJson_ParsesConditionalChoiceGroups()
    {
        const string json = """
        {
          "version": 2,
          "segments": [
            {
              "name": "start",
              "steps": [
                {
                  "kind": "choice",
                  "prompt": { "speaker": "掌柜", "text": "选择" },
                  "groups": [
                    {
                      "options": [
                        { "text": "离开", "steps": [] }
                      ]
                    },
                    {
                      "when": ["pred", "shop_open"],
                      "options": [
                        { "text": "购买", "steps": [] },
                        { "text": "出售", "steps": [] }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var script = StoryScriptJson.Parse(json);
        var choice = Assert.IsType<ChoiceStep>(Assert.Single(script.Segments[0].Steps));

        Assert.Equal(StoryScript.CurrentVersion, script.Version);
        Assert.Equal(ChoiceStyle.Regular, choice.Style);
        Assert.Null(choice.Groups[0].When);
        Assert.Equal("离开", Assert.Single(choice.Groups[0].Options).Text);
        var predicate = Assert.IsType<PredicateExprNode>(choice.Groups[1].When);
        Assert.Equal("shop_open", predicate.Name);
        Assert.Equal(["购买", "出售"], choice.Groups[1].Options.Select(static option => option.Text).ToArray());
    }

    [Fact]
    public void StoryScriptJson_ParsesChoiceStyle()
    {
        const string json = """
        {
          "version": 2,
          "segments": [{
            "name": "start",
            "steps": [{
              "kind": "choice",
              "style": "bold",
              "prompt": { "speaker": "", "text": "选择" },
              "groups": [{
                "options": [{ "text": "确认", "steps": [] }]
              }]
            }]
          }]
        }
        """;

        var script = StoryScriptJson.Parse(json);
        var choice = Assert.IsType<ChoiceStep>(Assert.Single(script.Segments[0].Steps));

        Assert.Equal(ChoiceStyle.Bold, choice.Style);
    }

    [Fact]
    public void StoryScriptJson_RejectsUnknownChoiceStyle()
    {
        const string json = """
        {
          "version": 2,
          "segments": [{
            "name": "start",
            "steps": [{
              "kind": "choice",
              "style": "unknown",
              "prompt": { "speaker": "", "text": "选择" },
              "groups": [{
                "options": [{ "text": "确认", "steps": [] }]
              }]
            }]
          }]
        }
        """;

        Assert.Throws<StoryRuntimeException>(() => StoryScriptJson.Parse(json));
    }

    [Theory]
    [MemberData(nameof(InvalidChoiceScripts))]
    public void StoryScriptJson_RejectsInvalidVersion2ChoiceShapes(string json)
    {
        Assert.Throws<StoryRuntimeException>(() => StoryScriptJson.Parse(json));
    }

    public static TheoryData<string> InvalidChoiceScripts { get; } = new()
    {
        {
            """
        { "version": 1, "segments": [] }
        """
        },
        {
            """
        {
          "version": 2,
          "segments": [{ "name": "start", "steps": [{
            "kind": "choice",
            "prompt": { "speaker": "", "text": "" },
            "options": []
          }] }]
        }
        """
        },
        {
            """
        {
          "version": 2,
          "segments": [{ "name": "start", "steps": [{
            "kind": "choice",
            "prompt": { "speaker": "", "text": "" },
            "groups": [{ "when": null, "options": [{ "text": "A", "steps": [] }] }]
          }] }]
        }
        """
        },
        {
            """
        {
          "version": 2,
          "segments": [{ "name": "start", "steps": [{
            "kind": "choice",
            "prompt": { "speaker": "", "text": "" },
            "groups": [{ "options": [] }]
          }] }]
        }
        """
        },
    };

    [Fact]
    public async Task StoryCommandBinder_AllowsCommandsToReturnJumpResults()
    {
        var target = new RecordingTarget();
        var binder = new StoryCommandBinder(target);

        var executed = binder.TryExecute(
            "jump",
            [ExprValue.FromString("next")],
            CancellationToken.None,
            out var result);

        Assert.True(executed);
        Assert.Equal("next", (await result).JumpTarget);
    }

    private sealed class RecordingTarget
    {
        public IReadOnlyList<string> TrackIds { get; private set; } = [];

        [StoryCommand("music")]
        private ValueTask ExecuteMusicAsync(params string[] trackIds)
        {
            TrackIds = trackIds;
            return ValueTask.CompletedTask;
        }

        [StoryCommand("playlist")]
        private ValueTask ExecutePlaylistAsync(IReadOnlyList<string> trackIds)
        {
            TrackIds = trackIds;
            return ValueTask.CompletedTask;
        }

        [StoryCommand("jump")]
        private StoryCommandResult ExecuteJump(string target) => StoryCommandResult.Jump(target);
    }
}
