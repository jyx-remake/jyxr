using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.Settings;

public static class UserSettingsApplier
{
	private const string BgmBusName = "Bgm";
	private const string SfxBusName = "SFX";
	private const int DefaultViewportWidth = 1920;
	private const int DefaultViewportHeight = 1080;

	public static void Apply(UserSettingsRecord settings, UserSettingsRecord? previous = null)
	{
		ArgumentNullException.ThrowIfNull(settings);

		if (Game.IsInitialized)
		{
			Game.Settings.AutoSave = settings.AutoSave;
			Game.Settings.DialogueTypewriterEnabled = settings.DialogueTypewriterEnabled;
			Game.Settings.ShowBattleBoard = settings.ShowBattleBoard;
			Game.Settings.LargeMapMovementAnimationEnabled = settings.LargeMapMovementAnimationEnabled;
		}

		if (previous?.MusicEnabled != settings.MusicEnabled) ApplyBusEnabled(BgmBusName, settings.MusicEnabled);
		if (previous?.SfxEnabled != settings.SfxEnabled) ApplyBusEnabled(SfxBusName, settings.SfxEnabled);
		if (Game.IsDesktopPlatform && previous?.WindowDisplayMode != settings.WindowDisplayMode)
		{
			ApplyWindowDisplayMode(settings.WindowDisplayMode);
		}
		if (previous?.ScreenAspectMode != settings.ScreenAspectMode) ApplyScreenAspect(settings.ScreenAspectMode);
	}

	private static void ApplyBusEnabled(string busName, bool enabled)
	{
		var busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
		{
			throw new InvalidOperationException($"音频总线不存在：{busName}");
		}

		AudioServer.SetBusMute(busIndex, !enabled);
	}

	private static void ApplyWindowDisplayMode(WindowDisplayMode mode)
	{
		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			return;
		}

		tree.Root.Mode = mode switch
		{
			WindowDisplayMode.Windowed => Window.ModeEnum.Windowed,
			WindowDisplayMode.Fullscreen => Window.ModeEnum.Fullscreen,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported window display mode."),
		};
	}

	private static void ApplyScreenAspect(ScreenAspectMode mode)
	{
		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			return;
		}

		var window = tree.Root;
		var size = ResolveContentScaleSize(mode);
		window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
		window.ContentScaleAspect = mode == ScreenAspectMode.Unlimited
			? Window.ContentScaleAspectEnum.Expand
			: Window.ContentScaleAspectEnum.Keep;
		window.ContentScaleSize = size;
	}

	private static Vector2I ResolveContentScaleSize(ScreenAspectMode mode) =>
		mode switch
		{
			ScreenAspectMode.Unlimited => new Vector2I(DefaultViewportWidth, DefaultViewportHeight),
			ScreenAspectMode.Ratio16x9 => new Vector2I(DefaultViewportWidth, DefaultViewportHeight),
			ScreenAspectMode.Ratio18x9 => new Vector2I(2160, DefaultViewportHeight),
			ScreenAspectMode.Ratio20x9 => new Vector2I(2400, DefaultViewportHeight),
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported screen aspect mode."),
		};
}
