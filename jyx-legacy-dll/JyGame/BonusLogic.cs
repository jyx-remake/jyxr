using System;
using System.Collections.Generic;

namespace JyGame
{
	public class BonusLogic
	{
		private Battle _battle;

		public int Yuanbao;

		public int Money;

		public List<ItemInstance> Items = new List<ItemInstance>();

		public int Exp;

		public BonusLogic(Battle battle)
		{
			_battle = battle;
			CalcMoney();
			CalcItems();
			CalcExp();
			CalcYuanbao();
		}

		private void CalcYuanbao()
		{
			if (RuntimeData.Instance.gameEngine.battleType == BattleType.Zhenlongqiju)
			{
				Yuanbao = ModData.ZhenlongqijuLevel / 2 + 1;
			}
			else if (Tools.ProbabilityTest(CommonSettings.YUANBAO_DROP_RATE))
			{
				Yuanbao = 1;
			}
		}

		private void CalcMoney()
		{
			Money = 0;
			if (!_battle.Bonus)
			{
				return;
			}
			foreach (BattleRole role in _battle.Roles)
			{
				if (role.Team != 1)
				{
					Money += (int)Math.Pow(1.2, role.role.Level);
				}
			}
			if (Money < 10)
			{
				Money = 10;
			}
		}

		private void CalcItems()
		{
			Items.Clear();
			if (RuntimeData.Instance.gameEngine.battleType == BattleType.Zhenlongqiju)
			{
				ZhenlongqijuLogic.CalcItems(Items);
			}
			else
			{
				if (!_battle.Bonus)
				{
					return;
				}
				foreach (BattleRole role2 in _battle.Roles)
				{
					if (role2.Team == 1)
					{
						continue;
					}
					List<Item> list = new List<Item>();
					Role role = role2.role;
					if (role != null)
					{
						int level = role.Level;
						foreach (Item item in ResourceManager.GetAll<Item>())
						{
							if (item.drop && level >= (item.level - 1) * 5)
							{
								list.Add(item);
							}
						}
					}
					if (list.Count > 0 && Tools.ProbabilityTest(0.1))
					{
						Items.Add(list[Tools.GetRandomInt(0, list.Count - 1)].Generate(true));
					}
					double num = 2.0;
					if (role != null)
					{
						num += (double)role.Level / 3.0;
						if (role.Level >= LuaManager.GetConfigInt("MAX_LEVEL"))
						{
							num = 99999.0;
						}
					}
					double num2 = 0.0;
					if (RuntimeData.Instance.GameMode == "hard")
					{
						num2 = LuaManager.GetConfigDouble("HARD_MODE_CANZHANG_DROPRATE");
					}
					else if (RuntimeData.Instance.GameMode == "crazy")
					{
						num2 = LuaManager.GetConfigDouble("CRAZY_MODE_CANZHANG_DROPRATE") + (double)(RuntimeData.Instance.Round - 1) * LuaManager.GetConfigDouble("CRAZY_MODE_CANZHANG_DROPRATE_PER_ROUND");
					}
					if (Tools.ProbabilityTest(num2))
					{
						Skill skill = null;
						int num3 = 0;
						while (num3 < 100)
						{
							num3++;
							skill = ResourceManager.GetRandom<Skill>();
							if ((role != null && role.Level >= 30 && skill.Hard < 5f) || (role != null && role.Level >= 20 && skill.Hard < 3f) || skill.Hard >= (float)LuaManager.GetConfigInt("CANZHANG_MAX_HARD_SKILL") || !((double)skill.Hard < num))
							{
								continue;
							}
							break;
						}
						Items.Add(Item.GetItem(skill.Name + "残章").Generate());
					}
					if (Tools.ProbabilityTest(num2 / LuaManager.GetConfigDouble("CANZHANG_DROP_RATE_INTERNAL_RATE")))
					{
						InternalSkill internalSkill = null;
						do
						{
							internalSkill = ResourceManager.GetRandom<InternalSkill>();
						}
						while (internalSkill.Hard >= (float)LuaManager.GetConfigInt("CANZHANG_MAX_HARD_INTERNALSKILL") || !((double)internalSkill.Hard < num));
						Items.Add(Item.GetItem(internalSkill.Name + "残章").Generate());
					}
				}
			}
		}

		private void CalcExp()
		{
			int num = 0;
			double num2 = 0.0;
			foreach (BattleRole role in _battle.Roles)
			{
				if (role.Team == 1)
				{
					num++;
				}
				else
				{
					num2 += (double)role.role.LevelupExp / 15.0;
				}
			}
			double num3 = num2 / (double)num;
			if (num3 < 5.0)
			{
				num3 = 5.0;
			}
			Exp = (int)num3;
		}

		public void Run()
		{
			if (!_battle.Bonus)
			{
				return;
			}
			int exp = Exp;
			int num = 0;
			foreach (BattleRole role in _battle.Roles)
			{
				if (role.Team == 1)
				{
					role.role.AddExp(exp);
				}
				else
				{
					num++;
				}
			}
			ModData.ParamAdd("total_kill", num);
			foreach (ItemInstance item in Items)
			{
				if (item.Type != ItemType.Canzhang)
				{
					RuntimeData.Instance.addItem(item);
					continue;
				}
				RuntimeData.Instance.addItem(item, -1);
				string canzhangSkill = item.CanzhangSkill;
				int num2 = ModData.AddSkillMaxLevel(canzhangSkill, 1, string.Empty);
				AudioManager.Instance.PlayEffect("音效.升级");
			}
			RuntimeData.Instance.Money += Money;
			RuntimeData.Instance.Yuanbao += Yuanbao;
		}
	}
}
