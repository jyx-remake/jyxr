using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.Settings;

public static class UserSettingsApplier
{
	private const string BgmBusName = "Bgm";
	private const string SfxBusName = "SFX";
	private const int DefaultViewportWidth = 1920;
	private const int DefaultViewportHeight = 1080;

	public static void Apply(UserSettingsRecord settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		if (Game.IsInitialized)
		{
			Game.Settings.AutoSave = settings.AutoSave;
		}

		ApplyBusEnabled(BgmBusName, settings.MusicEnabled);
		ApplyBusEnabled(SfxBusName, settings.SfxEnabled);
		ApplyBusVolume(BgmBusName, settings.MusicVolume);
		ApplyBusVolume(SfxBusName, settings.SfxVolume);
		ApplyScreenAspect(settings.ScreenAspectMode);
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

	private static void ApplyBusVolume(string busName, int volume)
	{
		var busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
		{
			throw new InvalidOperationException($"音频总线不存在：{busName}");
		}

		AudioServer.SetBusVolumeDb(busIndex, VolumePercentToDb(volume));
	}

	private static float VolumePercentToDb(int volume)
	{
		var normalized = Math.Clamp(volume, 0, 100) / 100f;
		return normalized <= 0f ? -80f : Mathf.LinearToDb(normalized);
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
