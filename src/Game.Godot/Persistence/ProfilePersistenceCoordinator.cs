using System.Text.Json;
using Game.Application;
using Game.Core.Persistence;
using Game.Core.Serialization;
using Game.Godot.Settings;
using Godot;

namespace Game.Godot.Persistence;

public partial class ProfilePersistenceCoordinator : Node
{
    private GameSession? _session;
    private UserSettingsService? _settings;
    private LocalProfileStore? _store;
    private string? _savedSnapshot;
    private long _lastCheckpoint;

    public override void _Ready() => ProcessMode = ProcessModeEnum.Always;

    public void Bind(GameSession session, LocalProfileStore store, UserSettingsService settings)
    {
        _session = session;
        _settings = settings;
        _store = store;
        _savedSnapshot = Snapshot();
        _lastCheckpoint = TimeProvider.System.GetTimestamp();
    }

    public void FlushNow()
    {
        TryPersist(() =>
        {
            if (_session is null) return;
            _session.PlayTimeService.Checkpoint();
            var snapshot = Snapshot();
            if (snapshot == _savedSnapshot) return;
            _store!.Save(snapshot);
            _savedSnapshot = snapshot;
        });
        TryPersist(() => _settings?.Flush());
    }

    public override void _Process(double delta)
    {
        if (_session is not null &&
            TimeProvider.System.GetElapsedTime(_lastCheckpoint) >= TimeSpan.FromSeconds(60))
        {
            _lastCheckpoint = TimeProvider.System.GetTimestamp();
            FlushNow();
        }
        TryPersist(() => _settings?.FlushIfDue());
    }

    public override void _ExitTree()
    {
        _session?.PlayTimeService.Stop();
        FlushNow();
    }

    private string Snapshot() => JsonSerializer.Serialize(GameProfileRecord.Create(_session!.Profile), GameJson.Default);

    private static void TryPersist(Action action)
    {
        try { action(); }
        catch (Exception exception) { Game.Logger.Error("Persisting user data failed; pending changes retained.", exception); }
    }
}
