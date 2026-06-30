using Godot;
using Game.Application.Mods;

namespace Game.Godot.UI;

public partial class PreviewRoot : Control
{
	
	public override void _Ready()
	{ 
		InitializeAndOpenMap();
	}

	private void InitializeAndOpenMap()
	{
		try
		{
			var root = ProjectDataRoot.FromPath(ProjectSettings.GlobalizePath("res://"));
			var mod = new ModRegistry(root).LoadRequired("jyxr-base");
			GameRuntimeBootstrap.Initialize(mod, GetTree());
			OpenMap();
		}
		catch (Exception exception)
		{
			GD.PushError(exception.ToString());
		}
	}

	private void OpenMap()
	{
		try
		{
			World.Instance.ShowMap("南贤屋内");
		}
		catch (Exception exception)
		{
			GD.PushError(exception.ToString());
		}
	}
}
