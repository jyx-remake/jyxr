using Game.Core.Model.Skills;

namespace Game.Godot.UI;

public static class CharacterSkillDetailActionFactory
{
	public static DetailPanelAction? CreateForgetAction(SkillInstance skill)
	{
		ArgumentNullException.ThrowIfNull(skill);

		return skill switch
		{
			FormSkillInstance or LegendSkillInstance => CreateDisabledForgetAction(),
			InternalSkillInstance { IsEquipped: true } => CreateDisabledForgetAction(),
			ExternalSkillInstance externalSkill => CreateForgetAction(() =>
				Game.CharacterService.RemoveExternalSkill(externalSkill.Owner, externalSkill.Id)),
			InternalSkillInstance internalSkill => CreateForgetAction(() =>
				Game.CharacterService.RemoveInternalSkill(internalSkill.Owner, internalSkill.Id)),
			SpecialSkillInstance specialSkill => CreateForgetAction(() =>
				Game.CharacterService.RemoveSpecialSkill(specialSkill.Owner, specialSkill.Id)),
			_ => null,
		};
	}

	private static DetailPanelAction CreateDisabledForgetAction() =>
		new("遗忘", false, () => Task.CompletedTask);

	private static DetailPanelAction CreateForgetAction(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		return new DetailPanelAction(
			"遗忘",
			true,
			() =>
			{
				action();
				return Task.CompletedTask;
			});
	}
}
