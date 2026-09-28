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
			var root = ProjectDataRootResolver.Resolve();
			var mod = new ModRegistry(root).LoadRequired("jyxr-base");
			GameRuntimeBootstrap.Initialize(new ModLoadout(mod, []), GetTree());
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
			World.Instance.EnterMap("南贤屋内");
		}
		catch (Exception exception)
		{
			GD.PushError(exception.ToString());
		}
	}
}
