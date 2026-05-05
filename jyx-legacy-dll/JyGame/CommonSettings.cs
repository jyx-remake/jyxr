using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JyGame
{
	public class CommonSettings
	{
		public delegate void VoidCallBack();

		public delegate void IntCallBack(int rst);

		public delegate void Int2CallBack(int x, int y);

		public delegate void ItemCallBack(Dictionary<string, int> items, int point);

		public delegate void StringCallBack(string rst);

		public delegate void ObjectCallBack(object obj);

		public delegate bool JudgeCallback(object obj);

		public const string HOME_PAGE = "http://www.jy-x.com";

		public const string FORUM_URL = "http://tieba.baidu.com/f?ie=utf-8&kw=%E6%B1%89%E5%AE%B6%E6%9D%BE%E9%BC%A0";

		public const string APPSTORE_PINGLUN_URL = "itms-apps://ax.itunes.apple.com/WebObjects/MZStore.woa/wa/viewContentsUserReviews?type=Purple+Software&id=1021093037";

		public const string GAME_VERSION = "1.1.0.6";

		public const string MOD_BASE_URL = "http://www.hanjiasongshu.com/jygamemod/";

		public const string XML_UPDATE_URL = "http://www.jy-x.com/jygame/assetbundle/xml";

		public const string XML_UPDATE_URL_ANDROID = "http://www.jy-x.com/jygame/assetbundle/xml-android";

		public const string XML_UPDATE_URL_IOS = "http://www.jy-x.com/jygame/assetbundle/xml-ios";

		public const bool USE_ASSETBUNDLE_IN_EDITOR_MODE = false;

		public const bool FORCE_UPDATE_XML = false;

		public const string DAILY_AWARD = "http://120.24.166.63:8080/JY-X/award";

		public const int VERSION = 1;

		public const string UPDATE_ROOT = "http://120.24.166.63:8080/";

		public const int MAX_SAVE_COUNT = 6;

		public const string TEST_BATTLE = "测试战斗";

		public const int SKILLTYPE_QUAN = 0;

		public const int SKILLTYPE_JIAN = 1;

		public const int SKILLTYPE_DAO = 2;

		public const int SKILLTYPE_QIMEN = 3;

		public const int SKILLTYPE_NEIGONG = 4;

		public const int BUFF_RUN_CYCLE = 50;

		public const double WEAPON_ATTACK_FIX = 0.9;

		public const double ZHENGLONGQIJU_HP_ADD = 0.1;

		public const double ZHENGLONGQIJU_MP_ADD = 0.1;

		public const int CHEAT_CHECK_ATTRIBUTE = 300;

		public const int AI_MAX_COMPUTE_SKILL = 5;

		public const int AI_MAX_COMPUTE_MOVERANGE = 20;

		public const string AutoSaveKey = "autosave";

		public const string FastSaveKey = "fastsave";

		public const float DIALOG_SPACE_KEY_SWITCH_DELTATIME = 0.3f;

		public static bool CHECK_SCRIPT_RESOURCE = false;

		private static string _persistentDataPath = string.Empty;

		public static bool SECURE_XML = false;

		public static bool DEBUG_FORCE_MOBILE_MODE = false;

		public static double[] timeOpacity = new double[12]
		{
			0.4, 0.4, 0.5, 0.5, 0.6, 0.7, 1.0, 1.0, 1.0, 0.8,
			0.6, 0.4
		};

		public static string[] RoleAttributeList = new string[16]
		{
			"hp", "maxhp", "mp", "maxmp", "gengu", "bili", "fuyuan", "shenfa", "dingli", "wuxing",
			"quanzhang", "jianfa", "daofa", "qimen", "female", "wuxue"
		};

		public static string[] RoleAttributeChineseList = new string[16]
		{
			"生命", "生命上限", "内力", "内力上限", "根骨", "臂力", "福缘", "身法", "定力", "悟性",
			"搏击格斗", "使剑技巧", "耍刀技巧", "奇门兵器", "是否女性", "武学常识"
		};

		public static string flagNoGlobalEvent = "NO_GLOBAL_EVENT";

		public static bool MOD_MODE
		{
			get
			{
				return GlobalData.CurrentMod != null;
			}
		}

		public static bool DEBUG_CONSOLE
		{
			get
			{
				if (MOD_MODE)
				{
					return LuaManager.GetConfig<bool>("CONSOLE");
				}
				return (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor) ? true : false;
			}
		}

		public static string persistentDataPath
		{
			get
			{
				if (Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer)
				{
					return Application.persistentDataPath;
				}
				if (string.IsNullOrEmpty(_persistentDataPath))
				{
					DirectoryInfo directoryInfo = new DirectoryInfo(".");
					_persistentDataPath = directoryInfo.FullName + "/gamedata/";
					if (!Directory.Exists(_persistentDataPath))
					{
						Directory.CreateDirectory(_persistentDataPath);
					}
				}
				return _persistentDataPath;
			}
		}

		public static bool TOUCH_MODE
		{
			get
			{
				return GameEngine.IsMobilePlatform;
			}
		}

		public static int DEFAULT_MAX_GAME_SPTIME
		{
			get
			{
				return LuaManager.GetConfigInt("DEFAULT_MAX_GAME_SPTIME");
			}
		}

		public static int PER_MAXLEVEL_ADD_BY_ZHOUMU
		{
			get
			{
				return LuaManager.GetConfigInt("PER_MAXLEVEL_ADD_BY_ZHOUMU");
			}
		}

		public static double ZHOUMU_ATTACK_ADD
		{
			get
			{
				return LuaManager.GetConfigDouble("ZHOUMU_ATTACK_ADD");
			}
		}

		public static double ZHOUMU_DEFENCE_ADD
		{
			get
			{
				return LuaManager.GetConfigDouble("ZHOUMU_DEFENCE_ADD");
			}
		}

		public static double ZHOUMU_HP_ADD
		{
			get
			{
				return LuaManager.GetConfigDouble("ZHOUMU_HP_ADD");
			}
		}

		public static double ZHOUMU_MP_ADD
		{
			get
			{
				return LuaManager.GetConfigDouble("ZHOUMU_MP_ADD");
			}
		}

		public static int MAX_INTERNALSKILL_COUNT
		{
			get
			{
				return LuaManager.GetConfigInt("MAX_INTERNALSKILL_COUNT");
			}
		}

		public static int MAX_SKILL_COUNT
		{
			get
			{
				return LuaManager.GetConfigInt("MAX_SKILL_COUNT");
			}
		}

		public static int MAX_ATTRIBUTE
		{
			get
			{
				return LuaManager.GetConfigInt("MAX_ATTRIBUTE");
			}
		}

		public static int MAX_SKILL_LEVEL
		{
			get
			{
				return LuaManager.GetConfigInt("MAX_SKILL_LEVEL");
			}
		}

		public static int MAX_INTERNALSKILL_LEVEL
		{
			get
			{
				return LuaManager.GetConfigInt("MAX_INTERNALSKILL_LEVEL");
			}
		}

		public static double YUANBAO_DROP_RATE
		{
			get
			{
				return LuaManager.GetConfigDouble("YUANBAO_DROP_RATE");
			}
		}

		public static int MAX_HPMP
		{
			get
			{
				return LuaManager.GetConfigInt("MAX_HPMP") + (RuntimeData.Instance.Round - 1) * LuaManager.GetConfigInt("MAX_HPMP_PER_ROUND");
			}
		}

		public static int MAX_LEVEL
		{
			get
			{
				return LuaManager.GetConfigInt("MAX_LEVEL");
			}
		}

		private static string[] randomBattleMusics
		{
			get
			{
				return LuaManager.GetConfig<string[]>("randomBattleMusics");
			}
		}

		public static string[] EnemyRandomTalentsList
		{
			get
			{
				return LuaManager.GetConfig<string[]>("EnemyRandomTalentsList");
			}
		}

		public static string[] EnemyRandomTalentListCrazyDefence
		{
			get
			{
				return LuaManager.GetConfig<string[]>("EnemyRandomTalentListCrazy1");
			}
		}

		public static string[] EnemyRandomTalentListCrazyAttack
		{
			get
			{
				return LuaManager.GetConfig<string[]>("EnemyRandomTalentListCrazy2");
			}
		}

		public static string[] EnemyRandomTalentListCrazyOther
		{
			get
			{
				return LuaManager.GetConfig<string[]>("EnemyRandomTalentListCrazy3");
			}
		}

		public static int SMALLGAME_MAX_ATTRIBUTE
		{
			get
			{
				return LuaManager.GetConfigInt("SMALLGAME_MAX_ATTRIBUTE");
			}
		}

		public static SkillCoverType GetDefaultCoverType(int skillTypeCode)
		{
			switch ((SkillCode)skillTypeCode)
			{
			case SkillCode.Quanzhang:
				return SkillCoverType.NORMAL;
			case SkillCode.Jianfa:
				return SkillCoverType.LINE;
			case SkillCode.Daofa:
				return SkillCoverType.FRONT;
			case SkillCode.Qimen:
				return SkillCoverType.CROSS;
			default:
				return SkillCoverType.NORMAL;
			}
		}

		public static string AttributeToChinese(string attr)
		{
			for (int i = 0; i < RoleAttributeList.Length; i++)
			{
				if (RoleAttributeList[i].Equals(attr))
				{
					return RoleAttributeChineseList[i];
				}
			}
			throw new Exception("invalid attribute " + attr);
		}

		public static string DateToGameTime(DateTime date)
		{
			return string.Format("江湖{0}年{1}月{2}日{3}时", Tools.chineseNumber[date.Year], Tools.chineseNumber[date.Month], Tools.chineseNumber[date.Day], Tools.chineseTime[date.Hour / 2]);
		}

		public static string HourToChineseTime(int hour)
		{
			int num = hour / 24;
			int num2 = hour % 24;
			string text = string.Empty;
			if (num > 0)
			{
				text += string.Format("{0}天", num);
			}
			if (num2 != 0)
			{
				text += string.Format("{0}个时辰", num2 / 2);
			}
			return text;
		}

		public static string getRoleName(string roleKey)
		{
			string empty = string.Empty;
			if (roleKey == "女主")
			{
				return RuntimeData.Instance.femaleName;
			}
			if (roleKey == "主角")
			{
				return RuntimeData.Instance.maleName;
			}
			Role role = ResourceManager.Get<Role>(roleKey);
			if (role != null)
			{
				return role.Name;
			}
			return roleKey;
		}

		public static string getRoleHead(string roleKey)
		{
			if (roleKey == "主角")
			{
				foreach (Role item in RuntimeData.Instance.Team)
				{
					if (item.Key == "主角")
					{
						return item.Head;
					}
				}
				return string.Empty;
			}
			return ResourceManager.Get<Role>(roleKey).Head;
		}

		public static void adjustAttr(Role role, string type, int value)
		{
			switch (type)
			{
			case "hp":
				role.hp += value;
				break;
			case "maxhp":
				role.maxhp += value;
				break;
			case "mp":
				role.mp += value;
				break;
			case "maxmp":
				role.maxmp += value;
				break;
			case "gengu":
				role.gengu += value;
				break;
			case "bili":
				role.bili += value;
				break;
			case "fuyuan":
				role.fuyuan += value;
				break;
			case "shenfa":
				role.shenfa += value;
				break;
			case "dingli":
				role.dingli += value;
				break;
			case "wuxing":
				role.wuxing += value;
				break;
			case "quanzhang":
				role.quanzhang += value;
				break;
			case "jianfa":
				role.jianfa += value;
				break;
			case "daofa":
				role.daofa += value;
				break;
			case "qimen":
				role.qimen += value;
				break;
			}
		}

		public static int LevelupExp(int level)
		{
			if (level <= 0)
			{
				return 0;
			}
			return (int)((double)(level * 20) + 1.1 * (double)LevelupExp(level - 1));
		}

		public static string GetRandomBattleMusic()
		{
			return randomBattleMusics[Tools.GetRandomInt(0, randomBattleMusics.Length - 1)];
		}

		public static string GetEnemyRandomTalentListCrazyDefence()
		{
			int randomInt = Tools.GetRandomInt(0, EnemyRandomTalentListCrazyDefence.Length);
			return EnemyRandomTalentListCrazyDefence[randomInt % EnemyRandomTalentListCrazyDefence.Length];
		}

		public static string GetEnemyRandomTalentListCrazyAttack()
		{
			int randomInt = Tools.GetRandomInt(0, EnemyRandomTalentListCrazyAttack.Length);
			return EnemyRandomTalentListCrazyAttack[randomInt % EnemyRandomTalentListCrazyAttack.Length];
		}

		public static string GetEnemyRandomTalentListCrazyOther()
		{
			int randomInt = Tools.GetRandomInt(0, EnemyRandomTalentListCrazyOther.Length);
			return EnemyRandomTalentListCrazyOther[randomInt % EnemyRandomTalentListCrazyOther.Length];
		}

		public static string GetEnemyRandomTalent(bool female)
		{
			string empty = string.Empty;
			string[] enemyRandomTalentsList = EnemyRandomTalentsList;
			do
			{
				int randomInt = Tools.GetRandomInt(0, enemyRandomTalentsList.Length);
				empty = enemyRandomTalentsList[randomInt % enemyRandomTalentsList.Length];
			}
			while ((female && empty == "好色") || (!female && empty == "大小姐"));
			return empty;
		}
	}
}
