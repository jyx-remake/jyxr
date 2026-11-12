using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Core.Story;
using Godot;
using Game.Godot.Persistence;
using Game.Godot.UI.Battle;
using Game.Godot.UI.Story;

namespace Game.Godot.UI;

public partial class UIRoot : Control
{
	public static UIRoot Instance { get; private set; } = null!;

	[ExportGroup("Panels")]
	[Export]
	public PackedScene PartyPanelScene { get; set; } = null!;

	[Export]
	public PackedScene CharacterRosterPanelScene { get; set; } = null!;

	[Export]
	public PackedScene InventoryPanelScene { get; set; } = null!;

	[Export]
	public PackedScene GameLogPanelScene { get; set; } = null!;

	[Export]
	public PackedScene HeroPanelScene { get; set; } = null!;

	[Export]
	public PackedScene SystemPanelScene { get; set; } = null!;

	[Export]
	public PackedScene ConsolePanelScene { get; set; } = null!;

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
	public PackedScene RefinementEquipmentSelectionPanelScene { get; set; } = null!;

	[Export]
	public PackedScene LightnessTrainingScreenScene { get; set; } = null!;

	[Export]
	public PackedScene StrengthTrainingScreenScene { get; set; } = null!;

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

	[Export]
	public PackedScene GameFinScreenScene { get; set; } = null!;

	public CanvasLayer HudLayer { get; private set; } = null!;
	public CanvasLayer PanelLayer { get; private set; } = null!;
	public CanvasLayer BattleLayer { get; private set; } = null!;
	public CanvasLayer ModalLayer { get; private set; } = null!;
	public CanvasLayer OverlayLayer { get; private set; } = null!;
	public StoryVisualEffects VisualEffects { get; private set; } = null!;
	public bool IsBattleActive => _battles.HasPanels;
	private HudPanel? _hud;
	private ToastPanel _toastPanel = null!;
	private HintBox _hintBox = null!;
	private ConfirmDialog _confirmDialog = null!;
	private DetailPanelHost _detailPanelHost = null!;
	private bool _isStoryPresentationActive;
	private bool _isHudSuppressed;
	private bool _isToastSuppressed;
	private PanelHost _panels = null!;
	private PanelHost _modals = null!;
	private PanelHost _battles = null!;
	private PanelHost _prompts = null!;
	private PanelBackInputRouter _backInput = null!;
	private StoryPresentationCoordinator _story = null!;
	private SessionPresentationCoordinator _sessionPresentation = null!;

	public override void _Ready()
	{
		HudLayer = GetNode<CanvasLayer>("%HudLayer");
		PanelLayer = GetNode<CanvasLayer>("%PanelLayer");
		BattleLayer = GetNode<CanvasLayer>("%BattleLayer");
		ModalLayer = GetNode<CanvasLayer>("%ModalLayer");
		OverlayLayer = GetNode<CanvasLayer>("%OverlayLayer");
		VisualEffects = GetNode<StoryVisualEffects>("%StoryVisualEffects");
		_hud = GetNodeOrNull<HudPanel>("%Hud");
		_toastPanel = GetNode<ToastPanel>("%ToastPanel");
		_hintBox = GetNode<HintBox>("%HintBox");
		_confirmDialog = GetNode<ConfirmDialog>("%ConfirmDialog");
		_detailPanelHost = GetNode<DetailPanelHost>("%DetailPanelHost");
		_panels = new PanelHost(PanelLayer);
		_modals = new PanelHost(ModalLayer);
		_battles = new PanelHost(BattleLayer);
		_prompts = new PanelHost(GetNode<CanvasLayer>("%PromptLayer"));
		_backInput = new PanelBackInputRouter(GetBackTarget);
		_story = new StoryPresentationCoordinator(
			GetNode<StoryDialoguePanel>("%StoryDialoguePanel"),
			GetNode<StoryChoicePanel>("%StoryRegularChoicePanel"),
			GetNode<StoryChoicePanel>("%StoryBoldChoicePanel"),
			GetNode<StoryIntertitlePanel>("%StoryIntertitlePanel"),
			GetNode<StoryVideoPlayer>("%StoryVideoPlayer"),
			VisualEffects);
		_sessionPresentation = new SessionPresentationCoordinator(this, _detailPanelHost);
		Instance = this;
	}

	public override void _Input(InputEvent @event)
	{
		if (_backInput.HandleInput(@event))
		{
			GetViewport().SetInputAsHandled();
		}
	}

	public void ConsumeRightClick() => _backInput.ConsumeRightClick();

	private Control? GetBackTarget() =>
		_prompts.BackTarget ?? _modals.BackTarget ??
		(IsBattleActive ? _battles.BackTarget : _panels.BackTarget);

	public void ShowModalPanel(Control panel) => _modals.Add(panel);

	internal void ResetTransientPanels()
	{
		_modals.CloseAll();
		_battles.CloseAll();
		_confirmDialog.ResetImmediate();
		_hintBox.ResetImmediate();
		_backInput.Reset();
	}

	public override void _ExitTree()
	{
		_sessionPresentation.Dispose();
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

	public void SetToastSuppressed(bool suppressed)
	{
		_isToastSuppressed = suppressed;
		if (suppressed)
		{
			_toastPanel.Clear();
		}
	}

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
			_story.Reset();
			ShowHud();
		}

		_sessionPresentation.PresentStoryActivity(active);
	}

	public void RefreshHud()
	{
		_hud?.Refresh();
	}

	public void ResetPresentationAfterLoad() => _sessionPresentation.ResetAfterLoad();

	public void BindSessionEvents(GameSession session) => _sessionPresentation.Bind(session);

	public Control ShowPartyPanel() => ShowMainPanel(PartyPanelScene, "party panel");

	public Control ShowCharacterRosterPanel(string characterId) =>
		ShowPopupPanel<CharacterRosterPanel>(CharacterRosterPanelScene, "character roster panel",
			panel => panel.CharacterId = characterId);

	public Control ShowInventoryPanel() => ShowMainPanel(InventoryPanelScene, "inventory panel");

	public Control ShowGameLogPanel() => ShowMainPanel(GameLogPanelScene, "game log panel");

	public Control ShowHeroPanel() => ShowMainPanel(HeroPanelScene, "hero panel");

	public Control ShowSystemPanel() => ShowMainPanel(SystemPanelScene, "system panel");

	public Control ShowConsolePanel() =>
		ShowPopupPanel<ConsolePanel>(ConsolePanelScene, "console panel");

	public Control ShowGameOverScreen() => ShowMainPanel(GameOverScreenScene, "game over screen");

	public Control ShowGameFinScreen() => ShowMainPanel(GameFinScreenScene, "game fin screen");

	public Control ShowSkillDetailPanel(SkillInstance skill, DetailPanelAction? action = null) =>
		_detailPanelHost.ShowSkill(skill, action);

	public Control ShowInventoryEntryDetailPanel(
		InventoryEntry entry,
		DetailPanelAction? action = null) =>
		_detailPanelHost.ShowInventoryEntry(entry, action);

	public Control ShowEquipmentDetailPanel(
		EquipmentInstance equipment,
		DetailPanelAction? action = null) =>
		_detailPanelHost.Show(DetailPanelContentFactory.CreateEquipment(equipment, action));

	public Control ShowShopProductDetailPanel(
		ShopProductView product,
		DetailPanelAction? action = null) =>
		_detailPanelHost.ShowShopProduct(product, action);

	public Control ShowSaveSlotSelectionPanel(SaveSlotPanelMode mode) =>
		ShowPopupPanel<SaveSlotSelectionPanel>(SaveSlotSelectionPanelScene, "save slot selection panel",
			panel => panel.Configure(mode));

	public async Task ShowShopPanelAsync(string shopId, CancellationToken cancellationToken = default)
	{
		var panel = ShowPopupPanel<ShopPanel>(ShopPanelScene, "shop panel", panel => panel.Configure(shopId));
		await PanelHost.AwaitClosedAsync(panel, cancellationToken);
	}

	public async Task ShowChestPanelAsync(CancellationToken cancellationToken = default)
	{
		var panel = ShowPopupPanel<ChestPanel>(ChestPanelScene, "chest panel");
		await PanelHost.AwaitClosedAsync(panel, cancellationToken);
	}

	public async Task<IReadOnlyList<string>> ShowCombatantSelectPanelAsync(string battleId, CancellationToken cancellationToken = default) =>
		await ShowCombatantSelectPanelAsync(battleId, EmptyForbiddenSet, cancellationToken);

	public async Task<IReadOnlyList<string>> ShowCombatantSelectPanelAsync(
		string battleId,
		IReadOnlySet<string> forbiddenCharacterIds,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
		ArgumentNullException.ThrowIfNull(forbiddenCharacterIds);

		var battle = Game.ContentRepository.GetBattle(battleId);
		if (CountPlayerDeploySlots(battle) == 0)
		{
			return Array.Empty<string>();
		}

		var panel = PanelHost.Create<CombatantSelectPanel>(CombatantSelectPanelScene, "combatant select panel");

		_modals.Add(panel);
		panel.Configure(battle, forbiddenCharacterIds);
		return await panel.AwaitDeploymentAsync(cancellationToken);
	}

	public async Task<bool> ShowBattleScreenAsync(
		SpecialBattleRequest request,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		return await ShowBattleScreenCoreAsync(
			screen => screen.Configure(request),
			cancellationToken);
	}

	public async Task<InventoryEntry?> ShowRefinementEquipmentSelectionPanelAsync(
		IReadOnlyList<InventoryEntry> entries,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entries);

		var panel = PanelHost.Create<RefinementEquipmentSelectionPanel>(RefinementEquipmentSelectionPanelScene, "refinement equipment selection panel");

		_modals.Add(panel);
		panel.Configure(entries);
		return await panel.AwaitSelectionAsync(cancellationToken);
	}

	public async Task<int> ShowLightnessTrainingScreenAsync(CancellationToken cancellationToken = default)
	{
		var screen = PanelHost.Create<LightnessTrainingScreen>(LightnessTrainingScreenScene, "lightness training screen");

		_modals.Add(screen);
		return await screen.AwaitCompletionAsync(cancellationToken);
	}

	public async Task<(int Score, IReadOnlyDictionary<string, int> ItemCounts)> ShowStrengthTrainingScreenAsync(
		IReadOnlyList<string> itemIds,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(itemIds);

		var screen = PanelHost.Create<StrengthTrainingScreen>(StrengthTrainingScreenScene, "strength training screen");

		screen.Configure(itemIds);
		_modals.Add(screen);
		return await screen.AwaitCompletionAsync(cancellationToken);
	}

	public async Task<SectDefinition> ShowSelectSectScreenAsync(CancellationToken cancellationToken = default)
	{
		var screen = PanelHost.Create<SelectSectScreen>(SelectSectScreenScene, "select sect screen");

		_modals.Add(screen);
		return await screen.AwaitSelectionAsync(cancellationToken);
	}

	public async Task<string> ShowInputNamePanelAsync(
		string characterId,
		string defaultName = "",
		CancellationToken cancellationToken = default)
	{
		var panel = PanelHost.Create<InputNamePanel>(InputNamePanelScene, "input name panel");

		_modals.Add(panel);
		return await panel.AwaitNameAsync(characterId, defaultName, cancellationToken);
	}

	public async Task<string> ShowSelectHeadPanelAsync(CancellationToken cancellationToken = default)
	{
		var panel = PanelHost.Create<SelectHeadPanel>(SelectHeadPanelScene, "select head panel");

		_modals.Add(panel);
		return await panel.AwaitHeadAsync(cancellationToken);
	}

	public async Task ShowRollStatsPanelAsync(string characterId, CancellationToken cancellationToken = default)
	{
		var panel = PanelHost.Create<RollStatsPanel>(RollStatsPanelScene, "roll stats panel");

		_modals.Add(panel);
		await panel.AwaitRollAsync(characterId, cancellationToken);
	}

	public void CloseMainPanel() => _panels.CloseMain();

	public void ClosePopupPanel() => _panels.ClosePopup();

	public void ClosePanel() => CloseMainPanel();

	public Task ShowDialogueAsync(string speaker, string text, CancellationToken cancellationToken = default) =>
		_story.ShowDialogueAsync(speaker, text, cancellationToken);

	public Task ShowIntertitleAsync(
		string text,
		string position = "center",
		string mode = "typewriter",
		double speed = 36d,
		CancellationToken cancellationToken = default) =>
		_story.ShowIntertitleAsync(text, position, mode, speed, cancellationToken);

	public Task ShowVideoAsync(VideoStream stream, CancellationToken cancellationToken = default) =>
		_story.ShowVideoAsync(stream, cancellationToken);

	public Task<int> ShowChoicesAsync(
		string? speaker,
		string prompt,
		IReadOnlyList<string> options,
		ChoiceStyle style = ChoiceStyle.Regular,
		CancellationToken cancellationToken = default) =>
		_story.ShowChoicesAsync(speaker, prompt, options, style, cancellationToken);

	public Task<int?> ShowCancelableChoicesAsync(
		string? speaker,
		string prompt,
		IReadOnlyList<string> options,
		ChoiceStyle style = ChoiceStyle.Regular,
		CancellationToken cancellationToken = default) =>
		_story.ShowCancelableChoicesAsync(speaker, prompt, options, style, cancellationToken);

	public void ShowSuggestion(string text)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);
		_ = ShowSuggestionAsync(text);
	}

	public Task ShowSuggestionAsync(string text, CancellationToken cancellationToken = default) =>
		_hintBox.ShowHintAsync(text, cancellationToken);

	public Task<bool> ShowConfirmAsync(
		string text,
		ConfirmDialogTone tone = ConfirmDialogTone.Normal,
		CancellationToken cancellationToken = default) =>
		_confirmDialog.ShowConfirmAsync(text, tone, cancellationToken);

	public void ShowToast(string text, ToastTone tone = ToastTone.Normal, string? soundEffectId = null)
	{
		if (_isToastSuppressed)
		{
			return;
		}

		_toastPanel.Enqueue(text, tone);
		if (soundEffectId is not null)
		{
			Game.Audio.PlaySfx(soundEffectId);
		}
	}

	private static IReadOnlySet<string> EmptyForbiddenSet { get; } = new HashSet<string>(StringComparer.Ordinal);

	private static int CountPlayerDeploySlots(BattleDefinition battle) =>
		battle.Participants.Count(participant =>
			participant.Team == Game.Config.BattlePlayerTeam &&
			participant.PartyIndex is not null &&
			participant.CharacterId is null);

	private async Task<bool> ShowBattleScreenCoreAsync(
		Action<BattleScreen> configure,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(configure);

		var screen = PanelHost.Create<BattleScreen>(BattleScreenScene, "battle screen");

		_battles.Add(screen);
		configure(screen);
		return await screen.AwaitBattleAsync(cancellationToken);
	}

	private Control ShowMainPanel(PackedScene? scene, string description, Action<Control>? configure = null)
	{
		var panel = PanelHost.Create<Control>(scene, description, configure);
		_panels.ShowMain(panel);
		return panel;
	}

	private T ShowPopupPanel<T>(PackedScene? scene, string description, Action<T>? configure = null) where T : Control
	{
		var panel = PanelHost.Create<T>(scene, description, configure);
		_panels.ShowPopup(panel);
		return panel;
	}

}
