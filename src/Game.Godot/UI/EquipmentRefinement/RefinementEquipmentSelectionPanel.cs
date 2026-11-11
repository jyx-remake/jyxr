using Game.Application;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class RefinementEquipmentSelectionPanel : JyPanel
{
	[Export]
	public PackedScene InventoryItemBoxScene { get; set; } = null!;

	private readonly TaskCompletionSource<InventoryEntry?> _selectionCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private VirtualGrid _gridContainer = null!;
	private Label _emptyLabel = null!;
	private Label _countLabel = null!;
	private ItemTagBar _tagBar = null!;
	private ScrollContainer _scrollContainer = null!;
	private IReadOnlyList<InventoryEntry> _entries = [];
	private IDisposable? _saveLoadedSubscription;

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<VirtualGrid>("%VirtualGrid");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_countLabel = GetNode<Label>("%CountLabel");
		_tagBar = GetNode<ItemTagBar>("%TagButtons");
		_scrollContainer = GetNode<ScrollContainer>("%EquipmentScroll");
		_tagBar.SelectionChanged += () =>
		{
			_scrollContainer.ScrollVertical = 0;
			Refresh();
		};
		ClosePanelRequested += () => _selectionCompletion.TrySetResult(null);
		_saveLoadedSubscription = Game.Session.Events.Subscribe<SaveLoadedEvent>(_ => QueueFree());
		Refresh();
	}

	public void Configure(IReadOnlyList<InventoryEntry> entries)
	{
		ArgumentNullException.ThrowIfNull(entries);
		_entries = entries;
		Refresh();
	}

	public async Task<InventoryEntry?> AwaitSelectionAsync(CancellationToken cancellationToken = default)
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

		_tagBar.SetItems(_entries.Select(entry => entry.Definition), ItemType.Equipment);
		var visibleEntries = _entries
			.Where(entry => _tagBar.Matches(entry.Definition))
			.OrderBy(entry => entry.EntryNumber)
			.ToArray();
		_countLabel.Text = $"{visibleEntries.Length} 项";
		_emptyLabel.Visible = visibleEntries.Length == 0;

		_gridContainer.SetItems(visibleEntries, CreateItemBox, (box, entry, _) => box.Setup(entry));
	}

	private InventoryItemBox CreateItemBox()
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

		itemBox.EntrySelected += OnEntrySelected;
		return itemBox;
	}

	private void OnEntrySelected(InventoryEntry entry)
	{
		if (!_entries.Contains(entry))
		{
			throw new InvalidOperationException("Refinement selection received an entry outside the candidate list.");
		}

		UIRoot.Instance.ShowInventoryEntryDetailPanel(
			entry,
			new DetailPanelAction(
				"选择",
				true,
				() =>
				{
					CompleteSelection(entry);
					return Task.CompletedTask;
				}));
	}

	private void CompleteSelection(InventoryEntry entry)
	{
		if (_selectionCompletion.TrySetResult(entry))
		{
			QueueFree();
		}
	}

}
