using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using XLua;

namespace JyGame
{
	[LuaCallCSharp]
	public class RollRoleUI : MonoBehaviour
	{
		public GameObject rolePanelObj;

		public GameObject headSelectPanelObj;

		public GameObject NameInputPanel;

		public GameObject selectPanelObj;

		public GameObject MultiSelectItemObj;

		public GameObject RoleConfirmButtonObj;

		public GameObject RoleResetButtonObj;

		private List<string> heads = new List<string>
		{
			"头像.主角", "头像.主角3", "头像.主角4", "头像.魔君", "头像.全冠清", "头像.李白", "头像.林平之瞎", "头像.侠客2", "头像.归辛树", "头像.狄云",
			"头像.独孤求败", "头像.陈近南", "头像.石中玉", "头像.商宝震", "头像.尹志平", "头像.流浪汉", "头像.梁发", "头像.卓一航", "头像.烟霞神龙", "头像.双手开碑",
			"头像.流星赶月", "头像.盖七省", "头像.公子1", "头像.主角2"
		};

		public List<int> results = new List<int>();

		public Role makeRole;

		public int makeMoney;

		public List<Item> makeItems;

		public string selectHeadKey;

		public NameInputPanel nameInputPanel
		{
			get
			{
				return NameInputPanel.GetComponent<NameInputPanel>();
			}
		}

		public HeadSelectMenu headSelectMenu
		{
			get
			{
				return headSelectPanelObj.transform.Find("HeadMenu").GetComponent<HeadSelectMenu>();
			}
		}

		public RolePanelUI rolePanel
		{
			get
			{
				return rolePanelObj.GetComponent<RolePanelUI>();
			}
		}

		public Text SelectTitle
		{
			get
			{
				return selectPanelObj.transform.Find("TitleText").GetComponent<Text>();
			}
		}

		public SelectMenu selectMenu
		{
			get
			{
				return selectPanelObj.transform.Find("SelectMenu").GetComponent<SelectMenu>();
			}
		}

		public void LoadSelection(string title, LuaTable opts, LuaFunction callback)
		{
			List<string> list = new List<string>();
			opts.ForEach(delegate(object key, object value)
			{
				list.Add(value.ToString());
			});
			LoadSelection(title, list, LuaTool.MakeIntCallBack(callback));
		}

		public void LoadSelection(string title, List<string> opts, CommonSettings.IntCallBack callback)
		{
			StoryAction storyAction = new StoryAction();
			storyAction.type = "SELECT";
			storyAction.value = "汉家松鼠#" + title;
			foreach (string opt in opts)
			{
				string text = opt;
				storyAction.value = storyAction.value + "#" + text;
			}
			LoadSelection(storyAction, callback);
		}

		public void LoadSelection(StoryAction selection, CommonSettings.IntCallBack callback)
		{
			selectMenu.Clear();
			string[] array = selection.value.Split('#');
			string text = array[0];
			string text2 = array[1];
			SelectTitle.text = text2;
			for (int i = 2; i < array.Length; i++)
			{
				int index = i - 2;
				GameObject gameObject = Object.Instantiate(MultiSelectItemObj);
				gameObject.transform.Find("Text").GetComponent<Text>().text = array[i];
				gameObject.GetComponent<Button>().onClick.AddListener(delegate
				{
					selectPanelObj.SetActive(false);
					selectMenu.Hide();
					callback(index);
				});
				selectMenu.AddItem(gameObject);
			}
			selectPanelObj.SetActive(true);
			selectMenu.Show();
		}

		public void Show()
		{
			if (!RuntimeData.Instance.IsInited)
			{
				RuntimeData.Instance.Init();
			}
			base.gameObject.SetActive(true);
			StoryAction storyAction = new StoryAction();
			storyAction.type = "SELECT";
			storyAction.value = "汉家松鼠#在来到这个世界之前，请允许询问您几个问题#继续...";
			LoadSelection(storyAction, SelectCallback1);
			results.Clear();
		}

		private void SelectCallback1(int rst)
		{
			List<string> list = new List<string>();
			list.Add("商人的儿子");
			list.Add("大草原上长大的孩子");
			list.Add("名门世家");
			list.Add("市井流浪的汉子");
			list.Add("疯子");
			list.Add("书香门第");
			LoadSelection("你希望你在武侠小说中的出身是", list, SelectCallback2);
		}

		private void SelectCallback2(int rst)
		{
			results.Add(rst);
			List<string> list = new List<string>();
			list.Add("无尽的财宝");
			list.Add("永恒的爱情");
			list.Add("坚强的意志");
			list.Add("自由");
			list.Add("至高无上的权力");
			LoadSelection("以下你觉得最宝贵的是", list, SelectCallback3);
		}

		private void SelectCallback3(int rst)
		{
			results.Add(rst);
			List<string> list = new List<string>();
			list.Add("调戏妇女");
			list.Add("欺软怕硬");
			list.Add("偷奸耍滑");
			list.Add("切JJ练神功");
			list.Add("有美女不泡");
			LoadSelection("以下你觉得最可恶的行为是", list, SelectCallback31);
		}

		private void SelectCallback31(int rst)
		{
			results.Add(rst);
			List<string> list = new List<string>();
			list.Add("拳");
			list.Add("剑");
			list.Add("刀");
			list.Add("暗器");
			LoadSelection("你最喜欢的兵刃是", list, SelectCallback32);
		}

		private void SelectCallback32(int rst)
		{
			results.Add(rst);
			List<string> list = new List<string>();
			list.Add("黄蓉");
			list.Add("小龙女");
			list.Add("郭襄");
			list.Add("梅超风");
			list.Add("周芷若");
			LoadSelection("以下女性角色你最喜欢的是", list, SelectCallback33);
		}

		private void SelectCallback33(int rst)
		{
			results.Add(rst);
			List<string> list = new List<string>();
			list.Add("张无忌");
			list.Add("郭靖");
			list.Add("杨过");
			list.Add("令狐冲");
			list.Add("林平之");
			LoadSelection("以下男性角色你最喜欢的是", list, SelectCallback4);
		}

		private void SelectCallback4(int rst)
		{
			results.Add(rst);
			List<string> list = new List<string>();
			list.Add("乔峰");
			list.Add("韦小宝");
			list.Add("金庸先生");
			list.Add("东方不败");
			list.Add("汉家松鼠");
			list.Add("半瓶神仙醋");
			LoadSelection("以下你觉得最牛逼的人是", list, SelectCallback5);
		}

		private void SelectCallback5(int rst)
		{
			results.Add(rst);
			if (RuntimeData.Instance.AutoSaveOnly && RuntimeData.Instance.Round > 1)
			{
				SelectCallback6(3);
				return;
			}
			List<string> list = new List<string>();
			if (RuntimeData.Instance.Round == 1)
			{
				list.Add("简单（新手推荐，仅一周目可选）");
			}
			list.Add("<color='yellow'>进阶（难度较高）</color>");
			list.Add("<color='red'>炼狱（极度变态狂选这个..请慎重)</color>");
			if (RuntimeData.Instance.Round == 1)
			{
				list.Add("<color='magenta'>无悔（变态+实时存档+仅1周目可选)</color>");
			}
			LoadSelection("选择你的游戏难度", list, SelectCallback6);
		}

		private void SelectCallback6(int rst)
		{
			results.Add(rst);
			List<string> list = new List<string>();
			list.Add("继续..");
			LoadSelection("请输入你的名字", list, SelectCallback7);
		}

		private void SelectCallback7(int rst)
		{
			nameInputPanel.Show("小虾米", delegate(string name)
			{
				RuntimeData.Instance.maleName = name;
				SelectCallback8();
			});
		}

		private void SelectCallback8()
		{
			headSelectPanelObj.SetActive(true);
			headSelectMenu.Show(heads.ToArray(), delegate(string selectKey)
			{
				headSelectPanelObj.SetActive(false);
				selectHeadKey = selectKey;
				LoadSelection("欢迎来到金庸群侠传的世界", new List<string> { "继续.." }, delegate
				{
					Reset();
				});
			});
		}

		private void Reset()
		{
			RoleConfirmButtonObj.SetActive(true);
			RoleResetButtonObj.SetActive(true);
			rolePanelObj.transform.Find("CancelButton").gameObject.SetActive(false);
			MakeBeginningCondition();
			MakeRandomCondition();
			ShowBeginningCondition();
		}

		private void ShowBeginningCondition()
		{
			rolePanel.Show(makeRole);
		}

		public List<Item> GenerateEmptyItems()
		{
			return new List<Item>();
		}

		public void MakeRandomCondition()
		{
			string[] array = new string[10] { "gengu", "bili", "fuyuan", "shenfa", "dingli", "wuxing", "quanzhang", "jianfa", "daofa", "qimen" };
			for (int i = 0; i < 3; i++)
			{
				int randomInt = Tools.GetRandomInt(0, array.Length - 1);
				string type = array[randomInt];
				CommonSettings.adjustAttr(makeRole, type, 10);
			}
			for (int j = 0; j < 10; j++)
			{
				int randomInt2 = Tools.GetRandomInt(0, array.Length - 1);
				string type2 = array[randomInt2];
				CommonSettings.adjustAttr(makeRole, type2, 1);
			}
		}

		private void MakeBeginningCondition()
		{
			makeRole = ResourceManager.Get<Role>("主角").Clone();
			makeRole.Head = selectHeadKey;
			makeRole.Name = RuntimeData.Instance.maleName;
			makeMoney = 100;
			makeItems = new List<Item>();
			makeItems.Add(Item.GetItem("小还丹"));
			makeItems.Add(Item.GetItem("小还丹"));
			makeItems.Add(Item.GetItem("小还丹"));
			switch (results[0])
			{
			case 0:
				makeMoney += 5000;
				CommonSettings.adjustAttr(makeRole, "bili", -5);
				makeItems.Add(Item.GetItem("黑玉断续膏"));
				makeItems.Add(Item.GetItem("九转熊蛇丸"));
				makeItems.Add(Item.GetItem("金丝道袍"));
				makeItems.Add(Item.GetItem("金头箍"));
				makeRole.Animation = "zydx";
				break;
			case 1:
				CommonSettings.adjustAttr(makeRole, "shenfa", 15);
				CommonSettings.adjustAttr(makeRole, "dingli", -2);
				CommonSettings.adjustAttr(makeRole, "quanzhang", 15);
				makeRole.TalentValue += "#猎人";
				makeRole.Animation = "caoyuan";
				break;
			case 2:
				CommonSettings.adjustAttr(makeRole, "fuyuan", 3);
				CommonSettings.adjustAttr(makeRole, "bili", -3);
				CommonSettings.adjustAttr(makeRole, "dingli", 2);
				CommonSettings.adjustAttr(makeRole, "wuxing", 20);
				CommonSettings.adjustAttr(makeRole, "jianfa", 2);
				CommonSettings.adjustAttr(makeRole, "gengu", 2);
				makeItems.Add(Item.GetItem("银手镯"));
				makeMoney += 100;
				makeRole.Animation = "huodu";
				break;
			case 3:
				CommonSettings.adjustAttr(makeRole, "fuyuan", -5);
				CommonSettings.adjustAttr(makeRole, "bili", 12);
				CommonSettings.adjustAttr(makeRole, "daofa", 15);
				CommonSettings.adjustAttr(makeRole, "qimen", 12);
				makeItems.Add(Item.GetItem("草帽"));
				makeRole.Animation = "shijing";
				makeMoney = 0;
				break;
			case 4:
				CommonSettings.adjustAttr(makeRole, "wuxing", 35);
				CommonSettings.adjustAttr(makeRole, "dingli", 10);
				CommonSettings.adjustAttr(makeRole, "gengu", 10);
				makeRole.TalentValue += "#神经病";
				makeRole.Animation = "fengzi";
				break;
			case 5:
				CommonSettings.adjustAttr(makeRole, "wuxing", 20);
				CommonSettings.adjustAttr(makeRole, "bili", 1);
				CommonSettings.adjustAttr(makeRole, "shenfa", -10);
				CommonSettings.adjustAttr(makeRole, "gengu", -5);
				makeRole.Animation = "xiake";
				break;
			}
			switch (results[1])
			{
			case 0:
				makeMoney += 1000;
				break;
			case 1:
				CommonSettings.adjustAttr(makeRole, "fuyuan", 15);
				break;
			case 2:
				CommonSettings.adjustAttr(makeRole, "dingli", 15);
				break;
			case 3:
				CommonSettings.adjustAttr(makeRole, "shenfa", 15);
				break;
			case 4:
				makeRole.TalentValue += "#自我主义";
				break;
			}
			switch (results[2])
			{
			case 0:
				CommonSettings.adjustAttr(makeRole, "dingli", 9);
				break;
			case 1:
				CommonSettings.adjustAttr(makeRole, "gengu", 6);
				CommonSettings.adjustAttr(makeRole, "bili", 6);
				break;
			case 2:
				CommonSettings.adjustAttr(makeRole, "wuxing", 10);
				break;
			case 3:
				CommonSettings.adjustAttr(makeRole, "gengu", 10);
				break;
			case 4:
				makeRole.TalentValue += "#好色";
				break;
			}
			switch (results[3])
			{
			case 0:
				CommonSettings.adjustAttr(makeRole, "quanzhang", 10);
				break;
			case 1:
				CommonSettings.adjustAttr(makeRole, "jianfa", 10);
				break;
			case 2:
				CommonSettings.adjustAttr(makeRole, "daofa", 20);
				break;
			case 3:
				CommonSettings.adjustAttr(makeRole, "qimen", 20);
				break;
			}
			switch (results[4])
			{
			case 0:
				CommonSettings.adjustAttr(makeRole, "wuxing", 5);
				break;
			case 1:
				CommonSettings.adjustAttr(makeRole, "dingli", 5);
				break;
			case 2:
				CommonSettings.adjustAttr(makeRole, "fuyuan", 5);
				CommonSettings.adjustAttr(makeRole, "gengu", 5);
				break;
			case 3:
				CommonSettings.adjustAttr(makeRole, "quanzhang", 6);
				CommonSettings.adjustAttr(makeRole, "bili", 6);
				break;
			case 4:
				CommonSettings.adjustAttr(makeRole, "dingli", 10);
				break;
			}
			switch (results[5])
			{
			case 0:
				CommonSettings.adjustAttr(makeRole, "wuxing", 5);
				CommonSettings.adjustAttr(makeRole, "gengu", 10);
				break;
			case 1:
				CommonSettings.adjustAttr(makeRole, "wuxing", -10);
				CommonSettings.adjustAttr(makeRole, "fuyuan", 15);
				CommonSettings.adjustAttr(makeRole, "bili", 5);
				break;
			case 2:
				CommonSettings.adjustAttr(makeRole, "wuxing", 5);
				CommonSettings.adjustAttr(makeRole, "dingli", 5);
				break;
			case 3:
				CommonSettings.adjustAttr(makeRole, "wuxing", 10);
				break;
			case 4:
				CommonSettings.adjustAttr(makeRole, "jianfa", 10);
				CommonSettings.adjustAttr(makeRole, "dingli", 10);
				break;
			}
			switch (results[6])
			{
			case 0:
				CommonSettings.adjustAttr(makeRole, "bili", 10);
				CommonSettings.adjustAttr(makeRole, "quanzhang", 9);
				break;
			case 1:
				CommonSettings.adjustAttr(makeRole, "fuyuan", 30);
				break;
			case 2:
				CommonSettings.adjustAttr(makeRole, "wuxing", 13);
				CommonSettings.adjustAttr(makeRole, "jianfa", 5);
				CommonSettings.adjustAttr(makeRole, "daofa", 5);
				CommonSettings.adjustAttr(makeRole, "quanzhang", 5);
				CommonSettings.adjustAttr(makeRole, "qimen", 5);
				break;
			case 3:
				CommonSettings.adjustAttr(makeRole, "gengu", 20);
				break;
			case 4:
				makeRole.InternalSkills[0].level = 20;
				CommonSettings.adjustAttr(makeRole, "gengu", 10);
				makeItems.Add(Item.GetItem("松果"));
				makeItems.Add(Item.GetItem("松果"));
				makeItems.Add(Item.GetItem("松果"));
				break;
			case 5:
				makeItems.Add(Item.GetItem("天王保命丹"));
				makeItems.Add(Item.GetItem("天王保命丹"));
				makeItems.Add(Item.GetItem("天王保命丹"));
				makeItems.Add(Item.GetItem("天王保命丹"));
				makeItems.Add(Item.GetItem("天王保命丹"));
				makeItems.Add(Item.GetItem("天王保命丹"));
				break;
			}
			if (RuntimeData.Instance.Round == 1)
			{
				switch (results[7])
				{
				case 0:
					RuntimeData.Instance.GameMode = "normal";
					RuntimeData.Instance.FriendlyFire = false;
					RuntimeData.Instance.AutoSaveOnly = false;
					break;
				case 1:
					RuntimeData.Instance.GameMode = "hard";
					RuntimeData.Instance.FriendlyFire = true;
					RuntimeData.Instance.AutoSaveOnly = false;
					break;
				case 2:
					RuntimeData.Instance.GameMode = "crazy";
					RuntimeData.Instance.FriendlyFire = true;
					RuntimeData.Instance.AutoSaveOnly = false;
					break;
				case 3:
					RuntimeData.Instance.GameMode = "crazy";
					RuntimeData.Instance.FriendlyFire = true;
					RuntimeData.Instance.AutoSaveOnly = true;
					break;
				}
			}
			else
			{
				switch (results[7])
				{
				case 0:
					RuntimeData.Instance.GameMode = "hard";
					RuntimeData.Instance.FriendlyFire = true;
					break;
				case 1:
					RuntimeData.Instance.GameMode = "crazy";
					RuntimeData.Instance.FriendlyFire = true;
					break;
				}
			}
			MakeZhoumuAndShilianBonus();
		}

		public void MakeZhoumuAndShilianBonus()
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			list.Clear();
			list2.Clear();
			switch (RuntimeData.Instance.Round)
			{
			case 1:
				if (RuntimeData.Instance.GameMode == "normal")
				{
					makeItems.Add(Item.GetItem("新手礼包-大蟠桃"));
					makeItems.Add(Item.GetItem("新手礼包-大蟠桃"));
					makeItems.Add(Item.GetItem("新手礼包-大蟠桃"));
					makeItems.Add(Item.GetItem("新手礼包-大蟠桃"));
					makeItems.Add(Item.GetItem("新手礼包-大蟠桃"));
				}
				break;
			case 2:
				list.Add("佛光普照");
				list.Add("百变千幻云雾十三式秘籍");
				list.Add("反两仪刀法");
				list.Add("伏魔杖法");
				list2.Add("灭仙爪");
				list2.Add("倚天剑");
				list2.Add("屠龙刀");
				list2.Add("打狗棒");
				makeItems.Add(Item.GetItem(list[Tools.GetRandomInt(0, list.Count) % list.Count]));
				makeItems.Add(Item.GetItem(list2[Tools.GetRandomInt(0, list2.Count) % list2.Count]));
				break;
			case 3:
				list.Add("隔空取物");
				list.Add("妙手仁心");
				list.Add("飞向天际");
				list.Add("血刀");
				list2.Add("仙丽雅的项链");
				list2.Add("李延宗的项链");
				list2.Add("王语嫣的武学概要");
				list2.Add("神木王鼎");
				makeItems.Add(Item.GetItem(list[Tools.GetRandomInt(0, list.Count) % list.Count]));
				makeItems.Add(Item.GetItem(list2[Tools.GetRandomInt(0, list2.Count) % list2.Count]));
				break;
			default:
				list.Add("碎裂的怒吼");
				list.Add("沾衣十八跌");
				list.Add("灵心慧质");
				list.Add("不老长春功法");
				list2.Add("仙丽雅的项链");
				list2.Add("李延宗的项链");
				list2.Add("王语嫣的武学概要");
				list2.Add("神木王鼎");
				makeItems.Add(Item.GetItem(list[Tools.GetRandomInt(0, list.Count) % list.Count]));
				makeItems.Add(Item.GetItem(list2[Tools.GetRandomInt(0, list2.Count) % list2.Count]));
				break;
			}
			string[] array = RuntimeData.Instance.TrialRoles.Split('#');
			int num = array.Length;
			List<string> list3 = new List<string>();
			if (num >= 3)
			{
				if (num >= 3 && num < 6)
				{
					makeItems.Add(Item.GetItem("王母蟠桃"));
					makeItems.Add(Item.GetItem("道家仙丹"));
				}
				else if (num >= 6 && num < 9)
				{
					makeItems.Add(Item.GetItem("灵心慧质"));
					makeItems.Add(Item.GetItem("妙手仁心"));
				}
				else if (num >= 9 && num < 12)
				{
					makeItems.Add(Item.GetItem("素心神剑心得"));
					makeItems.Add(Item.GetItem("太极心得手抄本"));
					makeItems.Add(Item.GetItem("乾坤大挪移心法"));
				}
				else if (num >= 12 && num < 15)
				{
					makeItems.Add(Item.GetItem("沾衣十八跌"));
					makeItems.Add(Item.GetItem("易筋经"));
					makeItems.Add(Item.GetItem("厚黑学"));
				}
				else if (num >= 15 && num < 20)
				{
					makeItems.Add(Item.GetItem("武穆遗书"));
					makeItems.Add(Item.GetItem("笑傲江湖曲"));
				}
				else if (num >= 20)
				{
					makeItems.Add(Item.GetItem("真葵花宝典"));
				}
			}
		}

		private void JumpSelectCallback(int rst)
		{
			RuntimeData.Instance.Money = makeMoney;
			RuntimeData.Instance.Team.Clear();
			RuntimeData.Instance.Follow.Clear();
			RuntimeData.Instance.Team.Add(makeRole);
			RuntimeData.Instance.Items.Clear();
			foreach (Item makeItem in makeItems)
			{
				Item item = makeItem;
				RuntimeData.Instance.addItem(item);
			}
			List<string> list = new List<string>();
			list.Clear();
			switch (RuntimeData.Instance.Round)
			{
			case 1:
			{
				string[] array2 = LuaManager.Call<string[]>("ROLLROLE_getBonusRole", new object[1] { 0 });
				foreach (string item3 in array2)
				{
					list.Add(item3);
				}
				break;
			}
			case 2:
			{
				string[] array4 = LuaManager.Call<string[]>("ROLLROLE_getBonusRole", new object[1] { 1 });
				foreach (string item5 in array4)
				{
					list.Add(item5);
				}
				break;
			}
			case 3:
			{
				string[] array5 = LuaManager.Call<string[]>("ROLLROLE_getBonusRole", new object[1] { 2 });
				foreach (string item6 in array5)
				{
					list.Add(item6);
				}
				break;
			}
			case 4:
			{
				string[] array3 = LuaManager.Call<string[]>("ROLLROLE_getBonusRole", new object[1] { 3 });
				foreach (string item4 in array3)
				{
					list.Add(item4);
				}
				break;
			}
			default:
			{
				string[] array = LuaManager.Call<string[]>("ROLLROLE_getBonusRole", new object[1] { 4 });
				foreach (string item2 in array)
				{
					list.Add(item2);
				}
				break;
			}
			}
			if (list.Count > 0)
			{
				RuntimeData.Instance.Team.Add(ResourceManager.Get<Role>(list[Tools.GetRandomInt(0, list.Count) % list.Count]).Clone());
			}
			if (rst == 0)
			{
				RuntimeData.Instance.gameEngine.NewGame();
			}
			else
			{
				RuntimeData.Instance.gameEngine.NewGameJump();
			}
			base.gameObject.SetActive(false);
		}

		public void okButton_Click()
		{
			JumpSelectCallback(1);
		}

		public void resetButton_Click()
		{
			AudioManager.Instance.PlayEffect("音效.装备");
			LuaManager.Call("ROLLROLE_Reset", this);
		}

		private void Start()
		{
			LuaManager.Call("ROLLROLE_start", this);
		}

		private void Update()
		{
		}
	}
}
