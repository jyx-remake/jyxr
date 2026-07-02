using Game.Application;
using Godot;

namespace Game.Godot.Persistence;

public partial class AutoSaveCoordinator : Node
{
	private readonly LocalSaveStore _saveStore = new();
	private IDisposable? _autoSaveRequestedSubscription;
	private GameSession? _session;

	public void Bind(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		Unbind();
		_session = session;
		_autoSaveRequestedSubscription = session.Events.Subscribe<AutoSaveRequestedEvent>(OnAutoSaveRequested);
	}

	public void Unbind()
	{
		_autoSaveRequestedSubscription?.Dispose();
		_autoSaveRequestedSubscription = null;
		_session = null;
	}

	public override void _ExitTree()
	{
		Unbind();
	}

	private void OnAutoSaveRequested(AutoSaveRequestedEvent sessionEvent) =>
		SaveIfEnabled(sessionEvent.Reason);

	private bool SaveIfEnabled(string reason)
	{
		if (_session is null || !Game.Settings.AutoSave)
		{
			return false;
		}

		try
		{
			_saveStore.SaveCurrentSessionToAutoSave();
			Game.Logger.Info($"Auto save completed: {reason}.");
			return true;
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Auto save failed: {reason}.", exception);
			return false;
		}
	}
}
