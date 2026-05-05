using Game.Application.Formatters;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterBiographyTab : Control
{
	private RichTextLabel _biographyLabel = null!;

	public override void _Ready()
	{
		_biographyLabel = GetNode<RichTextLabel>("%BiographyLabel");
	}

	public void Setup(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);
		_biographyLabel.Text = CharacterBiographyFormatter.FormatCn(character, Game.ContentRepository);
	}
}
