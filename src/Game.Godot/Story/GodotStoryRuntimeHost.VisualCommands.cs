using Game.Application;
using Game.Expressions;
using Game.Godot.Map;
using Game.Godot.UI;
using System.Globalization;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	[StoryCommand("shake")]
	private ValueTask ExecuteShakeAsync(
		double amplitude = 10d,
		double duration = 0.5d,
		CancellationToken cancellationToken = default)
	{
		ValidateShakeArguments(amplitude, duration);
		return new ValueTask(UIRoot.Instance.VisualEffects.ShakeAsync((float)amplitude, duration, cancellationToken));
	}

	[StoryCommand("fade")]
	private ValueTask ExecuteFadeAsync(string mode, double duration = 0.5d, CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.VisualEffects.FadeAsync(mode, duration, cancellationToken));

	[StoryCommand("fadein")]
	private async ValueTask ExecuteFadeInAsync(
		string backgroundId,
		ExpressionValue duration = default,
		ExpressionValue fullAlpha = default,
		CancellationToken cancellationToken = default)
	{
		// Legacy FADEIN swaps the backdrop in at alpha 0 and tweens the image's
		// own alpha up to the ambient time-of-day opacity (full alpha when the
		// optional third argument is present), so the scene cuts to black and
		// the new backdrop fades in. Clearing the black overlay in parallel
		// keeps a preceding `fade out` from swallowing the effect.
		World.Instance.SetBackground(ResolveBackdropCandidate(backgroundId), 0f);
		var fadeDuration = ResolveLegacyFadeSeconds(duration);
		var targetAlpha = fullAlpha == default
			? MapTimeLighting.GetAmbientOpacity(Game.State.Clock.TimeSlot)
			: 1f;
		var backdropFade = World.Instance.FadeBackdropAlphaAsync(targetAlpha, fadeDuration, cancellationToken);
		var overlayFade = UIRoot.Instance.VisualEffects.FadeAsync("in", fadeDuration, cancellationToken);
		await backdropFade;
		await overlayFade;
	}

	[StoryCommand("flash")]
	private ValueTask ExecuteFlashAsync(string preset = "white", double duration = 0.25d, double strength = 1d, CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.VisualEffects.FlashAsync(preset, duration, strength, cancellationToken));

	[StoryCommand("filter")]
	private ValueTask ExecuteFilterAsync(string preset, double strength = 1d, double duration = 0.3d, CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.VisualEffects.ApplyFilterAsync(preset, strength, duration, cancellationToken));

	[StoryCommand("clear_filter")]
	private ValueTask ExecuteFilterClearAsync(double duration = 0.3d, CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.VisualEffects.ClearFilterAsync(duration, cancellationToken));

	[StoryCommand("distort")]
	private ValueTask ExecuteDistortAsync(string preset, double strength = 1d, double duration = 0.3d, CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.VisualEffects.ApplyDistortionAsync(preset, strength, duration, cancellationToken));

	[StoryCommand("clear_distort")]
	private ValueTask ExecuteDistortClearAsync(double duration = 0.3d, CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.VisualEffects.ClearDistortionAsync(duration, cancellationToken));

	[StoryCommand("tint")]
	private ValueTask ExecuteTintAsync(string color, double strength = 0.25d, double duration = 0.3d, CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.VisualEffects.ApplyTintAsync(color, strength, duration, cancellationToken));

	[StoryCommand("clear_tint")]
	private ValueTask ExecuteTintClearAsync(double duration = 0.3d, CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.VisualEffects.ClearTintAsync(duration, cancellationToken));

	[StoryCommand("wait")]
	private ValueTask ExecuteWaitAsync(double duration, CancellationToken cancellationToken) =>
		new(UIRoot.Instance.VisualEffects.WaitAsync(duration, cancellationToken));

	[StoryCommand("intertitle")]
	private ValueTask ExecuteIntertitleAsync(
		string text,
		string position = "center",
		string mode = "typewriter",
		double speed = 36d,
		CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.ShowIntertitleAsync(text, position, mode, speed, cancellationToken));

	private static void ValidateShakeArguments(double amplitude, double duration)
	{
		if (!double.IsFinite(amplitude) || amplitude < 0d)
			throw new ArgumentOutOfRangeException(nameof(amplitude), "Command 'shake' amplitude must be finite and non-negative.");
		if (!double.IsFinite(duration) || duration < 0d)
			throw new ArgumentOutOfRangeException(nameof(duration), "Command 'shake' duration must be finite and non-negative.");
	}

	private const double LegacyFadeMinSeconds = 0.2d;
	private const double LegacyFadeMaxSeconds = 600d;

	private static string ResolveBackdropCandidate(string backgroundId)
	{
		// Legacy backdrop commands accept a '|'-separated candidate list and
		// pick one entry at random (used for variant scene art).
		var candidates = backgroundId.Split('|', StringSplitOptions.RemoveEmptyEntries);
		if (candidates.Length == 0)
		{
			return backgroundId;
		}

		return candidates.Length == 1
			? candidates[0]
			: candidates[Random.Shared.Next(candidates.Length)];
	}

	private static double ResolveLegacyFadeSeconds(ExpressionValue value)
	{
		// Legacy FADEIN serializes its duration as a quoted string (and some
		// scripts omit it entirely), while hand-authored v3 stories may use a
		// numeric literal. Accept both forms at this compatibility boundary,
		// then apply the legacy clamp: 0.2s minimum, 600s maximum, 0.2s default.
		var duration = value == default
			? LegacyFadeMinSeconds
			: value.Kind switch
			{
				ExpressionValueKind.Number => value.AsNumber("Command 'fadein' duration"),
				ExpressionValueKind.String => double.TryParse(
					value.AsString("Command 'fadein' duration"),
					NumberStyles.Float,
					CultureInfo.InvariantCulture,
					out var parsed)
					? parsed
					: throw new InvalidOperationException("Command 'fadein' duration must be a finite number."),
				_ => throw new InvalidOperationException("Command 'fadein' duration must be a finite number."),
			};

		if (!double.IsFinite(duration) || duration < 0d)
		{
			throw new ArgumentOutOfRangeException(nameof(value), "Command 'fadein' duration must be finite and non-negative.");
		}

		return Math.Clamp(duration, LegacyFadeMinSeconds, LegacyFadeMaxSeconds);
	}
}
