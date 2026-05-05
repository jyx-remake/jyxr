using System;
using System.Collections.Generic;
using System.Globalization;

namespace JyGame
{
	public class TriggerLogic
	{
		private static List<string> luaExtensionConditions;

		public static bool judge(Condition condition)
		{
			if (luaExtensionConditions == null)
			{
				luaExtensionConditions = new List<string>();
				string[] array = LuaManager.Call<string[]>("TriggerLogic_getExtensionConditions", new object[0]);
				foreach (string item in array)
				{
					luaExtensionConditions.Add(item);
				}
			}
			if (condition.type.StartsWith("!"))
			{
				Condition condition2 = new Condition();
				condition2.type = condition.type;
				condition2.value = condition.value;
				condition2.number = condition.number;
				Condition condition3 = condition2;
				return !judgeCondition(condition3);
			}
			return judgeCondition(condition);
		}

		public static bool judgeCondition(Condition condition)
		{
			if (luaExtensionConditions.Contains(condition.type))
			{
				return LuaManager.Call<bool>("TriggerLogic_judge", new object[1] { condition });
			}
			if (condition.type == "in_team" && !RuntimeData.Instance.NameInTeam(condition.value))
			{
				return false;
			}
			if (condition.type == "not_in_team" && RuntimeData.Instance.NameInTeam(condition.value))
			{
				return false;
			}
			if (condition.type == "key_in_team" && !RuntimeData.Instance.InTeam(condition.value))
			{
				return false;
			}
			if (condition.type == "key_not_in_team" && RuntimeData.Instance.InTeam(condition.value))
			{
				return false;
			}
			if (condition.type == "should_finish" && !RuntimeData.Instance.KeyValues.ContainsKey(condition.value))
			{
				return false;
			}
			if (condition.type == "should_not_finish" && RuntimeData.Instance.KeyValues.ContainsKey(condition.value))
			{
				return false;
			}
			if (condition.type == "follow_story" && !RuntimeData.Instance.PrevStory.Equals(condition.value))
			{
				return false;
			}
			if (condition.type == "has_time_key" && !RuntimeData.Instance.KeyValues.ContainsKey("TIMEKEY_" + condition.value))
			{
				return false;
			}
			if (condition.type == "not_has_time_key" && RuntimeData.Instance.KeyValues.ContainsKey("TIMEKEY_" + condition.value))
			{
				return false;
			}
			if (condition.type == "not_in_time")
			{
				string[] array = condition.value.Split('#');
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (Tools.IsChineseTime(RuntimeData.Instance.Date, text[0]))
					{
						return false;
					}
				}
			}
			if (condition.type == "in_time")
			{
				string[] array3 = condition.value.Split('#');
				string[] array4 = array3;
				foreach (string text2 in array4)
				{
					if (Tools.IsChineseTime(RuntimeData.Instance.Date, text2[0]))
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "in_month")
			{
				string[] array5 = condition.value.Split('#');
				string[] array6 = array5;
				foreach (string s in array6)
				{
					if (RuntimeData.Instance.Date.Month == int.Parse(s))
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "not_in_month")
			{
				string[] array7 = condition.value.Split('#');
				string[] array8 = array7;
				foreach (string s2 in array8)
				{
					if (RuntimeData.Instance.Date.Month == int.Parse(s2))
					{
						return false;
					}
				}
			}
			if (condition.type == "have_money" && RuntimeData.Instance.Money < int.Parse(condition.value))
			{
				return false;
			}
			if (condition.type == "have_yuanbao")
			{
				return RuntimeData.Instance.Yuanbao >= int.Parse(condition.value);
			}
			if (condition.type == "have_item")
			{
				string[] array9 = condition.value.Split('#');
				string text3 = array9[0];
				if (array9.Length > 1)
				{
					condition.number = int.Parse(array9[1]);
				}
				int num = 0;
				foreach (KeyValuePair<ItemInstance, int> item in RuntimeData.Instance.Items)
				{
					if (item.Key.Name == text3)
					{
						num += item.Value;
					}
				}
				if (num < condition.number || num == 0)
				{
					return false;
				}
			}
			if (condition.type == "not_have_item")
			{
				foreach (KeyValuePair<ItemInstance, int> item2 in RuntimeData.Instance.Items)
				{
					if (item2.Key.Name == condition.value)
					{
						return false;
					}
				}
			}
			if (condition.type == "game_mode" && RuntimeData.Instance.GameMode != condition.value)
			{
				return false;
			}
			if (condition.type == "is_wuhui")
			{
				return RuntimeData.Instance.AutoSaveOnly;
			}
			if (condition.type == "exceed_day" && (RuntimeData.Instance.Date - DateTime.ParseExact("0001-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)).Days <= int.Parse(condition.value))
			{
				return false;
			}
			if (condition.type == "not_exceed_day" && (RuntimeData.Instance.Date - DateTime.ParseExact("0001-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)).Days > int.Parse(condition.value))
			{
				return false;
			}
			if (condition.type == "in_menpai" && RuntimeData.Instance.Menpai != condition.value)
			{
				return false;
			}
			if (condition.type == "not_in_menpai" && RuntimeData.Instance.Menpai == condition.value)
			{
				return false;
			}
			if (condition.type == "has_menpai")
			{
				return RuntimeData.Instance.Menpai != string.Empty;
			}
			if (condition.type == "in_round" && RuntimeData.Instance.Round != int.Parse(condition.value))
			{
				return false;
			}
			if (condition.type == "not_in_round" && RuntimeData.Instance.Round == int.Parse(condition.value))
			{
				return false;
			}
			if (condition.type == "probability" && !Tools.ProbabilityTest((double)int.Parse(condition.value) / 100.0))
			{
				return false;
			}
			if (condition.type == "daode_more_than")
			{
				return RuntimeData.Instance.Daode >= int.Parse(condition.value);
			}
			if (condition.type == "daode_less_than")
			{
				return RuntimeData.Instance.Daode < int.Parse(condition.value);
			}
			if (condition.type == "haogan_more_than")
			{
				string[] array10 = condition.value.Split('#');
				string roleKey = "女主";
				int num2 = 0;
				if (array10.Length == 1)
				{
					num2 = int.Parse(array10[0]);
				}
				else
				{
					roleKey = array10[0];
					num2 = int.Parse(array10[1]);
				}
				return RuntimeData.Instance.getHaogan(roleKey) >= num2;
			}
			if (condition.type == "haogan_less_than")
			{
				string[] array11 = condition.value.Split('#');
				string roleKey2 = "女主";
				int num3 = 0;
				if (array11.Length == 1)
				{
					num3 = int.Parse(array11[0]);
				}
				else
				{
					roleKey2 = array11[0];
					num3 = int.Parse(array11[1]);
				}
				return RuntimeData.Instance.getHaogan(roleKey2) < num3;
			}
			if (condition.type == "skill_more_than")
			{
				string[] array12 = condition.value.Split('#');
				string text4 = array12[0];
				string text5 = array12[1];
				int num4 = int.Parse(array12[2]);
				foreach (Role item3 in RuntimeData.Instance.Team)
				{
					if (!(item3.Key == text4))
					{
						continue;
					}
					foreach (SkillInstance skill in item3.Skills)
					{
						if (skill.Skill.Name == text5 && skill.Level >= num4)
						{
							return true;
						}
					}
					foreach (InternalSkillInstance internalSkill in item3.InternalSkills)
					{
						if (internalSkill.Name == text5 && internalSkill.Level >= num4)
						{
							return true;
						}
					}
				}
				return false;
			}
			if (condition.type == "skill_less_than")
			{
				string[] array13 = condition.value.Split('#');
				string text6 = array13[0];
				string text7 = array13[1];
				int num5 = int.Parse(array13[2]);
				foreach (Role item4 in RuntimeData.Instance.Team)
				{
					if (!(item4.Key == text6))
					{
						continue;
					}
					bool flag = false;
					foreach (SkillInstance skill2 in item4.Skills)
					{
						if (skill2.Skill.Name == text7)
						{
							flag = true;
						}
						if (skill2.Skill.Name == text7 && skill2.Level < num5)
						{
							return true;
						}
					}
					foreach (InternalSkillInstance internalSkill2 in item4.InternalSkills)
					{
						if (internalSkill2.Name == text7)
						{
							flag = true;
						}
						if (internalSkill2.Name == text7 && internalSkill2.Level < num5)
						{
							return true;
						}
					}
					if (!flag)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "level_greater_than")
			{
				string[] array14 = condition.value.Split('#');
				string text8 = array14[0];
				int num6 = int.Parse(array14[1]);
				foreach (Role item5 in RuntimeData.Instance.Team)
				{
					if (item5.Key == text8 && item5.Level >= num6)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "level_less_than")
			{
				string[] array15 = condition.value.Split('#');
				string text9 = array15[0];
				int num7 = int.Parse(array15[1]);
				foreach (Role item6 in RuntimeData.Instance.Team)
				{
					if (item6.Key == text9 && item6.Level < num7)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "dingli_greater_than")
			{
				string[] array16 = condition.value.Split('#');
				string text10 = array16[0];
				int num8 = int.Parse(array16[1]);
				foreach (Role item7 in RuntimeData.Instance.Team)
				{
					if (item7.Key == text10 && item7.Attributes["dingli"] >= num8)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "wuxing_greater_than")
			{
				string[] array17 = condition.value.Split('#');
				string text11 = array17[0];
				int num9 = int.Parse(array17[1]);
				foreach (Role item8 in RuntimeData.Instance.Team)
				{
					if (item8.Key == text11 && item8.Attributes["wuxing"] >= num9)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "dingli_less_than")
			{
				string[] array18 = condition.value.Split('#');
				string text12 = array18[0];
				int num10 = int.Parse(array18[1]);
				foreach (Role item9 in RuntimeData.Instance.Team)
				{
					if (item9.Key == text12 && item9.Attributes["dingli"] < num10)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "wuxing_less_than")
			{
				string[] array19 = condition.value.Split('#');
				string text13 = array19[0];
				int num11 = int.Parse(array19[1]);
				foreach (Role item10 in RuntimeData.Instance.Team)
				{
					if (item10.Key == text13 && item10.Attributes["wuxing"] < num11)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "shenfa_greater_than")
			{
				string[] array20 = condition.value.Split('#');
				string text14 = array20[0];
				int num12 = int.Parse(array20[1]);
				foreach (Role item11 in RuntimeData.Instance.Team)
				{
					if (item11.Key == text14 && item11.Attributes["shenfa"] >= num12)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "quanzhang_less_than")
			{
				string[] array21 = condition.value.Split('#');
				string text15 = array21[0];
				int num13 = int.Parse(array21[1]);
				foreach (Role item12 in RuntimeData.Instance.Team)
				{
					if (item12.Key == text15 && item12.quanzhang < num13)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "jianfa_less_than")
			{
				string[] array22 = condition.value.Split('#');
				string text16 = array22[0];
				int num14 = int.Parse(array22[1]);
				foreach (Role item13 in RuntimeData.Instance.Team)
				{
					if (item13.Key == text16 && item13.jianfa < num14)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "daofa_less_than")
			{
				string[] array23 = condition.value.Split('#');
				string text17 = array23[0];
				int num15 = int.Parse(array23[1]);
				foreach (Role item14 in RuntimeData.Instance.Team)
				{
					if (item14.Key == text17 && item14.daofa < num15)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "qimen_less_than")
			{
				string[] array24 = condition.value.Split('#');
				string text18 = array24[0];
				int num16 = int.Parse(array24[1]);
				foreach (Role item15 in RuntimeData.Instance.Team)
				{
					if (item15.Key == text18 && item15.qimen < num16)
					{
						return true;
					}
				}
				return false;
			}
			if (condition.type == "friendCount")
			{
				if (RuntimeData.Instance.Team.Count >= int.Parse(condition.value))
				{
					return true;
				}
				return false;
			}
			if (condition.type == "zhoumu_greater_than")
			{
				if (RuntimeData.Instance.Round >= int.Parse(condition.value))
				{
					return true;
				}
				return false;
			}
			if (condition.type == "in_newbie_task")
			{
				if (RuntimeData.Instance.NewbieTask.Equals(condition.value))
				{
					return true;
				}
				return false;
			}
			if (condition.type == "rank")
			{
				if (RuntimeData.Instance.Rank == -1)
				{
					return false;
				}
				return RuntimeData.Instance.Rank <= int.Parse(condition.value);
			}
			return true;
		}
	}
}
