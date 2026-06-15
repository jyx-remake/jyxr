using Game.Core.Battle;
using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleBoardView : Control
{
	private const float DesignCellSize = 144f;
	private const int CellTextFontSize = 13;
	private const double StepMoveDurationSeconds = 0.3d;
	private const double FloatTextQueueInitialDelaySeconds = 0.1d;
	private const double FloatTextQueueSpacingSeconds = 0.4d;
	private const float FloatTextHeadOffsetY = 90f;
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
	private int _baseCellGap;
	private float _cellSize = DesignCellSize;
	private float _cellGap;
	private float _unitVisualScale = 1f;
	private Vector2 _contentOrigin;
	private Vector2 _contentSize;
	private GridPosition? _hoveredCell;
	private Node2D _combatantLayer = null!;
	private Node2D _effectLayer = null!;
	private readonly Dictionary<string, GridPosition> _unitPositions = new(StringComparer.Ordinal);

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
		int cellGap,
		IReadOnlyList<BattleBoardCellVisual> cells)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(gridWidth, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(gridHeight, 1);
		ArgumentNullException.ThrowIfNull(cells);

		_gridWidth = gridWidth;
		_gridHeight = gridHeight;
		_baseCellGap = Math.Max(0, cellGap);

		_cells.Clear();
		foreach (var cell in cells)
		{
			_cells[cell.Position] = cell;
		}

		CustomMinimumSize = Vector2.Zero;
		UpdateLayoutMetrics();
		RelayoutUnitViews();
		QueueRedraw();
	}

	public void RenderUnits(IReadOnlyList<BattleBoardUnitVisual> units)
	{
		ArgumentNullException.ThrowIfNull(units);

		var activeUnitIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var unit in units)
		{
			activeUnitIds.Add(unit.UnitId);
			_unitPositions[unit.UnitId] = unit.Position;
			var isExistingView = _unitViews.TryGetValue(unit.UnitId, out var unitView);
			unitView ??= GetOrCreateUnitView(unit.UnitId);
			unitView.Configure(unit);
			unitView.Scale = Vector2.One * _unitVisualScale;

			var targetPosition = GridPositionToUnitAnchor(unit.Position);
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
			_unitPositions.Remove(staleUnitId);
		}
	}

	public async Task PlayUnitMoveAsync(
		string unitId,
		IReadOnlyList<GridPosition> path,
		BattleMovementPresentationMode mode)
	{
		if (!_unitViews.TryGetValue(unitId, out var unitView) || path.Count == 0)
		{
			return;
		}

		unitView.PlayMoveLoop();
		foreach (var position in mode == BattleMovementPresentationMode.Instant ? [path[^1]] : path)
		{
			var targetPosition = GridPositionToUnitAnchor(position);
			if (targetPosition.X < unitView.Position.X)
			{
				unitView.ApplyFacing(BattleFacing.Left);
			}
			else if (targetPosition.X > unitView.Position.X)
			{
				unitView.ApplyFacing(BattleFacing.Right);
			}

			if (mode == BattleMovementPresentationMode.Instant)
			{
				unitView.Position = targetPosition;
				break;
			}

			var tween = CreateTween();
			tween.TweenProperty(unitView, "position", targetPosition, StepMoveDurationSeconds)
				.SetTrans(Tween.TransitionType.Linear)
				.SetEase(Tween.EaseType.InOut);
			await ToSignal(tween, Tween.SignalName.Finished);
		}

		unitView.PlayIdle();
	}

	public void PlayAttack(string actingUnitId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(actingUnitId);

		if (_unitViews.TryGetValue(actingUnitId, out var actingView))
		{
			actingView.PlayAttack();
		}
	}

	public void PlayHit(string unitId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

		if (_unitViews.TryGetValue(unitId, out var unitView))
		{
			unitView.PlayHit();
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

	public async Task PlaySkillImpactAsync(
		IReadOnlyList<GridPosition> impactPositions,
		string? skillAnimationId)
	{
		ArgumentNullException.ThrowIfNull(impactPositions);

		Task? firstImpactTask = null;
		foreach (var impactPosition in impactPositions)
		{
			var impactTask = PlaySkillAnimationAtAsync(GridPositionToUnitAnchor(impactPosition), skillAnimationId);
			if (firstImpactTask is null)
			{
				firstImpactTask = impactTask;
			}
			else
			{
				_ = impactTask;
			}
		}

		if (firstImpactTask is not null)
		{
			await firstImpactTask;
		}
	}

	public Task PlaySkillAnimationAtAsync(Vector2 position, string? skillAnimationId)
	{
		var skillAnimation = AssetResolver.LoadSkillAnimation(skillAnimationId);
		if (skillAnimation is null)
		{
			return Task.CompletedTask;
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
		effectView.Scale = Vector2.One * _unitVisualScale;
		_effectLayer.AddChild(effectView);
		return effectView.PlayAsync(skillAnimation);
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

		PlayFloatTextAt(_contentOrigin + _contentSize * 0.5f, text, color, popup: true);
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

				var rect = GridPositionToCellRect(position);
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
				if (TryGetGridPositionAt(button.Position, out var position) &&
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
		if (what == NotificationResized)
		{
			UpdateLayoutMetrics();
			RelayoutUnitViews();
			QueueRedraw();
		}

		if (what == NotificationMouseExit && _hoveredCell is not null)
		{
			_hoveredCell = null;
			HoveredCellChanged?.Invoke(null);
		}
	}

	public void RefreshLayout()
	{
		UpdateLayoutMetrics();
		RelayoutUnitViews();
		QueueRedraw();
	}

	private void UpdateHoveredCell(Vector2 mousePosition)
	{
		GridPosition? hovered = TryGetGridPositionAt(mousePosition, out var position) &&
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

	private bool TryGetGridPositionAt(Vector2 point, out GridPosition position)
	{
		if (_cellSize <= 0f)
		{
			position = default;
			return false;
		}

		var localPoint = point - _contentOrigin;
		if (localPoint.X < 0f || localPoint.Y < 0f || localPoint.X > _contentSize.X || localPoint.Y > _contentSize.Y)
		{
			position = default;
			return false;
		}

		var stride = _cellSize + _cellGap;
		var x = Mathf.FloorToInt(localPoint.X / stride);
		var y = Mathf.FloorToInt(localPoint.Y / stride);
		if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
		{
			position = default;
			return false;
		}

		var localX = localPoint.X - x * stride;
		var localY = localPoint.Y - y * stride;
		if (localX > _cellSize || localY > _cellSize)
		{
			position = default;
			return false;
		}

		position = new GridPosition(x, y);
		return true;
	}

	private Rect2 GridPositionToCellRect(GridPosition position) =>
		new(
			_contentOrigin.X + position.X * (_cellSize + _cellGap),
			_contentOrigin.Y + position.Y * (_cellSize + _cellGap),
			_cellSize,
			_cellSize);

	private Vector2 GridPositionToUnitAnchor(GridPosition position)
	{
		var rect = GridPositionToCellRect(position);
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
		unitView.Scale = Vector2.One * _unitVisualScale;
		_combatantLayer.AddChild(unitView);
		_unitViews[unitId] = unitView;
		return unitView;
	}

	private void UpdateLayoutMetrics()
	{
		if (_gridWidth <= 0 || _gridHeight <= 0 || Size.X <= 0f || Size.Y <= 0f)
		{
			_cellSize = DesignCellSize;
			_cellGap = _baseCellGap;
			_unitVisualScale = 1f;
			_contentOrigin = Vector2.Zero;
			_contentSize = Vector2.Zero;
			return;
		}

		var gapRatio = _baseCellGap / DesignCellSize;
		var widthUnits = _gridWidth + Math.Max(0, _gridWidth - 1) * gapRatio;
		var heightUnits = _gridHeight + Math.Max(0, _gridHeight - 1) * gapRatio;
		var nextCellSize = MathF.Floor(MathF.Min(Size.X / widthUnits, Size.Y / heightUnits));
		_cellSize = MathF.Max(1f, nextCellSize);
		_cellGap = MathF.Max(0f, MathF.Round(_baseCellGap * _cellSize / DesignCellSize));
		_contentSize = new Vector2(
			_gridWidth * _cellSize + Math.Max(0, _gridWidth - 1) * _cellGap,
			_gridHeight * _cellSize + Math.Max(0, _gridHeight - 1) * _cellGap);
		_contentOrigin = (Size - _contentSize) * 0.5f;
		_unitVisualScale = Math.Clamp(_cellSize / DesignCellSize, 0.55f, 1.1f);
	}

	private void RelayoutUnitViews()
	{
		foreach (var (unitId, unitView) in _unitViews)
		{
			if (!_unitPositions.TryGetValue(unitId, out var position))
			{
				continue;
			}

			unitView.Position = GridPositionToUnitAnchor(position);
			unitView.Scale = Vector2.One * _unitVisualScale;
		}
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
		floatText.PresentationScale = _unitVisualScale;
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
			await ToSignal(GetTree().CreateTimer(FloatTextQueueInitialDelaySeconds), SceneTreeTimer.SignalName.Timeout);
			while (_queuedFloatTexts.TryGetValue(unitId, out var queue) && queue.Count > 0)
			{
				var item = queue.Dequeue();
				if (_unitViews.TryGetValue(unitId, out var unitView))
				{
					PlayFloatTextAt(unitView.Position + new Vector2(0f, -FloatTextHeadOffsetY * _unitVisualScale), item.Text, item.Color);
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

public enum BattleMovementPresentationMode
{
	Instant,
	Step,
}
