using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Godot.UI;

internal enum MasterySkillFilter
{
	All,
	Quanzhang,
	Jianfa,
	Daofa,
	Qimen,
	Internal,
}

internal sealed class HeroSkillMasteryPresenter
{
	private const int PreviewSkillLevel = 20;
	private const string PreviewOwnerId = "__skill_mastery_preview__";

	private static readonly CharacterDefinition PreviewOwnerDefinition = new(
		PreviewOwnerId,
		"武学预览",
		new Dictionary<StatType, int>(),
		[],
		[],
		[],
		[]);

	private readonly IContentRepository _contentRepository;
	private readonly CharacterInstance _previewOwner = new()
	{
		Id = PreviewOwnerId,
		Name = "武学预览",
		Definition = PreviewOwnerDefinition,
	};

	public HeroSkillMasteryPresenter(IContentRepository contentRepository)
	{
		_contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
	}

	public IReadOnlyList<SkillInstance> GetSkills(MasterySkillFilter filter)
	{
		var skills = new List<SkillInstance>();

		if (filter != MasterySkillFilter.Internal)
		{
			foreach (var definition in _contentRepository.GetExternalSkills())
			{
				if (filter != MasterySkillFilter.All && !MatchesFilter(definition, filter))
				{
					continue;
				}

				skills.Add(CreatePreviewSkill(definition));
			}
		}

		if (filter is MasterySkillFilter.All or MasterySkillFilter.Internal)
		{
			foreach (var definition in _contentRepository.GetInternalSkills())
			{
				skills.Add(CreatePreviewSkill(definition));
			}
		}

		return skills;
	}

	private ExternalSkillInstance CreatePreviewSkill(ExternalSkillDefinition definition) =>
		new(definition, _previewOwner, active: false)
		{
			Level = PreviewSkillLevel,
			MaxLevel = PreviewSkillLevel,
			Exp = 0,
		};

	private InternalSkillInstance CreatePreviewSkill(InternalSkillDefinition definition) =>
		new(definition, _previewOwner)
		{
			Level = PreviewSkillLevel,
			MaxLevel = PreviewSkillLevel,
			Exp = 0,
		};

	private static bool MatchesFilter(ExternalSkillDefinition definition, MasterySkillFilter filter) =>
		filter switch
		{
			MasterySkillFilter.Quanzhang => definition.Type == WeaponType.Quanzhang,
			MasterySkillFilter.Jianfa => definition.Type == WeaponType.Jianfa,
			MasterySkillFilter.Daofa => definition.Type == WeaponType.Daofa,
			MasterySkillFilter.Qimen => definition.Type == WeaponType.Qimen,
			_ => false,
		};
}
