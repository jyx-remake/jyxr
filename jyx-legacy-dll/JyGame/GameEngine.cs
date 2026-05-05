using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace JyGame
{
	public class GameEngine
	{
		private string _type = "map";

		public int ArenaHardLevel = -1;

		private string _value = "大地图";

		private Story _story;

		private CommonSettings.VoidCallBack _rtcallback;

		public BattleType battleType;

		public Tower currentTower;

		public int CurrentTowerIndex;

		public List<string> BattleSelectRole_CurrentForbbidenKeys = new List<string>();

		public CommonSettings.VoidCallBack BattleSelectRole_CurrentCancelCallback;

		public Battle BattleSelectRole_GeneratedBattle;

		public CommonSettings.StringCallBack BattleSelectRole_BattleCallback;

		public Map CurrentLoadMap;

		public string CurrentInTrail = string.Empty;

		public static bool IsMobilePlatform
		{
			get
			{
				if (CommonSettings.DEBUG_FORCE_MOBILE_MODE)
				{
					return true;
				}
				return Application.isMobilePlatform;
			}
		}

		public string CurrentSceneType
		{
			get
			{
				return _type;
			}
			set
			{
				_type = value;
			}
		}

		public string CurrentSceneValue
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
			}
		}

		public Story RuntimeStory
		{
			get
			{
				return _story;
			}
			set
			{
				_story = value;
			}
		}

		public CommonSettings.VoidCallBack RuntimeCallback
		{
			get
			{
				return _rtcallback;
			}
			set
			{
				_rtcallback = value;
			}
		}

		public GameEngine()
		{
			Init();
			DOTween.Init();
		}

		public void Init()
		{
			ResourceManager.Init();
			Application.targetFrameRate = 100;
		}

		public void NewGame()
		{
			RuntimeData.Instance.KeyValues["original_主角之家.开场"] = "0";
			RuntimeData.Instance.SetLocation("大地图", LuaManager.GetConfig("gamestart_location"));
			RuntimeData.Instance.TrialRoles = string.Empty;
			RuntimeData.Instance.Rank = -1;
			string config = LuaManager.GetConfig("gamestart_story");
			SwitchGameScene("story", config);
		}

		public void NewGameJump()
		{
			RuntimeData.Instance.KeyValues["original_主角之家.开场"] = "0";
			RuntimeData.Instance.SetLocation("大地图", LuaManager.GetConfig("gamestart_location"));
			RuntimeData.Instance.TrialRoles = string.Empty;
			string config = LuaManager.GetConfig("gamestart_story");
			SwitchGameScene("story", config);
		}

		public void SwitchGameScene(string type, string value)
		{
			if (RuntimeData.Instance.judgeFinishTask())
			{
				return;
			}
			if (CurrentSceneType.Equals("story"))
			{
				RuntimeData.Instance.PrevStory = CurrentSceneValue;
			}
			CurrentSceneType = type;
			CurrentSceneValue = value;
			switch (type)
			{
			case "story":
				if (Application.loadedLevelName == "Map")
				{
					RuntimeData.Instance.mapUI.LoadStory(value);
				}
				else
				{
					LoadingUI.Load("Map");
				}
				return;
			case "map":
			{
				RuntimeData.Instance.ResetTeam();
				string text = RuntimeData.Instance.CheckTimeFlags();
				if (!text.Equals(string.Empty))
				{
					RuntimeData.Instance.mapUI.LoadStory(text);
					return;
				}
				if (Application.loadedLevelName == "Map")
				{
					RuntimeData.Instance.mapUI.LoadMap(value);
				}
				else
				{
					LoadingUI.Load("Map");
				}
				if (Configer.IsAutoSave)
				{
					string content = RuntimeData.Instance.Save();
					SaveManager.SetSave("autosave", content);
				}
				return;
			}
			case "runtimestory":
				if (Application.loadedLevelName == "Map")
				{
					RuntimeData.Instance.mapUI.LoadStory(RuntimeStory, RuntimeCallback);
				}
				else
				{
					LoadingUI.Load("Map");
				}
				return;
			case "url":
				Tools.openURL(value);
				SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
				return;
			case "rollroll":
				RollRole();
				return;
			case "tutorial":
				RuntimeData.Instance.mapUI.LoadMap("大地图");
				RuntimeData.Instance.mapUI.LoadStory("original_教学开始");
				return;
			case "battle":
				battleType = BattleType.Common;
				BattleSelectRole_CurrentForbbidenKeys.Clear();
				BattleSelectRole_CurrentCancelCallback = null;
				BattleSelectRole_BattleCallback = delegate(string rst)
				{
					if (rst == "win")
					{
						SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
					}
					else
					{
						SwitchGameScene("gameOver", string.Empty);
					}
				};
				Application.LoadLevel("BattleSelectRole");
				return;
			case "arena":
				battleType = BattleType.Arena;
				BattleSelectRole_CurrentForbbidenKeys.Clear();
				BattleSelectRole_CurrentCancelCallback = delegate
				{
					Application.LoadLevel("Map");
				};
				BattleSelectRole_BattleCallback = delegate
				{
					SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
				};
				Application.LoadLevel("BattleSelectScene");
				return;
			case "trial":
			{
				battleType = BattleType.Trial;
				BattleSelectRole_CurrentForbbidenKeys.Clear();
				string[] array = RuntimeData.Instance.TrialRoles.Split('#');
				foreach (string text2 in array)
				{
					string item = text2;
					BattleSelectRole_CurrentForbbidenKeys.Add(item);
				}
				CurrentSceneType = "battle";
				CurrentSceneValue = "试炼之地_战斗";
				BattleSelectRole_BattleCallback = delegate(string rst)
				{
					if (rst.Equals("win"))
					{
						string currentInTrail = RuntimeData.Instance.gameEngine.CurrentInTrail;
						Role role = null;
						foreach (Role item3 in RuntimeData.Instance.Team)
						{
							Role role2 = item3;
							if (role2.Key.Equals(currentInTrail))
							{
								role = role2;
								break;
							}
						}
						if (role != null)
						{
							RuntimeData instance = RuntimeData.Instance;
							instance.TrialRoles = instance.TrialRoles + "#" + role.Key;
							RuntimeData.Instance.TrialRoles.Trim('#');
							RuntimeData.Instance.AddLog(role.Name + "通过试炼之地");
							string text3 = "霹雳堂_" + role.Key;
							Story story = ResourceManager.Get<Story>(text3);
							if (story == null)
							{
								SwitchGameScene("story", "霹雳堂_胜利");
							}
							else
							{
								SwitchGameScene("story", text3);
							}
						}
					}
					else
					{
						SwitchGameScene("story", "original_试炼之地.失败");
					}
				};
				Application.LoadLevel("BattleSelectRole");
				return;
			}
			case "tower":
				battleType = BattleType.Tower;
				BattleSelectRole_CurrentForbbidenKeys.Clear();
				BattleSelectRole_CurrentCancelCallback = delegate
				{
					LoadingUI.Load("Map");
				};
				Application.LoadLevel("BattleSelectScene");
				return;
			case "nextTower":
			{
				battleType = BattleType.Tower;
				CurrentTowerIndex++;
				TowerMap towerMap3 = currentTower.Maps[CurrentTowerIndex];
				Battle battle3 = ResourceManager.Get<Battle>(towerMap3.Key);
				CurrentSceneValue = battle3.Key;
				Application.LoadLevel("BattleSelectRole");
				return;
			}
			case "huashan":
			{
				battleType = BattleType.Huashan;
				BattleSelectRole_CurrentForbbidenKeys.Clear();
				currentTower = ResourceManager.Get<Tower>("华山论剑");
				CurrentTowerIndex = 0;
				TowerMap towerMap2 = currentTower.Maps[CurrentTowerIndex];
				Battle battle2 = ResourceManager.Get<Battle>(towerMap2.Key);
				CurrentSceneValue = battle2.Key;
				BattleSelectRole_BattleCallback = delegate(string rst)
				{
					if (rst.Equals("win"))
					{
						SwitchGameScene("nextHuashan", string.Empty);
					}
					else
					{
						SwitchGameScene("gameOver", string.Empty);
					}
				};
				Application.LoadLevel("BattleSelectRole");
				return;
			}
			case "nextHuashan":
				battleType = BattleType.Huashan;
				CurrentTowerIndex++;
				if (CurrentTowerIndex < currentTower.Maps.Count)
				{
					TowerMap towerMap = currentTower.Maps[CurrentTowerIndex];
					Battle battle = ResourceManager.Get<Battle>(towerMap.Key);
					CurrentSceneValue = battle.Key;
					Application.LoadLevel("BattleSelectRole");
				}
				else
				{
					SwitchGameScene("story", "original_华山论剑分枝判断");
				}
				return;
			case "restart":
				RuntimeData.Instance.Rank = -1;
				RuntimeData.Instance.NextZhoumuClear();
				ModData.ParamAdd("end_count", 1);
				RollRole();
				return;
			case "nextZhoumu":
			{
				RuntimeData.Instance.Round++;
				int param = ModData.GetParam("max_round");
				if (RuntimeData.Instance.Round > param)
				{
					ModData.SetParam("max_round", RuntimeData.Instance.Round);
				}
				RuntimeData.Instance.Rank = -1;
				RuntimeData.Instance.NextZhoumuClear();
				ModData.ParamAdd("end_count", 1);
				RollRole();
				return;
			}
			case "gameOver":
				Application.LoadLevel("GameOver");
				return;
			case "gameFin":
				Application.LoadLevel("GameFin");
				return;
			case "shop":
			{
				Shop shop = ResourceManager.Get<Shop>(value);
				if (shop != null)
				{
					ShopUI.CurrentShop = shop;
					ShopUI.Type = ShopType.SHOP;
					Application.LoadLevel("Shop");
				}
				return;
			}
			case "game":
				PlaySmallGame(value);
				return;
			case "mainmenu":
				Application.LoadLevel("MainMenu");
				return;
			case "menpai":
				LoadingUI.Load("Menpai");
				return;
			case "xiangzi":
				ShopUI.Type = ShopType.XIANGZI;
				Application.LoadLevel("Shop");
				return;
			case "item":
				if (value.Split('#').Length > 1)
				{
					RuntimeData.Instance.addItem(new ItemInstance
					{
						Name = value.Split('#')[0]
					}, int.Parse(value.Split('#')[1]));
				}
				else
				{
					RuntimeData.Instance.addItem(new ItemInstance
					{
						Name = value
					});
				}
				return;
			case "randomitem":
			{
				for (int num = 0; num < int.Parse(value.Split('#')[1]); num++)
				{
					RuntimeData.Instance.addItem(ItemInstance.Generate(value.Split('#')[0], true));
				}
				return;
			}
			case "addround":
				RuntimeData.Instance.Round += int.Parse(value);
				return;
			case "clear":
				PlayerPrefs.DeleteKey(value);
				SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
				return;
			case "clearall":
				PlayerPrefs.DeleteAll();
				Application.LoadLevel("MainMenu");
				return;
			case "join":
				RuntimeData.Instance.addTeamMember(value);
				return;
			case "zhenlongqiju":
			{
				battleType = BattleType.Zhenlongqiju;
				BattleSelectRole_CurrentForbbidenKeys.Clear();
				CurrentSceneType = "battle";
				CurrentSceneValue = "珍珑棋局_战斗";
				string currentMode = RuntimeData.Instance.GameMode;
				RuntimeData.Instance.GameMode = "crazy";
				BattleSelectRole_BattleCallback = delegate(string rst)
				{
					RuntimeData.Instance.GameMode = currentMode;
					if (rst.Equals("win"))
					{
						ModData.ZhenlongqijuLevel++;
						SwitchGameScene("story", "珍珑棋局_胜利");
					}
					else
					{
						SwitchGameScene("story", "珍珑棋局_失败");
					}
				};
				Application.LoadLevel("BattleSelectRole");
				return;
			}
			case "zhenlonglevel":
				ModData.ZhenlongqijuLevel = int.Parse(value);
				return;
			case "xilian":
			{
				MapUI mapUI = RuntimeData.Instance.mapUI;
				Dictionary<ItemInstance, int> items = RuntimeData.Instance.GetItems(new ItemType[3]
				{
					ItemType.Armor,
					ItemType.Weapon,
					ItemType.Accessories
				});
				List<ItemInstance> list = new List<ItemInstance>();
				foreach (KeyValuePair<ItemInstance, int> item4 in items)
				{
					if (item4.Key.AdditionTriggers.Count == 0)
					{
						list.Add(item4.Key);
					}
				}
				foreach (ItemInstance item5 in list)
				{
					items.Remove(item5);
				}
				if (items.Count == 0)
				{
					SwitchGameScene("story", "洗练_没有装备");
					return;
				}
				mapUI.itemMenu.Show("选择要洗练的装备", items, delegate(object obj)
				{
					ItemInstance item2 = obj as ItemInstance;
					string itemKey = item2.PK;
					mapUI.ItemDetailPanelObj.GetComponent<ItemSkillDetailPanelUI>().Show(item2, ItemDetailMode.Selectable, delegate
					{
						ItemInstance selectItem = RuntimeData.Instance.GetItem(itemKey);
						List<string> opts = new List<string>();
						foreach (Trigger additionTrigger in selectItem.AdditionTriggers)
						{
							opts.Add(additionTrigger.ToString());
						}
						mapUI.LoadSelection("选择你要洗练的属性", opts, delegate(int selectIndex)
						{
							mapUI.itemMenu.Hide();
							List<string> opts2 = new List<string>();
							List<Trigger> newTriggers = new List<Trigger>();
							for (int i = 0; i < 8; i++)
							{
								Trigger trigger = selectItem.GenerateRandomTrigger();
								newTriggers.Add(trigger);
								opts2.Add(trigger.ToString());
							}
							opts2.Add("不替换了(" + opts[selectIndex] + ")");
							RuntimeData.Instance.Yuanbao--;
							mapUI.LoadSelection("选择你要替换的属性", opts2, delegate(int opt2SelectIndex)
							{
								if (opt2SelectIndex >= opts2.Count - 1)
								{
									SwitchGameScene("story", "洗练选择");
								}
								else
								{
									Trigger value2 = newTriggers[opt2SelectIndex];
									int index = -1;
									for (int j = 0; j < selectItem.AdditionTriggers.Count; j++)
									{
										if (item2.AdditionTriggers[j].ToString() == opts[selectIndex])
										{
											index = j;
											break;
										}
									}
									RuntimeData.Instance.addItem(selectItem, -1);
									selectItem.AdditionTriggers[index] = value2;
									RuntimeData.Instance.addItem(selectItem);
									AudioManager.Instance.PlayEffect("音效.装备");
									SwitchGameScene("story", "洗练_洗练成功");
								}
							});
						});
					}, delegate
					{
						SwitchGameScene("xilian", string.Empty);
					});
				}, delegate
				{
					SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
				}, (object obj) => (obj as ItemInstance).AdditionTriggers.Count > 0);
				return;
			}
			case "reloadlua":
				LuaManager.Reload();
				return;
			}
			if (!LuaManager.Call<bool>("GameEngine_extendConsole", new object[3] { this, type, value }))
			{
				string currentBigMap = RuntimeData.Instance.CurrentBigMap;
				CurrentSceneType = "map";
				CurrentSceneValue = currentBigMap;
				RuntimeData.Instance.ResetTeam();
				if (Application.loadedLevelName == "Map")
				{
					RuntimeData.Instance.mapUI.LoadMap(currentBigMap);
				}
				else
				{
					LoadingUI.Load("Map");
				}
			}
		}

		public void RollRole()
		{
			LoadingUI.Load("RollRole");
		}

		public void PlaySmallGame(string gameName)
		{
			switch (gameName)
			{
			case "levelup":
			{
				int num = 12;
				Condition condition = new Condition();
				condition.type = "should_finish";
				condition.value = "mainStory_黑暗的阴影1";
				if (TriggerLogic.judge(condition))
				{
					num = 18;
				}
				condition = new Condition();
				condition.type = "should_finish";
				condition.value = "mainStory_神秘剑客1";
				if (TriggerLogic.judge(condition))
				{
					num = 22;
				}
				condition = new Condition();
				condition.type = "should_finish";
				condition.value = "mainStory_紧急1";
				if (TriggerLogic.judge(condition))
				{
					num = 25;
				}
				List<string> list = new List<string>();
				foreach (Role item in RuntimeData.Instance.Team)
				{
					if (item.Level < num)
					{
						int add = item.LevelupExp - item.Exp + 1;
						item.AddExp(add);
						list.Add(item.Name);
					}
				}
				AudioManager.Instance.PlayEffect("音效.升级");
				RuntimeData.Instance.gameEngine.SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
				break;
			}
			case "qinggong":
				LoadingUI.Load("Dodge");
				break;
			case "dianxue":
				LoadingUI.Load("WhacAMole");
				break;
			default:
				RuntimeData.Instance.gameEngine.SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
				break;
			}
		}
	}
}
