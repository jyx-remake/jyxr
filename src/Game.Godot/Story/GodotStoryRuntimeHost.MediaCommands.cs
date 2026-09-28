using Game.Application;
using Game.Godot.Resources;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	[StoryCommand("music")]
	private ValueTask ExecuteMusicAsync(params string[] trackIds)
	{
		if (trackIds.Length == 0)
		{
			throw new InvalidOperationException("Command 'music' requires at least one argument.");
		}

		if (trackIds.Length == 1) Game.Audio.PlayBgm(trackIds[0]);
		else Game.Audio.PlayBgm(trackIds);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("sound", "effect")]
	private ValueTask ExecuteEffectAsync(string effectId)
	{
		Game.Audio.PlaySfx(effectId);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("video", "movie")]
	private async ValueTask ExecuteVideoAsync(string videoId, CancellationToken cancellationToken)
	{
		var stream = AssetResolver.LoadVideo(videoId)
			?? throw new InvalidOperationException(
				$"Video resource '{videoId}' could not be loaded. Expected an Ogg Theora .ogv file.");
		using var bgmSuspension = Game.Audio.SuspendBgm();
		await UIRoot.Instance.ShowVideoAsync(stream, cancellationToken);
	}

	[StoryCommand("suggest")]
	private ValueTask ExecuteSuggestAsync(string text, CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowSuggestionAsync(text, cancellationToken));

	[StoryCommand("toast")]
	private ValueTask ExecuteToastAsync(bool enabled)
	{
		UIRoot.Instance.SetToastSuppressed(!enabled);
		return ValueTask.CompletedTask;
	}
}
