using Game.Godot.Resources;
using Godot;

namespace Game.Godot.UI;

public partial class SelectHeadSlot : JyButton
{
	private TextureRect _textureRect = null!;
	private TextureRect _tickMark = null!;

	public string PortraitId { get; private set; } = string.Empty;

	public override void _Ready()
	{
		_textureRect = GetNode<TextureRect>("%TextureRect");
		_tickMark = GetNode<TextureRect>("%TickMark");
	}

	public void Setup(string portraitId, bool selected)
	{
		PortraitId = portraitId;
		Modulate = Colors.White;
		_textureRect.Texture = AssetResolver.LoadTexture(portraitId);
		_tickMark.Visible = selected;
	}
}
