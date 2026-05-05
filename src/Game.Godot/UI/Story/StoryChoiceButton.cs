using Game.Godot.UI;
using Godot;

namespace Game.Godot.UI.Story;

public partial class StoryChoiceButton : JyButton
{
	private RichTextLabel _label = null!;
	private string _text = string.Empty;

	public override void _Ready()
	{
		base._Ready();
		_label = GetNode<RichTextLabel>("%ChoiceLabel");
		Sync();
	}

	public void Configure(string? text)
	{
		_text = text ?? string.Empty;
		Sync();
	}

	private void Sync()
	{
		if (!IsInsideTree())
		{
			return;
		}

		_label.Text = _text;
	}
}
