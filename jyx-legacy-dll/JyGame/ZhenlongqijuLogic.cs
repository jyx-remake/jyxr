using System.Collections.Generic;
using System.Linq;

namespace JyGame
{
	public class ZhenlongqijuLogic
	{
		private static string[] randomWuqiName = LuaManager.GetConfig<string[]>("ZHENLONG_WUQI");

		private static string[] randomFangju = LuaManager.GetConfig<string[]>("ZHENLONG_FANGJU");

		private static string[] randomShipin = LuaManager.GetConfig<string[]>("ZHENLONG_SHIPIN");

		public static void CalcItems(List<ItemInstance> items)
		{
			int zhenlongqijuLevel = ModData.ZhenlongqijuLevel;
			Skill randomInCondition = ResourceManager.GetRandomInCondition<Skill>((object skill) => (skill as Skill).Hard < 8f);
			items.Add(Item.GetItem(randomInCondition.Name + "残章").Generate());
			if (Tools.ProbabilityTest(0.5))
			{
				Skill randomInCondition2 = ResourceManager.GetRandomInCondition<Skill>((object skill) => (skill as Skill).Hard < 8f);
				items.Add(Item.GetItem(randomInCondition2.Name + "残章").Generate());
			}
			for (int num = 0; num < zhenlongqijuLevel; num++)
			{
				if (Tools.ProbabilityTest(0.3))
				{
					Skill randomInCondition3 = ResourceManager.GetRandomInCondition<Skill>((object skill) => (skill as Skill).Hard >= 8f);
					items.Add(Item.GetItem(randomInCondition3.Name + "残章").Generate());
					break;
				}
			}
			for (int num2 = 0; num2 < zhenlongqijuLevel; num2++)
			{
				if (Tools.ProbabilityTest(0.3))
				{
					List<string> list = new List<string>();
					list.AddRange(randomWuqiName.AsEnumerable());
					list.AddRange(randomFangju.AsEnumerable());
					list.AddRange(randomShipin.AsEnumerable());
					ItemInstance itemInstance = Item.GetItem(list[Tools.GetRandomInt(0, list.Count - 1)]).Generate();
					itemInstance.AddRandomTriggers(4);
					items.Add(itemInstance);
					break;
				}
			}
		}

		public static void PowerupRole(Role role, int level)
		{
			role.maxhp = role.maxhp * (int)(1.0 + (double)level * 0.1) + level * 2000;
			role.maxmp = role.maxmp * (int)(1.0 + (double)level * 0.1) + level * 2000;
			role.bili += Tools.GetRandomInt(level * 2, level * 4);
			role.shenfa += Tools.GetRandomInt(level * 2, level * 4);
			role.gengu += Tools.GetRandomInt(level * 2, level * 4);
			role.dingli += Tools.GetRandomInt(level * 2, level * 4);
			role.fuyuan += Tools.GetRandomInt(level * 2, level * 4);
			role.wuxing += Tools.GetRandomInt(level * 2, level * 4);
			role.quanzhang += Tools.GetRandomInt(level * 2, level * 4);
			role.daofa += Tools.GetRandomInt(level * 2, level * 4);
			role.jianfa += Tools.GetRandomInt(level * 2, level * 4);
			role.qimen += Tools.GetRandomInt(level * 2, level * 4);
			foreach (InternalSkillInstance internalSkill in role.InternalSkills)
			{
				internalSkill.level += Tools.GetRandomInt(level / 5, level / 3);
			}
			foreach (SkillInstance skill in role.Skills)
			{
				skill.level += Tools.GetRandomInt(level / 5, level / 3);
			}
			role.addRandomTalentAndWeapons();
			if (role.Equipment == null)
			{
				role.Equipment = new List<ItemInstance>();
			}
			role.Equipment.Clear();
			if (role.Equipment.Count == 0)
			{
				ItemInstance itemInstance = ItemInstance.Generate(randomWuqiName[Tools.GetRandomInt(0, randomWuqiName.Length - 1)]);
				itemInstance.AddRandomTriggers(4);
				role.Equipment.Add(itemInstance);
				ItemInstance itemInstance2 = ItemInstance.Generate(randomFangju[Tools.GetRandomInt(0, randomFangju.Length - 1)]);
				itemInstance2.AddRandomTriggers(4);
				role.Equipment.Add(itemInstance2);
				ItemInstance itemInstance3 = ItemInstance.Generate(randomShipin[Tools.GetRandomInt(0, randomShipin.Length - 1)]);
				itemInstance3.AddRandomTriggers(4);
				role.Equipment.Add(itemInstance3);
			}
			role.Reset();
		}
	}
}
