using Game.Application;
using Game.Godot.Resources;
using Game.Godot.Map;
using Game.Godot.Persistence;
using Game.Godot.UI;
using Godot;

namespace Game.Godot;

public partial class World : Control
{
	public static World Instance { get; private set; } = null!;
	
	[Export]
	public PackedScene MapScreenScene { get; set; } = null!;

	private TextureRect _background = null!;

	public Control? CurrentScene { get; private set; }

	public AutoSaveCoordinator AutoSave { get; private set; } = null!;
	public PlayTimeCoordinator PlayTime { get; private set; } = null!;
	public ProfilePersistenceCoordinator Persistence { get; private set; } = null!;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("%Background");
		AutoSave = GetNode<AutoSaveCoordinator>("%AutoSaveCoordinator");
		PlayTime = GetNode<PlayTimeCoordinator>("%PlayTimeCoordinator");
		Instance = this;
		Persistence = new ProfilePersistenceCoordinator();
		AddChild(Persistence);
	}

	public MapScreen EnterMap(string mapId) =>
		ShowMap(Game.MapService.EnterMap(mapId));

	public MapScreen EnterMap(string mapId, string locationId) =>
		ShowMap(Game.MapService.EnterMap(mapId, locationId));

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
		_background.Texture = AssetResolver.LoadTexture(resourceId);
		_background.Visible = _background.Texture is not null;
	}

	private MapScreen ShowMap(MapEnterResult result)
	{
		var instance = MapScreenScene.Instantiate();
		if (instance is not MapScreen mapScreen)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Map screen scene root must be MapScreen.");
		}

		mapScreen.Initialize(result);
		ReplaceCurrentScene(mapScreen);
		return mapScreen;
	}

	public MapScreen RefreshCurrentMap()
	{
		var result = Game.MapService.GetCurrentMap();
		if (CurrentScene is MapScreen mapScreen)
		{
			mapScreen.Refresh(result);
			return mapScreen;
		}

		return ShowMap(result);
	}

	public MapScreen RestoreCurrentMap() => ShowMap(Game.MapService.GetCurrentMap());

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
