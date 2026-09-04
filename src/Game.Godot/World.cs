using Game.Application;
using Game.Godot.Assets;
using Game.Godot.Map;
using Game.Godot.Persistence;
using Game.Godot.UI;
using Godot;

namespace Game.Godot;

public partial class World : Control
{
	public static World Instance { get; private set; } = null!;
	
	[Export]
	public PackedScene MapScreenScene { get; set; } = null!;

	private TextureRect _background = null!;
	private Tween? _backdropAlphaTween;
	private TaskCompletionSource? _backdropAlphaCompletion;

	public Control? CurrentScene { get; private set; }

	/// <summary>
	/// Raised whenever the shared world backdrop changes. Both the map screen
	/// (which paints the current map picture) and the story runtime (the
	/// `background` command) write through <see cref="SetBackground"/>, so
	/// subscribers can tell a story-provided backdrop apart from the map's own.
	/// </summary>
	public event Action<string?>? BackdropChanged;

	/// <summary>
	/// Resource id of the backdrop currently shown behind every scene, or null
	/// when no backdrop is set.
	/// </summary>
	public string? CurrentBackdropId { get; private set; }

	public AutoSaveCoordinator AutoSave { get; private set; } = null!;
	public PlayTimeCoordinator PlayTime { get; private set; } = null!;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("%Background");
		AutoSave = GetNode<AutoSaveCoordinator>("%AutoSaveCoordinator");
		PlayTime = GetNode<PlayTimeCoordinator>("%PlayTimeCoordinator");
		Instance = this;
	}

	public override void _ExitTree()
	{
		CancelBackdropAlphaTween();
	}

	public MapScreen ShowMap(string mapId)
	{
		var result = Game.MapService.EnterMap(mapId);
		return ShowMap(result);
	}

	public MapScreen EnterMap(string mapId) =>
		ShowMap(Game.MapService.EnterMap(mapId));

	public MapScreen EnterMap(string mapId, string locationId) =>
		ShowMap(Game.MapService.EnterMap(mapId, locationId));

	public void ShowStoryAnimation(string animationId)
	{
		if (string.IsNullOrWhiteSpace(animationId))
		{
			throw new ArgumentException("Animation id cannot be empty.", nameof(animationId));
		}

		Game.Logger.Info($"Story animation requested: {animationId}");
	}

	public void SetBackground(string? resourceId) => SetBackground(resourceId, 1f);

	/// <summary>
	/// Replaces the shared backdrop and immediately applies <paramref name="alpha"/>.
	/// The legacy engine treats the background image's own alpha as state: the
	/// map paints it fully opaque while story backdrops carry the ambient
	/// time-of-day opacity, so every caller states the alpha it means.
	/// </summary>
	public void SetBackground(string? resourceId, float alpha)
	{
		CancelBackdropAlphaTween();
		_background.Texture = AssetResolver.LoadTexture(resourceId);
		_background.Visible = _background.Texture is not null;
		SetBackdropAlpha(_background.Texture is null ? 1f : alpha);
		CurrentBackdropId = _background.Texture is null ? null : resourceId;
		BackdropChanged?.Invoke(CurrentBackdropId);
	}

	public float BackdropAlpha => _background.Modulate.A;

	/// <summary>
	/// Tweens the shared backdrop's alpha over <paramref name="duration"/> seconds.
	/// Backs the story <c>fadein</c> command, mirroring the legacy
	/// <c>fadeinoutCoroutine</c>, which fades the background image itself instead
	/// of a full-screen overlay.
	/// </summary>
	public Task FadeBackdropAlphaAsync(float targetAlpha, double duration, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(duration);
		if (!double.IsFinite(duration))
		{
			throw new ArgumentOutOfRangeException(nameof(duration), "Backdrop fade duration must be finite.");
		}

		cancellationToken.ThrowIfCancellationRequested();
		CancelBackdropAlphaTween();
		if (duration == 0d)
		{
			SetBackdropAlpha(targetAlpha);
			return Task.CompletedTask;
		}

		var tween = CreateTween();
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_backdropAlphaTween = tween;
		_backdropAlphaCompletion = completion;

		void OnFinished()
		{
			if (ReferenceEquals(_backdropAlphaTween, tween))
			{
				_backdropAlphaTween = null;
				_backdropAlphaCompletion = null;
			}

			completion.TrySetResult();
		}

		tween.Finished += OnFinished;
		tween.TweenMethod(Callable.From<float>(SetBackdropAlpha), _background.Modulate.A, targetAlpha, duration);
		return AwaitBackdropAlphaTweenAsync(tween, completion, OnFinished, cancellationToken);
	}

	private static async Task AwaitBackdropAlphaTweenAsync(
		Tween tween,
		TaskCompletionSource completion,
		Action finishedHandler,
		CancellationToken cancellationToken)
	{
		using var registration = cancellationToken.Register(() =>
		{
			tween.Kill();
			completion.TrySetCanceled(cancellationToken);
		});

		try
		{
			await completion.Task;
		}
		finally
		{
			tween.Finished -= finishedHandler;
		}
	}

	private void CancelBackdropAlphaTween()
	{
		_backdropAlphaTween?.Kill();
		_backdropAlphaTween = null;
		_backdropAlphaCompletion?.TrySetCanceled();
		_backdropAlphaCompletion = null;
	}

	private void SetBackdropAlpha(float alpha)
	{
		var modulate = _background.Modulate;
		_background.Modulate = new Color(modulate.R, modulate.G, modulate.B, alpha);
	}

	private MapScreen ShowMap(MapEnterResult result)
	{
		// Every map (re)build funnels through here. The map screen is replaced
		// from scratch, so flush any large-map zoom change that the delayed
		// save timer has not persisted yet; otherwise the replacement view
		// restores a stale zoom (e.g. the minimum) right after a story event.
		if (CurrentScene is MapScreen currentMapScreen)
		{
			currentMapScreen.FlushLargeMapZoom();
		}

		var instance = MapScreenScene.Instantiate();
		if (instance is not MapScreen mapScreen)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Map screen scene root must be MapScreen.");
		}

		mapScreen.Initialize(result);
		ReplaceCurrentScene(mapScreen);
		return mapScreen;
	}

	public MapScreen RefreshCurrentMap() =>
		ShowMap(Game.State.Location.CurrentMapId);

	/// <summary>
	/// Removes the active gameplay map while the game is on a non-gameplay screen
	/// (for example, the main menu).  Keeping the map node alive would also keep
	/// its presentation layers, such as the scrolling cloud overlay, rendering
	/// over the menu.
	/// </summary>
	public void ClearCurrentScene()
	{
		if (CurrentScene is not null && GodotObject.IsInstanceValid(CurrentScene))
		{
			// Hide immediately; QueueFree is deferred until the idle frame and
			// should not produce a single-frame visual leak during a transition.
			CurrentScene.Hide();
			CurrentScene.QueueFree();
		}

		CurrentScene = null;
	}

	private void ReplaceCurrentScene(Control scene)
	{
		ClearCurrentScene();
		CurrentScene = scene;
		AddChild(scene);

		if (scene is MapScreen mapScreen && UIRoot.Instance is not null)
		{
			mapScreen.SetStoryPresentationActive(UIRoot.Instance.IsStoryPresentationActive);
		}
	}
}
