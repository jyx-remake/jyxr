using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using Godot;

namespace Game.Godot.UI;

public partial class SkillTab : Control
{
	public event Action<SkillInstance>? SkillToggleRequested;

	[Export]
	public PackedScene SkillBoxScene { get; set; } = null!;

	public bool IsInteractive { get; set; }

	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;

	private CharacterInstance? _character;

	public override void _Ready()
	{
		_gridContainer = GetNode<GridContainer>("%GridContainer");
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

		ClearGrid();
		if (_character is null)
		{
			_emptyLabel.Visible = true;
			return;
		}

		var skills = EnumerateSkills(_character).ToArray();
		_emptyLabel.Visible = skills.Length == 0;
		foreach (var skill in skills)
		{
			_gridContainer.AddChild(CreateSkillBox(skill));
		}
	}

	private SkillBox CreateSkillBox(SkillInstance skill)
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

		skillBox.Setup(skill, IsInteractive);
		skillBox.ToggleRequested += OnSkillBoxToggleRequested;
		return skillBox;
	}

	private void OnSkillBoxToggleRequested(SkillInstance skill)
	{
		SkillToggleRequested?.Invoke(skill);
	}

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
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
