using Godot;

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
			PreviewGameBootstrap.Initialize();
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
			var world = GetNode<World>("/root/World");
			world.ShowMap("南贤屋内");
		}
		catch (Exception exception)
		{
			GD.PushError(exception.ToString());
		}
	}
}
