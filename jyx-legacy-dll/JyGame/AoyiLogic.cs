using System.Collections.Generic;

namespace JyGame
{
	public class AoyiLogic
	{
		public static Aoyi ChangeToAoyi(BattleSprite sprite, SkillBox currentSkill)
		{
			IEnumerable<Aoyi> all = ResourceManager.GetAll<Aoyi>();
			Aoyi result = null;
			foreach (Aoyi item in all)
			{
				double num = 1.0;
				num *= 1.0 + (double)sprite.Role.wuxing / 150.0 * 0.2;
				if (sprite.Role.HasTalent("博览群书"))
				{
					num += 0.5;
				}
				if (sprite.Role.HasTalent("屌丝") && currentSkill.Name == "野球拳")
				{
					num += 0.1;
				}
				foreach (Trigger trigger in sprite.Role.GetTriggers("powerup_aoyi"))
				{
					if (trigger.Argvs[0] == item.Name)
					{
						num += (double)((float)int.Parse(trigger.Argvs[2]) / 100f);
					}
				}
				if (!(currentSkill.Name == item.start) || currentSkill.Level < item.level || !Tools.ProbabilityTest((double)item.probability * num))
				{
					continue;
				}
				bool flag = true;
				foreach (AoyiCondition condition in item.Conditions)
				{
					bool flag2 = false;
					if (condition.type == "skill")
					{
						foreach (SkillInstance skill in sprite.Role.Skills)
						{
							if (skill.Skill.Name == condition.value && skill.Level >= condition.level)
							{
								flag2 = true;
								break;
							}
						}
					}
					if (condition.type == "internalskill")
					{
						foreach (InternalSkillInstance internalSkill in sprite.Role.InternalSkills)
						{
							if (internalSkill.InternalSkill.Name == condition.value && internalSkill.Level >= condition.level)
							{
								flag2 = true;
								break;
							}
						}
					}
					if (condition.type == "talent" && sprite.Role.HasTalent(condition.value))
					{
						flag2 = true;
					}
					if (!flag2)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					result = item;
					break;
				}
			}
			return result;
		}
	}
}
