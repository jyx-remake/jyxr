using Godot;

namespace Game.Godot.UI;

public partial class SelectHeadSlot : JyButton
{
	private TextureRect _textureRect = null!;
	private TextureRect _tickMark = null!;

	public override void _Ready()
	{
		_textureRect = GetNode<TextureRect>("%TextureRect");
		_tickMark = GetNode<TextureRect>("%TickMark");
	}

	public void SetTexture(Texture2D? texture)
	{
		_textureRect.Texture = texture;
	}

	public void SetSelected(bool selected)
	{
		_tickMark.Visible = selected;
	}
}
