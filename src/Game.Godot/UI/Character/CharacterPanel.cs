using Game.Application;
using Game.Application.Formatters;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterPanel : JyPanel
{
	public string CharacterId
	{
		get;
		set
		{
			field = value;
			if (IsInsideTree())
			{
				Render();
			}
		}
	} = string.Empty;

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
	private JyButton _prevCharacterButton = null!;
	private JyButton _nextCharacterButton = null!;
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
		_prevCharacterButton = GetNode<JyButton>("%PrevCharacterButton");
		_nextCharacterButton = GetNode<JyButton>("%NextCharacterButton");
		_skillTab.IsInteractive = true;

		_attrButton.Pressed += () => ShowTab(0);
		_equipButton.Pressed += () => ShowTab(1);
		_skillButton.Pressed += () => ShowTab(2);
		_talentButton.Pressed += () => ShowTab(3);
		_biographyButton.Pressed += () => ShowTab(4);
		_prevCharacterButton.Pressed += () => SwitchCharacter(-1);
		_nextCharacterButton.Pressed += () => SwitchCharacter(1);
		_skillTab.SkillToggleRequested += OnSkillToggleRequested;
		_subscriptions.Add(Game.Session.Events.Subscribe<CharacterChangedEvent>(OnCharacterChanged));

		if (!string.IsNullOrWhiteSpace(CharacterId))
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
		if (string.IsNullOrWhiteSpace(CharacterId))
		{
			throw new InvalidOperationException("CharacterPanel.CharacterId is required.");
		}

		var character = Game.State.Party.GetMember(CharacterId);
		var portrait = AssetResolver.LoadCharacterPortrait(character);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}

		_nameLabel.Text = character.Name;
		_levelValueLabel.Text = character.Level.ToString();
		_hpValueLabel.Text = ToDisplayStat(character.GetStat(StatType.MaxHp)).ToString();
		_mpValueLabel.Text = ToDisplayStat(character.GetStat(StatType.MaxMp)).ToString();
		_xpValueLabel.Text = character.Level >= Game.Config.MaxLevel
			? "-/-"
			: FormatExperienceProgress(character);
		var combatStats = CharacterCombatStatFormatter.Calculate(character);
		_attackValueLabel.Text = combatStats.Attack.ToString();
		_defenceValueLabel.Text = combatStats.Defence.ToString();
		UpdateNavigationButtons();

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

	private void OnCharacterChanged(CharacterChangedEvent sessionEvent)
	{
		if (!string.Equals(sessionEvent.CharacterId, CharacterId, StringComparison.Ordinal))
		{
			return;
		}

		Render();
	}

	private void SwitchCharacter(int direction)
	{
		var party = Game.State.Party;
		if (party.Members.Count <= 1)
		{
			return;
		}

		CharacterId = direction < 0
			? party.GetPreviousMember(CharacterId).Id
			: party.GetNextMember(CharacterId).Id;
		Render();
	}

	private void UpdateNavigationButtons()
	{
		var hasMultipleMembers = Game.State.Party.Members.Count > 1;
		_prevCharacterButton.Disabled = !hasMultipleMembers;
		_nextCharacterButton.Disabled = !hasMultipleMembers;
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
