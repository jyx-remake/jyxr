using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using Godot;

namespace Game.Godot.UI;

public partial class SkillTab : Control
{
	public event Action<SkillInstance>? SkillToggleRequested;
	public event Action<SkillInstance>? SkillDetailRequested;

	[Export]
	public PackedScene SkillBoxScene { get; set; } = null!;

	public bool IsReadOnly { get; set; } = true;

	private VirtualGrid _gridContainer = null!;
	private Label _emptyLabel = null!;

	private CharacterInstance? _character;

	public override void _Ready()
	{
		_gridContainer = GetNode<VirtualGrid>("%VirtualGrid");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		Refresh();
	}

	public void Setup(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);
		_character = character;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree())
		{
			return;
		}

		if (_character is null)
		{
			_gridContainer.ClearItems();
			_emptyLabel.Visible = true;
			return;
		}

		var skills = EnumerateSkills(_character).ToArray();
		_emptyLabel.Visible = skills.Length == 0;
		_gridContainer.SetItems(skills, CreateSkillBox, (box, skill, _) => box.Setup(skill, isInteractive: !IsReadOnly));
	}

	private SkillBox CreateSkillBox()
	{
		if (SkillBoxScene is null)
		{
			throw new InvalidOperationException("SkillBoxScene is not assigned.");
		}

		var instance = SkillBoxScene.Instantiate();
		if (instance is not SkillBox skillBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("SkillBox scene root must be SkillBox.");
		}

		skillBox.ToggleRequested += OnSkillBoxToggleRequested;
		skillBox.DetailRequested += OnSkillBoxDetailRequested;
		return skillBox;
	}

	private void OnSkillBoxToggleRequested(SkillInstance skill)
	{
		SkillToggleRequested?.Invoke(skill);
	}

	private void OnSkillBoxDetailRequested(SkillInstance skill)
	{
		SkillDetailRequested?.Invoke(skill);
	}

	private static IEnumerable<SkillInstance> EnumerateSkills(CharacterInstance character)
	{
		foreach (var skill in character.SpecialSkills)
		{
			yield return skill;
		}

		foreach (var skill in character.ExternalSkills)
		{
			yield return skill;
			foreach (var formSkill in skill.GetFormSkills())
			{
				if (!formSkill.IsUnlocked)
				{
					continue;
				}

				yield return formSkill;
			}
		}

		foreach (var skill in character.InternalSkills)
		{
			yield return skill;
			foreach (var formSkill in skill.GetFormSkills())
			{
				if (!formSkill.IsUnlocked)
				{
					continue;
				}

				yield return formSkill;
			}
		}
	}
}
