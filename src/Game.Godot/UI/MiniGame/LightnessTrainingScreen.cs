using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class LightnessTrainingScreen : Control
{
	private const string HeroPortraitId = "头像.主角";
	private const string EastPortraitId = "头像.东";
	private const string SouthPortraitId = "头像.南";
	private const string WestPortraitId = "头像.西";
	private const string NorthPortraitId = "头像.北";
	private const double SpeedUpIntervalSeconds = 0.2d;
	private const float SpeedUpMultiplier = 1.01f;
	private const float InitialObstacleSpeed = 300f;

	private readonly TaskCompletionSource<int> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly List<ObstacleState> _obstacles = [];
	private Control _playArea = null!;
	private TextureRect _hero = null!;
	private Label _timeLabel = null!;
	private Label _hintLabel = null!;
	private Button _startButton = null!;
	private bool _isRunning;
	private double _elapsedSeconds;
	private double _speedUpElapsedSeconds;

	public override void _Ready()
	{
		_playArea = GetNode<Control>("%PlayArea");
		_hero = GetNode<TextureRect>("%Hero");
		_timeLabel = GetNode<Label>("%TimeLabel");
		_hintLabel = GetNode<Label>("%HintLabel");
		_startButton = GetNode<Button>("%StartButton");

		_hero.Texture = AssetResolver.LoadTextureResource(HeroPortraitId);
		_obstacles.Add(CreateObstacle("%EastObstacle", EastPortraitId));
		_obstacles.Add(CreateObstacle("%SouthObstacle", SouthPortraitId));
		_obstacles.Add(CreateObstacle("%WestObstacle", WestPortraitId));
		_obstacles.Add(CreateObstacle("%NorthObstacle", NorthPortraitId));
		_startButton.Pressed += StartTraining;
		CallDeferred(MethodName.ResetPositions);
		RefreshTimeLabel();
	}

	public async Task<int> AwaitCompletionAsync(CancellationToken cancellationToken = default)
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

	public override void _Input(InputEvent @event)
	{
		if (!_isRunning)
		{
			return;
		}

		switch (@event)
		{
			case InputEventMouseMotion:
				MoveHeroTo(GetGlobalMousePosition());
				break;
			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseButton:
				MoveHeroTo(mouseButton.GlobalPosition);
				break;
		}
	}

	public override void _Process(double delta)
	{
		if (!_isRunning)
		{
			return;
		}

		_elapsedSeconds += delta;
		_speedUpElapsedSeconds += delta;
		if (_speedUpElapsedSeconds >= SpeedUpIntervalSeconds)
		{
			_speedUpElapsedSeconds = 0d;
			foreach (var obstacle in _obstacles)
			{
				obstacle.Velocity *= SpeedUpMultiplier;
			}
		}

		foreach (var obstacle in _obstacles)
		{
			MoveObstacle(obstacle, (float)delta);
		}

		RefreshTimeLabel();
		if (_obstacles.Any(IsCollidingWithHero))
		{
			FinishTraining();
		}
	}

	private ObstacleState CreateObstacle(string nodePath, string portraitId)
	{
		var obstacle = GetNode<TextureRect>(nodePath);
		obstacle.Texture = AssetResolver.LoadTextureResource(portraitId);
		return new ObstacleState(obstacle, Vector2.Zero);
	}

	private void StartTraining()
	{
		if (_isRunning)
		{
			return;
		}

		_isRunning = true;
		_elapsedSeconds = 0d;
		_speedUpElapsedSeconds = 0d;
		_startButton.Hide();
		_hintLabel.Text = "拖动头像，躲开四方来客。";
		MoveHeroTo(GetGlobalMousePosition());
	}

	private void ResetPositions()
	{
		var areaSize = _playArea.Size;
		CenterControl(_hero, areaSize * 0.5f);
		CenterControl(_obstacles[0].Node, new Vector2(areaSize.X * 0.125f, areaSize.Y * 0.25f));
		CenterControl(_obstacles[1].Node, new Vector2(areaSize.X * 0.745f, areaSize.Y * 0.765f));
		CenterControl(_obstacles[2].Node, new Vector2(areaSize.X * 0.815f, areaSize.Y * 0.25f));
		CenterControl(_obstacles[3].Node, new Vector2(areaSize.X * 0.165f, areaSize.Y * 0.765f));

		foreach (var obstacle in _obstacles)
		{
			obstacle.Velocity = CreateInitialVelocity();
		}
	}

	private void MoveHeroTo(Vector2 globalPosition)
	{
		var localPosition = globalPosition - _playArea.GlobalPosition;
		CenterControl(_hero, ClampCenterToPlayArea(localPosition, _hero.Size));
	}

	private void MoveObstacle(ObstacleState obstacle, float delta)
	{
		var nextPosition = obstacle.Node.Position + obstacle.Velocity * delta;
		var areaSize = _playArea.Size;
		if (nextPosition.X < 0f)
		{
			nextPosition.X = 0f;
			obstacle.Velocity = new Vector2(Math.Abs(obstacle.Velocity.X), obstacle.Velocity.Y);
		}
		else if (nextPosition.X + obstacle.Node.Size.X > areaSize.X)
		{
			nextPosition.X = areaSize.X - obstacle.Node.Size.X;
			obstacle.Velocity = new Vector2(-Math.Abs(obstacle.Velocity.X), obstacle.Velocity.Y);
		}

		if (nextPosition.Y < 0f)
		{
			nextPosition.Y = 0f;
			obstacle.Velocity = new Vector2(obstacle.Velocity.X, Math.Abs(obstacle.Velocity.Y));
		}
		else if (nextPosition.Y + obstacle.Node.Size.Y > areaSize.Y)
		{
			nextPosition.Y = areaSize.Y - obstacle.Node.Size.Y;
			obstacle.Velocity = new Vector2(obstacle.Velocity.X, -Math.Abs(obstacle.Velocity.Y));
		}

		obstacle.Node.Position = nextPosition;
	}

	private bool IsCollidingWithHero(ObstacleState obstacle) =>
		GetLocalRect(_hero).Intersects(GetLocalRect(obstacle.Node));

	private void FinishTraining()
	{
		if (!_isRunning)
		{
			return;
		}

		_isRunning = false;
		var survivedSeconds = Math.Max(0, (int)Math.Floor(_elapsedSeconds));
		if (_completion.TrySetResult(survivedSeconds))
		{
			QueueFree();
		}
	}

	private void RefreshTimeLabel()
	{
		var seconds = Math.Max(0, (int)Math.Floor(_elapsedSeconds));
		_timeLabel.Text = $"坚持时间：{seconds} 秒";
	}

	private Vector2 ClampCenterToPlayArea(Vector2 center, Vector2 size)
	{
		var halfSize = size * 0.5f;
		return new Vector2(
			Math.Clamp(center.X, halfSize.X, _playArea.Size.X - halfSize.X),
			Math.Clamp(center.Y, halfSize.Y, _playArea.Size.Y - halfSize.Y));
	}

	private static void CenterControl(Control control, Vector2 center)
	{
		control.Position = center - control.Size * 0.5f;
	}

	private static Rect2 GetLocalRect(Control control) => new(control.Position, control.Size);

	private static Vector2 CreateInitialVelocity()
	{
		var x = (float)GD.RandRange(InitialObstacleSpeed * 0.2d, InitialObstacleSpeed * 0.8d);
		if (GD.Randf() <= 0.5f)
		{
			x = -x;
		}

		var yMagnitude = MathF.Sqrt(InitialObstacleSpeed * InitialObstacleSpeed - x * x);
		var y = GD.Randf() > 0.5f ? yMagnitude : -yMagnitude;
		return new Vector2(x, y);
	}

	private sealed class ObstacleState
	{
		public ObstacleState(TextureRect node, Vector2 velocity)
		{
			Node = node;
			Velocity = velocity;
		}

		public TextureRect Node { get; }

		public Vector2 Velocity { get; set; }
	}
}
