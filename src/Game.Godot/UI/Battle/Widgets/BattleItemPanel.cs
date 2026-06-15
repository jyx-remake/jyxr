using Game.Core.Model;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleItemPanel : JyPanel
{
	[Export]
	public PackedScene InventoryItemBoxScene { get; set; } = null!;

	private readonly TaskCompletionSource<InventoryEntry?> _selectionCompletion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private GridContainer _gridContainer = null!;
	private Label _countLabel = null!;
	private Label _emptyLabel = null!;
	private IReadOnlyList<InventoryEntry> _entries = [];

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_countLabel = GetNode<Label>("%CountLabel");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		ClosePanelRequested += () => _selectionCompletion.TrySetResult(null);
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
		_countLabel.Text = $"{_entries.Count} 项";
		_emptyLabel.Visible = _entries.Count == 0;

		foreach (var entry in _entries)
		{
			_gridContainer.AddChild(CreateItemBox(entry));
		}
	}

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
