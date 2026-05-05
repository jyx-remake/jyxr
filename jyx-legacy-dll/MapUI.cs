using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
	private static Sprite prevSprite;

	public GameObject BigMap;

	public GameObject LocationObjPrefab;

	public GameObject EventConfirmPanel;

	public GameObject BigMapPanel;

	public GameObject MapPanel;

	public GameObject MapRolePanel;

	public GameObject MapRoleObjPrefab;

	public GameObject LocationInfoText;

	public GameObject TimeInfoText;

	public GameObject DialogPanel;

	public GameObject ButtonPanel;

	public GameObject BackgroundImage;

	public GameObject SelectPanel;

	public GameObject ItemPanel;

	public GameObject LogPanel;

	public GameObject NameInputPanel;

	public GameObject UIStatePanelNickText;

	public GameObject UIStatePanelZhoumuText;

	public GameObject UIStatePanelHeadImage;

	public GameObject RoleSelectPanelObj;

	public GameObject TeamPanelObj;

	public GameObject RolePanel;

	public GameObject MultiSelectItemObj;

	public GameObject SystemPanelObj;

	public GameObject ItemDetailPanelObj;

	public GameObject MessageBoxUIObj;

	public GameObject MoneyTextObj;

	public GameObject YuanbaoTextObj;

	public GameObject InfoPanelObj;

	public GameObject RoleStatePanelObj;

	public GameObject AchievementPanelObj;

	public GameObject SkillSelectPanelObj;

	public GameObject MapDescriptionPanelObj;

	public GameObject SuggestPanelObj;

	public GameObject DailyAwardObj;

	private Story _story;

	private int storyActionIndex;

	private string _storyResult = "0";

	private bool jumpDialogFlag;

	private Map _map;

	public MessageBoxUI messageBox
	{
		get
		{
			return MessageBoxUIObj.GetComponent<MessageBoxUI>();
		}
	}

	public SelectMenu selectMenu
	{
		get
		{
			return SelectPanel.transform.Find("SelectMenu").GetComponent<SelectMenu>();
		}
	}

	public ItemMenu itemMenu
	{
		get
		{
			return ItemPanel.GetComponent<ItemMenu>();
		}
	}

	public LogMenu logMenu
	{
		get
		{
			return LogPanel.transform.Find("LogMenu").GetComponent<LogMenu>();
		}
	}

	public NameInputPanel nameInputPanel
	{
		get
		{
			return NameInputPanel.GetComponent<NameInputPanel>();
		}
	}

	public RoleSelectMenu roleSelectMenu
	{
		get
		{
			return RoleSelectPanelObj.transform.Find("RoleMenu").GetComponent<RoleSelectMenu>();
		}
	}

	public Map CurrentMap
	{
		get
		{
			return _map;
		}
	}

	private MapLocation CurrentLocation
	{
		get
		{
			foreach (MapLocation location in _map.Locations)
			{
				if (location.getName() == RuntimeData.Instance.GetLocation(_map.Name))
				{
					return location;
				}
			}
			return null;
		}
	}

	public void LoadMap(string mapName)
	{
		RoleStatePanelObj.GetComponent<RoleStatePanelUI>().Refresh();
		GlobalTrigger currentTrigger = GlobalTrigger.GetCurrentTrigger();
		if (currentTrigger != null)
		{
			LoadStory(currentTrigger.story);
			return;
		}
		RuntimeData.Instance.CurrentBigMap = mapName;
		Init();
		StartCoroutine(DrawMap(ResourceManager.Get<Map>(mapName)));
	}

	private void SetMapUIElementVisiable(bool isVisiable)
	{
		InfoPanelObj.SetActive(isVisiable);
		ButtonPanel.SetActive(isVisiable);
		MapPanel.SetActive(isVisiable);
		RoleStatePanelObj.SetActive(isVisiable);
	}

	public void LoadStory(string storyName)
	{
		Story story = ResourceManager.Get<Story>(storyName);
		if (story == null)
		{
			Debug.LogError("调用了未定义的story:" + story);
			LoadMap("大地图");
		}
		else
		{
			LoadStory(story);
		}
	}

	public void LoadStory(Story story, CommonSettings.VoidCallBack callback = null)
	{
		_story = story;
		storyActionIndex = 0;
		_storyResult = "0";
		SuggestPanelObj.SetActive(false);
		if ((BackgroundImage != null || BackgroundImage.GetComponent<Image>().sprite == null) && prevSprite != null)
		{
			float alpha = (float)CommonSettings.timeOpacity[RuntimeData.Instance.Date.Hour / 2];
			SetBackground(prevSprite, alpha);
			BigMapPanel.SetActive(false);
		}
		SetMapUIElementVisiable(false);
		ExecuteNextStoryAction(callback);
	}

	public void LoadSelection(string title, List<string> opts, CommonSettings.IntCallBack callback, string roleKey = "汉家松鼠")
	{
		StoryAction storyAction = new StoryAction();
		storyAction.type = "SELECT";
		storyAction.value = roleKey + "#" + title;
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
		if (text == "主角")
		{
			SelectPanel.transform.Find("HeadImage").GetComponent<Image>().sprite = Resource.GetZhujueHead();
		}
		else
		{
			SelectPanel.transform.Find("HeadImage").GetComponent<Image>().sprite = Resource.GetImage(ResourceManager.Get<Role>(text).Head);
		}
		string text2 = array[1];
		SelectPanel.transform.Find("TitleText").GetComponent<Text>().text = text2;
		for (int i = 2; i < array.Length; i++)
		{
			int index = i - 2;
			GameObject gameObject = UnityEngine.Object.Instantiate(MultiSelectItemObj);
			string text3 = array[i];
			text3 = text3.Replace("[[red:", "<color='red'>").Replace("[[yellow:", "<color='yellow'>").Replace("]]", "</color>");
			gameObject.transform.Find("Text").GetComponent<Text>().text = text3;
			gameObject.GetComponent<Button>().onClick.AddListener(delegate
			{
				SelectPanel.SetActive(false);
				selectMenu.Hide();
				callback(index);
			});
			selectMenu.AddItem(gameObject);
		}
		SelectPanel.SetActive(true);
		selectMenu.Show();
	}

	public void ExecuteNextStoryAction(CommonSettings.VoidCallBack callback = null)
	{
		if (_story == null || _story.Actions == null)
		{
			return;
		}
		storyActionIndex++;
		if (storyActionIndex > _story.Actions.Count)
		{
			jumpDialogFlag = false;
			if (callback == null)
			{
				StoryFinished();
			}
			else
			{
				callback();
			}
		}
		else
		{
			ExecuteAction(_story.Actions[storyActionIndex - 1], callback);
		}
	}

	public void JumpDialogs(CommonSettings.VoidCallBack callback = null)
	{
		jumpDialogFlag = true;
		ExecuteNextStoryAction(callback);
	}

	private void ExecuteAction(StoryAction action, CommonSettings.VoidCallBack callback = null)
	{
		string[] array = ((!action.value.Contains("#")) ? new string[1] { action.value } : action.value.Split('#'));
		if (action.type != "DIALOG")
		{
			jumpDialogFlag = false;
		}
		switch (action.type)
		{
		case "DIALOG":
			if (jumpDialogFlag)
			{
				ExecuteNextStoryAction(callback);
			}
			else
			{
				ShowDialog(action, callback);
			}
			break;
		case "SUGGEST":
		{
			string text13 = array[0];
			messageBox.Show("提示", text13, Color.white, delegate
			{
				ExecuteNextStoryAction(callback);
			});
			break;
		}
		case "SHOP":
			RuntimeData.Instance.gameEngine.SwitchGameScene("shop", array[0]);
			RuntimeData.Instance.StoryFinish(_story.Name, "0");
			break;
		case "SHAKE":
			Camera.main.transform.DOShakePosition(0.5f, 10f);
			ExecuteNextStoryAction(callback);
			break;
		case "SELECT":
		{
			selectMenu.Clear();
			string text27 = array[0];
			if (text27 == "主角")
			{
				SelectPanel.transform.Find("HeadImage").GetComponent<Image>().sprite = Resource.GetZhujueHead();
			}
			else
			{
				SelectPanel.transform.Find("HeadImage").GetComponent<Image>().sprite = Resource.GetImage(ResourceManager.Get<Role>(text27).Head);
			}
			string text28 = array[1];
			SelectPanel.transform.Find("TitleText").GetComponent<Text>().text = text28;
			for (int num17 = 2; num17 < array.Length; num17++)
			{
				int index = num17 - 2;
				GameObject gameObject = UnityEngine.Object.Instantiate(MultiSelectItemObj);
				string text29 = array[num17];
				text29 = text29.Replace("[[red:", "<color='red'>").Replace("[[yellow:", "<color='yellow'>").Replace("]]", "</color>");
				gameObject.transform.Find("Text").GetComponent<Text>().text = text29;
				gameObject.GetComponent<Button>().onClick.AddListener(delegate
				{
					SelectPanel.SetActive(false);
					selectMenu.Hide();
					_storyResult = index.ToString();
					ExecuteNextStoryAction(callback);
				});
				selectMenu.AddItem(gameObject);
			}
			SelectPanel.SetActive(true);
			selectMenu.Show();
			break;
		}
		case "SET_MAP":
			RuntimeData.Instance.CurrentBigMap = action.value;
			ExecuteNextStoryAction(callback);
			break;
		case "BACKGROUND":
		{
			BigMapPanel.SetActive(false);
			string key6 = array[0];
			Sprite image = Resource.GetImage(key6);
			if (image != null)
			{
				SetBackground(image, 1f);
			}
			ExecuteNextStoryAction(callback);
			break;
		}
		case "BATTLE":
			RuntimeData.Instance.gameEngine.CurrentSceneType = "battle";
			RuntimeData.Instance.gameEngine.CurrentSceneValue = action.value;
			RuntimeData.Instance.gameEngine.battleType = BattleType.Common;
			RuntimeData.Instance.gameEngine.BattleSelectRole_CurrentForbbidenKeys.Clear();
			RuntimeData.Instance.gameEngine.BattleSelectRole_CurrentCancelCallback = null;
			RuntimeData.Instance.gameEngine.BattleSelectRole_BattleCallback = delegate(string rst)
			{
				if (callback == null)
				{
					_storyResult = rst;
					StoryFinished();
				}
				else
				{
					callback();
				}
			};
			Application.LoadLevel("BattleSelectRole");
			break;
		case "SELECT_MENPAI":
			RuntimeData.Instance.gameEngine.SwitchGameScene("menpai", string.Empty);
			break;
		case "MUSIC":
		{
			string key7 = array[0];
			AudioManager.Instance.Play(key7);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "JOIN":
		{
			string value4 = action.value;
			string roleName24 = CommonSettings.getRoleName(value4);
			RuntimeData.Instance.addTeamMember(value4, roleName24);
			string text44 = "江湖" + Tools.chineseNumber[RuntimeData.Instance.Date.Year] + "年" + Tools.chineseNumber[RuntimeData.Instance.Date.Month] + "月" + Tools.chineseNumber[RuntimeData.Instance.Date.Day] + "日";
			RuntimeData instance7 = RuntimeData.Instance;
			string log = instance7.Log;
			instance7.Log = log + text44 + "，【" + roleName24 + "】加入队伍。\r\n";
			ShowDialog(value4, "【" + roleName24 + "】加入队伍。", callback);
			break;
		}
		case "LEAVE":
		{
			string value3 = action.value;
			string roleName21 = CommonSettings.getRoleName(value3);
			RuntimeData.Instance.removeTeamMember(value3);
			string text40 = "江湖" + Tools.chineseNumber[RuntimeData.Instance.Date.Year] + "年" + Tools.chineseNumber[RuntimeData.Instance.Date.Month] + "月" + Tools.chineseNumber[RuntimeData.Instance.Date.Day] + "日";
			RuntimeData instance6 = RuntimeData.Instance;
			string log = instance6.Log;
			instance6.Log = log + text40 + "，【" + roleName21 + "】离开。\r\n";
			ShowDialog(value3, "【" + roleName21 + "】离开队伍。", callback);
			break;
		}
		case "LEAVE_ALL":
		{
			RuntimeData.Instance.removeAllTeamMember();
			string text39 = "江湖" + Tools.chineseNumber[RuntimeData.Instance.Date.Year] + "年" + Tools.chineseNumber[RuntimeData.Instance.Date.Month] + "月" + Tools.chineseNumber[RuntimeData.Instance.Date.Day] + "日";
			RuntimeData instance5 = RuntimeData.Instance;
			instance5.Log = instance5.Log + text39 + "，【全体队友离开队伍。\r\n";
			ShowDialog("主角", "全体队友离开队伍。", callback);
			break;
		}
		case "FOLLOW":
		{
			string value2 = action.value;
			string roleName20 = CommonSettings.getRoleName(value2);
			RuntimeData.Instance.addFollowMember(value2, roleName20);
			string text37 = "江湖" + Tools.chineseNumber[RuntimeData.Instance.Date.Year] + "年" + Tools.chineseNumber[RuntimeData.Instance.Date.Month] + "月" + Tools.chineseNumber[RuntimeData.Instance.Date.Day] + "日";
			RuntimeData instance4 = RuntimeData.Instance;
			string log = instance4.Log;
			instance4.Log = log + text37 + "，【" + roleName20 + "】随队行动（非战斗角色）。\r\n";
			string text38 = "【" + roleName20 + "】随队行动（非战斗角色）。";
			messageBox.Show("提示", text38, Color.white, delegate
			{
				ExecuteNextStoryAction(callback);
			});
			break;
		}
		case "LEAVE_FOLLOW":
		{
			string value = action.value;
			string roleName15 = CommonSettings.getRoleName(value);
			RuntimeData.Instance.removeFollowMember(value);
			string text30 = "江湖" + Tools.chineseNumber[RuntimeData.Instance.Date.Year] + "年" + Tools.chineseNumber[RuntimeData.Instance.Date.Month] + "月" + Tools.chineseNumber[RuntimeData.Instance.Date.Day] + "日";
			RuntimeData instance3 = RuntimeData.Instance;
			string log = instance3.Log;
			instance3.Log = log + text30 + "，【" + roleName15 + "】离开（非战斗角色）。\r\n";
			string text31 = "【" + roleName15 + "】离开队伍。（非战斗角色）";
			messageBox.Show("提示", text31, Color.white, delegate
			{
				ExecuteNextStoryAction(callback);
			});
			break;
		}
		case "HAOGAN":
		{
			string text20 = "女主";
			int num11 = 0;
			if (array.Length == 1)
			{
				num11 = int.Parse(array[0]);
			}
			else
			{
				text20 = array[0];
				num11 = int.Parse(array[1]);
			}
			RuntimeData.Instance.addHaogan(num11, text20);
			Debug.Log("haogan:" + text20 + num11);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "DAODE":
		{
			int num8 = int.Parse(action.value);
			Debug.Log("daode:" + num8);
			RuntimeData.Instance.Daode = RuntimeData.Instance.Daode + num8;
			if (num8 > 0)
			{
				ShowDialog("主角", "道德值提高" + num8 + "点", callback);
			}
			else
			{
				ShowDialog("主角", "道德值降低" + -num8 + "点", callback);
			}
			break;
		}
		case "ITEM":
		{
			string text7 = array[0];
			int num5 = 1;
			if (array.Length > 1)
			{
				num5 = int.Parse(array[1]);
			}
			if (num5 > 0)
			{
				RuntimeData.Instance.addItem(Item.GetItem(text7), num5);
				ShowDialog("主角", "得到 " + text7 + " x " + Math.Abs(num5), callback);
				AudioManager.Instance.PlayEffect("音效.升级");
			}
			else
			{
				RuntimeData.Instance.addItem(Item.GetItem(text7), num5);
				ShowDialog("主角", "失去 " + text7 + " x " + Math.Abs(num5), callback);
				AudioManager.Instance.PlayEffect("音效.装备");
			}
			break;
		}
		case "NEWBIE":
		{
			RuntimeData.Instance.NewbieTask = action.value;
			Task task = ResourceManager.Get<Task>(action.value);
			RuntimeData.Instance.AddLog("接到新手任务：" + task.Desc);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "LOG":
		{
			string text5 = "江湖" + Tools.chineseNumber[RuntimeData.Instance.Date.Year] + "年" + Tools.chineseNumber[RuntimeData.Instance.Date.Month] + "月" + Tools.chineseNumber[RuntimeData.Instance.Date.Day] + "日";
			RuntimeData instance2 = RuntimeData.Instance;
			string log = instance2.Log;
			instance2.Log = log + text5 + "，" + action.value + "\r\n";
			ExecuteNextStoryAction(callback);
			break;
		}
		case "MENPAI":
		{
			string text3 = "江湖" + Tools.chineseNumber[RuntimeData.Instance.Date.Year] + "年" + Tools.chineseNumber[RuntimeData.Instance.Date.Month] + "月" + Tools.chineseNumber[RuntimeData.Instance.Date.Day] + "日";
			RuntimeData instance = RuntimeData.Instance;
			string log = instance.Log;
			instance.Log = log + text3 + "，加入" + action.value + "。\r\n";
			RuntimeData.Instance.Menpai = action.value;
			ExecuteNextStoryAction(callback);
			break;
		}
		case "NICK":
			ModData.addNick(action.value);
			RuntimeData.Instance.CurrentNick = action.value;
			UIStatePanelNickText.GetComponent<Text>().text = action.value;
			ShowDialog("主角", "获得称号：【" + action.value + "】！", callback);
			break;
		case "COST_MONEY":
		{
			int num = int.Parse(action.value);
			RuntimeData.Instance.Money = RuntimeData.Instance.Money - num;
			ShowDialog("主角", "失去 " + num + " 两银子。", callback);
			break;
		}
		case "GET_MONEY":
		{
			int num29 = int.Parse(action.value);
			RuntimeData.Instance.Money = RuntimeData.Instance.Money + num29;
			ShowDialog("主角", "得到 " + num29 + " 两银子。", callback);
			break;
		}
		case "YUANBAO":
		{
			int num27 = int.Parse(action.value);
			RuntimeData.Instance.Yuanbao += num27;
			if (num27 > 0)
			{
				ShowDialog("主角", "得到元宝" + num27, callback);
			}
			else
			{
				ShowDialog("主角", "失去元宝" + Math.Abs(num27), callback);
			}
			break;
		}
		case "COST_ITEM":
		{
			string text43 = array[0];
			int num26 = int.Parse(array[1]);
			RuntimeData.Instance.addItem(Item.GetItem(text43), -num26);
			ShowDialog("主角", "失去 " + text43 + " x " + num26, callback);
			break;
		}
		case "COST_DAY":
		{
			int num23 = int.Parse(action.value);
			RuntimeData.Instance.Date = RuntimeData.Instance.Date.AddDays(num23);
			ShowDialog("主角", "一共用了" + num23 + "天...", callback);
			break;
		}
		case "COST_HOUR":
		{
			int num22 = int.Parse(action.value);
			RuntimeData.Instance.Date = RuntimeData.Instance.Date.AddHours(num22);
			ShowDialog("主角", "过了" + num22 + "个时辰...", callback);
			break;
		}
		case "GET_POINT":
		{
			string text34 = array[0];
			int num19 = int.Parse(array[1]);
			foreach (Role item2 in RuntimeData.Instance.Team)
			{
				if (item2.Key == text34)
				{
					item2.leftpoint += num19;
					if (item2.leftpoint < 0)
					{
						item2.leftpoint = 0;
					}
				}
			}
			string roleName17 = CommonSettings.getRoleName(text34);
			if (num19 > 0)
			{
				ShowDialog(text34, roleName17 + "属性分配点增加【" + num19 + "】！", callback);
			}
			else
			{
				ShowDialog(text34, roleName17 + "属性分配点减少【" + -num19 + "】！", callback);
			}
			break;
		}
		case "MINUS_MAXPOINTS":
		{
			string text24 = array[0];
			int num14 = int.Parse(array[1]);
			float num15 = (float)num14 * 0.1f;
			bool flag12 = false;
			foreach (Role item3 in RuntimeData.Instance.Team)
			{
				if (item3.Key == text24)
				{
					item3.maxhp = (int)((float)item3.maxhp * num15);
					item3.maxmp = (int)((float)item3.maxmp * num15);
					item3.leftpoint = (int)((float)item3.leftpoint * num15);
					item3.quanzhang = (int)((float)item3.quanzhang * num15);
					item3.jianfa = (int)((float)item3.jianfa * num15);
					item3.daofa = (int)((float)item3.daofa * num15);
					item3.qimen = (int)((float)item3.qimen * num15);
					item3.bili = (int)((float)item3.bili * num15);
					item3.shenfa = (int)((float)item3.shenfa * num15);
					item3.dingli = (int)((float)item3.dingli * num15);
					item3.fuyuan = (int)((float)item3.fuyuan * num15);
					item3.wuxing = (int)((float)item3.wuxing * num15);
					item3.gengu = (int)((float)item3.gengu * num15);
					flag12 = true;
				}
			}
			if (!flag12)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName13 = CommonSettings.getRoleName(text24);
			ShowDialog(text24, roleName13 + "气血、内力、全属性降为【" + num14 + "成】！", callback);
			break;
		}
		case "URL":
		{
			string url = array[0];
			Tools.openURL(url);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "UPGRADE.MAXHP":
		{
			string text14 = array[0];
			int num7 = int.Parse(array[1]);
			bool flag6 = false;
			foreach (Role item4 in RuntimeData.Instance.Team)
			{
				if (item4.Key == text14)
				{
					item4.maxhp += num7;
					if (item4.maxhp <= 100)
					{
						item4.maxhp = 100;
					}
					if (item4.maxhp > CommonSettings.MAX_HPMP)
					{
						item4.maxhp = CommonSettings.MAX_HPMP;
					}
					item4.hp = item4.maxhp;
					flag6 = true;
				}
			}
			if (!flag6)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName7 = CommonSettings.getRoleName(text14);
			if (num7 > 0)
			{
				ShowDialog(text14, roleName7 + "气血上限增加【" + num7 + "】！", callback);
			}
			else
			{
				ShowDialog(text14, roleName7 + "气血上限减少【" + -num7 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.MAXMP":
		{
			string text6 = array[0];
			int num4 = int.Parse(array[1]);
			bool flag2 = false;
			foreach (Role item5 in RuntimeData.Instance.Team)
			{
				if (item5.Key == text6)
				{
					item5.maxmp += num4;
					if (item5.maxmp <= 100)
					{
						item5.maxmp = 100;
					}
					if (item5.maxmp > CommonSettings.MAX_HPMP)
					{
						item5.maxmp = CommonSettings.MAX_HPMP;
					}
					item5.mp = item5.maxmp;
					flag2 = true;
				}
			}
			if (!flag2)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName3 = CommonSettings.getRoleName(text6);
			if (num4 > 0)
			{
				ShowDialog(text6, roleName3 + "内力上限增加【" + num4 + "】！", callback);
			}
			else
			{
				ShowDialog(text6, roleName3 + "内力上限减少【" + -num4 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.根骨":
		{
			string text46 = array[0];
			int num30 = int.Parse(array[1]);
			bool flag20 = false;
			foreach (Role item6 in RuntimeData.Instance.Team)
			{
				if (item6.Key == text46)
				{
					item6.gengu += num30;
					flag20 = true;
				}
			}
			if (!flag20)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName26 = CommonSettings.getRoleName(text46);
			if (num30 > 0)
			{
				ShowDialog(text46, roleName26 + "根骨增加【" + num30 + "】！", callback);
			}
			else
			{
				ShowDialog(text46, roleName26 + "根骨减少【" + -num30 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.身法":
		{
			string text42 = array[0];
			int num25 = int.Parse(array[1]);
			bool flag18 = false;
			foreach (Role item7 in RuntimeData.Instance.Team)
			{
				if (item7.Key == text42)
				{
					item7.shenfa += num25;
					flag18 = true;
				}
			}
			if (!flag18)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName23 = CommonSettings.getRoleName(text42);
			if (num25 > 0)
			{
				ShowDialog(text42, roleName23 + "身法增加【" + num25 + "】！", callback);
			}
			else
			{
				ShowDialog(text42, roleName23 + "身法减少【" + -num25 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.悟性":
		{
			string text35 = array[0];
			int num20 = int.Parse(array[1]);
			bool flag15 = false;
			foreach (Role item8 in RuntimeData.Instance.Team)
			{
				if (item8.Key == text35)
				{
					item8.wuxing += num20;
					flag15 = true;
				}
			}
			if (!flag15)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName18 = CommonSettings.getRoleName(text35);
			if (num20 > 0)
			{
				ShowDialog(text35, roleName18 + "悟性增加【" + num20 + "】！", callback);
			}
			else
			{
				ShowDialog(text35, roleName18 + "悟性减少【" + -num20 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.臂力":
		{
			string text23 = array[0];
			int num13 = int.Parse(array[1]);
			bool flag11 = false;
			foreach (Role item9 in RuntimeData.Instance.Team)
			{
				if (item9.Key == text23)
				{
					item9.bili += num13;
					flag11 = true;
				}
			}
			if (!flag11)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName12 = CommonSettings.getRoleName(text23);
			if (num13 > 0)
			{
				ShowDialog(text23, roleName12 + "臂力增加【" + num13 + "】！", callback);
			}
			else
			{
				ShowDialog(text23, roleName12 + "臂力减少【" + -num13 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.福缘":
		{
			string text17 = array[0];
			int num9 = int.Parse(array[1]);
			bool flag8 = false;
			foreach (Role item10 in RuntimeData.Instance.Team)
			{
				if (item10.Key == text17)
				{
					item10.fuyuan += num9;
					flag8 = true;
				}
			}
			if (!flag8)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName9 = CommonSettings.getRoleName(text17);
			if (num9 > 0)
			{
				ShowDialog(text17, roleName9 + "福缘增加【" + num9 + "】！", callback);
			}
			else
			{
				ShowDialog(text17, roleName9 + "福缘减少【" + -num9 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.定力":
		{
			string text10 = array[0];
			int num6 = int.Parse(array[1]);
			bool flag4 = false;
			foreach (Role item11 in RuntimeData.Instance.Team)
			{
				if (item11.Key == text10)
				{
					item11.dingli += num6;
					flag4 = true;
				}
			}
			if (!flag4)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName5 = CommonSettings.getRoleName(text10);
			if (num6 > 0)
			{
				ShowDialog(text10, roleName5 + "定力增加【" + num6 + "】！", callback);
			}
			else
			{
				ShowDialog(text10, roleName5 + "定力减少【" + -num6 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.拳掌":
		{
			string text4 = array[0];
			int num3 = int.Parse(array[1]);
			bool flag = false;
			foreach (Role item12 in RuntimeData.Instance.Team)
			{
				if (item12.Key == text4)
				{
					item12.quanzhang += num3;
					flag = true;
				}
			}
			if (!flag)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName = CommonSettings.getRoleName(text4);
			if (num3 > 0)
			{
				ShowDialog(text4, roleName + "拳掌增加【" + num3 + "】！", callback);
			}
			else
			{
				ShowDialog(text4, roleName + "拳掌减少【" + -num3 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.剑法":
		{
			string text45 = array[0];
			int num28 = int.Parse(array[1]);
			bool flag19 = false;
			foreach (Role item13 in RuntimeData.Instance.Team)
			{
				if (item13.Key == text45)
				{
					item13.jianfa += num28;
					flag19 = true;
				}
			}
			if (!flag19)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName25 = CommonSettings.getRoleName(text45);
			if (num28 > 0)
			{
				ShowDialog(text45, roleName25 + "剑法增加【" + num28 + "】！", callback);
			}
			else
			{
				ShowDialog(text45, roleName25 + "剑法减少【" + -num28 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.刀法":
		{
			string text41 = array[0];
			int num24 = int.Parse(array[1]);
			bool flag17 = false;
			foreach (Role item14 in RuntimeData.Instance.Team)
			{
				if (item14.Key == text41)
				{
					item14.daofa += num24;
					flag17 = true;
				}
			}
			if (!flag17)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName22 = CommonSettings.getRoleName(text41);
			if (num24 > 0)
			{
				ShowDialog(text41, roleName22 + "刀法增加【" + num24 + "】！", callback);
			}
			else
			{
				ShowDialog(text41, roleName22 + "刀法减少【" + -num24 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.奇门":
		{
			string text36 = array[0];
			int num21 = int.Parse(array[1]);
			bool flag16 = false;
			foreach (Role item15 in RuntimeData.Instance.Team)
			{
				if (item15.Key == text36)
				{
					item15.qimen += num21;
					flag16 = true;
				}
			}
			if (!flag16)
			{
				ExecuteNextStoryAction(callback);
				break;
			}
			string roleName19 = CommonSettings.getRoleName(text36);
			if (num21 > 0)
			{
				ShowDialog(text36, roleName19 + "奇门增加【" + num21 + "】！", callback);
			}
			else
			{
				ShowDialog(text36, roleName19 + "奇门减少【" + -num21 + "】！", callback);
			}
			break;
		}
		case "UPGRADE.SKILL":
		{
			string text32 = array[0];
			string text33 = array[1];
			int num18 = int.Parse(array[2]);
			string roleName16 = CommonSettings.getRoleName(text32);
			bool flag14 = false;
			foreach (Role item16 in RuntimeData.Instance.Team)
			{
				if (item16.Key != text32)
				{
					continue;
				}
				flag14 = true;
				SkillInstance skillInstance4 = null;
				foreach (SkillInstance skill in item16.Skills)
				{
					if (skill.Skill.Name == text33)
					{
						skillInstance4 = skill;
						break;
					}
				}
				if (skillInstance4 == null)
				{
					SkillInstance skillInstance2 = new SkillInstance();
					skillInstance2.name = text33;
					skillInstance2.level = num18;
					skillInstance2.Owner = item16;
					SkillInstance skillInstance5 = skillInstance2;
					skillInstance5.RefreshUniquSkills();
					skillInstance5.Exp = 0;
					item16.Skills.Add(skillInstance5);
					ShowDialog(text32, roleName16 + "掌握了武功【" + text33 + "】(" + num18 + "级)！", callback);
				}
				else if (skillInstance4.Level >= skillInstance4.MaxLevel)
				{
					ShowDialog(text32, roleName16 + "的武功【" + text33 + "】已经到达等级上限（" + skillInstance4.MaxLevel + "级)，无法再提升了！", callback);
				}
				else
				{
					skillInstance4.level += num18;
					if (skillInstance4.Level > skillInstance4.MaxLevel)
					{
						skillInstance4.level = skillInstance4.MaxLevel;
					}
					ShowDialog(text32, roleName16 + "的武功【" + text33 + "】提升了" + num18 + "级！", callback);
				}
				break;
			}
			if (!flag14)
			{
				ExecuteNextStoryAction(callback);
			}
			break;
		}
		case "LEARN.SKILL":
		{
			string text25 = array[0];
			string text26 = array[1];
			int num16 = int.Parse(array[2]);
			string roleName14 = CommonSettings.getRoleName(text25);
			bool flag13 = false;
			foreach (Role item17 in RuntimeData.Instance.Team)
			{
				if (item17.Key != text25)
				{
					continue;
				}
				flag13 = true;
				SkillInstance skillInstance = null;
				foreach (SkillInstance skill2 in item17.Skills)
				{
					if (skill2.Skill.Name == text26)
					{
						skillInstance = skill2;
						break;
					}
				}
				if (skillInstance == null)
				{
					SkillInstance skillInstance2 = new SkillInstance();
					skillInstance2.name = text26;
					skillInstance2.level = num16;
					skillInstance2.Owner = item17;
					SkillInstance skillInstance3 = skillInstance2;
					skillInstance3.RefreshUniquSkills();
					skillInstance3.Exp = 0;
					item17.Skills.Add(skillInstance3);
				}
				else
				{
					skillInstance.level = Math.Max(skillInstance.Level, num16);
				}
			}
			if (flag13)
			{
				ShowDialog(text25, roleName14 + "掌握了武功【" + text26 + "】" + num16 + "级！", callback);
			}
			else
			{
				ExecuteNextStoryAction(callback);
			}
			break;
		}
		case "UPGRADE.INTERNALSKILL":
		{
			string text21 = array[0];
			string text22 = array[1];
			int num12 = int.Parse(array[2]);
			string roleName11 = CommonSettings.getRoleName(text21);
			bool flag10 = false;
			foreach (Role item18 in RuntimeData.Instance.Team)
			{
				if (item18.Key != text21)
				{
					continue;
				}
				flag10 = true;
				InternalSkillInstance internalSkillInstance4 = null;
				foreach (InternalSkillInstance internalSkill in item18.InternalSkills)
				{
					if (internalSkill.Name == text22)
					{
						internalSkillInstance4 = internalSkill;
						break;
					}
				}
				if (internalSkillInstance4 == null)
				{
					InternalSkillInstance internalSkillInstance2 = new InternalSkillInstance();
					internalSkillInstance2.name = text22;
					internalSkillInstance2.level = num12;
					internalSkillInstance2.Owner = item18;
					InternalSkillInstance internalSkillInstance5 = internalSkillInstance2;
					internalSkillInstance5.RefreshUniquSkills();
					internalSkillInstance5.Exp = 0;
					item18.InternalSkills.Add(internalSkillInstance5);
					ShowDialog(text21, roleName11 + "掌握了内功【" + text22 + "】(" + num12 + "级)！", callback);
				}
				else if (internalSkillInstance4.Level >= internalSkillInstance4.MaxLevel)
				{
					ShowDialog(text21, roleName11 + "的内功【" + text22 + "】已经到达等级上限(" + internalSkillInstance4.MaxLevel + "级)！无法再提升了！", callback);
				}
				else
				{
					internalSkillInstance4.level += num12;
					if (internalSkillInstance4.Level > internalSkillInstance4.MaxLevel)
					{
						internalSkillInstance4.level = internalSkillInstance4.MaxLevel;
					}
					ShowDialog(text21, roleName11 + "的内功【" + text22 + "】提升了" + num12 + "级！", callback);
				}
				break;
			}
			if (!flag10)
			{
				ExecuteNextStoryAction(callback);
			}
			break;
		}
		case "LEARN.INTERNALSKILL":
		{
			string text18 = array[0];
			string text19 = array[1];
			int num10 = int.Parse(array[2]);
			string roleName10 = CommonSettings.getRoleName(text18);
			bool flag9 = false;
			foreach (Role item19 in RuntimeData.Instance.Team)
			{
				if (item19.Key != text18)
				{
					continue;
				}
				flag9 = true;
				InternalSkillInstance internalSkillInstance = null;
				foreach (InternalSkillInstance internalSkill2 in item19.InternalSkills)
				{
					if (internalSkill2.Name == text19)
					{
						internalSkillInstance = internalSkill2;
						break;
					}
				}
				if (internalSkillInstance == null)
				{
					InternalSkillInstance internalSkillInstance2 = new InternalSkillInstance();
					internalSkillInstance2.name = text19;
					internalSkillInstance2.level = num10;
					internalSkillInstance2.Owner = item19;
					internalSkillInstance2.equipped = 0;
					InternalSkillInstance internalSkillInstance3 = internalSkillInstance2;
					internalSkillInstance3.RefreshUniquSkills();
					internalSkillInstance3.Exp = 0;
					item19.InternalSkills.Add(internalSkillInstance3);
					item19.InitBind();
				}
				else
				{
					internalSkillInstance.level = Math.Max(internalSkillInstance.Level, num10);
				}
			}
			if (flag9)
			{
				ShowDialog(text18, roleName10 + "掌握了内功【" + text19 + "】" + num10 + "级！", callback);
			}
			else
			{
				ExecuteNextStoryAction(callback);
			}
			break;
		}
		case "LEARN.SPECIALSKILL":
		{
			string text15 = array[0];
			string text16 = array[1];
			string roleName8 = CommonSettings.getRoleName(text15);
			bool flag7 = false;
			foreach (Role item20 in RuntimeData.Instance.Team)
			{
				if (item20.Key != text15)
				{
					continue;
				}
				flag7 = true;
				SpecialSkillInstance specialSkillInstance = null;
				foreach (SpecialSkillInstance specialSkill in item20.SpecialSkills)
				{
					if (specialSkill.Name == text16)
					{
						specialSkillInstance = specialSkill;
						break;
					}
				}
				if (specialSkillInstance == null)
				{
					SpecialSkillInstance specialSkillInstance2 = new SpecialSkillInstance();
					specialSkillInstance2.name = text16;
					specialSkillInstance2.Owner = item20;
					SpecialSkillInstance item = specialSkillInstance2;
					item20.SpecialSkills.Add(item);
					item20.InitBind();
				}
			}
			if (flag7)
			{
				ShowDialog(text15, roleName8 + "掌握了特殊攻击【" + text16 + "】", callback);
			}
			else
			{
				ExecuteNextStoryAction(callback);
			}
			break;
		}
		case "LEARN.TALENT":
		{
			string text11 = array[0];
			string text12 = array[1];
			string roleName6 = CommonSettings.getRoleName(text11);
			bool flag5 = false;
			foreach (Role item21 in RuntimeData.Instance.Team)
			{
				if (item21.Key == text11)
				{
					if (!item21.Talents.Contains(text12))
					{
						item21.Talents.Add(text12);
					}
					flag5 = true;
				}
			}
			if (!flag5)
			{
				ExecuteNextStoryAction(callback);
			}
			else
			{
				ShowDialog(text11, roleName6 + "领悟了天赋【" + text12 + "】", callback);
			}
			break;
		}
		case "REMOVE.TALENT":
		{
			string text8 = array[0];
			string text9 = array[1];
			string roleName4 = CommonSettings.getRoleName(text8);
			bool flag3 = false;
			foreach (Role item22 in RuntimeData.Instance.Team)
			{
				if (item22.Key == text8)
				{
					flag3 = true;
					if (item22.Talents.Contains(text9))
					{
						item22.Talents.Remove(text9);
					}
				}
			}
			if (!flag3)
			{
				ExecuteNextStoryAction(callback);
			}
			else
			{
				ShowDialog(text8, roleName4 + "移除了天赋【" + text9 + "】", callback);
			}
			break;
		}
		case "CHANGE_FEMALE_NAME":
			nameInputPanel.Show("铃兰", delegate(string name)
			{
				RuntimeData.Instance.femaleName = name;
				ExecuteNextStoryAction(callback);
			});
			break;
		case "EFFECT":
		{
			string key5 = array[0];
			AudioManager.Instance.PlayEffect(key5);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "SET_TIME_KEY":
		{
			string key4 = array[0];
			int days = int.Parse(array[1]);
			string story = string.Empty;
			if (array.Length > 2)
			{
				story = array[2];
			}
			RuntimeData.Instance.AddTimeKeyStory(key4, days, story);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "CLEAR_TIME_KEY":
		{
			string key3 = array[0];
			RuntimeData.Instance.RemoveTimeKey(key3);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "SET_FLAG":
		{
			string key2 = array[0];
			RuntimeData.Instance.AddFlag(key2);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "CLEAR_FLAG":
		{
			string key = array[0];
			RuntimeData.Instance.RemoveFlag(key);
			ExecuteNextStoryAction(callback);
			break;
		}
		case "ANIMATION":
		{
			string roleKey = array[0];
			string animation = array[1];
			string roleName2 = CommonSettings.getRoleName(roleKey);
			foreach (Role item23 in RuntimeData.Instance.Team)
			{
				if (item23.Name == roleName2)
				{
					item23.Animation = animation;
				}
			}
			ExecuteNextStoryAction(callback);
			break;
		}
		case "HEAD":
			RuntimeData.Instance.Team[0].Head = array[0];
			RoleStatePanelObj.GetComponent<RoleStatePanelUI>().Refresh();
			ExecuteNextStoryAction(callback);
			break;
		case "GROWTEMPLATE":
			if (array.Length == 2)
			{
				foreach (Role item24 in RuntimeData.Instance.Team)
				{
					if (item24.Key == array[0])
					{
						item24.GrowTemplateValue = array[1];
					}
				}
			}
			ExecuteNextStoryAction(callback);
			break;
		case "MAXLEVEL":
		{
			string text = array[0];
			int level = int.Parse(array[1]);
			string text2 = _story.Name;
			int num2 = ModData.AddSkillMaxLevel(text, level, text2 + "_" + text);
			if (num2 == -1)
			{
				ExecuteNextStoryAction(callback);
			}
			else
			{
				ShowDialog("主角", string.Format("解锁技能精通！{0}的等级上限提升至{1}", text, num2), callback);
			}
			AudioManager.Instance.PlayEffect("音效.升级");
			break;
		}
		default:
			if (!LuaManager.Call<bool>("GameEngine_extendStoryAction", new object[4] { this, action, array, callback }))
			{
				Debug.LogError("invalid story action type:" + action.type);
				ExecuteNextStoryAction(callback);
			}
			break;
		}
	}

	public void ShowDialog(string role, string msg, CommonSettings.VoidCallBack callback)
	{
		DialogPanel.GetComponent<DialogUI>().Show(role, msg, callback);
	}

	public void ShowDialogs(List<StoryAction> actions, CommonSettings.VoidCallBack callback)
	{
		Story story = new Story();
		story.Actions = actions;
		LoadStory(story, callback);
	}

	public void ShowDialog(StoryAction action, CommonSettings.VoidCallBack callback)
	{
		DialogPanel.GetComponent<DialogUI>().Show(action, callback);
	}

	private void StoryFinished()
	{
		RuntimeData.Instance.StoryFinish(_story.Name, _storyResult);
		foreach (StoryResult result in _story.Results)
		{
			if (result.ret == null)
			{
				result.ret = "0";
			}
			if (!result.ret.Equals(_storyResult))
			{
				continue;
			}
			bool flag = true;
			foreach (Condition condition in result.Conditions)
			{
				if (!condition.IsTrue)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				RuntimeData.Instance.gameEngine.SwitchGameScene(result.type, result.value);
				return;
			}
		}
		if (_storyResult == "lose")
		{
			Application.LoadLevel("GameOver");
		}
		else
		{
			RuntimeData.Instance.gameEngine.SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
		}
	}

	private void SetBackground(Sprite sp, float alpha = 1f)
	{
		if (sp == null)
		{
			BackgroundImage.GetComponent<Image>().color = Color.black;
			BackgroundImage.GetComponent<Image>().sprite = null;
		}
		else
		{
			BackgroundImage.GetComponent<Image>().color = new Color(1f, 1f, 1f, alpha);
			BackgroundImage.GetComponent<Image>().sprite = sp;
			prevSprite = sp;
		}
	}

	private IEnumerator DrawMap(Map map)
	{
		_map = map;
		Clear();
		SetMapUIElementVisiable(true);
		float alpha = (float)CommonSettings.timeOpacity[RuntimeData.Instance.Date.Hour / 2];
		Music bg = map.GetRandomMusic();
		if (bg != null)
		{
			AudioManager.Instance.Play(bg.Name);
		}
		if (map.Name == "大地图")
		{
			BigMapPanel.SetActive(true);
			MapPanel.SetActive(false);
			prevSprite = null;
			SetBackground(null, 1f);
			BigMap.GetComponent<Image>().color = new Color(1f, 1f, 1f, alpha);
			BigMap.GetComponent<Image>().sprite = Resource.GetImage(map.Pic);
		}
		else
		{
			BigMapPanel.SetActive(false);
			MapPanel.SetActive(true);
			SetBackground(Resource.GetImage(map.Pic), alpha);
			Text descText = MapDescriptionPanelObj.transform.Find("DescText").GetComponent<Text>();
			descText.text = map.Desc;
			MapDescriptionPanelObj.SetActive(true);
		}
		if (map.Locations.Count > 0)
		{
			LocationInfoText.GetComponent<Text>().text = string.Format("{0}:{1}", RuntimeData.Instance.CurrentBigMap, RuntimeData.Instance.GetLocation(RuntimeData.Instance.CurrentBigMap));
		}
		else
		{
			LocationInfoText.GetComponent<Text>().text = map.Name.TrimEnd('1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
		}
		TimeInfoText.GetComponent<Text>().text = CommonSettings.DateToGameTime(RuntimeData.Instance.Date);
		MoneyTextObj.GetComponent<Text>().text = RuntimeData.Instance.Money.ToString();
		YuanbaoTextObj.GetComponent<Text>().text = RuntimeData.Instance.Yuanbao.ToString();
		if (!Configer.IsBigmapFullScreen)
		{
			foreach (MapLocation location in map.Locations)
			{
				if (location.getName().Equals(RuntimeData.Instance.GetLocation(RuntimeData.Instance.CurrentBigMap)))
				{
					float x = -location.X + 640;
					float y = -location.Y - 320;
					if (x < -1140f)
					{
						x = -1140f;
					}
					if (y > 640f)
					{
						y = 640f;
					}
					if (x > 0f)
					{
						x = 0f;
					}
					if (y < 0f)
					{
						y = 0f;
					}
					BigMap.transform.localPosition = new Vector3(-570f + x, 320f + y, 0f);
					break;
				}
			}
			yield return 0;
		}
		int locationCount = 0;
		foreach (MapLocation location2 in map.Locations)
		{
			AddLocation(location2);
			locationCount++;
			if (locationCount % 10 == 0)
			{
				yield return 0;
			}
		}
		yield return 0;
		int i = 0;
		foreach (MapRole maprole in map.MapRoles)
		{
			if (AddMapRole(maprole, i))
			{
				i++;
			}
		}
		yield return 0;
	}

	private void AddLocation(MapLocation location)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(LocationObjPrefab);
		MapLocation currentLocation = CurrentLocation;
		int num = 2;
		if (currentLocation != null)
		{
			double num2 = Math.Sqrt((currentLocation.x - location.x) * (currentLocation.x - location.x) + (currentLocation.y - location.y) * (currentLocation.y - location.y));
			num += (int)(num2 / 50.0 * 10.0);
		}
		gameObject.transform.SetParent(BigMap.transform.Find("MapLocationContainer"));
		gameObject.GetComponent<MapLocationUI>().Bind(this, location, num);
	}

	private bool AddMapRole(MapRole mapRole, int index)
	{
		MapEvent activeEvent = mapRole.GetActiveEvent();
		if (activeEvent == null)
		{
			return false;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(MapRoleObjPrefab);
		gameObject.transform.SetParent(MapRolePanel.transform);
		gameObject.GetComponent<MapRoleUI>().Bind(this, mapRole, index, activeEvent);
		return true;
	}

	private void Clear()
	{
		foreach (Transform item in BigMap.transform.Find("MapLocationContainer"))
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		BigMap.transform.Find("MapLocationContainer").DetachChildren();
		foreach (Transform item2 in MapRolePanel.transform)
		{
			UnityEngine.Object.Destroy(item2.gameObject);
		}
		MapRolePanel.transform.DetachChildren();
		SuggestPanelObj.SetActive(false);
	}

	public void ShowEventConfirmPanel(Sprite image, MapEvent evt, string name, string desc, int timeCost)
	{
		EventConfirmPanel.GetComponent<EventConfirmPanel>().Show(image, evt, name, desc, timeCost);
	}

	public void HideEventConfirmPanel()
	{
		EventConfirmPanel.gameObject.SetActive(false);
	}

	private void Start()
	{
		if (!RuntimeData.Instance.IsInited)
		{
			RuntimeData.Instance.Init();
			for (int i = 0; i < 10; i++)
			{
				string text = "save" + i;
				if (PlayerPrefs.HasKey(text))
				{
					string save = SaveManager.GetSave(text);
					RuntimeData.Instance.Load(save);
					break;
				}
			}
		}
		Init();
		RuntimeData.Instance.mapUI = this;
		GameEngine gameEngine = RuntimeData.Instance.gameEngine;
		if (gameEngine.CurrentSceneType == "map")
		{
			LoadMap(gameEngine.CurrentSceneValue);
		}
		else if (gameEngine.CurrentSceneType == "story")
		{
			LoadStory(gameEngine.CurrentSceneValue);
		}
		else if (gameEngine.CurrentSceneType == "runtimestory")
		{
			LoadStory(gameEngine.RuntimeStory, gameEngine.RuntimeCallback);
		}
	}

	private void Init()
	{
		DialogPanel.SetActive(false);
		SelectPanel.SetActive(false);
		RolePanel.SetActive(false);
		SystemPanelObj.SetActive(false);
		ItemDetailPanelObj.SetActive(false);
		SuggestPanelObj.SetActive(false);
		roleSelectMenu.Hide();
		nameInputPanel.Hide();
		AchievementPanelObj.SetActive(false);
		SkillSelectPanelObj.SetActive(false);
		SuggestPanelObj.SetActive(false);
		MessageBoxUIObj.SetActive(false);
		LogPanel.SetActive(false);
		itemMenu.Hide();
		EventConfirmPanel.SetActive(false);
		TeamPanelObj.SetActive(false);
		RoleSelectPanelObj.SetActive(false);
		ItemPanel.SetActive(false);
		InitScale();
	}

	private void InitDailyAward()
	{
		if (!GlobalData.KeyValues.ContainsKey("_dailyAward"))
		{
			GlobalData.KeyValues.Add("_dailyAward", string.Empty);
			DailyAwardObj.SetActive(true);
			return;
		}
		string text = GlobalData.KeyValues["_dailyAward"];
		string strB = DateTime.Today.ToString();
		if (text.CompareTo(strB) < 0)
		{
			DailyAwardObj.SetActive(true);
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F1) && !DialogPanel.activeSelf && !SelectPanel.activeSelf && !RuntimeData.Instance.AutoSaveOnly)
		{
			string content = RuntimeData.Instance.Save();
			SaveManager.SetSave("fastsave", content);
			AudioManager.Instance.PlayEffect("音效.装备");
		}
		else if (Input.GetKeyDown(KeyCode.F2) && !DialogPanel.activeSelf && !SelectPanel.activeSelf && !RuntimeData.Instance.AutoSaveOnly)
		{
			string save = SaveManager.GetSave("fastsave");
			RuntimeData.Instance.Load(save);
			RuntimeData.Instance.gameEngine.SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
		}
		else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
		{
			TeamPanelObj.SetActive(false);
			RolePanel.SetActive(false);
			SystemPanelObj.SetActive(false);
			ItemDetailPanelObj.SetActive(false);
			AchievementPanelObj.SetActive(false);
			LogPanel.SetActive(false);
			ItemPanel.SetActive(false);
		}
		else if (Input.GetKeyDown(KeyCode.F9))
		{
			LuaManager.Reload();
		}
	}

	public void OnTeamButton()
	{
		if (TeamPanelObj.activeSelf)
		{
			TeamPanelObj.SetActive(false);
		}
		else
		{
			ShowTeam();
		}
	}

	public void OnItemButton()
	{
		if (ItemPanel.activeSelf)
		{
			ItemPanel.SetActive(false);
		}
		else
		{
			ShowItemPanel();
		}
	}

	public void OnLogButton()
	{
		if (LogPanel.activeSelf)
		{
			LogPanel.SetActive(false);
		}
		else
		{
			ShowLogPanel();
		}
	}

	public void OnSystemButton()
	{
		SystemPanelObj.GetComponent<SystemPanelUI>().Show();
	}

	public void ShowItemPanel()
	{
		itemMenu.Show("物品列表", RuntimeData.Instance.Items, delegate(object ret)
		{
			ItemInstance item = ret as ItemInstance;
			SelectItem(item);
		}, delegate
		{
		});
	}

	public void ShowTeam()
	{
		TeamPanelObj.SetActive(true);
		TeamPanelObj.GetComponent<TeamPanelUI>().Show(delegate(string roleKey)
		{
			RoleSelectPanelObj.SetActive(false);
			TeamPanelObj.SetActive(false);
			RolePanel.GetComponent<RolePanelUI>().Show(RuntimeData.Instance.GetTeamRole(roleKey), delegate
			{
				ShowTeam();
			});
		});
	}

	public void SelectItem(ItemInstance item)
	{
		ItemDetailMode itemMode = ItemDetailMode.Disable;
		switch (item.Type)
		{
		case ItemType.Weapon:
		case ItemType.Armor:
		case ItemType.Accessories:
			itemMode = ItemDetailMode.Equipable;
			break;
		case ItemType.Book:
			itemMode = ItemDetailMode.Studiable;
			break;
		case ItemType.Costa:
		case ItemType.Mission:
			itemMode = ItemDetailMode.Disable;
			break;
		case ItemType.Canzhang:
			RuntimeData.Instance.addItem(item, -1);
			return;
		default:
			itemMode = ItemDetailMode.Usable;
			break;
		}
		ItemDetailPanelObj.GetComponent<ItemSkillDetailPanelUI>().Show(item, itemMode, delegate
		{
			itemMenu.Hide();
			RoleSelectPanelObj.SetActive(true);
			roleSelectMenu.Show(RuntimeData.Instance.Team, delegate(string ret)
			{
				RoleSelectPanelObj.SetActive(false);
				Role teamRole = RuntimeData.Instance.GetTeamRole(ret);
				OnUseItem(item, teamRole, itemMode);
			}, delegate(object obj)
			{
				Role r = obj as Role;
				return item.CanEquip(r);
			});
		}, delegate
		{
			itemMenu.gameObject.SetActive(true);
		});
	}

	public void OnUseItem(ItemInstance item, Role role, ItemDetailMode itemMode)
	{
		if (!item.CanEquip(role))
		{
			messageBox.Show("装备选取错误", "你的人物不满足物品使用条件，需要：\n<color='red'>" + item.EquipCase + "</color>", Color.white, delegate
			{
				itemMenu.gameObject.SetActive(true);
			});
			return;
		}
		switch (itemMode)
		{
		case ItemDetailMode.Equipable:
		case ItemDetailMode.Studiable:
		{
			ItemInstance equipment = role.GetEquipment(item.Type);
			if (equipment != null)
			{
				role.Equipment.Remove(equipment);
				AudioManager.Instance.PlayEffect("音效.装备");
				RuntimeData.Instance.addItem(equipment);
			}
			role.Equipment.Add(item);
			AudioManager.Instance.PlayEffect("音效.装备");
			RuntimeData.Instance.addItem(item, -1);
			RolePanel.GetComponent<RolePanelUI>().Show(role);
			RolePanel.GetComponent<RolePanelUI>().SetFocusZhuangbei();
			break;
		}
		case ItemDetailMode.Usable:
		{
			List<StoryAction> dialogs = new List<StoryAction>();
			if (item.Type == ItemType.Upgrade)
			{
				ItemResult itemResult = item.TryUse(role, role);
				if (itemResult.MaxHp != 0)
				{
					role.maxhp = role.Attributes["maxhp"] + itemResult.MaxHp;
					if (role.maxhp >= CommonSettings.MAX_HPMP)
					{
						role.maxhp = CommonSettings.MAX_HPMP;
					}
					role.hp = role.Attributes["maxhp"];
					dialogs.Add(StoryAction.CreateDialog(role, "气血上限增加了【" + itemResult.MaxHp + "】！"));
				}
				if (itemResult.MaxMp != 0)
				{
					role.maxmp = role.Attributes["maxmp"] + itemResult.MaxMp;
					if (role.maxmp >= CommonSettings.MAX_HPMP)
					{
						role.maxmp = CommonSettings.MAX_HPMP;
					}
					role.mp = role.Attributes["maxmp"];
					dialogs.Add(StoryAction.CreateDialog(role, "内力上限增加了【" + itemResult.MaxMp + "】！"));
				}
				LuaManager.Call("ITEM_OnUseUpgradeItem", itemResult, role);
				RuntimeData.Instance.addItem(item, -1);
				AudioManager.Instance.PlayEffect("音效.升级");
			}
			else if (item.Type == ItemType.SpeicalSkillBook)
			{
				foreach (SpecialSkillInstance specialSkill in role.SpecialSkills)
				{
					if (specialSkill.name == item.GetItemSkill().SkillName)
					{
						messageBox.Show("错误", "不能使用,该角色已经学会该项技能", Color.white, delegate
						{
							itemMenu.gameObject.SetActive(true);
						});
						return;
					}
				}
				SpecialSkillInstance specialSkillInstance = new SpecialSkillInstance();
				specialSkillInstance.name = item.GetItemSkill().SkillName;
				SpecialSkillInstance specialSkillInstance2 = specialSkillInstance;
				specialSkillInstance2.Owner = role;
				role.SpecialSkills.Add(specialSkillInstance2);
				dialogs.Add(StoryAction.CreateDialog(role, "领悟了特殊技能：【" + item.GetItemSkill().SkillName + "】！"));
				RuntimeData.Instance.addItem(item, -1);
			}
			else if (item.Type == ItemType.TalentBook)
			{
				string skillName = item.GetItemSkill().SkillName;
				foreach (string talent in role.Talents)
				{
					if (talent.Equals(skillName))
					{
						messageBox.Show("错误", "不能使用,该角色已经有该项天赋", Color.white, delegate
						{
							itemMenu.gameObject.SetActive(true);
						});
						return;
					}
				}
				int need = 0;
				if (!role.CanLearnTalent(skillName, ref need))
				{
					messageBox.Show("错误", "不能使用,该角色剩余武学常识不够，需要" + need, Color.white, delegate
					{
						itemMenu.gameObject.SetActive(true);
					});
					break;
				}
				role.Talents.Add(skillName);
				dialogs.Add(StoryAction.CreateDialog(role, "领悟了天赋：【" + skillName + "】！"));
				RuntimeData.Instance.addItem(item, -1);
			}
			else if (item.Type == ItemType.Special)
			{
				if (item.Name == "刀")
				{
					if (role.Female || role.Animal)
					{
						messageBox.Show("无法阉割！", "只有男性可以自宫", Color.white, delegate
						{
							itemMenu.gameObject.SetActive(true);
						});
						break;
					}
					if (role.HasTalent("阉人"))
					{
						messageBox.Show("无法阉割！", "已经阉割过了！想割也没得割喽~", Color.white, delegate
						{
							itemMenu.gameObject.SetActive(true);
						});
						break;
					}
					role.AddTalent("阉人");
					int num = (int)((double)role.Attributes["maxhp"] / 3.0);
					int num2 = (int)((double)role.Attributes["maxmp"] / 2.0);
					role.maxhp -= num;
					role.hp -= num;
					role.maxmp -= num2;
					role.mp -= num2;
					dialogs.Add(StoryAction.CreateDialog(role, role.Name + "已经变成了太监！从今以后可以重新做人，开启第二人生了。"));
					dialogs.Add(StoryAction.CreateDialog(role, role.Name + "减少最大气血" + num + "点！ T_T"));
					dialogs.Add(StoryAction.CreateDialog(role, role.Name + "减少最大内力" + num2 + "点！"));
				}
				if (item.Name == "洗练书")
				{
					List<SkillBox> list = new List<SkillBox>();
					foreach (SpecialSkillInstance specialSkill2 in role.SpecialSkills)
					{
						list.Add(specialSkill2);
					}
					foreach (SkillInstance skill in role.Skills)
					{
						list.Add(skill);
					}
					foreach (InternalSkillInstance internalSkill in role.InternalSkills)
					{
						if (internalSkill != role.GetEquippedInternalSkill())
						{
							list.Add(internalSkill);
						}
					}
					SkillSelectPanelObj.GetComponent<SkillSelectPanelUI>().Show(list, delegate(object skill)
					{
						SkillBox skillBox = skill as SkillBox;
						if (skillBox != null)
						{
							if (skillBox is SkillInstance)
							{
								role.Skills.Remove(skillBox as SkillInstance);
							}
							else if (skillBox is InternalSkillInstance)
							{
								role.InternalSkills.Remove(skillBox as InternalSkillInstance);
							}
							else if (skillBox is SpecialSkillInstance)
							{
								role.SpecialSkills.Remove(skillBox as SpecialSkillInstance);
							}
							dialogs.Add(StoryAction.CreateDialog(role, string.Format("{0}的技能【{1}】移除了！", role.Name, skillBox.Name)));
							RuntimeData.Instance.addItem(item, -1);
							AudioManager.Instance.PlayEffect("音效.恢复2");
							ShowDialogs(dialogs, null);
						}
					});
				}
			}
			else if (item.Type == ItemType.Canzhang)
			{
				string canzhangSkill = item.CanzhangSkill;
				bool flag = false;
				foreach (SkillInstance skill2 in role.Skills)
				{
					if (skill2.Name == canzhangSkill && skill2.MaxLevel < CommonSettings.MAX_SKILL_LEVEL)
					{
						dialogs.Add(StoryAction.CreateDialog(role, string.Format("{0}等级上限提高！", skill2.Name)));
						flag = true;
						break;
					}
					if (skill2.Name == canzhangSkill && skill2.MaxLevel >= CommonSettings.MAX_SKILL_LEVEL)
					{
						dialogs.Add(StoryAction.CreateDialog(role, string.Format("{0}等级已经达到上限，不能再提高了", skill2.Name)));
						break;
					}
				}
				foreach (InternalSkillInstance internalSkill2 in role.InternalSkills)
				{
					if (internalSkill2.Name == canzhangSkill && internalSkill2.MaxLevel < CommonSettings.MAX_SKILL_LEVEL)
					{
						dialogs.Add(StoryAction.CreateDialog(role, string.Format("{0}等级上限提高！", internalSkill2.Name)));
						flag = true;
						break;
					}
					if (internalSkill2.Name == canzhangSkill && internalSkill2.MaxLevel >= CommonSettings.MAX_SKILL_LEVEL)
					{
						dialogs.Add(StoryAction.CreateDialog(role, string.Format("{0}等级已经达到上限，不能再提高了", internalSkill2.Name)));
						break;
					}
				}
				if (flag)
				{
					RuntimeData.Instance.addItem(item, -1);
				}
				else
				{
					messageBox.Show("错误！", string.Format("错误,【{0}】没有技能【{1}】", role.Name, canzhangSkill), Color.white, delegate
					{
						itemMenu.gameObject.SetActive(true);
					});
				}
			}
			if (dialogs.Count > 0)
			{
				ShowDialogs(dialogs, null);
			}
			break;
		}
		}
	}

	public void ShowLogPanel()
	{
		LogPanel.SetActive(true);
		logMenu.Show();
	}

	public void OnMapScaleChanged()
	{
		float num = 1f;
		num = ((!Configer.IsBigmapFullScreen) ? 1f : 0.5f);
		BigMap.GetComponent<RectTransform>().localScale = new Vector3(num, num, 1f);
		ResetBigMapLocalPosition();
	}

	private void InitScale()
	{
		OnMapScaleChanged();
	}

	private void ResetBigMapLocalPosition()
	{
		BigMap.transform.localPosition = new Vector3(-570f, 320f, 0f);
	}

	public void DailyAward(GameObject sender)
	{
		try
		{
			string value = SystemInfo.operatingSystem + "_" + SystemInfo.deviceUniqueIdentifier + "_" + SystemInfo.deviceName;
			Hashtable hashtable = new Hashtable();
			hashtable.Add("uid", value);
			hashtable.Add("time", DateTime.Now);
			StartCoroutine(Tools.ServerRequest("http://120.24.166.63:8080/JY-X/award", hashtable, delegate(object resp)
			{
				Hashtable hashtable2 = resp as Hashtable;
				if (hashtable2 == null)
				{
					ShowDialog("主角", "获取奖励出错", null);
				}
				else
				{
					if (hashtable2["status"].ToString() != "0")
					{
						ShowDialog("主角", "奖品虽好，不要作弊哦！", null);
					}
					else
					{
						GlobalData.KeyValues["_dailyAward"] = DateTime.Today.ToString();
						LoadStory(hashtable2["story"].ToString());
					}
					sender.SetActive(false);
				}
			}));
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			ShowDialog("主角", "无法连接服务器，请检查网络设置", null);
		}
	}

	public void RefreshRoleState()
	{
		LoadMap(RuntimeData.Instance.CurrentBigMap);
	}
}
