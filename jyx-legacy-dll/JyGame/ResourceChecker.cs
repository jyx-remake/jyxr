using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JyGame
{
	public class ResourceChecker
	{
		public static bool CheckAll(ILogger logger)
		{
			bool result = true;
			logger.Log("开始检测脚本...");
			foreach (Story item in ResourceManager.GetAll<Story>())
			{
				foreach (StoryAction action in item.Actions)
				{
					if (action.type == "BACKGROUND" || action.type == "MUSIC")
					{
						if (ResourceManager.Get<Resource>(action.value) == null)
						{
							logger.LogError(string.Format("剧本【{0}】调用了未定义的资源【{1}】:  【{2}】", item.Name, action.type, action.value));
							result = false;
						}
					}
					else if (action.type == "BATTLE")
					{
						if (ResourceManager.Get<Battle>(action.value) == null)
						{
							logger.LogError(string.Format("剧本【{0}】调用了未定义的战斗【{1}】", item.Name, action.value));
						}
					}
					else if (action.type == "ITEM")
					{
						string text = action.value.Split('#')[0];
						if (ResourceManager.Get<Item>(text) == null)
						{
							logger.LogError(string.Format("剧本【{0}】调用了未定义的物品【{1}】", item.Name, text));
						}
					}
				}
				foreach (StoryResult result2 in item.Results)
				{
					if (result2.type == "story")
					{
						if (ResourceManager.Get<Story>(result2.value) == null)
						{
							logger.LogError(string.Format("剧本【{0}】企图跳到未定义的story 【{1}】", item.Name, result2.value));
						}
					}
					else if (result2.type == "map" && ResourceManager.Get<Map>(result2.value) == null)
					{
						logger.LogError(string.Format("剧本【{0}】企图跳到未定义的map 【{1}】", item.Name, result2.value));
					}
				}
			}
			foreach (Map item2 in ResourceManager.GetAll<Map>())
			{
				foreach (MapRole mapRole in item2.MapRoles)
				{
					if (!string.IsNullOrEmpty(mapRole.pic) && ResourceManager.Get<Resource>(mapRole.pic) == null)
					{
						logger.LogError(string.Format("地图【{0}】调用了未定义的缩略图:   【{1}】", item2.Name, mapRole.pic));
						result = false;
					}
				}
			}
			foreach (Role item3 in ResourceManager.GetAll<Role>())
			{
				foreach (string talent in item3.Talents)
				{
					if (ResourceManager.Get<Resource>("天赋." + talent) == null)
					{
						logger.LogError(string.Format("角色【{0}】的天赋【{1}】未填写天赋描述！", item3.Key, talent));
					}
				}
				foreach (SkillInstance skill in item3.Skills)
				{
					if (ResourceManager.Get<Skill>(skill.Name) == null)
					{
						logger.LogError(string.Format("角色【{0}】的外功技能【{1}】未定义！", item3.Key, skill.Name));
					}
				}
				foreach (InternalSkillInstance internalSkill in item3.InternalSkills)
				{
					if (ResourceManager.Get<InternalSkill>(internalSkill.Name) == null)
					{
						logger.LogError(string.Format("角色【{0}】的内功技能【{1}】未定义！", item3.Key, internalSkill.Name));
					}
				}
				foreach (SpecialSkillInstance specialSkill in item3.SpecialSkills)
				{
					if (ResourceManager.Get<SpecialSkill>(specialSkill.Name) == null)
					{
						logger.LogError(string.Format("角色【{0}】的特殊技能【{1}】未定义！", item3.Key, specialSkill.Name));
					}
				}
				foreach (ItemInstance item4 in item3.Equipment)
				{
					if (ResourceManager.Get<Item>(item4.Name) == null)
					{
						logger.LogError(string.Format("角色【{0}】的装备【{1}】未定义！", item3.Key, item4.Name));
					}
				}
				if (ResourceManager.Get<Resource>(item3.Head) == null)
				{
					logger.LogError(string.Format("角色【{0}】的头像【{1}】未定义！", item3.Key, item3.Head));
				}
			}
			foreach (Map item5 in ResourceManager.GetAll<Map>())
			{
				if (ResourceManager.Get<Resource>(item5.Pic) == null)
				{
					logger.LogError(string.Format("地图【{0}】的背景图【{1}】未定义！", item5.Name, item5.Pic));
				}
				foreach (Music music in item5.Musics)
				{
					if (ResourceManager.Get<Resource>(music.Name) == null)
					{
						logger.LogError(string.Format("地图【{0}】的背景音乐【{1}】未定义！", item5.Name, music.Name));
					}
				}
				foreach (MapLocation mapUnit in item5.MapUnits)
				{
					foreach (MapEvent @event in mapUnit.Events)
					{
						if (@event.type == "story")
						{
							if (ResourceManager.Get<Story>(@event.value) == null)
							{
								logger.LogError(string.Format("地图【{0}】的单元【{1}】企图跳到未定义的story 【{2}】", item5.Name, mapUnit.Name, @event.value));
							}
						}
						else if (@event.type == "map" && ResourceManager.Get<Map>(@event.value) == null)
						{
							logger.LogError(string.Format("地图【{0}】的单元【{1}】企图跳到未定义的map 【{2}】", item5.Name, mapUnit.Name, @event.value));
						}
						if (!string.IsNullOrEmpty(@event.image) && ResourceManager.Get<Resource>(@event.image) == null)
						{
							logger.LogError(string.Format("地图【{0}】的单元【{1}】的事件【{2}】调用了未定义的缩略图 【{3}】", item5.Name, mapUnit.Name, @event.value, @event.image));
						}
					}
				}
			}
			foreach (Item item6 in ResourceManager.GetAll<Item>())
			{
				if (ResourceManager.Get<Resource>(item6.pic) == null)
				{
					logger.LogError(string.Format("物品【{0}】缩略图【{1}】未定义！", item6.Name, item6.pic));
				}
			}
			foreach (Battle item7 in ResourceManager.GetAll<Battle>())
			{
				bool flag = true;
				Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
				foreach (BattleRole role in item7.Roles)
				{
					string text2 = string.Format("{0}_{1}", role.X, role.Y);
					if (dictionary.ContainsKey(text2))
					{
						flag = false;
						logger.LogError("战斗位置重叠  " + item7.Key + "," + text2);
					}
					else
					{
						dictionary[text2] = true;
					}
					if (role.X < 0 || role.X >= BattleField.MOVEBLOCK_MAX_X || role.Y < 0 || role.Y >= BattleField.MOVEBLOCK_MAX_Y)
					{
						logger.LogError("战斗位置超出范围  " + item7.Key + "," + text2);
					}
				}
				foreach (BattleRole randomRole in item7.randomBattleRoles.randomRoles)
				{
					string text3 = string.Format("{0}_{1}", randomRole.X, randomRole.Y);
					if (dictionary.ContainsKey(text3))
					{
						logger.LogError("战斗位置重叠  " + item7.Key + "," + text3);
						flag = false;
					}
					else
					{
						dictionary[text3] = true;
					}
					if (randomRole.X < 0 || randomRole.X >= BattleField.MOVEBLOCK_MAX_X || randomRole.Y < 0 || randomRole.Y >= BattleField.MOVEBLOCK_MAX_Y)
					{
						logger.LogError("战斗位置超出范围  " + item7.Key + "," + text3);
					}
				}
				if (!flag)
				{
					logger.LogError(string.Format("战斗【{0}】存在两个角色放在同一个位置！", item7.Key));
				}
			}
			if (!CommonSettings.MOD_MODE)
			{
				foreach (Resource item8 in ResourceManager.GetAll<Resource>())
				{
					if (item8.Key.StartsWith("音乐.") || item8.Key.StartsWith("音效."))
					{
						string text4 = Path.Combine(Application.dataPath + "/AssetBundleSource/Editor", item8.Value);
						if (!File.Exists(text4))
						{
							logger.LogError(string.Format("音乐/音效【{0}】对应文件不存在【{1}】", item8.Key, text4));
							result = false;
						}
					}
					else if (item8.Key.StartsWith("头像."))
					{
						string text5 = Path.Combine(Application.dataPath + "/Resources", item8.Value);
						if (!File.Exists(text5 + ".jpg") && !File.Exists(text5 + ".png"))
						{
							logger.LogError(string.Format("头像【{0}】对应文件不存在【{1}】", item8.Key, text5));
							result = false;
						}
					}
					else if (item8.Key.StartsWith("物品."))
					{
						string text6 = Path.Combine(Application.dataPath + "/Resources", item8.Value);
						if (!File.Exists(text6 + ".jpg") && !File.Exists(text6 + ".png"))
						{
							logger.LogError(string.Format("物品【{0}】对应文件不存在【{1}】", item8.Key, text6));
							result = false;
						}
					}
					else if (item8.Key.StartsWith("地图."))
					{
						string text7 = Path.Combine(Application.dataPath + "/AssetBundleSource/Editor", item8.Value);
						if (!File.Exists(text7 + ".jpg") && !File.Exists(text7 + ".png"))
						{
							logger.LogError(string.Format("地图【{0}】对应文件不存在【{1}】", item8.Key, text7));
							result = false;
						}
					}
				}
			}
			logger.Log("脚本检测完毕.");
			return result;
		}
	}
}
