using Godot;

namespace Game.Godot.UI;

public partial class InventoryTagButton : Button
{
	public void Configure(string displayName, bool selected, Action pressed)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
		ArgumentNullException.ThrowIfNull(pressed);

		Text = displayName;
		Disabled = selected;
		Modulate = selected ? new Color(1.0f, 0.92f, 0.68f) : Colors.White;
		Pressed += pressed;
	}
}
