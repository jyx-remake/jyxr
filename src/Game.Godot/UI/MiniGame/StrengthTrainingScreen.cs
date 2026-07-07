using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class StrengthTrainingScreen : Control
{
	private const string BadTargetPortraitId = "头像.东";
	private const string FourPointTargetPortraitId = "头像.西";
	private const string TwoPointTargetPortraitId = "头像.南";
	private const string ThreePointTargetPortraitId = "头像.北";
	private const string HitEffectTextureId = "物品.点击爆炸";
	private const string MaleHitSfxId = "音效.男惨叫";
	private const string BadHitSfxId = "音效.敢点老娘";
	private const double DurationSeconds = 30d;
	private const double TargetVisibleSeconds = 0.7d;
	private const double HitEffectVisibleSeconds = 0.5d;

	private readonly TaskCompletionSource<(int Score, IReadOnlyDictionary<string, int> ItemCounts)> _completion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly List<TargetState> _targets = [];
	private readonly Dictionary<string, int> _itemCounts = new(StringComparer.Ordinal);
	private Control _playArea = null!;
	private TextureRect _hitEffect = null!;
	private Label _timeLabel = null!;
	private Label _scoreLabel = null!;
	private IReadOnlyList<string> _itemCandidates = [];
	private bool _isRunning;
	private double _elapsedSeconds;
	private double _hitEffectHideAtSeconds;
	private int _score;

	public void Configure(IReadOnlyList<string> itemCandidates)
	{
		ArgumentNullException.ThrowIfNull(itemCandidates);
		_itemCandidates = itemCandidates
			.Where(static itemId => !string.IsNullOrWhiteSpace(itemId))
			.Select(static itemId => itemId.Trim())
			.Distinct(StringComparer.Ordinal)
			.ToArray();
	}

	public override void _Ready()
	{
		_playArea = GetNode<Control>("%PlayArea");
		_hitEffect = GetNode<TextureRect>("%HitEffect");
		_timeLabel = GetNode<Label>("%TimeLabel");
		_scoreLabel = GetNode<Label>("%ScoreLabel");
		_hitEffect.Texture = AssetResolver.LoadTextureResource(HitEffectTextureId);
		_hitEffect.Hide();

		_targets.Add(CreatePointTarget("%BadTarget", -2, BadHitSfxId, BadTargetPortraitId));
		_targets.Add(CreatePointTarget("%FourPointTarget", 4, MaleHitSfxId, FourPointTargetPortraitId));
		_targets.Add(CreatePointTarget("%TwoPointTarget", 2, MaleHitSfxId, TwoPointTargetPortraitId));
		_targets.Add(CreatePointTarget("%ThreePointTarget", 3, MaleHitSfxId, ThreePointTargetPortraitId));
		_targets.Add(CreateItemTarget("%ItemTarget"));

		_isRunning = true;
		foreach (var target in _targets)
		{
			HideAndSchedule(target, isInitialSchedule: true);
		}

		RefreshLabels();
	}

	public async Task<(int Score, IReadOnlyDictionary<string, int> ItemCounts)> AwaitCompletionAsync(
		CancellationToken cancellationToken = default)
	{
		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_completion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		return await _completion.Task;
	}

	public override void _ExitTree()
	{
		if (!_completion.Task.IsCompleted)
		{
			_completion.TrySetCanceled();
		}
	}

	public override void _Process(double delta)
	{
		if (!_isRunning)
		{
			return;
		}

		_elapsedSeconds += delta;
		if (_hitEffect.Visible && _elapsedSeconds >= _hitEffectHideAtSeconds)
		{
			_hitEffect.Hide();
		}

		if (_elapsedSeconds >= DurationSeconds)
		{
			FinishTraining();
			return;
		}

		foreach (var target in _targets)
		{
			if (target.Node.Visible)
			{
				if (_elapsedSeconds >= target.HideAtSeconds)
				{
					HideAndSchedule(target);
				}

				continue;
			}

			if (_elapsedSeconds >= target.ShowAtSeconds)
			{
				ShowTarget(target);
			}
		}

		RefreshLabels();
	}

	private TargetState CreatePointTarget(string nodePath, int point, string audioId, string portraitId)
	{
		var node = GetNode<TextureButton>(nodePath);
		node.TextureNormal = AssetResolver.LoadTextureResource(portraitId);
		var target = new TargetState(node, point, audioId, IsItem: false);
		node.Pressed += () => HitTarget(target);
		return target;
	}

	private TargetState CreateItemTarget(string nodePath)
	{
		var node = GetNode<TextureButton>(nodePath);
		var target = new TargetState(node, 0, null, IsItem: true);
		node.Pressed += () => HitTarget(target);
		return target;
	}

	private void ShowTarget(TargetState target)
	{
		if (target.IsItem)
		{
			if (_itemCandidates.Count == 0)
			{
				HideAndSchedule(target);
				return;
			}

			target.ActiveItemId = _itemCandidates[Random.Shared.Next(_itemCandidates.Count)];
			target.Node.TextureNormal = ResolveItemTexture(target.ActiveItemId);
		}

		PlaceTarget(target.Node);
		target.HideAtSeconds = _elapsedSeconds + TargetVisibleSeconds;
		target.Node.Show();
	}

	private void HitTarget(TargetState target)
	{
		if (!_isRunning || !target.Node.Visible)
		{
			return;
		}

		if (target.IsItem)
		{
			if (!string.IsNullOrWhiteSpace(target.ActiveItemId))
			{
				_itemCounts[target.ActiveItemId] = _itemCounts.GetValueOrDefault(target.ActiveItemId) + 1;
			}
		}
		else
		{
			_score = checked(_score + target.Point);
			Game.Audio.PlaySfx(target.AudioId);
			ShowHitEffect(target.Node);
		}

		HideAndSchedule(target);

		RefreshLabels();
	}

	private void ShowHitEffect(Control target)
	{
		_hitEffect.Position = target.Position + target.Size * 0.5f - _hitEffect.Size * 0.5f;
		_hitEffectHideAtSeconds = _elapsedSeconds + HitEffectVisibleSeconds;
		_hitEffect.Show();
	}

	private void HideAndSchedule(TargetState target, bool isInitialSchedule = false)
	{
		target.Node.Hide();
		target.ActiveItemId = null;
		var minDelay = target.IsItem ? 4d : 0.7d;
		var maxDelay = target.IsItem ? 10d : isInitialSchedule ? 3d : 5d;
		target.ShowAtSeconds = _elapsedSeconds + GD.RandRange(minDelay, maxDelay);
	}

	private void PlaceTarget(Control target)
	{
		var areaSize = _playArea.Size;
		var maxX = Math.Max(0f, areaSize.X - target.Size.X);
		var maxY = Math.Max(0f, areaSize.Y - target.Size.Y);
		target.Position = new Vector2(
			(float)GD.RandRange(0d, maxX),
			(float)GD.RandRange(72d, Math.Max(72f, maxY)));
	}

	private void FinishTraining()
	{
		if (!_isRunning)
		{
			return;
		}

		_isRunning = false;
		foreach (var target in _targets)
		{
			target.Node.Hide();
		}

		if (_completion.TrySetResult((_score, _itemCounts.ToDictionary())))
		{
			QueueFree();
		}
	}

	private void RefreshLabels()
	{
		var remainingSeconds = Math.Max(0, (int)Math.Ceiling(DurationSeconds - _elapsedSeconds));
		_timeLabel.Text = $"剩余时间：{remainingSeconds} 秒";
		_scoreLabel.Text = $"点穴得分：{_score}";
	}

	private static Texture2D? ResolveItemTexture(string itemId)
	{
		if (!Game.ContentRepository.TryGetItem(itemId, out var item))
		{
			return null;
		}

		return AssetResolver.LoadTextureResource(item.Picture);
	}

	private sealed class TargetState
	{
		public TargetState(TextureButton node, int point, string? audioId, bool IsItem)
		{
			Node = node;
			Point = point;
			AudioId = audioId;
			this.IsItem = IsItem;
		}

		public TextureButton Node { get; }

		public int Point { get; }

		public string? AudioId { get; }

		public bool IsItem { get; }

		public double ShowAtSeconds { get; set; }

		public double HideAtSeconds { get; set; }

		public string? ActiveItemId { get; set; }
	}
}
