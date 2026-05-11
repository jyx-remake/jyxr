using Game.Application;
using Game.Godot.Map;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Story;

public partial class TimedStoryCoordinator : Node
{
	private readonly Queue<string> _pendingStories = [];
	private readonly List<IDisposable> _subscriptions = [];
	private GameSession? _session;
	private bool _isRunning;

	public void Bind(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);

		if (ReferenceEquals(_session, session))
		{
			EnqueueExpiredStories();
			return;
		}

		Unbind();
		_session = session;
		_subscriptions.Add(session.Events.Subscribe<ClockChangedEvent>(OnClockChanged));
		_subscriptions.Add(session.Events.Subscribe<SaveLoadedEvent>(OnSaveLoaded));
		EnqueueExpiredStories();
	}

	public void Unbind()
	{
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
		_session = null;
		_pendingStories.Clear();
		_isRunning = false;
	}

	public override void _ExitTree()
	{
		Unbind();
	}

	public void StartDraining()
	{
		_ = DrainAsync();
	}

	private void OnClockChanged(ClockChangedEvent _) => EnqueueExpiredStories();

	private void OnSaveLoaded(SaveLoadedEvent _) => EnqueueExpiredStories();

	private void EnqueueExpiredStories()
	{
		if (_session is null)
		{
			return;
		}

		foreach (var expired in _session.StoryTimeKeyExpirationService.CheckExpired())
		{
			_pendingStories.Enqueue(expired.TargetStoryId);
		}

		if (_pendingStories.Count > 0)
		{
			CallDeferred(nameof(StartDraining));
		}
	}

	private async Task DrainAsync()
	{
		if (_isRunning)
		{
			return;
		}

		_isRunning = true;
		try
		{
			while (_pendingStories.Count > 0)
			{
				await WaitForExternalStoryAsync();
				var storyId = _pendingStories.Dequeue();
				await RunStoryAsync(storyId);
			}
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Timed story execution failed.", exception);
			if (GodotObject.IsInstanceValid(UIRoot.Instance))
			{
				UIRoot.Instance.ShowSuggestion(exception.Message);
			}
		}
		finally
		{
			_isRunning = false;
		}
	}

	private async Task WaitForExternalStoryAsync()
	{
		while (GodotObject.IsInstanceValid(UIRoot.Instance) && UIRoot.Instance.IsStoryPresentationActive)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task RunStoryAsync(string storyId)
	{
		var world = World.Instance;
		UIRoot.Instance.ClosePanel();
		UIRoot.Instance.ClosePopupPanel();
		UIRoot.Instance.SetStoryPresentationActive(true);

		try
		{
			await Game.StoryService.ExecuteAsync(storyId);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(UIRoot.Instance))
			{
				UIRoot.Instance.SetStoryPresentationActive(false);
			}
		}

		if (!GodotObject.IsInstanceValid(world))
		{
			return;
		}

		if (world.CurrentScene is MapScreen)
		{
			world.RefreshCurrentMap();
		}
	}
}
