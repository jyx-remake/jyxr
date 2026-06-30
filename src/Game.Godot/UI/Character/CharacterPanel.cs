using Game.Application;
using Game.Application.Formatters;
using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterPanel : JyPanel
{
	private string _characterId = string.Empty;
	private CharacterPanelMode _mode = CharacterPanelMode.Editable;
	private CharacterInstance? _character;
	private BattleUnit? _battleUnit;

	public string CharacterId
	{
		get => _characterId;
		set
		{
			_characterId = value;
			_character = null;
			_battleUnit = null;
			_mode = CharacterPanelMode.Editable;
			if (IsInsideTree())
			{
				Render();
			}
		}
	}

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _levelValueLabel = null!;
	private Label _hpValueLabel = null!;
	private Label _mpValueLabel = null!;
	private Label _xpValueLabel = null!;
	private Label _attackValueLabel = null!;
	private Label _defenceValueLabel = null!;
	private TabContainer _tabContainer = null!;
	private CharacterAttributeTab _attributeTab = null!;
	private SkillTab _skillTab = null!;
	private CharacterTalentTab _talentTab = null!;
	private CharacterBiographyTab _biographyTab = null!;
	private CharacterEquipmentTab _equipmentTab = null!;
	private JyButton _attrButton = null!;
	private JyButton _equipButton = null!;
	private JyButton _talentButton = null!;
	private JyButton _skillButton = null!;
	private JyButton _biographyButton = null!;
	private readonly List<IDisposable> _subscriptions = [];

	public override void _Ready()
	{
		base._Ready();
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_levelValueLabel = GetNode<Label>("%LevelValueLabel");
		_hpValueLabel = GetNode<Label>("%HpValueLabel");
		_mpValueLabel = GetNode<Label>("%MpValueLabel");
		_xpValueLabel = GetNode<Label>("%XpValueLabel");
		_attackValueLabel = GetNode<Label>("%AttackValueLabel");
		_defenceValueLabel = GetNode<Label>("%DefenceValueLabel");
		_tabContainer = GetNode<TabContainer>("%TabContainer");
		_attributeTab = GetNode<CharacterAttributeTab>("%AttributeTab");
		_skillTab = GetNode<SkillTab>("%SkillTab");
		_talentTab = GetNode<CharacterTalentTab>("%TalentTab");
		_biographyTab = GetNode<CharacterBiographyTab>("%BiographyTab");
		_equipmentTab = GetNode<CharacterEquipmentTab>("%EquipmentTab");
		_attrButton = GetNode<JyButton>("%AttrButton");
		_equipButton = GetNode<JyButton>("%EquipButton");
		_talentButton = GetNode<JyButton>("%TalentButton");
		_skillButton = GetNode<JyButton>("%SkillButton");
		_biographyButton = GetNode<JyButton>("%BiographyButton");
		_attrButton.Pressed += () => ShowTab(0);
		_equipButton.Pressed += () => ShowTab(1);
		_skillButton.Pressed += () => ShowTab(2);
		_talentButton.Pressed += () => ShowTab(3);
		_biographyButton.Pressed += () => ShowTab(4);
		_skillTab.SkillToggleRequested += OnSkillToggleRequested;
		_skillTab.SkillDetailRequested += OnSkillDetailRequested;
		_subscriptions.Add(Game.Session.Events.Subscribe<CharacterChangedEvent>(OnCharacterChanged));

		if (!string.IsNullOrWhiteSpace(CharacterId))
		{
			Render();
		}
	}

	public void Configure(
		CharacterInstance character,
		CharacterPanelMode mode = CharacterPanelMode.Editable,
		BattleUnit? battleUnit = null)
	{
		ArgumentNullException.ThrowIfNull(character);
		_character = character;
		_characterId = character.Id;
		_mode = mode;
		_battleUnit = battleUnit;
		if (IsInsideTree())
		{
			Render();
		}
	}

	public override void _ExitTree()
	{
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
	}

	private void Render()
	{
		var character = ResolveCharacter();
		if (character is null)
		{
			throw new InvalidOperationException("CharacterPanel.CharacterId is required.");
		}

		var portrait = AssetResolver.LoadCharacterPortrait(character);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}

		_nameLabel.Text = character.Name;
		_levelValueLabel.Text = character.Level.ToString();
		_hpValueLabel.Text = _battleUnit is null
			? ToDisplayStat(character.GetStat(StatType.MaxHp)).ToString()
			: $"{_battleUnit.Hp}/{_battleUnit.MaxHp}";
		_mpValueLabel.Text = _battleUnit is null
			? ToDisplayStat(character.GetStat(StatType.MaxMp)).ToString()
			: $"{_battleUnit.Mp}/{_battleUnit.MaxMp}";
		_xpValueLabel.Text = character.Level >= Game.Config.MaxLevel
			? "-/-"
			: FormatExperienceProgress(character);
		var combatStats = CharacterCombatStatFormatter.Calculate(character);
		_attackValueLabel.Text = combatStats.Attack.ToString();
		_defenceValueLabel.Text = combatStats.Defence.ToString();

		var isReadOnly = _mode == CharacterPanelMode.ReadOnly;
		_attributeTab.IsReadOnly = isReadOnly;
		_equipmentTab.IsReadOnly = isReadOnly;
		_skillTab.IsReadOnly = isReadOnly;
		_attributeTab.Setup(character);
		_equipmentTab.Setup(character);
		_skillTab.Setup(character);
		_talentTab.Setup(character);
		_biographyTab.Setup(character);
		ShowTab(_tabContainer.CurrentTab);
	}

	private void ShowTab(int index)
	{
		_tabContainer.CurrentTab = index;
	}

	private void OnSkillToggleRequested(SkillInstance skill)
	{
		if (_mode == CharacterPanelMode.ReadOnly)
		{
			return;
		}

		switch (skill)
		{
			case ExternalSkillInstance externalSkill:
				Game.CharacterService.SetExternalSkillActive(CharacterId, externalSkill.Id, !externalSkill.IsActive);
				break;
			case SpecialSkillInstance specialSkill:
				Game.CharacterService.SetSpecialSkillActive(CharacterId, specialSkill.Id, !specialSkill.IsActive);
				break;
			case InternalSkillInstance internalSkill when !internalSkill.IsEquipped:
				Game.CharacterService.EquipInternalSkill(CharacterId, internalSkill.Id);
				break;
		}
	}

	private void OnSkillDetailRequested(SkillInstance skill)
	{
		var action = _mode == CharacterPanelMode.ReadOnly
			? null
			: CharacterSkillDetailActionFactory.CreateForgetAction(skill);
		UIRoot.Instance.ShowSkillDetailPanel(skill, action);
	}

	private void OnCharacterChanged(CharacterChangedEvent sessionEvent)
	{
		if (!string.Equals(sessionEvent.CharacterId, CharacterId, StringComparison.Ordinal))
		{
			return;
		}

		Render();
	}

	private CharacterInstance? ResolveCharacter()
	{
		if (_character is not null)
		{
			return _character;
		}

		return string.IsNullOrWhiteSpace(CharacterId)
			? null
			: Game.State.Party.GetMember(CharacterId);
	}

	private static string FormatExperienceProgress(CharacterInstance character)
	{
		var experienceProgress = CharacterLevelProgression.GetDisplayProgress(
			character.Level,
			character.Experience,
			Game.Config.MaxLevel);
		return $"{experienceProgress.CurrentExperience}/{experienceProgress.NextLevelExperience}";
	}

	private static int ToDisplayStat(double value) => Mathf.RoundToInt(value);
}
