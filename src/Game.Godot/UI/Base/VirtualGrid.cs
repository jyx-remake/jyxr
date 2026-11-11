using Game.Presentation.Collections;
using Godot;

namespace Game.Godot.UI;

public partial class VirtualGrid : Control
{
	[Export(PropertyHint.Range, "1,32,1")]
	public int Columns { get; set; } = 1;

	[Export]
	public Vector2 CellSize { get; set; } = new(180, 180);

	[Export]
	public Vector2 Separation { get; set; } = new(4, 4);

	private sealed record Cell(Control Control, string Kind, object? Item);

	private readonly Dictionary<int, Cell> _cells = [];
	private ScrollContainer _scroll = null!;
	private Func<int, Control>? _create;
	private Action<Control, int>? _bind;
	private Func<int, string>? _getKind;
	private Func<int, object?>? _getItem;
	private int _count;
	private int? _pinnedIndex;
	private int? _pendingFocusIndex;
	private int _pendingFocusStep;
	private bool _refreshQueued;
	private bool _rebind;

	private VirtualGridLayout Layout => new(
		_count, Columns, CellSize.X, CellSize.Y, Separation.X, Separation.Y);

	public int ItemCount => _count;

	public override void _Ready()
	{
		_scroll = GetParent<ScrollContainer>();
		MouseFilter = MouseFilterEnum.Pass;
		_scroll.GetVScrollBar().ValueChanged += OnScrollChanged;
		_scroll.GetVScrollBar().Changed += RequestRefresh;
		_scroll.Resized += RequestRefresh;
		Resized += RequestRefresh;
		VisibilityChanged += RequestRefresh;
		RequestRefresh();
	}

	public override void _ExitTree()
	{
		_scroll.GetVScrollBar().ValueChanged -= OnScrollChanged;
		_scroll.GetVScrollBar().Changed -= RequestRefresh;
		_scroll.Resized -= RequestRefresh;
	}

	public void SetItems<TItem, TControl>(
		IReadOnlyList<TItem> items,
		Func<TControl> create,
		Action<TControl, TItem, int> bind) where TControl : Control
	{
		ArgumentNullException.ThrowIfNull(create);
		SetItems(items, _ => create(), bind, _ => typeof(TControl).FullName!);
	}

	public void SetItems<TItem, TControl>(
		IReadOnlyList<TItem> items,
		Func<TItem, TControl> create,
		Action<TControl, TItem, int> bind,
		Func<TItem, string> getKind) where TControl : Control
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(create);
		ArgumentNullException.ThrowIfNull(bind);
		ArgumentNullException.ThrowIfNull(getKind);
		_count = items.Count;
		_create = index => create(items[index]);
		_bind = (control, index) => bind((TControl)control, items[index], index);
		_getKind = index => getKind(items[index]);
		_getItem = index => items[index];
		_pinnedIndex = null;
		_pendingFocusIndex = null;
		_rebind = true;
		RequestRefresh();
	}

	public void RefreshItems()
	{
		_rebind = true;
		RequestRefresh();
	}

	public void ClearItems()
	{
		_count = 0;
		_create = null;
		_bind = null;
		_getKind = null;
		_getItem = null;
		_pinnedIndex = null;
		_pendingFocusIndex = null;
		RequestRefresh();
	}

	public void RefreshItem(int index)
	{
		if (_cells.TryGetValue(index, out var cell) && index < _count)
		{
			_bind!(cell.Control, index);
		}
	}

	public void PinItem(int? index)
	{
		if (index is int value)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, _count);
		}

		_pinnedIndex = index;
		RequestRefresh();
	}

	public Rect2 GetItemRect(int index)
	{
		var (x, y) = Layout.GetPosition(index);
		return new Rect2(new Vector2(x, y), CellSize);
	}

	public int GetInsertionSlot(Vector2 localPosition) =>
		Layout.GetInsertionSlot(localPosition.X, localPosition.Y);

	public override void _Input(InputEvent @event)
	{
		if (!IsVisibleInTree() || _count == 0)
		{
			return;
		}

		var offset = 0;
		if (@event.IsActionPressed("ui_left"))
		{
			offset = -1;
		}
		else if (@event.IsActionPressed("ui_right"))
		{
			offset = 1;
		}
		else if (@event.IsActionPressed("ui_up"))
		{
			offset = -Columns;
		}
		else if (@event.IsActionPressed("ui_down"))
		{
			offset = Columns;
		}
		if (offset == 0 || FindFocusedIndex() is not int index)
		{
			return;
		}

		var next = index + offset;
		if (!MoveFocusTo(next, offset))
		{
			return;
		}

		GetViewport().SetInputAsHandled();
	}

	private bool MoveFocusTo(int index, int step)
	{
		if (index < 0 || index >= _count)
		{
			_pendingFocusIndex = null;
			return false;
		}

		var rect = GetItemRect(index);
		var top = _scroll.ScrollVertical;
		var height = _scroll.GetVScrollBar().Page;
		if (rect.Position.Y < top)
		{
			_scroll.ScrollVertical = (int)rect.Position.Y;
		}
		else if (rect.End.Y > top + height)
		{
			_scroll.ScrollVertical = (int)Math.Ceiling(rect.End.Y - height);
		}
		_pendingFocusIndex = index;
		_pendingFocusStep = step;
		RequestRefresh();
		return true;
	}

	private int? FindFocusedIndex()
	{
		var focused = GetViewport().GuiGetFocusOwner();
		if (focused is null)
		{
			return null;
		}

		foreach (var (index, cell) in _cells)
		{
			if (cell.Control == focused || cell.Control.IsAncestorOf(focused))
			{
				return index;
			}
		}

		return null;
	}

	private void OnScrollChanged(double _) => RequestRefresh();

	private void RequestRefresh()
	{
		if (_refreshQueued || !IsInsideTree())
		{
			return;
		}

		_refreshQueued = true;
		CallDeferred(MethodName.RefreshVisible);
	}

	private void RefreshVisible()
	{
		_refreshQueued = false;
		if (!IsInsideTree())
		{
			return;
		}

		for (Node? node = this; node is not null; node = node.GetParent())
		{
			if (node.IsQueuedForDeletion())
			{
				return;
			}
		}

		var layout = Layout;
		CustomMinimumSize = new Vector2(layout.Width, layout.Height);
		var viewportHeight = (float)_scroll.GetVScrollBar().Page;
		if (!IsVisibleInTree() || viewportHeight <= 0)
		{
			return;
		}

		var (start, end) = layout.GetVisibleRange(_scroll.ScrollVertical, viewportHeight);
		var focusedIndex = FindFocusedIndex();
		var recycled = new Dictionary<string, Stack<Control>>();
		foreach (var (index, cell) in _cells.ToArray())
		{
			var retained = (index >= start && index < end) || index == _pinnedIndex || index == focusedIndex;
			if (index < _count && retained && cell.Kind == _getKind!(index)
				&& Equals(cell.Item, _getItem!(index)))
			{
				continue;
			}

			_cells.Remove(index);
			cell.Control.Hide();
			if (!recycled.TryGetValue(cell.Kind, out var pool))
			{
				pool = new Stack<Control>();
				recycled.Add(cell.Kind, pool);
			}

			pool.Push(cell.Control);
		}

		for (var index = start; index < end; index++)
		{
			if (_cells.ContainsKey(index))
			{
				continue;
			}

			var kind = _getKind!(index);
			var control = recycled.TryGetValue(kind, out var pool) && pool.TryPop(out var reused)
				? reused
				: _create!(index);
			if (control.GetParent() is null)
			{
				AddChild(control);
			}

			_cells.Add(index, new Cell(control, kind, _getItem!(index)));
			if (!_rebind)
			{
				_bind!(control, index);
			}

			control.Show();
		}

		foreach (var (index, cell) in _cells)
		{
			var (x, y) = layout.GetPosition(index);
			cell.Control.Position = new Vector2(x, y);
			cell.Control.Size = CellSize;
			if (_rebind)
			{
				_bind!(cell.Control, index);
			}
		}

		foreach (var control in recycled.Values.SelectMany(pool => pool))
		{
			RemoveChild(control);
			control.QueueFree();
		}

		_rebind = false;
		if (_pendingFocusIndex is int focusIndex && _cells.TryGetValue(focusIndex, out var focusCell))
		{
			if (focusCell.Control.FocusMode != FocusModeEnum.All || focusCell.Control is BaseButton { Disabled: true })
			{
				MoveFocusTo(focusIndex + _pendingFocusStep, _pendingFocusStep);
				return;
			}

			focusCell.Control.GrabFocus();
			_pendingFocusIndex = null;
		}
	}
}
