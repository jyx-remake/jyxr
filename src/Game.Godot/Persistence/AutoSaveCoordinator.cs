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

	private void OnAutoSaveRequested(AutoSaveRequestedEvent request)
	{
		if (_session is null ||
			!_session.Execution.IsCurrentCheckpoint(request) ||
			(!Game.Settings.AutoSave && !_session.State.Adventure.NoRegret))
		{
			return;
		}

		try
		{
			_saveStore.SaveAutoSave(request);
			Game.Logger.Info($"Auto save completed: {request.Reason}.");
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Auto save failed: {request.Reason}.", exception);
		}
	}
}
