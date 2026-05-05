using System;
using System.Globalization;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("gamesave")]
	public class GameSave : BasePojo
	{
		[XmlAttribute("name")]
		public string Name = "save";

		[XmlAttribute("newbieTask")]
		public string NewbieTask = string.Empty;

		[XmlElement]
		public GameSaveRole[] Roles;

		[XmlElement]
		public GameSaveRole[] Follows;

		[XmlElement("i")]
		public GameSaveItem[] GameItems;

		[XmlElement]
		public GameSaveItem[] XiangziItems;

		[XmlElement("k")]
		public GameSaveKeyValues[] KeyValues;

		public override string PK
		{
			get
			{
				return Name;
			}
		}

		[XmlIgnore]
		public DateTime Time
		{
			get
			{
				string valueByKey = GetValueByKey("date");
				return DateTime.ParseExact(valueByKey, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
			}
		}

		[XmlIgnore]
		public string GameMode
		{
			get
			{
				if (GetValueByKey("mode") == null)
				{
					return "normal";
				}
				return GetValueByKey("mode");
			}
		}

		[XmlIgnore]
		public bool AutoSaveOnly
		{
			get
			{
				if (GetValueByKey("AutoSaveOnly") == null)
				{
					return false;
				}
				return bool.Parse(GetValueByKey("AutoSaveOnly"));
			}
		}

		[XmlIgnore]
		public int Round
		{
			get
			{
				if (GetValueByKey("round") == null)
				{
					return 1;
				}
				return int.Parse(GetValueByKey("round"));
			}
		}

		[XmlIgnore]
		public string Locate
		{
			get
			{
				return GetValueByKey("currentBigMap");
			}
		}

		public override void InitBind()
		{
			GameSaveRole[] roles = Roles;
			foreach (GameSaveRole gameSaveRole in roles)
			{
			}
		}

		public string GetValueByKey(string key)
		{
			GameSaveKeyValues[] keyValues = KeyValues;
			foreach (GameSaveKeyValues gameSaveKeyValues in keyValues)
			{
				if (gameSaveKeyValues.key == key)
				{
					return gameSaveKeyValues.value;
				}
			}
			return null;
		}

		public override string ToString()
		{
			string text = CommonSettings.DateToGameTime(Time);
			string text2 = "难度:<color='white'>简单</color>";
			if (GameMode == "hard")
			{
				text2 = "<color='yellow'>难度:进阶</color>";
			}
			if (GameMode == "crazy")
			{
				text2 = "<color='red'>难度:炼狱</color>";
			}
			if (AutoSaveOnly)
			{
				text2 = "<color='magenta'>难度:无悔</color>";
			}
			string text3 = "周目:" + Round;
			return string.Format("{0}  <color='yellow'>({5})</color>  队伍人数{6}\n{1}\n{3}<color='white'>\t\t{4}\n当前位置:{2}</color>", Roles[0].name, text, Locate, text2, text3, GetValueByKey("currentNick"), Roles.Length);
		}
	}
}
