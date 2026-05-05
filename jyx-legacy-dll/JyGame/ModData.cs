using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JyGame
{
	public class ModData
	{
		public const string DEAD_KEY = "dead";

		public const string SAVE_KEY = "save";

		public const string LAST_SAVE_INDEX = "last_save_index";

		public const string TOTALKILL_KEY = "total_kill";

		public const string END_COUNT_KEY = "end_count";

		public const string MAX_ROUND_KEY = "max_round";

		public static List<string> Nicks;

		private static Dictionary<string, string> KeyValues;

		public static Dictionary<string, int> SkillMaxLevels;

		public static ModInfo CurrentMod
		{
			get
			{
				return GlobalData.CurrentMod;
			}
		}

		public static int ZhenlongqijuLevel
		{
			get
			{
				return GetParam("zhenlongqiju");
			}
			set
			{
				SetParam("zhenlongqiju", value);
				Save();
			}
		}

		public static int Yuanbao
		{
			get
			{
				return GetParam("yuanbao");
			}
			set
			{
				SetParam("yuanbao", value);
				Save();
			}
		}

		public static string ModXmlPath
		{
			get
			{
				if (GlobalData.CurrentMod == null)
				{
					return CommonSettings.persistentDataPath + "/moddata.xml";
				}
				return CommonSettings.persistentDataPath + "/modcache/" + GlobalData.CurrentMod.key + "/moddata.xml";
			}
		}

		static ModData()
		{
			Nicks = new List<string>();
			KeyValues = new Dictionary<string, string>();
			SkillMaxLevels = new Dictionary<string, int>();
			Load();
		}

		public static void addNick(string nick)
		{
			if (!Nicks.Contains(nick))
			{
				Nicks.Add(nick);
				Save();
			}
		}

		public static void SetParam(string key, int value)
		{
			KeyValues[key] = value.ToString();
		}

		public static int GetParam(string key)
		{
			if (KeyValues.ContainsKey(key))
			{
				return int.Parse(KeyValues[key]);
			}
			return 0;
		}

		public static int ParamAdd(string key, int value)
		{
			int param = GetParam(key);
			int num = param + value;
			SetParam(key, num);
			Save();
			return num;
		}

		public static int GetSkillMaxLevel(string skillName)
		{
			int num = RuntimeData.Instance.Round / CommonSettings.PER_MAXLEVEL_ADD_BY_ZHOUMU;
			int num2 = 0;
			return Math.Min(val2: (!SkillMaxLevels.Keys.Contains(skillName)) ? (10 + num) : (SkillMaxLevels[skillName] + num), val1: CommonSettings.MAX_SKILL_LEVEL);
		}

		public static int AddSkillMaxLevel(string skillName, int level, string storyKey = "")
		{
			if (!string.IsNullOrEmpty(storyKey) && KeyValues.ContainsKey(storyKey))
			{
				return -1;
			}
			int skillMaxLevel = GetSkillMaxLevel(skillName);
			int num = ((skillMaxLevel + level >= CommonSettings.MAX_SKILL_LEVEL) ? CommonSettings.MAX_SKILL_LEVEL : (skillMaxLevel + level));
			SkillMaxLevels[skillName] = num;
			if (!string.IsNullOrEmpty(storyKey))
			{
				KeyValues.Add(storyKey, "1");
			}
			Save();
			return num;
		}

		public static void Load()
		{
			if (File.Exists(ModXmlPath))
			{
				using (StreamReader streamReader = new StreamReader(ModXmlPath))
				{
					string xml = streamReader.ReadToEnd();
					LoadFromXmlString(xml);
					return;
				}
			}
			ClearAll();
		}

		private static void LoadFromXmlString(string xml)
		{
			GlobalSave globalSave = Tools.LoadObjectFromXML<GlobalSave>(xml);
			if (globalSave == null)
			{
				return;
			}
			Nicks = ((globalSave.Nicks != null) ? globalSave.Nicks.ToList() : new List<string>());
			KeyValues.Clear();
			if (globalSave.KeyValues != null)
			{
				GameSaveKeyValues[] keyValues = globalSave.KeyValues;
				foreach (GameSaveKeyValues gameSaveKeyValues in keyValues)
				{
					KeyValues.Add(gameSaveKeyValues.key, gameSaveKeyValues.value);
				}
			}
			SkillMaxLevels.Clear();
			if (globalSave.SkillMaxLevels != null)
			{
				GlobalSkillMaxLevel[] skillMaxLevels = globalSave.SkillMaxLevels;
				foreach (GlobalSkillMaxLevel globalSkillMaxLevel in skillMaxLevels)
				{
					SkillMaxLevels.Add(globalSkillMaxLevel.key, globalSkillMaxLevel.value);
				}
			}
		}

		public static void Save()
		{
			GlobalSave globalSave = new GlobalSave();
			globalSave.Nicks = Nicks.ToArray();
			globalSave.KeyValues = new GameSaveKeyValues[KeyValues.Count];
			int num = 0;
			foreach (KeyValuePair<string, string> keyValue in KeyValues)
			{
				globalSave.KeyValues[num] = new GameSaveKeyValues
				{
					key = keyValue.Key,
					value = keyValue.Value
				};
				num++;
			}
			globalSave.SkillMaxLevels = new GlobalSkillMaxLevel[SkillMaxLevels.Count];
			num = 0;
			foreach (KeyValuePair<string, int> skillMaxLevel in SkillMaxLevels)
			{
				globalSave.SkillMaxLevels[num] = new GlobalSkillMaxLevel
				{
					key = skillMaxLevel.Key,
					value = skillMaxLevel.Value
				};
				num++;
			}
			string value = Tools.SerializeXML(globalSave);
			using (StreamWriter streamWriter = new StreamWriter(ModXmlPath, false, Encoding.UTF8))
			{
				streamWriter.Write(value);
			}
		}

		public static void ClearAll()
		{
			KeyValues.Clear();
			Nicks.Clear();
			SkillMaxLevels.Clear();
			Save();
		}
	}
}
