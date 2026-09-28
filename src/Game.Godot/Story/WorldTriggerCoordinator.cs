using Game.Application;
using Game.Godot.Map;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Story;

public partial class WorldTriggerCoordinator : Node
{
	private GameSession? _session;
	private IDisposable? _saveLoadedSubscription;
	private CancellationTokenSource? _executionCancellation;

	public void Bind(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		Unbind();
		_session = session;
		_saveLoadedSubscription = session.Events.Subscribe<SaveLoadedEvent>(_ =>
		{
			_executionCancellation?.Cancel();
			session.WorldTriggerService.RequestCheck();
		});
	}

	public void Unbind()
	{
		_saveLoadedSubscription?.Dispose();
		_saveLoadedSubscription = null;
		_session = null;
		_executionCancellation?.Cancel();
	}

	public override void _ExitTree() => Unbind();

	public override void _Process(double delta)
	{
		if (_session is not { } session || _executionCancellation is not null ||
			GameFlow.IsMainMenuActive ||
			!session.WorldTriggerService.HasPendingCheck ||
			session.State.WorldTriggers.IsBlocked ||
			World.Instance.CurrentScene is not MapScreen { IsHandlingInteraction: false } ||
			UIRoot.Instance.IsStoryPresentationActive || UIRoot.Instance.IsBattleActive)
		{
			return;
		}

		_ = ExecuteAsync(session);
	}

	private async Task ExecuteAsync(GameSession session)
	{
		using var cancellation = new CancellationTokenSource();
		_executionCancellation = cancellation;
		var cancellationToken = cancellation.Token;
		var state = session.State;
		try
		{
			var completed = await session.WorldTriggerService.ExecutePendingAsync(cancellationToken);
			if (completed && !cancellationToken.IsCancellationRequested && !GameFlow.IsMainMenuActive &&
				ReferenceEquals(session, _session) && ReferenceEquals(state, session.State) &&
				World.Instance.CurrentScene is MapScreen)
			{
				World.Instance.RefreshCurrentMap();
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception exception)
		{
			Game.Logger.Error("World trigger execution failed; the event remains eligible for retry.", exception);
		}
		finally
		{
			_executionCancellation = null;
		}
	}
}
