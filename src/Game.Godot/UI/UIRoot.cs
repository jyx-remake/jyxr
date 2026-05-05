using Game.Application;
using Game.Core.Definitions;
using Godot;
using Game.Godot.Assets;
using Game.Godot.Map;
using Game.Godot.Persistence;
using Game.Godot.UI.Battle;
using Game.Godot.UI.Story;
using System.Threading.Tasks;

namespace Game.Godot.UI;

public partial class UIRoot : Control
{
	public static UIRoot Instance { get; private set; } = null!;

	[ExportGroup("Panels")]
	[Export]
	public PackedScene PartyPanelScene { get; set; } = null!;

	[Export]
	public PackedScene CharacterPanelScene { get; set; } = null!;

	[Export]
	public PackedScene InventoryPanelScene { get; set; } = null!;

	[Export]
	public PackedScene GameLogPanelScene { get; set; } = null!;

	[Export]
	public PackedScene HeroPanelScene { get; set; } = null!;

	[Export]
	public PackedScene SystemPanelScene { get; set; } = null!;

	[Export]
	public PackedScene SaveSlotSelectionPanelScene { get; set; } = null!;

	[Export]
	public PackedScene ShopPanelScene { get; set; } = null!;

	[Export]
	public PackedScene ChestPanelScene { get; set; } = null!;

	[Export]
	public PackedScene CombatantSelectPanelScene { get; set; } = null!;

	[Export]
	public PackedScene BattleScreenScene { get; set; } = null!;

	[Export]
	public PackedScene SelectSectScreenScene { get; set; } = null!;

	[Export]
	public PackedScene InputNamePanelScene { get; set; } = null!;

	[Export]
	public PackedScene SelectHeadPanelScene { get; set; } = null!;

	[Export]
	public PackedScene RollStatsPanelScene { get; set; } = null!;

	[Export]
	public PackedScene GameOverScreenScene { get; set; } = null!;

	public CanvasLayer HudLayer { get; private set; } = null!;
	public CanvasLayer PanelLayer { get; private set; } = null!;
	public CanvasLayer ModalLayer { get; private set; } = null!;
	public CanvasLayer OverlayLayer { get; private set; } = null!;
	private HudPanel? _hud;
	private StoryDialoguePanel _storyDialoguePanel = null!;
	private StoryChoicePanel _storyChoicePanel = null!;
	private ToastPanel _toastPanel = null!;
	private HintBox _hintBox = null!;
	private ConfirmDialog _confirmDialog = null!;
	private SessionEvents? _sessionEvents;
	private readonly List<IDisposable> _sessionSubscriptions = [];
	private readonly LocalProfileStore _profileStore = new();
	private Control? _mainPanel;
	private Control? _popupPanel;
	private bool _isStoryPresentationActive;
	private bool _isHudSuppressed;
	private bool _isToastSuppressed;

	public override void _Ready()
	{
		HudLayer = GetNode<CanvasLayer>("%HudLayer");
		PanelLayer = GetNode<CanvasLayer>("%PanelLayer");
		ModalLayer = GetNode<CanvasLayer>("%ModalLayer");
		OverlayLayer = GetNode<CanvasLayer>("%OverlayLayer");
		_hud = GetNodeOrNull<HudPanel>("%Hud");
		_storyDialoguePanel = GetNode<StoryDialoguePanel>("%StoryDialoguePanel");
		_storyChoicePanel = GetNode<StoryChoicePanel>("%StoryChoicePanel");
		_toastPanel = GetNode<ToastPanel>("%ToastPanel");
		_hintBox = GetNode<HintBox>("%HintBox");
		_confirmDialog = GetNode<ConfirmDialog>("%ConfirmDialog");
		Instance = this;
	}
	
	public void ShowHud()
	{
		if (_isHudSuppressed)
		{
			return;
		}

		_hud?.Show();
	}

	public void HideHud() => _hud?.Hide();

	public void SetToastSuppressed(bool suppressed) => _isToastSuppressed = suppressed;

	public void SetHudSuppressed(bool suppressed)
	{
		_isHudSuppressed = suppressed;
		if (suppressed)
		{
			HideHud();
			return;
		}

		if (!_isStoryPresentationActive)
		{
			ShowHud();
		}
	}

	public bool IsStoryPresentationActive => _isStoryPresentationActive;

	public void SetStoryPresentationActive(bool active)
	{
		_isStoryPresentationActive = active;

		if (active)
		{
			HideHud();
		}
		else
		{
			ShowHud();
		}

		if (World.Instance.CurrentScene is MapScreen mapScreen)
		{
			mapScreen.SetStoryPresentationActive(active);
		}
	}

	public void RefreshHud()
	{
		_hud?.Refresh();
	}

	public void BindSessionEvents(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);

		if (ReferenceEquals(_sessionEvents, session.Events))
		{
			RefreshHud();
			return;
		}

		if (_sessionEvents is not null)
		{
			DisposeSessionSubscriptions();
		}

		_sessionEvents = session.Events;
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<MapChangedEvent>(OnMapChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ClockChangedEvent>(OnClockChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<AdventureStateChangedEvent>(OnAdventureStateChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ItemAcquiredEvent>(OnItemAcquired));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ToastRequestedEvent>(OnToastRequested));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<CharacterLeveledUpEvent>(OnCharacterLeveledUp));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<SaveLoadedEvent>(OnSaveLoaded));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ProfileChangedEvent>(OnProfileChanged));
		RefreshHud();
	}

	public Control ShowPartyPanel() => ShowMainPanel(PartyPanelScene, "party panel");

	public Control ShowCharacterPanel(string characterId) =>
		ShowPopupPanel(CharacterPanelScene, "character panel", panel =>
		{
			if (panel is not CharacterPanel characterPanel)
			{
				throw new InvalidOperationException("Character panel scene root must be CharacterPanel.");
			}

			characterPanel.CharacterId = characterId;
		});

	public Control ShowInventoryPanel() => ShowMainPanel(InventoryPanelScene, "inventory panel");

	public Control ShowGameLogPanel() => ShowMainPanel(GameLogPanelScene, "game log panel");

	public Control ShowHeroPanel() => ShowMainPanel(HeroPanelScene, "hero panel");

	public Control ShowSystemPanel() => ShowMainPanel(SystemPanelScene, "system panel");

	public Control ShowGameOverScreen() => ShowMainPanel(GameOverScreenScene, "game over screen");

	public Control ShowSaveSlotSelectionPanel(SaveSlotPanelMode mode) =>
		ShowPopupPanel(SaveSlotSelectionPanelScene, "save slot selection panel", panel =>
		{
			if (panel is not SaveSlotSelectionPanel saveSlotSelectionPanel)
			{
				throw new InvalidOperationException("Save slot selection panel scene root must be SaveSlotSelectionPanel.");
			}

			saveSlotSelectionPanel.Configure(mode);
		});

	public Control ShowShopPanel(string shopId) =>
		ShowPopupPanel(ShopPanelScene, "shop panel", panel =>
		{
			if (panel is not ShopPanel shopPanel)
			{
				throw new InvalidOperationException("Shop panel scene root must be ShopPanel.");
			}

			shopPanel.Configure(shopId);
		});

	public Control ShowChestPanel() =>
		ShowPopupPanel(ChestPanelScene, "chest panel", panel =>
		{
			if (panel is not ChestPanel)
			{
				throw new InvalidOperationException("Chest panel scene root must be ChestPanel.");
			}
		});

	public async Task<IReadOnlyList<string>> ShowCombatantSelectPanelAsync(string battleId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(battleId);

		if (CombatantSelectPanelScene.Instantiate() is not CombatantSelectPanel panel)
		{
			throw new InvalidOperationException("Combatant select panel scene root must be CombatantSelectPanel.");
		}

		ModalLayer.AddChild(panel);
		panel.Configure(battleId);
		return await panel.AwaitDeploymentAsync(cancellationToken);
	}

	public async Task<bool> ShowBattleScreenAsync(
		string battleId,
		IReadOnlyList<string> selectedCharacterIds,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
		ArgumentNullException.ThrowIfNull(selectedCharacterIds);

		if (BattleScreenScene.Instantiate() is not BattleScreen screen)
		{
			throw new InvalidOperationException("Battle screen scene root must be BattleScreen.");
		}

		ModalLayer.AddChild(screen);
		screen.Configure(battleId, selectedCharacterIds);
		return await screen.AwaitBattleAsync(cancellationToken);
	}

	public async Task<SectDefinition> ShowSelectSectScreenAsync(CancellationToken cancellationToken = default)
	{
		if (SelectSectScreenScene.Instantiate() is not SelectSectScreen screen)
		{
			throw new InvalidOperationException("Select sect screen scene root must be SelectSectScreen.");
		}

		ModalLayer.AddChild(screen);
		return await screen.AwaitSelectionAsync(cancellationToken);
	}

	public async Task<string> ShowInputNamePanelAsync(
		string characterId,
		string defaultName = "",
		CancellationToken cancellationToken = default)
	{
		if (InputNamePanelScene.Instantiate() is not InputNamePanel panel)
		{
			throw new InvalidOperationException("Input name panel scene root must be InputNamePanel.");
		}

		ModalLayer.AddChild(panel);
		return await panel.AwaitNameAsync(characterId, defaultName, cancellationToken);
	}

	public async Task<string> ShowSelectHeadPanelAsync(CancellationToken cancellationToken = default)
	{
		if (SelectHeadPanelScene.Instantiate() is not SelectHeadPanel panel)
		{
			throw new InvalidOperationException("Select head panel scene root must be SelectHeadPanel.");
		}

		ModalLayer.AddChild(panel);
		return await panel.AwaitHeadAsync(cancellationToken);
	}

	public async Task ShowRollStatsPanelAsync(string characterId, CancellationToken cancellationToken = default)
	{
		if (RollStatsPanelScene.Instantiate() is not RollStatsPanel panel)
		{
			throw new InvalidOperationException("Roll stats panel scene root must be RollStatsPanel.");
		}

		ModalLayer.AddChild(panel);
		await panel.AwaitRollAsync(characterId, cancellationToken);
	}

	public void CloseMainPanel()
	{
		ClosePopupPanel();

		if (_mainPanel is not null && GodotObject.IsInstanceValid(_mainPanel))
		{
			_mainPanel.QueueFree();
		}

		_mainPanel = null;
	}

	public void ClosePopupPanel()
	{
		if (_popupPanel is not null && GodotObject.IsInstanceValid(_popupPanel))
		{
			_popupPanel.QueueFree();
		}

		_popupPanel = null;
	}

	public void ClosePanel() => CloseMainPanel();

	public async Task ShowDialogueAsync(string speaker, string text, CancellationToken cancellationToken = default)
	{
		var dialog = _storyDialoguePanel;
		dialog.Configure(speaker, text);
		var version = dialog.PresentationVersion;

		try
		{
			await dialog.AwaitCompletionAsync(cancellationToken);
		}
		finally
		{
			_ = HideDialogueWhenIdleAsync(dialog, version);
		}
	}

	public async Task<int> ShowChoicesAsync(
		string? speaker,
		string prompt,
		IReadOnlyList<string> options,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(options);
		if (options.Count == 0)
		{
			throw new InvalidOperationException("Choices cannot be empty.");
		}

		_storyDialoguePanel.HidePanel();

		var dialog = _storyChoicePanel;
		dialog.Configure(speaker, prompt, options);

		try
		{
			return await dialog.AwaitSelectionAsync(cancellationToken);
		}
		finally
		{
			dialog.HidePanel();
		}
	}

	public void ShowSuggestion(string text)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);
		_ = ShowSuggestionAsync(text);
	}

	public Task ShowSuggestionAsync(string text, CancellationToken cancellationToken = default) =>
		_hintBox.ShowHintAsync(text, cancellationToken);

	public Task<bool> ShowConfirmAsync(string text, CancellationToken cancellationToken = default) =>
		_confirmDialog.ShowConfirmAsync(text, cancellationToken);

	public void ShowToast(string text)
	{
		if (_isToastSuppressed)
		{
			return;
		}

		_toastPanel.Enqueue(text);
	}

	private Control ShowMainPanel(PackedScene? scene, string description, Action<Control>? configure = null)
	{
		CloseMainPanel();
		var panel = CreatePanel(scene, description, configure);
		PanelLayer.AddChild(panel);
		_mainPanel = panel;
		panel.TreeExited += () => ClearPanelReference(panel);
		return panel;
	}

	private Control ShowPopupPanel(PackedScene? scene, string description, Action<Control>? configure = null)
	{
		ClosePopupPanel();
		var panel = CreatePanel(scene, description, configure);
		PanelLayer.AddChild(panel);
		_popupPanel = panel;
		panel.TreeExited += () => ClearPanelReference(panel);
		return panel;
	}

	private static Control CreatePanel(PackedScene? scene, string description, Action<Control>? configure = null)
	{
		if (scene is null)
		{
			throw new InvalidOperationException($"UIRoot panel scene is not assigned: {description}.");
		}

		var instance = scene.Instantiate();
		if (instance is not Control panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException($"Panel scene root must be a Control: {description}.");
		}

		try
		{
			configure?.Invoke(panel);
		}
		catch
		{
			panel.QueueFree();
			throw;
		}

		return panel;
	}

	private void ClearPanelReference(Control panel)
	{
		if (ReferenceEquals(_popupPanel, panel))
		{
			_popupPanel = null;
		}

		if (ReferenceEquals(_mainPanel, panel))
		{
			_mainPanel = null;
		}
	}

	private async Task HideDialogueWhenIdleAsync(StoryDialoguePanel dialog, int version)
	{
		await ToSignal(GetTree().CreateTimer(0.01d), SceneTreeTimer.SignalName.Timeout);

		if (!GodotObject.IsInstanceValid(dialog))
		{
			return;
		}

		if (dialog.PresentationVersion != version)
		{
			return;
		}

		dialog.HidePanel();
	}

	private void OnMapChanged(MapChangedEvent _) => RefreshHud();

	private void OnClockChanged(ClockChangedEvent _) => RefreshHud();

	private void OnCurrencyChanged(CurrencyChangedEvent _) => RefreshHud();

	private void OnAdventureStateChanged(AdventureStateChangedEvent _) => RefreshHud();

	private void OnItemAcquired(ItemAcquiredEvent sessionEvent)
	{
		var quantitySuffix = sessionEvent.Quantity > 1
			? $" x{sessionEvent.Quantity}"
			: string.Empty;
		ShowToast($"获得物品【{sessionEvent.ItemName}】{quantitySuffix}");
	}

	private void OnToastRequested(ToastRequestedEvent sessionEvent) => ShowToast(sessionEvent.Message);

	private void OnCharacterLeveledUp(CharacterLeveledUpEvent sessionEvent)
	{
		var characterName = Game.State.Party.TryGetMember(sessionEvent.CharacterId, out var character) && character is not null
			? character.Name
			: sessionEvent.CharacterId;
		ShowToast($"【{characterName}】升到{sessionEvent.NewLevel}级");
	}

	private void OnSaveLoaded(SaveLoadedEvent _) => RefreshHud();

	private void OnAchievementUnlocked(AchievementUnlockedEvent sessionEvent)
	{
		ShowToast($"获得称号【{sessionEvent.AchievementId}】");
	}

	private void OnProfileChanged(ProfileChangedEvent _) => PersistProfile();

	private void PersistProfile()
	{
		try
		{
			_profileStore.SaveCurrentProfile();
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Persisting global profile failed.", exception);
		}
	}

	private void DisposeSessionSubscriptions()
	{
		foreach (var subscription in _sessionSubscriptions)
		{
			subscription.Dispose();
		}

		_sessionSubscriptions.Clear();
	}
}
