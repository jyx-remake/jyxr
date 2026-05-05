using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterTalentTab : Control
{
	[Export]
	public PackedScene TalentBoxScene { get; set; } = null!;

	private Label _pointLabel = null!;
	private VBoxContainer _container = null!;

	public override void _Ready()
	{
		_pointLabel = GetNode<Label>("%PointLabel");
		_container = GetNode<VBoxContainer>("%TalentContainer");
	}

	public void Setup(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);

		var spentPoints = character.UnlockedTalents.Sum(static talent => talent.Point);
		var wuxuePoints = character.GetBaseStat(StatType.Wuxue);
		var unlockedIds = new HashSet<string>(
			character.UnlockedTalents.Select(static talent => talent.Id),
			StringComparer.Ordinal);
		var talents = character.UnlockedTalents
			.Concat(character.EffectiveTalents)
			.GroupBy(static talent => talent.Id, StringComparer.Ordinal)
			.Select(static group => group.First())
			.OrderByDescending(talent => unlockedIds.Contains(talent.Id))
			.ThenBy(talent => talent.Name, StringComparer.Ordinal)
			.ToArray();

		_pointLabel.Text = $"武学常识 {spentPoints}/{wuxuePoints}";
		foreach (var child in _container.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var talent in talents)
		{
			_container.AddChild(CreateTalentBox(talent, unlockedIds.Contains(talent.Id)));
		}
	}

	private CharacterTalentBox CreateTalentBox(TalentDefinition talent, bool isUnlocked)
	{
		if (TalentBoxScene is null)
		{
			throw new InvalidOperationException("TalentBoxScene is not assigned.");
		}

		var instance = TalentBoxScene.Instantiate();
		if (instance is not CharacterTalentBox talentBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("TalentBox scene root must be CharacterTalentBox.");
		}

		talentBox.Setup(talent, isUnlocked);
		return talentBox;
	}
}
