using Game.Application;
using Game.Godot.Map;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Story;

public partial class WorldEventCoordinator : Node
{
	private readonly List<IDisposable> _subscriptions = [];
	private GameSession? _session;
	private CancellationTokenSource? _executionCancellation;
	private bool _checkTimedStories;

	public void Bind(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		Unbind();
		_session = session;
		_subscriptions.Add(session.Events.Subscribe<ClockChangedEvent>(_ => _checkTimedStories = true));
		_subscriptions.Add(session.Events.Subscribe<SaveLoadedEvent>(_ =>
		{
			_executionCancellation?.Cancel();
			_checkTimedStories = true;
			session.WorldTriggerService.RequestCheck();
		}));
		_checkTimedStories = true;
	}

	public void Unbind()
	{
		foreach (var subscription in _subscriptions) subscription.Dispose();
		_subscriptions.Clear();
		_session = null;
		_executionCancellation?.Cancel();
		_checkTimedStories = false;
	}

	public override void _ExitTree() => Unbind();

	public override void _Process(double delta)
	{
		if (_session is not { } session || _executionCancellation is not null ||
			!session.Execution.CanSave || GameFlow.IsMainMenuActive ||
			World.Instance.CurrentScene is not MapScreen { IsHandlingInteraction: false } ||
			UIRoot.Instance.IsStoryPresentationActive || UIRoot.Instance.IsBattleActive)
		{
			return;
		}

		if (_checkTimedStories)
		{
			var expired = session.StoryTimeKeyExpirationService.CheckExpired();
			_checkTimedStories = expired.Count > 0;
			if (_checkTimedStories)
			{
				_ = ExecuteAsync(session, expired[0]);
				return;
			}
		}

		if (session.WorldTriggerService.HasPendingCheck && !session.State.WorldTriggers.IsBlocked)
		{
			_ = ExecuteAsync(session, timedStory: null);
		}
	}

	private async Task ExecuteAsync(GameSession session, ExpiredStoryTimeKey? timedStory)
	{
		var state = session.State;
		using var cancellation = new CancellationTokenSource();
		_executionCancellation = cancellation;
		try
		{
			if (timedStory is null)
			{
				await session.WorldTriggerService.ExecutePendingAsync(cancellation.Token);
			}
			else
			{
				UIRoot.Instance.ClosePanel();
				UIRoot.Instance.ClosePopupPanel();
				UIRoot.Instance.SetStoryPresentationActive(true);
				await session.StoryTimeKeyExpirationService.ExecuteAsync(timedStory, cancellation.Token);
			}
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested || !ReferenceEquals(state, session.State))
		{
		}
		catch (Exception exception)
		{
			Game.Logger.Error("World event execution failed.", exception);
			if (timedStory is not null)
			{
				_checkTimedStories = false;
				UIRoot.Instance.ShowSuggestion(exception.Message);
			}
		}
		finally
		{
			_executionCancellation = null;
			if (timedStory is not null && ReferenceEquals(session, Game.Session) &&
				ReferenceEquals(state, session.State) && GodotObject.IsInstanceValid(UIRoot.Instance))
			{
				UIRoot.Instance.SetStoryPresentationActive(false);
			}
		}
	}
}
