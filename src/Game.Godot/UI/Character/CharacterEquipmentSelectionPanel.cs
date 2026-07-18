using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterEquipmentSelectionPanel : JyPanel
{
	[Export]
	public PackedScene InventoryItemBoxScene { get; set; } = null!;

	private GridContainer _gridContainer = null!;
	private Label _titleLabel = null!;
	private Label _emptyLabel = null!;
	private Label _hintLabel = null!;

	private string _characterId = string.Empty;
	private EquipmentSlotType _slotType;
	private IDisposable? _saveLoadedSubscription;

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_hintLabel = GetNode<Label>("%HintLabel");
		_saveLoadedSubscription = Game.Session.Events.Subscribe<SaveLoadedEvent>(_ => QueueFree());
		Refresh();
	}

	public override void _ExitTree()
	{
		_saveLoadedSubscription?.Dispose();
		_saveLoadedSubscription = null;
		base._ExitTree();
	}

	public void Configure(string characterId, EquipmentSlotType slotType)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
		_characterId = characterId;
		_slotType = slotType;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || string.IsNullOrWhiteSpace(_characterId))
		{
			return;
		}

		ClearGrid();

		var character = Game.State.Party.GetMember(_characterId);
		var entries = Game.State.Inventory.Entries
			.Where(IsMatchingEquipmentSlot)
			.Where(entry => Game.ItemUseService.AnalyzeTarget(entry, character).CanUse)
			.OrderBy(entry => entry.EntryNumber)
			.ToList();

		_titleLabel.Text = $"选择{FormatSlotName(_slotType)}";
		_hintLabel.Text = $"为【{character.Name}】选择可装备的{FormatSlotName(_slotType)}。";
		_emptyLabel.Visible = entries.Count == 0;

		foreach (var entry in entries)
		{
			var itemBox = CreateItemBox(entry);
			_gridContainer.AddChild(itemBox);
		}
	}

	private bool IsMatchingEquipmentSlot(InventoryEntry entry) =>
		entry.Definition is EquipmentDefinition equipment &&
		equipment.SlotType == _slotType;

	private InventoryItemBox CreateItemBox(InventoryEntry entry)
	{
		if (InventoryItemBoxScene is null)
		{
			throw new InvalidOperationException("InventoryItemBoxScene is not assigned.");
		}

		var instance = InventoryItemBoxScene.Instantiate();
		if (instance is not InventoryItemBox itemBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("InventoryItemBox scene root must be InventoryItemBox.");
		}

		itemBox.Setup(entry);
		itemBox.EntrySelected += OnEntrySelected;
		return itemBox;
	}

	private void OnEntrySelected(InventoryEntry entry)
	{
		var character = Game.State.Party.GetMember(_characterId);
		var candidate = Game.ItemUseService.AnalyzeTarget(entry, character);
		Control? detailPanel = null;
		var action = new DetailPanelAction(
			candidate.CanUse ? "装备" : "不可装备",
			candidate.CanUse,
			() =>
			{
				EquipEntry(entry, detailPanel);
				return Task.CompletedTask;
			},
			CloseAfterExecute: false);
		detailPanel = UIRoot.Instance.ShowInventoryEntryDetailPanel(entry, action);
	}

	private void EquipEntry(InventoryEntry entry, Control? detailPanel)
	{
		try
		{
			var result = Game.ItemUseService.Use(entry, _characterId);
			if (!result.Success)
			{
				UIRoot.Instance.ShowSuggestion(result.Message);
				return;
			}

			UIRoot.Instance.ShowToast(result.Message);
			if (detailPanel is not null && GodotObject.IsInstanceValid(detailPanel))
			{
				detailPanel.QueueFree();
			}

			QueueFree();
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Equipping character equipment failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private static string FormatSlotName(EquipmentSlotType slotType) =>
		slotType switch
		{
			EquipmentSlotType.Weapon => "武器",
			EquipmentSlotType.Armor => "防具",
			EquipmentSlotType.Accessory => "饰品",
			_ => slotType.ToString(),
		};
}
