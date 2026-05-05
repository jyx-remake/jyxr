using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class SceneSelectUI : MonoBehaviour
{
	public GameObject SelectMenuObj;

	public GameObject SelectMenuItem;

	public GameObject titleTextObj;

	private string[] levelList = new string[6] { "江湖宵小", "小有名气", "成名高手", "威震四方", "惊世骇俗", "天人合一" };

	private List<string> bonuses = new List<string>();

	public SelectMenu selectMenu
	{
		get
		{
			return SelectMenuObj.GetComponent<SelectMenu>();
		}
	}

	public Text titleText
	{
		get
		{
			return titleTextObj.GetComponent<Text>();
		}
	}

	public void LoadArena()
	{
		selectMenu.Clear();
		titleText.text = "请选择战斗模式..";
		foreach (Battle item in ResourceManager.GetAll<Battle>())
		{
			if (!item.IsArena)
			{
				continue;
			}
			GameObject gameObject = Object.Instantiate(SelectMenuItem);
			gameObject.gameObject.SetActive(true);
			gameObject.transform.Find("Text").GetComponent<Text>().text = item.Key.Replace("arena_", string.Empty);
			string battleKey = item.Key;
			gameObject.GetComponent<Button>().onClick.AddListener(delegate
			{
				GameEngine gm = RuntimeData.Instance.gameEngine;
				gm.CurrentSceneValue = battleKey;
				titleText.text = "请选择战斗难度..";
				selectMenu.Clear();
				for (int i = 0; i < levelList.Length; i++)
				{
					string text = levelList[i];
					GameObject gameObject2 = Object.Instantiate(SelectMenuItem);
					gameObject2.gameObject.SetActive(true);
					gameObject2.transform.Find("Text").GetComponent<Text>().text = text;
					int hard = i + 1;
					gameObject2.GetComponent<Button>().onClick.AddListener(delegate
					{
						gm.ArenaHardLevel = hard;
						Application.LoadLevel("BattleSelectRole");
					});
					selectMenu.AddItem(gameObject2);
				}
				selectMenu.Show();
			});
			selectMenu.AddItem(gameObject);
		}
		selectMenu.Show();
	}

	public void LoadTowers()
	{
		List<Tower> list = new List<Tower>();
		foreach (Tower item in ResourceManager.GetAll<Tower>())
		{
			Tower tower = item;
			string key = tower.Key;
			if (JudgeToOpenTower(key))
			{
				list.Add(tower);
			}
		}
		RuntimeData.Instance.gameEngine.BattleSelectRole_BattleCallback = delegate(string rst)
		{
			towerMapFinished(rst);
		};
		foreach (Tower item2 in list)
		{
			Tower tower2 = item2;
			string towerKey = tower2.Key;
			GameObject gameObject = Object.Instantiate(SelectMenuItem);
			gameObject.gameObject.SetActive(true);
			gameObject.transform.Find("Text").GetComponent<Text>().text = towerKey;
			gameObject.GetComponent<Button>().onClick.AddListener(delegate
			{
				Tower tower3 = ResourceManager.Get<Tower>(towerKey);
				TowerMap towerMap = null;
				int num = int.MaxValue;
				for (int i = 0; i < tower3.Maps.Count; i++)
				{
					if (tower3.Maps[i].Index < num)
					{
						num = tower3.Maps[i].Index;
						towerMap = tower3.Maps[i];
					}
				}
				Battle battle = ResourceManager.Get<Battle>(towerMap.Key);
				GameEngine gameEngine = RuntimeData.Instance.gameEngine;
				gameEngine.currentTower = tower3;
				gameEngine.CurrentTowerIndex = num;
				bonuses.Clear();
				gameEngine.CurrentSceneValue = battle.Key;
				Application.LoadLevel("BattleSelectRole");
			});
			selectMenu.AddItem(gameObject);
		}
		selectMenu.Show();
	}

	public bool JudgeToOpenTower(string towerKey)
	{
		Tower tower = ResourceManager.Get<Tower>(towerKey);
		foreach (Condition condition in tower.Conditions)
		{
			if (!TriggerLogic.judge(condition))
			{
				return false;
			}
		}
		return true;
	}

	private void towerMapFinished(string result)
	{
		GameEngine gm = RuntimeData.Instance.gameEngine;
		if (result.Equals("win"))
		{
			judgeTowerNick();
			if (gm.CurrentTowerIndex == gm.currentTower.Maps.Count - 1)
			{
				Story story = new Story();
				StoryAction storyAction = new StoryAction();
				storyAction.value = "北丑#恭喜你取得了胜利！你本场战斗的奖励为【" + towerGetBonusItem() + "】！";
				storyAction.type = "DIALOG";
				story.Actions.Add(storyAction);
				StoryAction storyAction2 = new StoryAction();
				storyAction2.type = "DIALOG";
				storyAction2.value = "北丑#恭喜你挑战天关【" + gm.currentTower.Key + "】成功！";
				story.Actions.Add(storyAction2);
				Story story2 = towerGetBonusStory();
				for (int i = 0; i < story2.Actions.Count; i++)
				{
					story.Actions.Add(story2.Actions[i]);
				}
				gm.RuntimeStory = story;
				gm.RuntimeCallback = null;
				gm.SwitchGameScene("runtimestory", string.Empty);
				return;
			}
			Story story3 = new Story();
			StoryAction storyAction3 = new StoryAction();
			storyAction3.value = "北丑#恭喜你取得了胜利！你本场战斗的奖励为【" + towerGetBonusItem() + "】！";
			storyAction3.type = "DIALOG";
			story3.Actions.Add(storyAction3);
			StoryAction storyAction4 = new StoryAction();
			storyAction4.value = "北丑#截止目前，你的奖励有：";
			for (int j = 0; j < bonuses.Count; j++)
			{
				if (j != bonuses.Count - 1)
				{
					storyAction4.value = storyAction4.value + "【" + bonuses[j] + "】、";
				}
				else
				{
					storyAction4.value = storyAction4.value + "【" + bonuses[j] + "】！";
				}
			}
			storyAction4.type = "DIALOG";
			story3.Actions.Add(storyAction4);
			StoryAction dialog3 = new StoryAction();
			dialog3.type = "SELECT";
			dialog3.value = "北丑#要继续挑战下一关吗？#挑战！（注意：下一场战斗失败则失去所有奖励！）#算了，拿着现在的奖励走人吧。";
			CommonSettings.VoidCallBack callback31 = delegate
			{
				if (gm.BattleSelectRole_CurrentForbbidenKeys.Count < RuntimeData.Instance.Team.Count)
				{
					gm.SwitchGameScene("nextTower", string.Empty);
				}
				else
				{
					Story story5 = new Story();
					StoryAction item = new StoryAction
					{
						type = "DIALOG",
						value = "北丑#你已经无人可以应战了，我只能强制结束本次天关挑战了！"
					};
					story5.Actions.Add(item);
					Story story6 = towerGetBonusStory();
					for (int k = 0; k < story6.Actions.Count; k++)
					{
						story5.Actions.Add(story6.Actions[k]);
					}
					gm.RuntimeStory = story5;
					gm.RuntimeCallback = null;
					gm.SwitchGameScene("runtimestory", string.Empty);
				}
			};
			CommonSettings.VoidCallBack callback32 = delegate
			{
				Story story5 = new Story();
				StoryAction item = new StoryAction
				{
					type = "DIALOG",
					value = "北丑#知难而退，也是一种勇气！"
				};
				story5.Actions.Add(item);
				Story story6 = towerGetBonusStory();
				for (int k = 0; k < story6.Actions.Count; k++)
				{
					story5.Actions.Add(story6.Actions[k]);
				}
				gm.RuntimeStory = story5;
				gm.RuntimeCallback = null;
				gm.SwitchGameScene("runtimestory", string.Empty);
			};
			CommonSettings.IntCallBack intCallback = delegate(int rst)
			{
				if (rst == 0)
				{
					callback31();
				}
				else
				{
					callback32();
				}
			};
			CommonSettings.VoidCallBack runtimeCallback = delegate
			{
				RuntimeData.Instance.mapUI.LoadSelection(dialog3, intCallback);
			};
			gm.RuntimeStory = story3;
			gm.RuntimeCallback = runtimeCallback;
			gm.SwitchGameScene("runtimestory", string.Empty);
		}
		else
		{
			Story story4 = new Story();
			StoryAction storyAction5 = new StoryAction();
			storyAction5.type = "DIALOG";
			storyAction5.value = "北丑#哦！你挂了。于是你毛都没有得到。";
			story4.Actions.Add(storyAction5);
			StoryAction storyAction6 = new StoryAction();
			storyAction6.type = "DIALOG";
			storyAction6.value = "主角#......";
			story4.Actions.Add(storyAction6);
			bonuses.Clear();
			gm.RuntimeStory = story4;
			gm.RuntimeCallback = null;
			gm.SwitchGameScene("runtimestory", string.Empty);
		}
	}

	public void judgeTowerNick()
	{
		GameEngine gameEngine = RuntimeData.Instance.gameEngine;
		foreach (TowerNick nick in gameEngine.currentTower.Maps[gameEngine.CurrentTowerIndex].Nicks)
		{
			ModData.addNick(nick.Name);
		}
	}

	public string towerGetBonusItem()
	{
		int num = 0;
		while (true)
		{
			num++;
			if (num > 100)
			{
				break;
			}
			GameEngine gameEngine = RuntimeData.Instance.gameEngine;
			List<TowerItem> items = gameEngine.currentTower.Maps[gameEngine.CurrentTowerIndex].Items;
			TowerItem towerItem = items[Tools.GetRandomInt(0, items.Count - 1) % items.Count];
			string key = "bonus_" + towerItem.Key;
			if (RuntimeData.Instance.KeyValues.ContainsKey(key) && towerItem.Number > 0)
			{
				int num2 = int.Parse(RuntimeData.Instance.KeyValues[key]);
				if (num2 >= towerItem.Number)
				{
					continue;
				}
			}
			if (!Tools.ProbabilityTest(towerItem.Probability))
			{
				continue;
			}
			if (towerItem.Number > 0)
			{
				if (!RuntimeData.Instance.KeyValues.ContainsKey(key))
				{
					RuntimeData.Instance.KeyValues[key] = "1";
				}
				else
				{
					int num3 = int.Parse(RuntimeData.Instance.KeyValues[key]);
					RuntimeData.Instance.KeyValues[key] = (num3 + 1).ToString();
				}
			}
			bonuses.Add(towerItem.Key);
			return towerItem.Key;
		}
		return "黑玉断续膏";
	}

	public Story towerGetBonusStory()
	{
		GameEngine gameEngine = RuntimeData.Instance.gameEngine;
		Story story = new Story();
		for (int i = 0; i <= gameEngine.CurrentTowerIndex; i++)
		{
			Battle battle = ResourceManager.Get<Battle>(gameEngine.currentTower.Maps[i].Key);
			string text = bonuses[i];
			if (text == "元宝")
			{
				RuntimeData.Instance.Yuanbao++;
			}
			else
			{
				RuntimeData.Instance.addItem(ItemInstance.Generate(text, true));
			}
			StoryAction storyAction = new StoryAction();
			storyAction.type = "DIALOG";
			storyAction.value = "北丑#这是你在第【" + (i + 1) + "】关【" + battle.Key + "】所获得的奖励！";
			StoryAction storyAction2 = new StoryAction();
			storyAction2.type = "DIALOG";
			storyAction2.value = "主角#获得【" + text + "】。";
			story.Actions.Add(storyAction);
			story.Actions.Add(storyAction2);
		}
		return story;
	}

	private void Start()
	{
		Init();
		GameEngine gameEngine = RuntimeData.Instance.gameEngine;
		if (gameEngine.battleType == BattleType.Tower)
		{
			LoadTowers();
		}
		else if (gameEngine.battleType == BattleType.Arena)
		{
			LoadArena();
		}
	}

	private void Init()
	{
	}

	private void Update()
	{
	}
}
