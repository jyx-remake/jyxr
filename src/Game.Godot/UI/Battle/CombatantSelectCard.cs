using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class CombatantSelectCard : Button
{
	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _levelLabel = null!;
	private Label _attackLabel = null!;
	private Label _defenceLabel = null!;
	private TextureRect _maleLogo = null!;
	private TextureRect _femaleLogo = null!;
	private TextureRect _selectedMark = null!;

	private CharacterInstance? _character;
	private bool _isRequired;
	private bool _isSelected;

	public event Action<string>? SelectionRequested;

	public string CharacterId => _character?.Id ?? string.Empty;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_levelLabel = GetNode<Label>("%LevelLabel");
		_attackLabel = GetNode<Label>("%AttackLabel");
		_defenceLabel = GetNode<Label>("%DefenceLabel");
		_maleLogo = GetNode<TextureRect>("%MaleLogo");
		_femaleLogo = GetNode<TextureRect>("%FemaleLogo");
		_selectedMark = GetNode<TextureRect>("%SelectedMark");

		Pressed += OnPressed;
		Refresh();
	}

	public void Setup(CharacterInstance character, bool isSelected, bool isRequired)
	{
		ArgumentNullException.ThrowIfNull(character);
		_character = character;
		_isSelected = isSelected;
		_isRequired = isRequired;
		Refresh();
	}

	public void SetSelected(bool selected)
	{
		_isSelected = selected;
		Refresh();
	}

	private void OnPressed()
	{
		if (_character is null || _isRequired)
		{
			return;
		}

		SelectionRequested?.Invoke(_character.Id);
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _character is null)
		{
			return;
		}

		_nameLabel.Text = _character.Name;
		_levelLabel.Text = $"等级:{_character.Level}";
		_attackLabel.Text = $"攻:{ToDisplayStat(_character.GetStat(StatType.Attack))}";
		_defenceLabel.Text = $"防:{ToDisplayStat(_character.GetStat(StatType.Defence))}";
		_selectedMark.Visible = _isSelected;
		TooltipText = _isRequired ? "剧情要求出战" : _character.Name;

		var portrait = AssetResolver.LoadCharacterPortrait(_character);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}

		_maleLogo.Visible = _character.Definition.Gender == CharacterGender.Male;
		_femaleLogo.Visible = _character.Definition.Gender == CharacterGender.Female;
	}

	private static int ToDisplayStat(double value) => Mathf.RoundToInt(value);
}
