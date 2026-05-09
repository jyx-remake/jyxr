using Game.Core.Battle;
using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleBoardView : Control
{
	private const int CellTextFontSize = 13;
	private const double FloatTextQueueSpacingSeconds = 0.4d;
	private static readonly Color BorderColor = new(0f, 0f, 0f, 0.45f);
	private static readonly Color TextColor = Colors.White;

	[Export]
	public PackedScene BattleUnitViewScene { get; set; } = null!;

	[Export]
	public PackedScene BattleSkillViewScene { get; set; } = null!;

	[Export]
	public PackedScene BattleFloatTextScene { get; set; } = null!;

	private readonly Dictionary<GridPosition, BattleBoardCellVisual> _cells = [];
	private readonly Dictionary<string, BattleUnitView> _unitViews = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Queue<QueuedFloatText>> _queuedFloatTexts = new(StringComparer.Ordinal);
	private readonly HashSet<string> _processingFloatTextUnits = new(StringComparer.Ordinal);

	private int _gridWidth;
	private int _gridHeight;
	private int _cellWidth;
	private int _cellHeight;
	private int _cellGap;
	private GridPosition? _hoveredCell;
	private Node2D _combatantLayer = null!;
	private Node2D _effectLayer = null!;

	private sealed record QueuedFloatText(string Text, Color Color);

	public event Action<GridPosition>? CellPressed;

	public event Action<GridPosition?>? HoveredCellChanged;

	public override void _Ready()
	{
		_combatantLayer = GetNode<Node2D>("%CombatantLayer");
		_effectLayer = GetNode<Node2D>("%EffectLayer");
	}

	public void RenderGrid(
		int gridWidth,
		int gridHeight,
		int cellWidth,
		int cellHeight,
		int cellGap,
		IReadOnlyList<BattleBoardCellVisual> cells)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(gridWidth, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(gridHeight, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(cellWidth, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(cellHeight, 1);
		ArgumentNullException.ThrowIfNull(cells);

		_gridWidth = gridWidth;
		_gridHeight = gridHeight;
		_cellWidth = cellWidth;
		_cellHeight = cellHeight;
		_cellGap = Math.Max(0, cellGap);

		_cells.Clear();
		foreach (var cell in cells)
		{
			_cells[cell.Position] = cell;
		}

		CustomMinimumSize = new Vector2(
			_gridWidth * _cellWidth + Math.Max(0, _gridWidth - 1) * _cellGap,
			_gridHeight * _cellHeight + Math.Max(0, _gridHeight - 1) * _cellGap);
		QueueRedraw();
	}

	public void RenderUnits(IReadOnlyList<BattleBoardUnitVisual> units)
	{
		ArgumentNullException.ThrowIfNull(units);

		var activeUnitIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var unit in units)
		{
			activeUnitIds.Add(unit.UnitId);
			var isExistingView = _unitViews.TryGetValue(unit.UnitId, out var unitView);
			unitView ??= GetOrCreateUnitView(unit.UnitId);
			unitView.Configure(unit);

			var targetPosition = ResolveUnitAnchor(unit.Position);
			if (!isExistingView)
			{
				unitView.Position = targetPosition;
				unitView.PlayIdle();
				continue;
			}

			if (unitView.Position.DistanceSquaredTo(targetPosition) > 1f)
			{
				unitView.PlayMoveLoop();
				var tween = CreateTween();
				tween.TweenProperty(unitView, "position", targetPosition, 0.18d);
				tween.Finished += unitView.PlayIdle;
			}
			else
			{
				unitView.Position = targetPosition;
			}
		}

		foreach (var staleUnitId in _unitViews.Keys.Except(activeUnitIds, StringComparer.Ordinal).ToArray())
		{
			if (_unitViews.Remove(staleUnitId, out var staleView))
			{
				staleView.QueueFree();
			}

			_queuedFloatTexts.Remove(staleUnitId);
			_processingFloatTextUnits.Remove(staleUnitId);
		}
	}

	public void PlayAttack(string actingUnitId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(actingUnitId);

		if (_unitViews.TryGetValue(actingUnitId, out var actingView))
		{
			actingView.PlayAttack();
		}
	}

	public void ApplyUnitFacing(string unitId, BattleFacing facing)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

		if (_unitViews.TryGetValue(unitId, out var unitView))
		{
			unitView.ApplyFacing(facing);
		}
	}

	public void PlaySkillImpact(
		IReadOnlyList<string> targetUnitIds,
		IReadOnlyList<GridPosition> impactPositions,
		string? skillAnimationId)
	{
		ArgumentNullException.ThrowIfNull(targetUnitIds);
		ArgumentNullException.ThrowIfNull(impactPositions);

		foreach (var impactPosition in impactPositions)
		{
			PlaySkillAnimationAt(ResolveUnitAnchor(impactPosition), skillAnimationId);
		}

		foreach (var targetUnitId in targetUnitIds)
		{
			if (_unitViews.TryGetValue(targetUnitId, out var targetView))
			{
				targetView.PlayHit();
			}
		}
	}

	public void PlaySkillAnimationAt(Vector2 position, string? skillAnimationId)
	{
		var skillAnimation = AssetResolver.LoadSkillAnimation(skillAnimationId);
		if (skillAnimation is null)
		{
			return;
		}

		if (BattleSkillViewScene is null)
		{
			throw new InvalidOperationException("BattleSkillViewScene is not assigned.");
		}

		var instance = BattleSkillViewScene.Instantiate();
		if (instance is not BattleSkillView effectView)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle skill view scene root must be BattleSkillView.");
		}

		effectView.Position = position;
		_effectLayer.AddChild(effectView);
		effectView.Play(skillAnimation);
	}

	public void PlayFloatText(string unitId, string text, Color color)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(unitId);
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		if (!_unitViews.ContainsKey(unitId))
		{
			return;
		}

		if (!_queuedFloatTexts.TryGetValue(unitId, out var queue))
		{
			queue = new Queue<QueuedFloatText>();
			_queuedFloatTexts[unitId] = queue;
		}

		queue.Enqueue(new QueuedFloatText(text, color));
		if (_processingFloatTextUnits.Add(unitId))
		{
			ProcessFloatTextQueueAsync(unitId);
		}
	}

	public void PlayPopupText(string text, Color color)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		PlayFloatTextAt(CustomMinimumSize * 0.5f, text, color, popup: true);
	}

	public void PlaySpeech(string unitId, string text)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(unitId);
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		if (_unitViews.TryGetValue(unitId, out var unitView))
		{
			unitView.ShowSpeech(text);
		}
	}

	public override void _Draw()
	{
		base._Draw();
		var font = GetThemeDefaultFont();
		if (font is null)
		{
			return;
		}

		for (var y = 0; y < _gridHeight; y++)
		{
			for (var x = 0; x < _gridWidth; x++)
			{
				var position = new GridPosition(x, y);
				if (!_cells.TryGetValue(position, out var cell))
				{
					continue;
				}

				var rect = ResolveCellRect(position);
				DrawRect(rect, cell.Color);
				DrawRect(rect, BorderColor, false, 2f);
				DrawCellText(font, rect, cell.Label);
			}
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		base._GuiInput(@event);

		switch (@event)
		{
			case InputEventMouseMotion motion:
				UpdateHoveredCell(motion.Position);
				break;
			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } button:
				if (TryGetCellAt(button.Position, out var position) &&
					_cells.TryGetValue(position, out var cell) &&
					cell.IsInteractive)
				{
					CellPressed?.Invoke(position);
				}
				break;
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationMouseExit && _hoveredCell is not null)
		{
			_hoveredCell = null;
			HoveredCellChanged?.Invoke(null);
		}
	}

	private void UpdateHoveredCell(Vector2 mousePosition)
	{
		GridPosition? hovered = TryGetCellAt(mousePosition, out var position) &&
			_cells.TryGetValue(position, out var cell) &&
			cell.IsInteractive
			? position
			: null;
		if (_hoveredCell == hovered)
		{
			return;
		}

		_hoveredCell = hovered;
		HoveredCellChanged?.Invoke(hovered);
	}

	private bool TryGetCellAt(Vector2 point, out GridPosition position)
	{
		if (_cellWidth <= 0 || _cellHeight <= 0)
		{
			position = default;
			return false;
		}

		var strideX = _cellWidth + _cellGap;
		var strideY = _cellHeight + _cellGap;
		var x = Mathf.FloorToInt(point.X / strideX);
		var y = Mathf.FloorToInt(point.Y / strideY);
		if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
		{
			position = default;
			return false;
		}

		var localX = point.X - x * strideX;
		var localY = point.Y - y * strideY;
		if (localX > _cellWidth || localY > _cellHeight)
		{
			position = default;
			return false;
		}

		position = new GridPosition(x, y);
		return true;
	}

	private Rect2 ResolveCellRect(GridPosition position) =>
		new(
			position.X * (_cellWidth + _cellGap),
			position.Y * (_cellHeight + _cellGap),
			_cellWidth,
			_cellHeight);

	private Vector2 ResolveUnitAnchor(GridPosition position)
	{
		var rect = ResolveCellRect(position);
		return new Vector2(
			rect.Position.X + rect.Size.X * 0.5f,
			rect.Position.Y + rect.Size.Y * 0.5f);
	}

	private BattleUnitView GetOrCreateUnitView(string unitId)
	{
		if (_unitViews.TryGetValue(unitId, out var existingView))
		{
			return existingView;
		}

		if (BattleUnitViewScene is null)
		{
			throw new InvalidOperationException("BattleUnitViewScene is not assigned.");
		}

		var instance = BattleUnitViewScene.Instantiate();
		if (instance is not BattleUnitView unitView)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle unit view scene root must be BattleUnitView.");
		}

		unitView.Name = $"Unit_{unitId}";
		_combatantLayer.AddChild(unitView);
		_unitViews[unitId] = unitView;
		return unitView;
	}

	private void PlayFloatTextAt(Vector2 position, string text, Color color, bool popup = false)
	{
		if (BattleFloatTextScene is null)
		{
			throw new InvalidOperationException("BattleFloatTextScene is not assigned.");
		}

		var instance = BattleFloatTextScene.Instantiate();
		if (instance is not BattleFloatText floatText)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Battle float text scene root must be BattleFloatText.");
		}

		floatText.Position = position;
		_effectLayer.AddChild(floatText);
		if (popup)
		{
			floatText.PlayPopup(text, color);
			return;
		}

		floatText.Play(text, color);
	}

	private async void ProcessFloatTextQueueAsync(string unitId)
	{
		try
		{
			while (_queuedFloatTexts.TryGetValue(unitId, out var queue) && queue.Count > 0)
			{
				var item = queue.Dequeue();
				if (_unitViews.TryGetValue(unitId, out var unitView))
				{
					PlayFloatTextAt(unitView.Position + new Vector2(0f, -90f), item.Text, item.Color);
				}

				await ToSignal(GetTree().CreateTimer(FloatTextQueueSpacingSeconds), SceneTreeTimer.SignalName.Timeout);
			}
		}
		finally
		{
			_processingFloatTextUnits.Remove(unitId);
			if (_queuedFloatTexts.TryGetValue(unitId, out var queue) && queue.Count == 0)
			{
				_queuedFloatTexts.Remove(unitId);
			}

			if (_queuedFloatTexts.TryGetValue(unitId, out var pendingQueue) &&
				pendingQueue.Count > 0 &&
				_processingFloatTextUnits.Add(unitId))
			{
				ProcessFloatTextQueueAsync(unitId);
			}
		}
	}

	private void DrawCellText(Font font, Rect2 rect, string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (lines.Length == 0)
		{
			return;
		}

		var lineHeight = CellTextFontSize + 6f;
		var totalHeight = lines.Length * lineHeight;
		var startY = rect.Position.Y + (rect.Size.Y - totalHeight) * 0.5f + CellTextFontSize;
		for (var index = 0; index < lines.Length; index++)
		{
			var line = lines[index];
			var size = font.GetStringSize(line, HorizontalAlignment.Left, -1, CellTextFontSize);
			var x = rect.Position.X + (rect.Size.X - size.X) * 0.5f;
			var y = startY + index * lineHeight;
			DrawString(font, new Vector2(x, y), line, HorizontalAlignment.Left, -1, CellTextFontSize, TextColor);
		}
	}
}

public sealed record BattleBoardCellVisual(
	GridPosition Position,
	string Label,
	Color Color,
	bool IsInteractive);

public sealed record BattleBoardUnitVisual(
	string UnitId,
	string Name,
	GridPosition Position,
	BattleFacing Facing,
	AnimationLibrary? AnimationLibrary,
	bool IsActing,
	bool IsAlive,
	bool IsPlayerUnit,
	int Hp,
	int MaxHp,
	int Mp,
	int MaxMp,
	int Rage,
	int ActionGauge,
	Texture2D? PortraitTexture,
	IReadOnlyList<BattleBoardBuffVisual> Buffs);

public sealed record BattleBoardBuffVisual(
	string Name,
	bool IsDebuff,
	int Level,
	int RemainingTurns);
