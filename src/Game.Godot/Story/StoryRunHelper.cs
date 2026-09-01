using Game.Core.Story;

namespace Game.Godot.Story;

public static class StoryRunHelper
{
	public static async Task<StoryRunResult> RunAsync(
		string storyId,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(storyId);

		var result = StoryRunResult.Incomplete;
		await foreach (var storyEvent in Game.StoryService.RunAsync(storyId, cancellationToken: cancellationToken))
		{
			result = storyEvent switch
			{
				SegmentStartedEvent when !result.Terminated => StoryRunResult.Incomplete,
				SegmentCompletedEvent when !result.Terminated => StoryRunResult.Completed,
				StoryTerminatedEvent => StoryRunResult.TerminatedResult,
				_ => result,
			};
		}

		return result;
	}
}

public readonly record struct StoryRunResult(bool SegmentCompleted, bool Terminated)
{
	public static StoryRunResult Incomplete { get; } = new(false, false);
	public static StoryRunResult Completed { get; } = new(true, false);
	public static StoryRunResult TerminatedResult { get; } = new(false, true);

	public bool WasHandled => SegmentCompleted || Terminated;
}
