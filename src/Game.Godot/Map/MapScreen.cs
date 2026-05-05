using Game.Application;
using Game.Core.Definitions;
using Game.Godot.Assets;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Map;

public partial class MapScreen : Control
{
	private const float LargeMapXScale = 2.4f;
	private const float LargeMapYScale = 1.8f;
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
	private Control _cloud = null!;
	private Control _mapEntitySlots = null!;
	private Control _cameraButton = null!;
	private HBoxContainer _mapEntityList = null!;
	private Control _bottomBox = null!;
	private RichTextLabel _mapDescriptionLabel = null!;
	private Control _mapPin = null!;
	private TextureRect _pinAvatar = null!;
	private MapInteractionResult? _pendingInteraction;
	private bool _isStoryPresentationActive;

	public override void _Ready()
	{
		_mapBigTab = GetNode<Control>("%MapBigTab");
		_mapSmallTab = GetNode<Control>("%MapSmallTab");
		_cloud = GetNode<Control>("%Cloud");
		_mapEntitySlots = GetNode<Control>("%MapEntitySlots");
		_cameraButton = GetNode<Control>("%CameraButton");
		_mapEntityList = GetNode<HBoxContainer>("%MapEntityList");
		_bottomBox = GetNode<Control>("%BottomBox");
		_mapDescriptionLabel = GetNode<RichTextLabel>("%MapDescriptionLabel");
		_mapPin = GetNode<Control>("%MapPin");
		_pinAvatar = GetNode<TextureRect>("%PinAvatar");

		if (_pendingInitialResult is not null)
		{
			Apply(_pendingInitialResult);
			SchedulePendingInteraction(_pendingInitialResult);
			_pendingInitialResult = null;
			return;
		}

		ShowMap(InitialMapId);
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

	public void ShowMap(string mapId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

		var result = Game.MapService.EnterMap(mapId);
		Apply(result);
		SchedulePendingInteraction(result);
	}

	private void Apply(MapEnterResult result)
	{
		World.Instance.SetBackground(result.Map.Picture);
		if (result.Map.Musics.Any())
		{
			Game.Audio.PlayBgm(result.Map.Musics);
		}
		
		_mapDescriptionLabel.Text = result.Map.Description ?? "";

		if (result.Map.Kind == MapKind.Large)
		{
			_mapBigTab.Show();
			_mapSmallTab.Hide();
			FillLargeMap(result);
		}
		else
		{
			_mapBigTab.Hide();
			_mapSmallTab.Show();
			FillSmallMap(result);
		}

		ApplyStoryPresentationVisibility();
	}

	private void FillLargeMap(MapEnterResult result)
	{
		ClearChildren(_mapEntitySlots);

		foreach (var location in result.Locations)
		{
			var button = CreateEntityButton(MapEntitySlotScene, location);
			if (location.Location.Position is { } position)
			{
				button.Position = new Vector2(position.X * LargeMapXScale, position.Y * LargeMapYScale);
			}

			_mapEntitySlots.AddChild(button);
		}

		if (result.HeroPosition is { } heroPosition)
		{
			_mapPin.Position = new Vector2(heroPosition.X * LargeMapXScale, heroPosition.Y * LargeMapYScale);
		}
	}

	private void FillSmallMap(MapEnterResult result)
	{
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
		var result = Game.MapService.InteractWithLocation(location);
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
				await OpenBattleAsync(result.TargetId);
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

	private static async Task OpenBattleAsync(string? battleId)
	{
		if (string.IsNullOrWhiteSpace(battleId))
		{
			throw new InvalidOperationException("Map battle event is missing target battle id.");
		}

		var selected = await UIRoot.Instance.ShowCombatantSelectPanelAsync(battleId);
		var isWin = await UIRoot.Instance.ShowBattleScreenAsync(battleId, selected);
		UIRoot.Instance.ShowToast(isWin ? "战斗胜利" : "战斗失败");
	}

	private async Task RunStoryAsync(string? storyId)
	{
		if (string.IsNullOrWhiteSpace(storyId))
		{
			throw new InvalidOperationException("Map story event is missing target story id.");
		}

		var world = GetNode<World>("/root/World");
		UIRoot.Instance.SetStoryPresentationActive(true);

		try
		{
			await foreach (var _ in Game.StoryService.RunAsync(storyId))
			{
			}
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
		_mapEntityList.Show();
		_bottomBox.Show();
		_cameraButton.Show();
	}
}
