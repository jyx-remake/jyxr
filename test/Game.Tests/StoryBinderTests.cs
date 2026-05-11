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

        [StoryCommand("jump")]
        private StoryCommandResult ExecuteJump(string target) => StoryCommandResult.Jump(target);
    }
}
