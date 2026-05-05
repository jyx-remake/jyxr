using Game.Core.Definitions;
using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

[GlobalClass]
public partial class AvatarBox : Control
{
	private TextureRect _avatar = null!;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
	}

	public void SetAvatarTexture(Texture2D? texture)
	{
		_avatar.Texture = texture;
	}
}
