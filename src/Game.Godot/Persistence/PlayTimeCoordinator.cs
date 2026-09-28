using Game.Application;
using Godot;

namespace Game.Godot.Persistence;

public partial class PlayTimeCoordinator : Node
{
	private GameSession? _session;
	private bool _isGameplayActive;
	private bool _hasApplicationFocus = true;
	private bool _isApplicationSuspended;
	private bool _wasTreePaused;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_wasTreePaused = GetTree().Paused;
	}

	public override void _Process(double delta)
	{
		var isTreePaused = GetTree().Paused;
		if (isTreePaused != _wasTreePaused)
		{
			_wasTreePaused = isTreePaused;
			SynchronizeRunningState();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut)
		{
			_hasApplicationFocus = false;
			SynchronizeRunningState();
			PersistUserData();
		}
		else if (what == NotificationApplicationFocusIn)
		{
			_hasApplicationFocus = true;
			SynchronizeRunningState();
		}
		else if (what == NotificationApplicationPaused)
		{
			_isApplicationSuspended = true;
			SynchronizeRunningState();
			// The process may be killed after suspension; do not defer this write.
			PersistUserData();
		}
		else if (what == NotificationApplicationResumed)
		{
			_isApplicationSuspended = false;
			SynchronizeRunningState();
		}
	}

	public override void _ExitTree()
	{
		if (_session is null)
		{
			return;
		}

		_session.PlayTimeService.Stop();
		_session = null;
	}

	public void Bind(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		_session = session;
		_isGameplayActive = false;
	}

	public void StartGameplay()
	{
		EnsureBound();
		_isGameplayActive = true;
		SynchronizeRunningState();
	}

	public void StopGameplay()
	{
		if (_session is null)
		{
			return;
		}

		_isGameplayActive = false;
		_session.PlayTimeService.Stop();
		PersistUserData();
	}

	private bool ShouldRun() =>
		_session is not null &&
		_isGameplayActive &&
		_hasApplicationFocus &&
		!_isApplicationSuspended &&
		!GetTree().Paused;

	private void SynchronizeRunningState()
	{
		if (_session is null)
		{
			return;
		}

		if (!ShouldRun())
		{
			_session.PlayTimeService.Pause();
			return;
		}

		if (_session.PlayTimeService.IsStarted)
		{
			_session.PlayTimeService.Resume();
		}
		else
		{
			_session.PlayTimeService.Start();
		}
	}

	private void PersistUserData()
	{
		if (_session is null || !Game.IsInitialized)
		{
			return;
		}

		World.Instance.Persistence.FlushNow();
	}

	private void EnsureBound()
	{
		if (_session is null)
		{
			throw new InvalidOperationException("Play-time coordinator is not bound to a game session.");
		}
	}
}
