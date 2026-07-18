using Game.Application;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class RefinementEquipmentSelectionPanel : JyPanel
{
	[Export]
	public PackedScene InventoryItemBoxScene { get; set; } = null!;

	private readonly TaskCompletionSource<EquipmentInstanceInventoryEntry?> _selectionCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private IReadOnlyList<EquipmentInstanceInventoryEntry> _entries = [];
	private IDisposable? _saveLoadedSubscription;

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		ClosePanelRequested += () => _selectionCompletion.TrySetResult(null);
		_saveLoadedSubscription = Game.Session.Events.Subscribe<SaveLoadedEvent>(_ => QueueFree());
		Refresh();
	}

	public void Configure(IReadOnlyList<EquipmentInstanceInventoryEntry> entries)
	{
		ArgumentNullException.ThrowIfNull(entries);
		_entries = entries;
		Refresh();
	}

	public async Task<EquipmentInstanceInventoryEntry?> AwaitSelectionAsync(CancellationToken cancellationToken = default)
	{
		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_selectionCompletion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		return await _selectionCompletion.Task;
	}

	public override void _ExitTree()
	{
		_saveLoadedSubscription?.Dispose();
		_saveLoadedSubscription = null;
		base._ExitTree();
		if (!_selectionCompletion.Task.IsCompleted)
		{
			_selectionCompletion.TrySetResult(null);
		}
	}

	private void Refresh()
	{
		if (!IsInsideTree())
		{
			return;
		}

		ClearGrid();
		_emptyLabel.Visible = _entries.Count == 0;

		foreach (var entry in _entries)
		{
			_gridContainer.AddChild(CreateItemBox(entry));
		}
	}

	private InventoryItemBox CreateItemBox(EquipmentInstanceInventoryEntry entry)
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
		if (entry is not EquipmentInstanceInventoryEntry equipmentEntry)
		{
			throw new InvalidOperationException("Refinement selection received a non-equipment inventory entry.");
		}

		UIRoot.Instance.ShowInventoryEntryDetailPanel(
			equipmentEntry,
			new DetailPanelAction(
				"选择",
				true,
				() =>
				{
					CompleteSelection(equipmentEntry);
					return Task.CompletedTask;
				}));
	}

	private void CompleteSelection(EquipmentInstanceInventoryEntry entry)
	{
		if (_selectionCompletion.TrySetResult(entry))
		{
			QueueFree();
		}
	}

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}
}
