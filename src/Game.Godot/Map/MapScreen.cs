using Game.Application;
using Game.Core.Definitions;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Map;

public partial class MapScreen : Control
{
	private MapEnterResult? _pendingInitialResult;
	private bool _isHandlingInteraction;

	[Export]
	public PackedScene MapEntityBoxScene { get; set; } = null!;

	private Control _mapBigTab = null!;
	private Control _mapSmallTab = null!;
	private Control _cameraButton = null!;
	private TextureRect _smallMapBackground = null!;
	private ColorRect _smallMapTimeDim = null!;
	private Control _smallMapLocationArea = null!;
	private GridContainer _mapEntityList = null!;
	private MapLocationTooltipLayer _locationTooltipLayer = null!;
	private Control _bottomBox = null!;
	private RichTextLabel _mapDescriptionLabel = null!;
	private MapInteractionResult? _pendingInteraction;
	private IDisposable? _clockChangedSubscription;
	private IDisposable? _adventureStateChangedSubscription;
	private bool _isStoryPresentationActive;

	public override void _Ready()
	{
		_mapBigTab = GetNode<Control>("%MapBigTab");
		_mapSmallTab = GetNode<Control>("%MapSmallTab");
		_locationTooltipLayer = GetNode<MapLocationTooltipLayer>("%TooltipHost");
		_locationTooltipLayer.LocationActivated += OnLocationPressed;
		InitializeLargeMapNodes();
		_smallMapBackground = GetNode<TextureRect>("%SmallMapBackground");
		_smallMapTimeDim = GetNode<ColorRect>("%SmallMapTimeDim");
		_cameraButton = GetNode<Control>("%CameraButton");
		_smallMapLocationArea = GetNode<Control>("%SmallMapLocationArea");
		_mapEntityList = GetNode<GridContainer>("%MapEntityList");
		_bottomBox = GetNode<Control>("%BottomBox");
		_mapDescriptionLabel = GetNode<RichTextLabel>("%MapDescriptionLabel");
		_clockChangedSubscription = Game.Session.Events.Subscribe<ClockChangedEvent>(OnClockChanged);
		_adventureStateChangedSubscription = Game.Session.Events.Subscribe<AdventureStateChangedEvent>(OnAdventureStateChanged);
		GetViewport().SizeChanged += OnViewportSizeChanged;

		if (_pendingInitialResult is not null)
		{
			Apply(_pendingInitialResult);
			SchedulePendingInteraction(_pendingInitialResult);
			_pendingInitialResult = null;
			return;
		}
	}

	public override void _ExitTree()
	{
		_locationTooltipLayer.Dismiss();
		ReleaseLargeMapNodes();
		GetViewport().SizeChanged -= OnViewportSizeChanged;
		_clockChangedSubscription?.Dispose();
		_clockChangedSubscription = null;
		_adventureStateChangedSubscription?.Dispose();
		_adventureStateChangedSubscription = null;
	}

	private void OnClockChanged(ClockChangedEvent _)
	{
		if (_mapBigTab.Visible)
		{
			ApplyLargeMapTimeLighting();
			return;
		}

		if (_mapSmallTab.Visible)
		{
			ApplySmallMapTimeLighting();
		}
	}

	private void OnAdventureStateChanged(AdventureStateChangedEvent _) => ApplyLargeMapCloudVisibility();

	public void SetStoryPresentationActive(bool active)
	{
		_isStoryPresentationActive = active;
		ApplyStoryPresentationVisibility();
	}

	public void Initialize(MapEnterResult result)
	{
		ArgumentNullException.ThrowIfNull(result);
		_pendingInitialResult = result;
	}

	public void ShowMap(string mapId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

		var result = Game.MapService.EnterMap(mapId);
		Apply(result);
		SchedulePendingInteraction(result);
	}

	private void Apply(MapEnterResult result)
	{
		_locationTooltipLayer.Dismiss();
		if (result.Map.Musics.Any())
		{
			Game.Audio.PlayBgm(result.Map.Musics);
		}
		
		_mapDescriptionLabel.Text = result.Map.Description ?? "";

		if (result.Map.Kind == MapKind.Large)
		{
			World.Instance.SetBackground(result.Map.Picture);
			_mapBigTab.Show();
			_mapSmallTab.Hide();
			FillLargeMap(result);
		}
		else
		{
			World.Instance.SetBackground(result.Map.Picture);
			_mapBigTab.Hide();
			_mapSmallTab.Show();
			FillSmallMap(result);
		}

		ApplyStoryPresentationVisibility();
	}

	private void FillSmallMap(MapEnterResult result)
	{
		SetSmallMapBackground(result.Map.Picture);
		ClearChildren(_mapEntityList);

		_mapEntityList.Columns = SmallMapLocationLayout.ResolveColumns(result.Locations.Count);
		foreach (var location in result.Locations)
		{
			_mapEntityList.AddChild(CreateEntityButton(MapEntityBoxScene, location));
		}

		CallDeferred(nameof(FitSmallMapLocations));
	}

	/// <summary>
	/// Fits every location node on screen like the legacy small map: no
	/// scrolling, the whole grid is uniformly scaled to the strip area and
	/// kept centered.
	/// </summary>
	private void FitSmallMapLocations()
	{
		if (!IsInsideTree())
		{
			return;
		}

		ApplyFit(_mapEntityList, _smallMapLocationArea.Size, GetViewportRect().Size);
	}

	internal static void ApplyFit(GridContainer grid, Vector2 areaSize, Vector2 viewportSize)
	{
		ArgumentNullException.ThrowIfNull(grid);

		var liveChildCount = 0;
		foreach (var child in grid.GetChildren())
		{
			if (!child.IsQueuedForDeletion())
			{
				liveChildCount++;
			}
		}

		if (liveChildCount == 0)
		{
			return;
		}

		var columns = Math.Max(1, grid.Columns);
		var rows = SmallMapLocationLayout.ResolveRows(liveChildCount, columns);
		var cellSize = ResolveGridCellSize(grid);
		var separation = new Vector2(
			grid.GetThemeConstant("h_separation"),
			grid.GetThemeConstant("v_separation"));
		var gridSize = SmallMapLocationLayout.ResolveGridSize(columns, rows, cellSize, separation);
		var available = new Vector2(Math.Max(1f, areaSize.X - 80f), Math.Max(1f, areaSize.Y - 48f));
		var scale = Math.Min(
			Math.Min(1f, viewportSize.X / SmallMapLocationLayout.DesignWidth),
			SmallMapLocationLayout.ResolveScale(available, gridSize));

		// Flow left to right, vertically centered: anchor left, center the
		// block on the strip middle, scale about the left-middle pivot so
		// the left edge never moves.
		grid.AnchorLeft = 0f;
		grid.AnchorTop = 0.5f;
		grid.AnchorRight = 0f;
		grid.AnchorBottom = 0.5f;
		grid.OffsetLeft = 40f;
		grid.OffsetTop = -gridSize.Y / 2f;
		grid.OffsetRight = 40f + gridSize.X;
		grid.OffsetBottom = gridSize.Y / 2f;
		grid.PivotOffset = new Vector2(0f, gridSize.Y / 2f);
		grid.Scale = new Vector2(scale, scale);
	}

	private static Vector2 ResolveGridCellSize(GridContainer grid)
	{
		var cellSize = new Vector2(256f, 256f);
		foreach (var child in grid.GetChildren())
		{
			if (child is Control control && !child.IsQueuedForDeletion())
			{
				var minimum = control.GetCombinedMinimumSize();
				cellSize.X = Math.Max(cellSize.X, minimum.X);
				cellSize.Y = Math.Max(cellSize.Y, minimum.Y);
			}
		}

		return cellSize;
	}

	private void OnViewportSizeChanged()
	{
		if (_mapSmallTab.Visible)
		{
			CallDeferred(nameof(FitSmallMapLocations));
		}
	}

	private MapEntityButton CreateEntityButton(
		PackedScene scene,
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location)
	{
		var instance = scene.Instantiate();
		if (instance is not MapEntityButton button)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Map entity scene root must be MapEntityButton.");
		}

		button.Setup(location);
		button.LocationPressed += _locationTooltipLayer.Request;
		return button;
	}

	private async void OnLocationPressed((string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location)
	{
		if (_isHandlingInteraction)
		{
			return;
		}

		_isHandlingInteraction = true;
		_locationTooltipLayer.Dismiss();

		try
		{
			await HandleLocationPressedAsync(location);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Handling map interaction failed.", exception);
			throw;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(this))
			{
				_isHandlingInteraction = false;
			}
		}
	}

	private async Task HandleLocationPressedAsync((string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location)
	{
		BeginLargeMapTimeLightingDeferral();
		MapInteractionResult result;
		try
		{
			result = Game.MapService.InteractWithLocation(location);
		}
		catch
		{
			EndLargeMapTimeLightingDeferral();
			throw;
		}

		await CompleteMapInteractionAsync(result);
	}

	private async Task CompleteMapInteractionAsync(MapInteractionResult result)
	{
		await PlayLargeMapInteractionMovementAsync(result.Movement);
		var completed = await HandleMapInteractionResultAsync(result);
		if (completed)
		{
			Game.Session.Events.Publish(
				new AutoSaveRequestedEvent(
					$"map interaction command completed: '{result.Command?.Root.Name}'"));
		}
	}

	private async Task<bool> HandleMapInteractionResultAsync(MapInteractionResult result)
	{
		if (result.Command is null)
		{
			Game.Logger.Info("Map interaction is blocked because it has no command.");
			return false;
		}

		try
		{
			await Game.StoryService.CommandDispatcher.ExecuteCallAsync(result.Command);
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			// Map actions use the command dispatcher directly (rather than the
			// story session), so apply the same compatibility policy here: tell the
			// player what was skipped and keep the map usable.
			await Game.StoryService.Host.CommandFailedAsync(
				result.Command.Root.Name,
				exception.Message,
				CancellationToken.None);
		}
		Game.MapService.CompleteInteraction(result);

		if (GodotObject.IsInstanceValid(World.Instance) && World.Instance.CurrentScene is MapScreen)
		{
			World.Instance.RefreshCurrentMap();
		}

		return true;
	}

	private void SchedulePendingInteraction(MapEnterResult result)
	{
		if (result.PendingInteraction is null || _isHandlingInteraction)
		{
			return;
		}

		_pendingInteraction = result.PendingInteraction;
		_isHandlingInteraction = true;
		CallDeferred(nameof(ProcessPendingInteractionDeferred));
	}

	private async void ProcessPendingInteractionDeferred()
	{
		try
		{
			if (_pendingInteraction is { } pendingInteraction)
			{
				_pendingInteraction = null;
				await CompleteMapInteractionAsync(pendingInteraction);
			}
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Handling map enter interaction failed.", exception);
			throw;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(this))
			{
				_isHandlingInteraction = false;
			}
		}
	}

	private void SetSmallMapBackground(string? resourceId)
	{
		var texture = AssetResolver.LoadTexture(resourceId);
		_smallMapBackground.Texture = texture;
		_smallMapBackground.Visible = texture is not null && !_isStoryPresentationActive;

		if (texture is null)
		{
			_smallMapTimeDim.Hide();
			return;
		}

		ApplySmallMapTimeLighting();
	}

	private void ApplySmallMapTimeLighting()
	{
		if (_isStoryPresentationActive || !_smallMapBackground.Visible || _smallMapBackground.Texture is null)
		{
			_smallMapTimeDim.Hide();
			return;
		}

		var dimAlpha = MapTimeLighting.GetDimAlpha(Game.State.Clock.TimeSlot);
		_smallMapTimeDim.Color = new Color(0f, 0f, 0f, dimAlpha);
		_smallMapTimeDim.Visible = dimAlpha > 0f;
	}

	private static void ClearChildren(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void ApplyStoryPresentationVisibility()
	{
		_locationTooltipLayer.Dismiss();
		if (_isStoryPresentationActive)
		{
			if (_mapBigTab.Visible)
			{
				// Keep the large map on screen while a story plays. Large-map
				// events are triggered from the map itself, so the dialogue is
				// expected to run against the zoom/pan the player is using.
				// Hiding it would reveal World.Background, which paints the same
				// map picture stretched to fill the screen - visually identical
				// to snapping back to the minimum zoom. Only a story-provided
				// backdrop (`background` command) takes the screen over.
				if (HasForeignStoryBackdrop)
				{
					_largeMapView.Hide();
				}
				else
				{
					_largeMapView.Show();
				}

				_largeMapView.ResetInputState();
				_largeMapView.SetInteractionEnabled(false);
			}

			if (_mapSmallTab.Visible)
			{
				_smallMapBackground.Hide();
				_smallMapTimeDim.Hide();
			}

			_smallMapLocationArea.Hide();
			_bottomBox.Hide();
			_cameraButton.Hide();
			ApplyLargeMapSafeAreaForStoryPresentation();
			return;
		}

		_largeMapView.SetInteractionEnabled(true);

		if (_mapBigTab.Visible)
		{
			_largeMapView.Show();
			_smallMapLocationArea.Hide();
			_bottomBox.Hide();
			_cameraButton.Hide();
			ApplyLargeMapSafeAreaForStoryPresentation();
			return;
		}

		_largeMapView.Hide();
		_largeMapView.ResetInputState();
		_smallMapBackground.Visible = _smallMapBackground.Texture is not null;
		ApplySmallMapTimeLighting();
		_smallMapLocationArea.Show();
		_bottomBox.Show();
		//_cameraButton.Show();
		ApplyLargeMapSafeAreaForStoryPresentation();
	}

	private bool _isLargeMapSafeAreaExpanded;
	private float _safeAreaSavedTop;
	private float _safeAreaSavedBottom;

	/// <summary>
	/// While a story plays with the HUD hidden, the map bands outside the safe
	/// area are exposed - and the World backdrop showing there is a stretched
	/// copy of the map picture that does not line up with the safe area's
	/// cover-fit art (穿帮). Expand the safe area to the full screen for the
	/// duration of the story so the map art stays continuous, then restore the
	/// player's safe area afterwards.
	/// </summary>
	private void ApplyLargeMapSafeAreaForStoryPresentation()
	{
		if (_largeMapSafeArea is null || !GodotObject.IsInstanceValid(_largeMapSafeArea))
		{
			return;
		}

		var expand = _isStoryPresentationActive && _mapBigTab.Visible && !HasForeignStoryBackdrop;
		if (expand && !_isLargeMapSafeAreaExpanded)
		{
			_safeAreaSavedTop = _largeMapSafeArea.OffsetTop;
			_safeAreaSavedBottom = _largeMapSafeArea.OffsetBottom;
			_largeMapSafeArea.OffsetTop = 0;
			_largeMapSafeArea.OffsetBottom = 0;
			_isLargeMapSafeAreaExpanded = true;
		}
		else if (!expand && _isLargeMapSafeAreaExpanded)
		{
			_largeMapSafeArea.OffsetTop = _safeAreaSavedTop;
			_largeMapSafeArea.OffsetBottom = _safeAreaSavedBottom;
			_isLargeMapSafeAreaExpanded = false;
		}
	}
}
