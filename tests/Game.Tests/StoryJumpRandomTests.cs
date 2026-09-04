using Game.Application;
using Game.Core.Model;
using Game.Core.Story;
using Game.Expressions;

namespace Game.Tests;

public sealed class StoryJumpRandomTests
{
    private static StoryScript TwoSegmentScript() => StoryScriptJson.Parse("""
        {"version":3,"segments":[
          {"name":"first","steps":[{"kind":"command","call":"journal('first')"}]},
          {"name":"second","steps":[{"kind":"command","call":"journal('second')"}]}
        ]}
        """);

    [Fact]
    public async Task JumpRandomReturnsOneOfTheValidatedCandidates()
    {
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [TwoSegmentScript()]));
        var parser = new ExpressionParser();

        var result = await session.StoryService.CommandDispatcher.ExecuteCallAsync(
            parser.ParseCall("jump_random(['first', 'second'])"));

        Assert.Contains(result.JumpTarget, ["first", "second"]);
    }

    [Fact]
    public async Task JumpRandomRejectsEmptyAndUnknownCandidates()
    {
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [TwoSegmentScript()]));
        var parser = new ExpressionParser();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await session.StoryService.CommandDispatcher.ExecuteCallAsync(
                parser.ParseCall("jump_random([])")));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await session.StoryService.CommandDispatcher.ExecuteCallAsync(
                parser.ParseCall("jump_random(['missing'])")));
    }

    [Fact]
    public async Task RunAsyncPipedReferenceCompletesExactlyOneCandidate()
    {
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [TwoSegmentScript()]));

        await session.StoryService.ExecuteAsync("first|second");

        Assert.True(session.State.Story.IsStoryCompleted("first") != session.State.Story.IsStoryCompleted("second"));
    }

    [Fact]
    public async Task RunAsyncPrefersExactSegmentOverPipeSplit()
    {
        var script = StoryScriptJson.Parse("""
            {"version":3,"segments":[
              {"name":"a|b","steps":[{"kind":"command","call":"journal('literal')"}]},
              {"name":"a","steps":[{"kind":"command","call":"journal('a')"}]},
              {"name":"b","steps":[{"kind":"command","call":"journal('b')"}]}
            ]}
            """);
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [script]));

        await session.StoryService.ExecuteAsync("a|b");

        Assert.True(session.State.Story.IsStoryCompleted("a|b"));
        Assert.False(session.State.Story.IsStoryCompleted("a"));
        Assert.False(session.State.Story.IsStoryCompleted("b"));
    }
}
