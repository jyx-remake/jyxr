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
	private HBoxContainer _mapEntityList = null!;
	private ScrollContainer _smallMapLocationScroll = null!;
	private MapLocationTooltipLayer _locationTooltipLayer = null!;
	private Control _bottomBox = null!;
	private RichTextLabel _mapDescriptionLabel = null!;
	private IDisposable? _clockChangedSubscription;
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
		_smallMapLocationScroll = GetNode<ScrollContainer>("%SmallMapLocationScroll");
		_mapEntityList = GetNode<HBoxContainer>("%MapEntityList");
		_bottomBox = GetNode<Control>("%BottomBox");
		_mapDescriptionLabel = GetNode<RichTextLabel>("%MapDescriptionLabel");
		_clockChangedSubscription = Game.Session.Events.Subscribe<ClockChangedEvent>(OnClockChanged);
		_smallMapLocationScroll.ScrollStarted += _locationTooltipLayer.Dismiss;

		if (_pendingInitialResult is not null)
		{
			Game.Audio.PlayBgm(_pendingInitialResult.Map.Musics);
			Apply(_pendingInitialResult);
			_pendingInitialResult = null;
		}
	}

	public override void _ExitTree()
	{
		_locationTooltipLayer.Dismiss();
		_clockChangedSubscription?.Dispose();
		_clockChangedSubscription = null;
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

	public bool IsHandlingInteraction => _isHandlingInteraction;

	public void Refresh(MapEnterResult result) => Apply(result);

	private void Apply(MapEnterResult result)
	{
		_locationTooltipLayer.Dismiss();
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

		foreach (var location in result.Locations)
		{
			_mapEntityList.AddChild(CreateEntityButton(MapEntityBoxScene, location));
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
		var session = Game.Session;
		BeginLargeMapTimeLightingDeferral();
		MapInteractionResult result;
		try
		{
			result = session.MapService.InteractWithLocation(location);
		}
		catch
		{
			EndLargeMapTimeLightingDeferral();
			throw;
		}

		await PlayLargeMapInteractionMovementAsync(result.Movement);
		if (!ReferenceEquals(session, Game.Session))
		{
			return;
		}

		if (await session.MapService.ExecuteInteractionAsync(result) &&
			ReferenceEquals(session, Game.Session) && !GameFlow.IsMainMenuActive &&
			GodotObject.IsInstanceValid(World.Instance) && World.Instance.CurrentScene is MapScreen)
		{
			World.Instance.RefreshCurrentMap();
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
				_largeMapView.Hide();
				_largeMapView.ResetInputState();
			}

			if (_mapSmallTab.Visible)
			{
				_smallMapBackground.Hide();
				_smallMapTimeDim.Hide();
			}

			_smallMapLocationScroll.Hide();
			_bottomBox.Hide();
			_cameraButton.Hide();
			return;
		}

		if (_mapBigTab.Visible)
		{
			_largeMapView.Show();
			_smallMapLocationScroll.Hide();
			_bottomBox.Hide();
			_cameraButton.Hide();
			return;
		}

		_largeMapView.Hide();
		_largeMapView.ResetInputState();
		_smallMapBackground.Visible = _smallMapBackground.Texture is not null;
		ApplySmallMapTimeLighting();
		_smallMapLocationScroll.Show();
		_bottomBox.Show();
		//_cameraButton.Show();
	}
}
