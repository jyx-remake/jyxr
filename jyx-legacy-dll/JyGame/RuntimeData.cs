using System;
using System.Collections.Generic;
using System.Globalization;

namespace JyGame
{
	public class RuntimeData
	{
		public const string TIMEKEY_PREF = "TIMEKEY_";

		public const string FLAG_PREF = "FLAG_";

		public bool IsInited;

		private static RuntimeData _instance;

		public GameEngine gameEngine;

		public MapUI mapUI;

		public RoleSelectUI roleSelectUI;

		public BattleField battleFieldUI;

		public List<Role> Team = new List<Role>();

		public List<Role> Follow = new List<Role>();

		public string NewbieTask = string.Empty;

		public Dictionary<ItemInstance, int> Xiangzi = new Dictionary<ItemInstance, int>();

		public Dictionary<ItemInstance, int> Items = new Dictionary<ItemInstance, int>();

		public Dictionary<string, string> KeyValues = new Dictionary<string, string>();

		public static RuntimeData Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new RuntimeData();
				}
				return _instance;
			}
		}

		public bool IsCheat
		{
			get
			{
				if (CommonSettings.MOD_MODE)
				{
					return false;
				}
				foreach (Role item in Team)
				{
					if (item.maxhp > CommonSettings.MAX_HPMP + 2000 || item.maxmp > CommonSettings.MAX_HPMP + 2000)
					{
						return true;
					}
					if (item.quanzhang > 300)
					{
						return true;
					}
					if (item.jianfa > 300)
					{
						return true;
					}
					if (item.daofa > 300)
					{
						return true;
					}
					if (item.qimen > 300)
					{
						return true;
					}
					if (item.bili > 300)
					{
						return true;
					}
					if (item.shenfa > 300)
					{
						return true;
					}
					if (item.dingli > 300)
					{
						return true;
					}
					if (item.fuyuan > 300)
					{
						return true;
					}
					if (item.wuxing > 300)
					{
						return true;
					}
					if (item.gengu > 300)
					{
						return true;
					}
					foreach (SkillInstance skill in item.Skills)
					{
						if (skill.Level > CommonSettings.MAX_SKILL_LEVEL + 3)
						{
							return true;
						}
					}
					foreach (InternalSkillInstance internalSkill in item.InternalSkills)
					{
						if (internalSkill.Level > CommonSettings.MAX_INTERNALSKILL_LEVEL + 3)
						{
							return true;
						}
					}
				}
				return false;
			}
		}

		public int XiangziCount
		{
			get
			{
				int num = 0;
				foreach (KeyValuePair<ItemInstance, int> item in Xiangzi)
				{
					num += item.Value;
				}
				return num;
			}
		}

		public int MaxXiangziItemCount
		{
			get
			{
				return LuaManager.GetConfigInt("MAX_XIANGZI_ITEM_COUNT");
			}
		}

		public string CurrentNick
		{
			get
			{
				return getDataOrInit("currentNick", "初出茅庐");
			}
			set
			{
				KeyValues["currentNick"] = value.ToString();
			}
		}

		public string PrevStory
		{
			get
			{
				return getDataOrInit("prevStory", string.Empty);
			}
			set
			{
				KeyValues["prevStory"] = value.ToString();
			}
		}

		public int Round
		{
			get
			{
				return int.Parse(getDataOrInit("round", "1"));
			}
			set
			{
				KeyValues["round"] = value.ToString();
			}
		}

		public string UUID
		{
			get
			{
				return getDataOrInit("UUID", Guid.NewGuid().ToString());
			}
			set
			{
				KeyValues["UUID"] = value.ToString();
			}
		}

		public int Daode
		{
			get
			{
				return int.Parse(getDataOrInit("daode", "50"));
			}
			set
			{
				KeyValues["daode"] = value.ToString();
			}
		}

		public string femaleName
		{
			get
			{
				return getDataOrInit("femaleName", "铃兰");
			}
			set
			{
				KeyValues["femaleName"] = value.ToString();
			}
		}

		public string maleName
		{
			get
			{
				return getDataOrInit("maleName", "小虾米");
			}
			set
			{
				KeyValues["maleName"] = value.ToString();
			}
		}

		public int Money
		{
			get
			{
				return int.Parse(getDataOrInit("money", "0"));
			}
			set
			{
				KeyValues["money"] = value.ToString();
			}
		}

		public int Yuanbao
		{
			get
			{
				return ModData.Yuanbao;
			}
			set
			{
				ModData.Yuanbao = value;
			}
		}

		public DateTime Date
		{
			get
			{
				if (!KeyValues.ContainsKey("date"))
				{
					KeyValues.Add("date", DateTime.ParseExact("0001-01-01 14:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture).ToString("yyyy-MM-dd HH:mm:ss"));
				}
				try
				{
					return DateTime.Parse(KeyValues["date"]);
				}
				catch
				{
					return DateTime.ParseExact(KeyValues["date"], "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
				}
			}
			set
			{
				if (!KeyValues.ContainsKey("date"))
				{
					KeyValues.Add("date", DateTime.ParseExact("0001-01-01 14:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture).ToString("yyyy-MM-dd HH:mm:ss"));
				}
				KeyValues["date"] = value.ToString("yyyy-MM-dd HH:mm:ss");
			}
		}

		public string GameMode
		{
			get
			{
				return getDataOrInit("mode", "normal");
			}
			set
			{
				KeyValues["mode"] = value.ToString();
			}
		}

		public bool AutoSaveOnly
		{
			get
			{
				return bool.Parse(getDataOrInit("AutoSaveOnly", "false"));
			}
			set
			{
				KeyValues["AutoSaveOnly"] = value.ToString();
			}
		}

		public string GameModeChinese
		{
			get
			{
				if (AutoSaveOnly)
				{
					return "无悔";
				}
				switch (GameMode)
				{
				case "normal":
					return "简单";
				case "hard":
					return "进阶";
				case "crazy":
					return "炼狱";
				default:
					return "未定义";
				}
			}
		}

		public bool FriendlyFire
		{
			get
			{
				return bool.Parse(getDataOrInit("friendlyfire", "false"));
			}
			set
			{
				KeyValues["friendlyfire"] = value.ToString();
			}
		}

		public string Menpai
		{
			get
			{
				return getDataOrInit("menpai", string.Empty);
			}
			set
			{
				KeyValues["menpai"] = value.ToString();
			}
		}

		public string Log
		{
			get
			{
				return getDataOrInit("log", string.Empty);
			}
			set
			{
				KeyValues["log"] = value.ToString();
			}
		}

		public int DodgePoint
		{
			get
			{
				return int.Parse(getDataOrInit("dodgePoint", "0"));
			}
			set
			{
				KeyValues["dodgePoint"] = value.ToString();
			}
		}

		public int biliPoint
		{
			get
			{
				return int.Parse(getDataOrInit("biliPoint", "0"));
			}
			set
			{
				KeyValues["biliPoint"] = value.ToString();
			}
		}

		public int Rank
		{
			get
			{
				return int.Parse(getDataOrInit("rank", "0"));
			}
			set
			{
				KeyValues["rank"] = value.ToString();
			}
		}

		public string CurrentBigMap
		{
			get
			{
				return getDataOrInit("currentBigMap", "大地图");
			}
			set
			{
				KeyValues["currentBigMap"] = value.ToString();
			}
		}

		public string TrialRoles
		{
			get
			{
				return getDataOrInit("trailRoles", string.Empty);
			}
			set
			{
				KeyValues["trailRoles"] = value.ToString();
			}
		}

		private RuntimeData()
		{
		}

		public void Init()
		{
			gameEngine = new GameEngine();
			Clear();
			UUID = Guid.NewGuid().ToString();
			SetLocation("大地图", "南贤居");
			addTeamMember("主角", "小虾米");
			addTeamMember("段正淳");
			addTeamMember("杨过");
			addTeamMember("小龙女");
			addTeamMember("乔峰");
			addTeamMember("慕容复");
			addTeamMember("慕容博");
			addTeamMember("无崖子");
			addTeamMember("逍遥子");
			addTeamMember("神级主角");
			addTeamMember("欧阳锋");
			addTeamMember("黄药师");
			addTeamMember("周伯通");
			addTeamMember("张三丰");
			addTeamMember("炼狱风清扬");
			addTeamMember("松鼠洪七公");
			addItem(ItemInstance.Generate("大还丹"), 10);
			Money = 100;
			IsInited = true;
		}

		public void Clear()
		{
			Team.Clear();
			Follow.Clear();
			Items.Clear();
			Xiangzi.Clear();
			KeyValues.Clear();
			TrialRoles = string.Empty;
			NewbieTask = string.Empty;
			Rank = -1;
			IsInited = false;
		}

		public bool InTeam(string roleKey)
		{
			foreach (Role item in Team)
			{
				if (item.Key.Equals(roleKey))
				{
					return true;
				}
			}
			foreach (Role item2 in Follow)
			{
				if (item2.Key.Equals(roleKey))
				{
					return true;
				}
			}
			return false;
		}

		public void ResetTeam()
		{
			foreach (Role item in Team)
			{
				item.Reset();
			}
		}

		public bool NameInTeam(string roleName)
		{
			foreach (Role item in Team)
			{
				if (item.Name.Equals(roleName))
				{
					return true;
				}
			}
			foreach (Role item2 in Follow)
			{
				if (item2.Name.Equals(roleName))
				{
					return true;
				}
			}
			return false;
		}

		public Role GetTeamRole(string roleKey)
		{
			foreach (Role item in Team)
			{
				if (item.Key.Equals(roleKey))
				{
					return item;
				}
			}
			return null;
		}

		public Role GetFollowRole(string roleKey)
		{
			foreach (Role item in Follow)
			{
				if (item.Key.Equals(roleKey))
				{
					return item;
				}
			}
			return null;
		}

		public void addTeamMember(string roleKey)
		{
			Team.Add(ResourceManager.Get<Role>(roleKey).Clone());
		}

		public void addTeamMember(string roleKey, string changeName)
		{
			Role role = ResourceManager.Get<Role>(roleKey).Clone();
			role.Name = changeName;
			Team.Add(role);
		}

		public void addFollowMember(string roleKey)
		{
			Follow.Add(ResourceManager.Get<Role>(roleKey).Clone());
		}

		public void addFollowMember(string roleKey, string changeName)
		{
			Role role = ResourceManager.Get<Role>(roleKey).Clone();
			role.Name = changeName;
			Follow.Add(role);
		}

		public void removeTeamMember(string roleKey)
		{
			Role role = null;
			foreach (Role item in Team)
			{
				if (item.Key.Equals(roleKey))
				{
					role = item;
					break;
				}
			}
			if (role == null)
			{
				return;
			}
			foreach (ItemInstance item2 in role.Equipment)
			{
				addItem(item2);
			}
			Team.Remove(role);
		}

		public void removeAllTeamMember()
		{
			Role item = null;
			foreach (Role item2 in Team)
			{
				if (item2.Key.Equals("主角"))
				{
					item = item2;
					continue;
				}
				foreach (ItemInstance item3 in item2.Equipment)
				{
					addItem(item3);
				}
			}
			Team.Clear();
			Team.Add(item);
		}

		public void removeFollowMember(string roleKey)
		{
			Role role = null;
			foreach (Role item in Follow)
			{
				if (item.Key.Equals(roleKey))
				{
					role = item;
					break;
				}
			}
			if (role != null)
			{
				Follow.Remove(role);
			}
		}

		public bool isLocationInTask(string location)
		{
			Task task = ResourceManager.Get<Task>(NewbieTask);
			if (task == null)
			{
				return false;
			}
			foreach (TaskLocation location2 in task.Locations)
			{
				if (location2.name.Equals(location))
				{
					return true;
				}
			}
			return false;
		}

		public void setTask(string task)
		{
			NewbieTask = task;
		}

		public void removeTask(string task)
		{
			NewbieTask = string.Empty;
		}

		public bool hasTask()
		{
			if (NewbieTask == null || NewbieTask.Equals(string.Empty))
			{
				return false;
			}
			return true;
		}

		public bool judgeFinishTask()
		{
			Task task = ResourceManager.Get<Task>(NewbieTask);
			if (task == null)
			{
				return false;
			}
			foreach (TaskFinish finish in task.Finishes)
			{
				bool flag = true;
				foreach (Condition condition in finish.Conditions)
				{
					if (!TriggerLogic.judge(condition))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					NewbieTask = string.Empty;
					if (task.Result != null)
					{
						gameEngine.SwitchGameScene(task.Result.type, task.Result.value);
					}
					return true;
				}
			}
			return false;
		}

		public Dictionary<ItemInstance, int> GetItems(ItemType type)
		{
			Dictionary<ItemInstance, int> dictionary = new Dictionary<ItemInstance, int>();
			foreach (ItemInstance key in Items.Keys)
			{
				if (key.Type == type)
				{
					dictionary.Add(key, Items[key]);
				}
			}
			return dictionary;
		}

		public ItemInstance GetItem(string pk)
		{
			foreach (KeyValuePair<ItemInstance, int> item in Items)
			{
				if (item.Key.PK == pk)
				{
					return item.Key;
				}
			}
			return null;
		}

		public Dictionary<ItemInstance, int> GetItems(ItemType[] types)
		{
			Dictionary<ItemInstance, int> dictionary = new Dictionary<ItemInstance, int>();
			foreach (ItemInstance key in Items.Keys)
			{
				for (int i = 0; i < types.Length; i++)
				{
					if (types[i] == key.Type)
					{
						dictionary.Add(key, Items[key]);
					}
				}
			}
			return dictionary;
		}

		public void addItem(Item item, int number = 1)
		{
			addItem(ItemInstance.Generate(item.Name), number);
		}

		public void addItem(ItemInstance item, int number = 1)
		{
			if (number > 0)
			{
				for (int i = 0; i < number; i++)
				{
					if (Items.ContainsKey(item))
					{
						Dictionary<ItemInstance, int> items;
						Dictionary<ItemInstance, int> dictionary = (items = Items);
						ItemInstance key2;
						ItemInstance key = (key2 = item);
						int num = items[key2];
						dictionary[key] = num + 1;
					}
					else
					{
						Items.Add(item, 1);
					}
				}
			}
			else if (Items.ContainsKey(item))
			{
				Dictionary<ItemInstance, int> items2;
				Dictionary<ItemInstance, int> dictionary2 = (items2 = Items);
				ItemInstance key2;
				ItemInstance key3 = (key2 = item);
				int num = items2[key2];
				dictionary2[key3] = num + number;
				if (Items[item] <= 0)
				{
					Items.Remove(item);
				}
			}
		}

		public void xiangziAddItem(ItemInstance item, int number = 1)
		{
			if (number > 0)
			{
				for (int i = 0; i < number; i++)
				{
					if (Xiangzi.ContainsKey(item))
					{
						Dictionary<ItemInstance, int> xiangzi;
						Dictionary<ItemInstance, int> dictionary = (xiangzi = Xiangzi);
						ItemInstance key2;
						ItemInstance key = (key2 = item);
						int num = xiangzi[key2];
						dictionary[key] = num + 1;
					}
					else
					{
						Xiangzi.Add(item, 1);
					}
				}
			}
			else if (Xiangzi.ContainsKey(item))
			{
				Dictionary<ItemInstance, int> xiangzi2;
				Dictionary<ItemInstance, int> dictionary2 = (xiangzi2 = Xiangzi);
				ItemInstance key2;
				ItemInstance key3 = (key2 = item);
				int num = xiangzi2[key2];
				dictionary2[key3] = num + number;
				if (Xiangzi[item] <= 0)
				{
					Xiangzi.Remove(item);
				}
			}
		}

		public void NextZhoumuClear()
		{
			int round = Round;
			Dictionary<ItemInstance, int> dictionary = new Dictionary<ItemInstance, int>();
			foreach (KeyValuePair<ItemInstance, int> item in Xiangzi)
			{
				dictionary.Add(item.Key, item.Value);
			}
			string trialRoles = TrialRoles;
			bool autoSaveOnly = AutoSaveOnly;
			Clear();
			IsInited = true;
			Round = round;
			Xiangzi = dictionary;
			AutoSaveOnly = autoSaveOnly;
			addTeamMember("主角", "小虾米");
			Money = 100;
		}

		public int getHaogan(string roleKey = "女主")
		{
			string key = "HAOGAN" + roleKey;
			return int.Parse(getDataOrInit(key, "50"));
		}

		public void addHaogan(int value, string roleKey = "女主")
		{
			string key = "HAOGAN" + roleKey;
			int num = int.Parse(getDataOrInit(key, "50"));
			KeyValues[key] = (num + value).ToString();
		}

		public void SetLocation(string mapKey, string location)
		{
			string key = "location." + mapKey;
			if (!KeyValues.ContainsKey(key))
			{
				KeyValues.Add(key, string.Empty);
			}
			KeyValues[key] = location;
		}

		public string GetLocation(string mapKey)
		{
			string key = "location." + mapKey;
			if (!KeyValues.ContainsKey(key))
			{
				KeyValues.Add(key, string.Empty);
			}
			return KeyValues[key];
		}

		public void AddLog(string info)
		{
			string text = "江湖" + Tools.DateToString(Date);
			RuntimeData instance = Instance;
			string log = instance.Log;
			instance.Log = log + text + "，" + info + "\r\n";
		}

		public void AddTimeKeyStory(string key, int days, string story)
		{
			string key2 = "TIMEKEY_" + key;
			Instance.KeyValues[key2] = string.Format("{0}#{1}#{2}", Instance.Date, days, story);
		}

		public void RemoveTimeKey(string key)
		{
			string key2 = "TIMEKEY_" + key;
			if (Instance.KeyValues.ContainsKey(key2))
			{
				Instance.KeyValues.Remove(key2);
			}
		}

		public string CheckTimeFlags()
		{
			List<string> list = new List<string>();
			foreach (string key in KeyValues.Keys)
			{
				if (key.StartsWith("TIMEKEY_"))
				{
					string text = KeyValues[key];
					DateTime minValue = DateTime.MinValue;
					try
					{
						minValue = DateTime.Parse(text.Split('#')[0]);
					}
					catch
					{
						minValue = DateTime.ParseExact(text.Split('#')[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
					}
					int num = int.Parse(text.Split('#')[1]);
					if ((Date - minValue).TotalDays > (double)num)
					{
						list.Add(key);
					}
				}
			}
			string result = string.Empty;
			string text2 = string.Empty;
			foreach (string item in list)
			{
				string text3 = KeyValues[item];
				if (text3.Split('#').Length > 2)
				{
					result = text3.Split('#')[2];
					text2 = item;
				}
				else
				{
					KeyValues.Remove(item);
				}
			}
			if (text2 != null && !text2.Equals(string.Empty))
			{
				KeyValues.Remove(text2);
			}
			return result;
		}

		public void AddFlag(string key)
		{
			string key2 = "FLAG_" + key;
			Instance.KeyValues[key2] = string.Format("{0}", Instance.Date);
		}

		public void RemoveFlag(string key)
		{
			string key2 = "FLAG_" + key;
			if (Instance.KeyValues.ContainsKey(key2))
			{
				Instance.KeyValues.Remove(key2);
			}
		}

		public bool HasFlag(string key)
		{
			string key2 = "FLAG_" + key;
			if (Instance.KeyValues.ContainsKey(key2))
			{
				return true;
			}
			return false;
		}

		public bool IsStoryFinished(string storyName)
		{
			return KeyValues.ContainsKey(storyName);
		}

		public void StoryFinish(string storyName, string storyResult)
		{
			if (!string.IsNullOrEmpty(storyName))
			{
				KeyValues[storyName] = storyResult;
			}
		}

		private string getDataOrInit(string key, string initValue = "")
		{
			if (!KeyValues.ContainsKey(key))
			{
				KeyValues[key] = initValue;
			}
			return KeyValues[key];
		}

		public string Save()
		{
			GameSave gameSave = new GameSave();
			gameSave.Roles = GameSaveRole.Create(Team);
			gameSave.Follows = GameSaveRole.Create(Follow);
			gameSave.NewbieTask = NewbieTask;
			gameSave.GameItems = new GameSaveItem[Items.Count];
			int num = 0;
			foreach (KeyValuePair<ItemInstance, int> item in Items)
			{
				if (item.Value > 0)
				{
					gameSave.GameItems[num] = new GameSaveItem
					{
						name = item.Key.Name,
						triggers = item.Key.AdditionTriggers.ToArray(),
						count = item.Value
					};
				}
				num++;
			}
			gameSave.XiangziItems = new GameSaveItem[Xiangzi.Count];
			num = 0;
			foreach (KeyValuePair<ItemInstance, int> item2 in Xiangzi)
			{
				if (item2.Value > 0)
				{
					gameSave.XiangziItems[num] = new GameSaveItem
					{
						name = item2.Key.Name,
						triggers = item2.Key.AdditionTriggers.ToArray(),
						count = item2.Value
					};
				}
				num++;
			}
			gameSave.KeyValues = new GameSaveKeyValues[KeyValues.Count];
			num = 0;
			foreach (KeyValuePair<string, string> keyValue in KeyValues)
			{
				gameSave.KeyValues[num] = new GameSaveKeyValues
				{
					key = keyValue.Key,
					value = keyValue.Value
				};
				num++;
			}
			return Tools.SerializeXML(gameSave);
		}

		public bool Load(string str)
		{
			LuaManager.Reload();
			GameSave gameSave = BasePojo.Create<GameSave>(str);
			Clear();
			if (gameSave.Roles != null)
			{
				GameSaveRole[] roles = gameSave.Roles;
				foreach (GameSaveRole gameSaveRole in roles)
				{
					Team.Add(gameSaveRole.GenerateRole());
				}
			}
			if (gameSave.Follows != null)
			{
				GameSaveRole[] follows = gameSave.Follows;
				foreach (GameSaveRole gameSaveRole2 in follows)
				{
					Follow.Add(gameSaveRole2.GenerateRole());
				}
			}
			NewbieTask = gameSave.NewbieTask;
			Items.Clear();
			if (gameSave.GameItems != null)
			{
				GameSaveItem[] gameItems = gameSave.GameItems;
				foreach (GameSaveItem gameSaveItem in gameItems)
				{
					Items.Add(gameSaveItem.Generate(), gameSaveItem.count);
				}
			}
			Xiangzi.Clear();
			if (gameSave.XiangziItems != null)
			{
				GameSaveItem[] xiangziItems = gameSave.XiangziItems;
				foreach (GameSaveItem gameSaveItem2 in xiangziItems)
				{
					Xiangzi.Add(gameSaveItem2.Generate(), gameSaveItem2.count);
				}
			}
			KeyValues.Clear();
			GameSaveKeyValues[] keyValues = gameSave.KeyValues;
			foreach (GameSaveKeyValues gameSaveKeyValues in keyValues)
			{
				KeyValues.Add(gameSaveKeyValues.key, gameSaveKeyValues.value);
			}
			IsInited = true;
			return true;
		}
	}
}
