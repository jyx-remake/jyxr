using Game.Application;
using Game.Godot.Assets;
using Game.Godot.Map;
using Game.Godot.UI;
using Godot;

namespace Game.Godot;

public partial class World : Control
{
	public static World Instance { get; private set; } = null!;
	
	[Export]
	public PackedScene MapScreenScene { get; set; } = null!;

	private Vector2 _basePosition;
	private TextureRect _background = null!;

	public Control? CurrentScene { get; private set; }

	public override void _Ready()
	{
		_basePosition = Position;
		_background = GetNode<TextureRect>("%Background");
		Instance = this;
	}

	public MapScreen ShowMap(string mapId)
	{
		var result = Game.MapService.EnterMap(mapId);
		return ShowMap(result);
	}

	public MapScreen EnterMap(string mapId) =>
		ShowMap(Game.MapService.EnterMap(mapId));

	public void ShowStoryAnimation(string animationId)
	{
		if (string.IsNullOrWhiteSpace(animationId))
		{
			throw new ArgumentException("Animation id cannot be empty.", nameof(animationId));
		}

		Game.Logger.Info($"Story animation requested: {animationId}");
	}

	public void SetBackground(string? resourceId)
	{
		_background.Texture = AssetResolver.LoadTextureResource(resourceId);
		_background.Visible = _background.Texture is not null;
	}

	public void PlayScreenShake(float amplitude = 12f, double durationSeconds = 0.22d)
	{
		if (MapScreenScene is null)
		{
			throw new InvalidOperationException("World map screen scene is not assigned.");
		}

		var tween = CreateTween();
		tween.SetParallel(false);
		tween.TweenProperty(this, "position", _basePosition + new Vector2(amplitude, 0f), durationSeconds * 0.25d);
		tween.TweenProperty(this, "position", _basePosition + new Vector2(-amplitude, 0f), durationSeconds * 0.25d);
		tween.TweenProperty(this, "position", _basePosition + new Vector2(amplitude * 0.5f, 0f), durationSeconds * 0.2d);
		tween.TweenProperty(this, "position", _basePosition, durationSeconds * 0.3d);
	}

	private MapScreen ShowMap(MapEnterResult result)
	{
		var instance = MapScreenScene.Instantiate();
		if (instance is not MapScreen mapScreen)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Map screen scene root must be MapScreen.");
		}

		mapScreen.InitialMapId = result.Map.Id;
		mapScreen.Initialize(result);
		ReplaceCurrentScene(mapScreen);
		return mapScreen;
	}

	public MapScreen RefreshCurrentMap() =>
		ShowMap(Game.State.Location.CurrentMapId);

	private void ReplaceCurrentScene(Control scene)
	{
		CurrentScene?.QueueFree();
		CurrentScene = scene;
		AddChild(scene);

		if (scene is MapScreen mapScreen && UIRoot.Instance is not null)
		{
			mapScreen.SetStoryPresentationActive(UIRoot.Instance.IsStoryPresentationActive);
		}
	}
}
