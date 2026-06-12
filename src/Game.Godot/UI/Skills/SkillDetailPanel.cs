using Game.Application.Formatters;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class SkillDetailPanel : JyPanel
{
	public event Action? PrimaryActionPressed;

	private TextureRect _skillTexture = null!;
	private Label _nameLabel = null!;
	private Label _typeLabel = null!;
	private Label _metaLabel = null!;
	private RichTextLabel _contentLabel = null!;
	private TextureButton _primaryActionButton = null!;
	private Label _primaryActionLabel = null!;

	private SkillInstance? _skill;
	private string _primaryActionText = string.Empty;
	private bool _primaryActionEnabled;

	public override void _Ready()
	{
		base._Ready();
		_skillTexture = GetNode<TextureRect>("%SkillTexture");
		_nameLabel = GetNode<Label>("%NameLabel");
		_typeLabel = GetNode<Label>("%TypeLabel");
		_metaLabel = GetNode<Label>("%MetaLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		_primaryActionButton = GetNode<TextureButton>("%PrimaryActionButton");
		_primaryActionLabel = GetNode<Label>("%PrimaryActionLabel");

		_primaryActionButton.Pressed += OnPrimaryActionPressed;
		Refresh();
	}

	public void Configure(SkillInstance skill, string primaryActionText = "", bool primaryActionEnabled = false)
	{
		ArgumentNullException.ThrowIfNull(skill);
		_skill = skill;
		_primaryActionText = primaryActionText;
		_primaryActionEnabled = primaryActionEnabled;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _skill is null)
		{
			return;
		}

		_skillTexture.Texture = ResolveTexture(_skill) ?? _skillTexture.Texture;
		_nameLabel.Text = _skill.Name;
		_typeLabel.Text = FormatSkillKind(_skill);
		_metaLabel.Text = BuildMetaText(_skill);
		_contentLabel.Text = SkillDescriptionFormatter.FormatBbCodeCn(_skill, Game.ContentRepository);
		RefreshPrimaryAction();
	}

	private void RefreshPrimaryAction()
	{
		var visible = !string.IsNullOrWhiteSpace(_primaryActionText);
		_primaryActionButton.Visible = visible;
		_primaryActionButton.Disabled = !_primaryActionEnabled;
		_primaryActionButton.Modulate = _primaryActionEnabled
			? Colors.White
			: new Color(0.62f, 0.62f, 0.62f, 0.82f);
		_primaryActionLabel.Text = _primaryActionText;
	}

	private void OnPrimaryActionPressed()
	{
		if (!_primaryActionEnabled)
		{
			return;
		}

		PrimaryActionPressed?.Invoke();
		QueueFree();
	}

	private static Texture2D? ResolveTexture(SkillInstance skill)
	{
		var texture = AssetResolver.LoadSkillIconResource(skill.Icon);
		if (texture is not null)
		{
			return texture;
		}

		return skill is FormSkillInstance formSkill
			? AssetResolver.LoadSkillIconResource(formSkill.Parent.Icon)
			: null;
	}

	private static string BuildMetaText(SkillInstance skill) =>
		skill switch
		{
			ExternalSkillInstance externalSkill => $"等级 {externalSkill.Level}/{externalSkill.MaxLevel}   经验 {externalSkill.Exp}/{externalSkill.LevelUpExp}",
			InternalSkillInstance internalSkill => $"等级 {internalSkill.Level}/{internalSkill.MaxLevel}   经验 {internalSkill.Exp}/{internalSkill.LevelUpExp}",
			FormSkillInstance formSkill => $"所属 {formSkill.SourceSkillName}",
			LegendSkillInstance legendSkill => $"所属 {legendSkill.Parent.Name}",
			SpecialSkillInstance => skill.IsActive ? "已启用" : "未启用",
			_ => string.Empty,
		};

	private static string FormatSkillKind(SkillInstance skill) =>
		skill switch
		{
			ExternalSkillInstance => "外功",
			InternalSkillInstance => "内功",
			FormSkillInstance => "招式",
			LegendSkillInstance => "奥义",
			SpecialSkillInstance => "绝技",
			_ => skill.SkillKind.ToString(),
		};
}
