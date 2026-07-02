using Game.Core.Story;

namespace Game.Godot.Story;

public static class StoryRunHelper
{
	public static async Task<bool> RunAsync(
		string storyId,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(storyId);

		var completed = false;
		await foreach (var storyEvent in Game.StoryService.RunAsync(storyId, cancellationToken))
		{
			completed = storyEvent is SegmentCompletedEvent;
		}

		return completed;
	}
}
