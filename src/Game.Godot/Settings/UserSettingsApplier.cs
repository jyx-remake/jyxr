using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.Settings;

public static class UserSettingsApplier
{
	private const string BgmBusName = "Bgm";
	private const string SfxBusName = "SFX";

	public static void Apply(UserSettingsRecord settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		if (Game.IsInitialized)
		{
			Game.Settings.AutoSave = settings.AutoSave;
		}

		ApplyBusEnabled(BgmBusName, settings.MusicEnabled);
		ApplyBusEnabled(SfxBusName, settings.SfxEnabled);
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
}
