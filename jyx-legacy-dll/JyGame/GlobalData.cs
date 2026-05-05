using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JyGame
{
	public class GlobalData
	{
		public const string CURRENT_MOD_KEY = "current_mod";

		public const string CURRENT_MOD_DIR = "current_mod_dir";

		public const string SHARE_KEY = "share_key";

		public static Dictionary<string, string> KeyValues = new Dictionary<string, string>();

		private static ModInfo _modCache = null;

		public static ModInfo CurrentMod
		{
			get
			{
				if (!KeyValues.ContainsKey("current_mod"))
				{
					return null;
				}
				_modCache = BasePojo.Create<ModInfo>(KeyValues["current_mod"]);
				return _modCache;
			}
			set
			{
				if (value == null)
				{
					KeyValues.Remove("current_mod");
					return;
				}
				_modCache = value;
				KeyValues["current_mod"] = Tools.SerializeXML(value);
			}
		}

		public static int ShareTag
		{
			get
			{
				return GetParam("share_key");
			}
			set
			{
				SetParam("share_key", value);
				Save();
			}
		}

		public static string GlobalXmlPath
		{
			get
			{
				return CommonSettings.persistentDataPath + "/global.xml";
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

		public static void Save()
		{
			GlobalSave globalSave = new GlobalSave();
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
			string value = Tools.SerializeXML(globalSave);
			using (StreamWriter streamWriter = new StreamWriter(GlobalXmlPath, false, Encoding.UTF8))
			{
				streamWriter.Write(value);
			}
		}

		private static void LoadFromXmlString(string xml)
		{
			GlobalSave globalSave = Tools.LoadObjectFromXML<GlobalSave>(xml);
			if (globalSave == null)
			{
				return;
			}
			KeyValues.Clear();
			if (globalSave.KeyValues != null)
			{
				GameSaveKeyValues[] keyValues = globalSave.KeyValues;
				foreach (GameSaveKeyValues gameSaveKeyValues in keyValues)
				{
					KeyValues.Add(gameSaveKeyValues.key, gameSaveKeyValues.value);
				}
			}
		}

		public static void Load()
		{
			if (File.Exists(GlobalXmlPath))
			{
				using (StreamReader streamReader = new StreamReader(GlobalXmlPath))
				{
					string xml = streamReader.ReadToEnd();
					LoadFromXmlString(xml);
					return;
				}
			}
			ClearAll();
		}

		public static void ClearAll()
		{
			KeyValues.Clear();
			Save();
		}
	}
}
