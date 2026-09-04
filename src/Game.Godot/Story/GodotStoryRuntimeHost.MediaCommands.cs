using Game.Application;
using Game.Godot.Assets;
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

		// XMJH often appends a numeric fade/transition argument (for example
		// music('音乐.风之海逗趣', '1')). It is not a second track.
		var tracks = trackIds
			.Where(static id => !double.TryParse(id, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
			.ToArray();
		if (tracks.Length == 0)
		{
			return ValueTask.CompletedTask;
		}

		if (tracks.Length == 1) Game.Audio.PlayBgm(tracks[0]);
		else Game.Audio.PlayBgm(tracks);
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
	private ValueTask ExecuteSuggestAsync(
		string text,
		string title = "提示",
		CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.ShowSuggestionAsync(text, title, cancellationToken: cancellationToken));

	[StoryCommand("suggest2")]
	private ValueTask ExecuteSuggest2Async(
		string text,
		string title = "提示",
		string acknowledgeText = "确认",
		CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.ShowSuggestionAsync(text, title, acknowledgeText, cancellationToken));

	[StoryCommand("show_favorability")]
	private ValueTask ExecuteShowFavorabilityAsync(
		string target,
		CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.ShowSuggestionAsync(
			$"{target}：{Game.State.Adventure.GetFavorability(target)}",
			cancellationToken: cancellationToken));

	[StoryCommand("toast")]
	private ValueTask ExecuteToastAsync(bool enabled)
	{
		UIRoot.Instance.SetToastSuppressed(!enabled);
		return ValueTask.CompletedTask;
	}
}
