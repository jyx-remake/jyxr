using Game.Application.Mods;
using Godot;

namespace Game.Godot.UI.ModLauncher;

public partial class ModShowcasePage : Control
{
	[Export]
	public PackedScene ModItemCardScene { get; set; } = null!;

	private VBoxContainer _cardList = null!;
	private Control _emptyState = null!;
	private Label _emptyStateLabel = null!;
	private Label _validationLabel = null!;
	private Button _refreshModsButton = null!;
	private IReadOnlyList<ModContext> _mods = [];
	private string? _primaryModId;
	private List<string> _enabledAddonIds = [];
	private ModLoadout? _resolvedLoadout;

	public ModLoadout? ResolvedLoadout => _resolvedLoadout;
	public event Action? SelectionChanged;
	public event Action? RefreshRequested;

	public override void _Ready()
	{
		_cardList = GetNode<VBoxContainer>("%CardList");
		_emptyState = GetNode<Control>("%EmptyState");
		_emptyStateLabel = GetNode<Label>("%EmptyStateLabel");
		_validationLabel = GetNode<Label>("%ValidationLabel");
		_refreshModsButton = GetNode<Button>("%RefreshModsButton");
		_refreshModsButton.Pressed += () => RefreshRequested?.Invoke();
		ClearCards();
	}

	public void SetEmptyStateMessage(string message) => _emptyStateLabel.Text = message;
	public void SetRefreshButtonText(string text) => _refreshModsButton.Text = text;

	public void ShowLoadError(string message)
		=> ShowMessage(message, isError: true);

	public void ShowMessage(string message, bool isError)
	{
		_validationLabel.AddThemeColorOverride("font_color",
			isError ? new Color(1f, 0.45f, 0.3f) : new Color(0.5f, 1f, 0.5f));
		_validationLabel.Text = message;
		_validationLabel.Show();
		_refreshModsButton.Show();
	}

	public void Configure(IReadOnlyList<ModContext> mods, LauncherSettingsRecord settings)
	{
		ArgumentNullException.ThrowIfNull(mods);
		ArgumentNullException.ThrowIfNull(settings);
		_mods = mods;
		_primaryModId = mods.Any(mod =>
				mod.Manifest.Type == ModType.Game &&
				string.Equals(mod.ModId, settings.PrimaryModId, StringComparison.Ordinal))
			? settings.PrimaryModId
			: mods.FirstOrDefault(static mod => mod.Manifest.Type == ModType.Game)?.ModId;
		_enabledAddonIds = settings.EnabledAddonIds
			.Where(id => mods.Any(mod =>
				mod.Manifest.Type == ModType.Addon &&
				string.Equals(mod.ModId, id, StringComparison.Ordinal)))
			.Distinct(StringComparer.Ordinal)
			.ToList();
		ResolveAndRefresh();
	}

	private void ResolveAndRefresh()
	{
		_resolvedLoadout = null;
		var error = string.Empty;
		if (_primaryModId is null)
		{
			error = "没有可用的主 MOD。";
		}
		else
		{
			try
			{
				var resolver = new ModLoadoutResolver(_mods);
				_resolvedLoadout = resolver.Resolve(_primaryModId, _enabledAddonIds);
				_enabledAddonIds = _resolvedLoadout.AddonMods.Select(static mod => mod.ModId).ToList();
			}
			catch (Exception exception)
			{
				error = exception.Message;
			}
		}

		RefreshCards();
		_validationLabel.Visible = _mods.Count > 0 && _resolvedLoadout is null;
		_validationLabel.Text = error;
		_validationLabel.RemoveThemeColorOverride("font_color");
		_refreshModsButton.Visible = _resolvedLoadout is null;
		SelectionChanged?.Invoke();
	}

	private void RefreshCards()
	{
		ClearCards();
		_emptyState.Visible = _mods.Count == 0;
		var enabledPositions = _enabledAddonIds
			.Select((id, index) => KeyValuePair.Create(id, index))
			.ToDictionary(StringComparer.Ordinal);
		var orderedMods = _mods
			.OrderBy(static mod => mod.Manifest.Type)
			.ThenBy(mod => mod.Manifest.Type == ModType.Addon &&
				enabledPositions.TryGetValue(mod.ModId, out var position) ? position : int.MaxValue)
			.ThenBy(static mod => mod.Manifest.Name, StringComparer.Ordinal)
			.ToArray();
		var resolver = _resolvedLoadout is null ? null : new ModLoadoutResolver(_mods);

		foreach (var mod in orderedMods)
		{
			var card = CreateCard();
			card.PrimarySelected += OnPrimarySelected;
			card.AddonToggled += OnAddonToggled;
			card.MoveRequested += OnMoveRequested;
			_cardList.AddChild(card);

			var enabled = enabledPositions.TryGetValue(mod.ModId, out var index);
			var addons = _resolvedLoadout?.AddonMods ?? [];
			card.Configure(
				mod,
				string.Equals(mod.ModId, _primaryModId, StringComparison.Ordinal),
				enabled,
				enabled && resolver is not null && resolver.CanMove(addons, index, index - 1),
				enabled && resolver is not null && resolver.CanMove(addons, index, index + 1));
		}
	}

	private void OnPrimarySelected(ModContext context)
	{
		_primaryModId = context.ModId;
		ResolveAndRefresh();
	}

	private void OnAddonToggled(ModContext context, bool enabled)
	{
		if (enabled)
		{
			if (!_enabledAddonIds.Contains(context.ModId, StringComparer.Ordinal))
			{
				_enabledAddonIds.Add(context.ModId);
			}
		}
		else
		{
			var removed = new HashSet<string>(StringComparer.Ordinal) { context.ModId };
			var changed = true;
			while (changed)
			{
				changed = false;
				foreach (var addonId in _enabledAddonIds.ToArray())
				{
					var addon = _mods.First(mod => string.Equals(mod.ModId, addonId, StringComparison.Ordinal));
					if (addon.Manifest.ResolvedDependencies.Any(removed.Contains) && removed.Add(addonId))
					{
						changed = true;
					}
				}
			}

			_enabledAddonIds.RemoveAll(removed.Contains);
		}

		ResolveAndRefresh();
	}

	private void OnMoveRequested(ModContext context, int offset)
	{
		if (_resolvedLoadout is null)
		{
			return;
		}

		var addons = _resolvedLoadout.AddonMods.ToList();
		var fromIndex = addons.FindIndex(mod => string.Equals(mod.ModId, context.ModId, StringComparison.Ordinal));
		var toIndex = fromIndex + offset;
		var resolver = new ModLoadoutResolver(_mods);
		if (!resolver.CanMove(addons, fromIndex, toIndex))
		{
			return;
		}

		var moved = addons[fromIndex];
		addons.RemoveAt(fromIndex);
		addons.Insert(toIndex, moved);
		_enabledAddonIds = addons.Select(static mod => mod.ModId).ToList();
		ResolveAndRefresh();
	}

	private ModItemCard CreateCard()
	{
		var instance = ModItemCardScene.Instantiate();
		if (instance is ModItemCard card)
		{
			return card;
		}

		instance.QueueFree();
		throw new InvalidOperationException("Mod item card scene root must be ModItemCard.");
	}

	private void ClearCards()
	{
		foreach (var child in _cardList.GetChildren())
		{
			if (child == _emptyState)
			{
				continue;
			}

			child.QueueFree();
		}
	}
}
