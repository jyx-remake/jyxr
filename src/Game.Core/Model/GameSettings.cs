namespace Game.Core.Model;

public sealed class GameSettings
{
    public bool AutoSave { get; set; } = true;
    public int TimeScale { get; set; } = 1;
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 0.8f;
}
