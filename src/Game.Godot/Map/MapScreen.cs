using Game.Application;
using Game.Core.Definitions;
using Game.Godot.Assets;
using Game.Godot.Persistence;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Map;

public partial class MapScreen : Control
{
	private readonly LocalSaveStore _saveStore = new();
	private MapEnterResult? _pendingInitialResult;
	private bool _isHandlingInteraction;

	[Export]
	public string InitialMapId { get; set; } = string.Empty;

	[Export]
	public PackedScene MapEntitySlotScene { get; set; } = null!;

	[Export]
	public PackedScene MapEntityBoxScene { get; set; } = null!;

	private Control _mapBigTab = null!;
	private Control _mapSmallTab = null!;
	private Control _cameraButton = null!;
	private TextureRect _smallMapBackground = null!;
	private ColorRect _smallMapTimeDim = null!;
	private HBoxContainer _mapEntityList = null!;
	private Control _bottomBox = null!;
	private RichTextLabel _mapDescriptionLabel = null!;
	private TextureRect _pinAvatar = null!;
	private MapInteractionResult? _pendingInteraction;
	private IDisposable? _clockChangedSubscription;
	private bool _isStoryPresentationActive;

	public override void _Ready()
	{
		_mapBigTab = GetNode<Control>("%MapBigTab");
		_mapSmallTab = GetNode<Control>("%MapSmallTab");
		InitializeLargeMapNodes();
		_smallMapBackground = GetNode<TextureRect>("%SmallMapBackground");
		_smallMapTimeDim = GetNode<ColorRect>("%SmallMapTimeDim");
		_cameraButton = GetNode<Control>("%CameraButton");
		_mapEntityList = GetNode<HBoxContainer>("%MapEntityList");
		_bottomBox = GetNode<Control>("%BottomBox");
		_mapDescriptionLabel = GetNode<RichTextLabel>("%MapDescriptionLabel");
		_pinAvatar = GetNode<TextureRect>("%PinAvatar");
		_clockChangedSubscription = Game.Session.Events.Subscribe<ClockChangedEvent>(OnClockChanged);

		if (_pendingInitialResult is not null)
		{
			Apply(_pendingInitialResult);
			SchedulePendingInteraction(_pendingInitialResult);
			_pendingInitialResult = null;
			return;
		}

		ShowMap(InitialMapId);
	}

	public override void _ExitTree()
	{
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

	private void AutoSaveIfEnabled()
	{
		if (!Game.Settings.AutoSave)
		{
			return;
		}

		try
		{
			_saveStore.SaveCurrentSessionToAutoSave();
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Auto save failed.", exception);
		}
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
		AutoSaveIfEnabled();
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
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
	{
		var instance = scene.Instantiate();
		if (instance is not MapEntityButton button)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Map entity scene root must be MapEntityButton.");
		}

		button.Setup(location);
		button.LocationPressed += OnLocationPressed;
		return button;
	}

	private async void OnLocationPressed((string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
	{
		if (_isHandlingInteraction)
		{
			return;
		}

		_isHandlingInteraction = true;

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

	private async Task HandleLocationPressedAsync((string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
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

		await PlayLargeMapInteractionMovementAsync(result.Movement);
		await HandleMapInteractionResultAsync(result);
	}

	private async Task HandleMapInteractionResultAsync(MapInteractionResult result)
	{
		if (result.EnterResult is not null)
		{
			Apply(result.EnterResult);
			if (result.EnterResult.PendingInteraction is not null)
			{
				await HandleMapInteractionResultAsync(result.EnterResult.PendingInteraction);
			}

			return;
		}

		switch (result.Outcome)
		{
			case MapService.MapInteractionOutcome.StoryRequested:
				await RunStoryAsync(result.TargetId);
				return;
			case MapService.MapInteractionOutcome.ShopRequested:
				OpenShop(result.TargetId);
				return;
			case MapService.MapInteractionOutcome.ChestRequested:
				OpenChest();
				return;
			case MapService.MapInteractionOutcome.BattleRequested:
				var isWin = await OpenBattleAsync(result.TargetId);
				if (isWin && GodotObject.IsInstanceValid(this))
				{
					World.Instance.RefreshCurrentMap();
				}

				return;
			case MapService.MapInteractionOutcome.PlaceholderInteraction:
			case MapService.MapInteractionOutcome.Blocked:
				Game.Logger.Info($"Map event requested: {result.Outcome}, target={result.TargetId}");
				return;
			default:
				throw new InvalidOperationException($"Unsupported map interaction outcome '{result.Outcome}'.");
		}
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
				await HandleMapInteractionResultAsync(pendingInteraction);
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

	private static void OpenShop(string? shopId)
	{
		if (string.IsNullOrWhiteSpace(shopId))
		{
			throw new InvalidOperationException("Map shop event is missing target shop id.");
		}

		UIRoot.Instance.ShowShopPanel(shopId);
	}

	private static void OpenChest()
	{
		UIRoot.Instance.ShowChestPanel();
	}

	private static async Task<bool> OpenBattleAsync(string? battleId)
	{
		if (string.IsNullOrWhiteSpace(battleId))
		{
			throw new InvalidOperationException("Map battle event is missing target battle id.");
		}

		var selected = await UIRoot.Instance.ShowCombatantSelectPanelAsync(battleId);
		var isWin = await UIRoot.Instance.ShowBattleScreenAsync(battleId, selected);
		if (!isWin)
		{
			GameFlow.GameOver();
			return false;
		}

		return true;
	}

	private void SetSmallMapBackground(string? resourceId)
	{
		var texture = AssetResolver.LoadTextureResource(resourceId);
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

	private async Task RunStoryAsync(string? storyId)
	{
		if (string.IsNullOrWhiteSpace(storyId))
		{
			throw new InvalidOperationException("Map story event is missing target story id.");
		}

		var world = World.Instance;
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

		if (!GodotObject.IsInstanceValid(world) || !GodotObject.IsInstanceValid(this))
		{
			return;
		}

		if (world.CurrentScene == this)
		{
			world.RefreshCurrentMap();
		}
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
		if (_isStoryPresentationActive)
		{
			if (_mapBigTab.Visible)
			{
				_largeMapViewportContainer.Hide();
			}

			if (_mapSmallTab.Visible)
			{
				_smallMapBackground.Hide();
				_smallMapTimeDim.Hide();
			}

			_cloud.Hide();
			_mapEntitySlots.Hide();
			_mapPin.Hide();
			_mapEntityList.Hide();
			_bottomBox.Hide();
			_cameraButton.Hide();
			return;
		}

		if (_mapBigTab.Visible)
		{
			_largeMapViewportContainer.Show();
			_cloud.Show();
			_mapEntitySlots.Show();
			_mapPin.Show();
			_mapEntityList.Hide();
			_bottomBox.Hide();
			_cameraButton.Hide();
			return;
		}

		_cloud.Hide();
		_mapEntitySlots.Hide();
		_mapPin.Hide();
		_smallMapBackground.Visible = _smallMapBackground.Texture is not null;
		ApplySmallMapTimeLighting();
		_mapEntityList.Show();
		_bottomBox.Show();
		//_cameraButton.Show();
	}
}
