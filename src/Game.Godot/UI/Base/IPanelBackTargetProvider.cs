using Godot;

namespace Game.Godot.UI;

public interface IPanelBackTargetProvider
{
	Control? BackTarget { get; }
}
