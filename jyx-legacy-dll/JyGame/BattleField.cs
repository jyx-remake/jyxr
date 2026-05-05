using System;
using System.Collections;
using System.Collections.Generic;
using XLua;
using UnityEngine;
using UnityEngine.UI;

namespace JyGame
{
	public class BattleField : MonoBehaviour
	{
		private delegate void StatusLogicFun();

		public GameObject backgroundObj;

		public GameObject blockPrefab;

		public Camera mainCamera;

		public GameObject spriteActionMenu;

		public GameObject selectMenuObj;

		public GameObject cancelButtonObj;

		public GameObject screenEffectCover;

		public GameObject attackInfoLayer;

		public GameObject attackInfoPrefab;

		public GameObject screenEffectText;

		public GameObject aoyiRoleHeadImage;

		public GameObject battleResultObj;

		public GameObject skillSelectItemObj;

		public GameObject itemPanelObj;

		public GameObject messageBoxObj;

		public GameObject itemDetailPanelObj;

		public GameObject rolePanelObj;

		public GameObject logPanelObj;

		public GameObject BattleRoleListPanelObj;

		public GameObject suggestTextObj;

		public GameObject actionBarRoleHeadObj;

		public GameObject chuzhaoAnimationObj;

		public GameObject uiCanvasObj;

		private List<GameObject> _sprites = new List<GameObject>();

		private int _battleTimestamp;

		private GameObject[,] _blocks;

		private Battle _battle;

		private BattleAI _ai;

		private bool selectingBlock;

		private BattleBlock currentBlock;

		private BattleStatus _status;

		private Dictionary<BattleStatus, StatusLogicFun> _statusMap = new Dictionary<BattleStatus, StatusLogicFun>();

		public GameObject TimeLabelObj;

		private Queue<BattleSprite> activePersons = new Queue<BattleSprite>();

		public GameObject SkillSuggestPanel;

		private bool _isAI;

		private AIResult _aiResult;

		private int rollbackCurrentX;

		private int rollbackCurrentY;

		private bool rollbackCurrentFace;

		private bool isEnd;

		[HideInInspector]
		public BattleSprite currentSprite;

		public GameObject SkillSelectPanelObj;

		private SkillBox currentSkill;

		private int skilltarget_x;

		private int skilltarget_y;

		private bool skillCallbackTag;

		private ItemInstance currentItem;

		private bool _win;

		private bool isLog;

		public Text suggestText
		{
			get
			{
				return suggestTextObj.GetComponent<Text>();
			}
		}

		public SelectMenu selectMenu
		{
			get
			{
				return selectMenuObj.GetComponent<SelectMenu>();
			}
		}

		public Button cancelButton
		{
			get
			{
				return cancelButtonObj.GetComponent<Button>();
			}
		}

		public Image background
		{
			get
			{
				return backgroundObj.GetComponent<Image>();
			}
		}

		public ItemMenu itemMenu
		{
			get
			{
				return itemPanelObj.GetComponent<ItemMenu>();
			}
		}

		public MessageBoxUI messageBox
		{
			get
			{
				return messageBoxObj.GetComponent<MessageBoxUI>();
			}
		}

		public ItemSkillDetailPanelUI itemDetailPanel
		{
			get
			{
				return itemDetailPanelObj.GetComponent<ItemSkillDetailPanelUI>();
			}
		}

		public static int MOVEBLOCK_MAX_X
		{
			get
			{
				return LuaManager.GetConfigInt("BATTLE_MOVEBLOCK_MAX_X");
			}
		}

		public static int MOVEBLOCK_MAX_Y
		{
			get
			{
				return LuaManager.GetConfigInt("BATTLE_MOVEBLOCK_MAX_Y");
			}
		}

		public IEnumerable<BattleSprite> Sprites
		{
			get
			{
				foreach (GameObject s in _sprites)
				{
					yield return s.GetComponent<BattleSprite>();
				}
			}
		}

		public LuaTable SpritesTable
		{
			get
			{
				return Sprites.toLuaTable();
			}
		}

		public BattleAI AI
		{
			get
			{
				return _ai;
			}
		}

		public BattleStatus Status
		{
			get
			{
				return _status;
			}
			set
			{
				suggestText.text = string.Empty;
				_status = value;
				if (_statusMap.ContainsKey(_status))
				{
					_statusMap[_status]();
				}
			}
		}

		public IEnumerator Init(Battle battle)
		{
			_battle = battle;
			uiCanvasObj.SetActive(false);
			if (!string.IsNullOrEmpty(battle.Music))
			{
				AudioManager.Instance.Play(battle.Music);
			}
			else
			{
				AudioManager.Instance.Play(CommonSettings.GetRandomBattleMusic());
			}
			if (!string.IsNullOrEmpty(battle.MapKey))
			{
				background.sprite = Resource.GetBattleBg(battle.MapKey);
			}
			else if (!string.IsNullOrEmpty(battle.Map))
			{
				background.sprite = Resource.GetImage(battle.Map);
			}
			else
			{
				background.sprite = Resource.GetBattleBg("zhulin");
			}
			yield return 0;
			selectMenu.Clear();
			selectMenu.Hide();
			cancelButton.gameObject.SetActive(false);
			screenEffectCover.SetActive(false);
			battleResultObj.SetActive(false);
			_battleTimestamp = 0;
			spriteActionMenu.SetActive(false);
			_sprites.Clear();
			foreach (BattleRole r in battle.Roles)
			{
				GameObject sprite = BattleSprite.Create(this, r);
				if (sprite != null)
				{
					_sprites.Add(sprite);
					yield return 0;
				}
			}
			if (currentSprite != null)
			{
				currentSprite.IsCurrent = false;
				currentSprite = null;
			}
			Status = BattleStatus.Starting;
			uiCanvasObj.SetActive(true);
			yield return 0;
		}

		public bool IsRoleInField(string role, int team = 1)
		{
			foreach (BattleSprite sprite in Sprites)
			{
				if (sprite.Role.Name == role && sprite.Team == team)
				{
					return true;
				}
			}
			return false;
		}

		private void InitMoveBlocks()
		{
			_blocks = new GameObject[MOVEBLOCK_MAX_X, MOVEBLOCK_MAX_Y];
			for (int i = 0; i < MOVEBLOCK_MAX_X; i++)
			{
				for (int j = 0; j < MOVEBLOCK_MAX_Y; j++)
				{
					_blocks[i, j] = MakeBlock(i, j);
				}
			}
		}

		private GameObject MakeBlock(int x, int y)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(blockPrefab);
			gameObject.transform.position = new Vector3(ToScreenX(x), ToScreenY(y), 0f);
			gameObject.transform.parent = base.transform;
			return gameObject;
		}

		private void Start()
		{
			AttackLogic.field = this;
			if (!RuntimeData.Instance.IsInited)
			{
				RuntimeData.Instance.Init();
			}
			RuntimeData.Instance.battleFieldUI = this;
			InitStatusMap();
			InitMoveBlocks();
			_ai = new BattleAI(this);
			if (RuntimeData.Instance.gameEngine.BattleSelectRole_GeneratedBattle == null)
			{
				RuntimeData.Instance.gameEngine.BattleSelectRole_GeneratedBattle = ResourceManager.Get<Battle>("测试战斗");
			}
			StartCoroutine(Init(RuntimeData.Instance.gameEngine.BattleSelectRole_GeneratedBattle));
		}

		private void Update()
		{
			DoBlocksLogic(BattleStatus.UISelectMove, delegate(object s)
			{
				(s as BattleBlock).SetFocus(false);
			}, delegate(object s)
			{
				(s as BattleBlock).SetFocus(true);
			}, delegate(int x, int y)
			{
				RoleMoveTo(currentSprite, x, y);
			});
			DoBlocksLogic(BattleStatus.UISelectAction, delegate
			{
				ShowCurrentAttackRange();
			}, delegate(object s)
			{
				BattleBlock battleBlock = s as BattleBlock;
				battleBlock.SetFocus(true);
				foreach (LocationBlock relatedBlock in battleBlock.RelatedBlocks)
				{
					if (relatedBlock.X >= 0 && relatedBlock.X < MOVEBLOCK_MAX_X && relatedBlock.Y >= 0 && relatedBlock.Y < MOVEBLOCK_MAX_Y)
					{
						BattleBlock component = _blocks[relatedBlock.X, relatedBlock.Y].GetComponent<BattleBlock>();
						component.Status = BattleBlockStatus.HighLightGreen;
					}
				}
			}, delegate(int x, int y)
			{
				skilltarget_x = x;
				skilltarget_y = y;
				cancelButton.gameObject.SetActive(false);
				cancelButton.onClick.RemoveAllListeners();
				currentSkill = currentSprite.CurrentSkill;
				spriteActionMenu.SetActive(false);
				PreCastSkill();
			});
			DoBlocksLogic(BattleStatus.UISelectItemTarget, delegate(object s)
			{
				(s as BattleBlock).SetFocus(false);
			}, delegate(object s)
			{
				(s as BattleBlock).SetFocus(true);
			}, delegate(int x, int y)
			{
				cancelButton.gameObject.SetActive(false);
				cancelButton.onClick.RemoveAllListeners();
				BattleSprite sprite = GetSprite(x, y);
				if (sprite != null)
				{
					ClearAllBlocks();
					OnUseItemTarget(currentItem, sprite);
				}
				else
				{
					ClearAllBlocks();
					Status = BattleStatus.UISelectItem;
				}
			});
			if (Status == BattleStatus.UISelectAction)
			{
				if (Input.GetKeyDown(KeyCode.Q))
				{
					RoleListButtonClicked();
				}
				else if (Input.GetKeyDown(KeyCode.W))
				{
					OnMoveButtonClicked();
				}
				else if (Input.GetKeyDown(KeyCode.E))
				{
					OnItemButtonClicked();
				}
				else if (Input.GetKeyDown(KeyCode.R))
				{
					OnRestButtonClicked();
				}
			}
			if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButton(1))
			{
				if (Status == BattleStatus.UISelectAction && (currentSprite.X != rollbackCurrentX || currentSprite.Y != rollbackCurrentY))
				{
					ClearAllBlocks();
					RollBackRolePosition();
					ShowCurrentAttackRange();
				}
				else if (Status == BattleStatus.UISelectTarget || Status == BattleStatus.UISelectItemTarget || Status == BattleStatus.UISelectMove)
				{
					if (cancelButton.IsActive())
					{
						cancelButton.onClick.Invoke();
					}
				}
				else if (Status == BattleStatus.UISelectItem)
				{
					itemMenu.Hide();
					Status = BattleStatus.UISelectAction;
				}
				if (rolePanelObj.activeSelf)
				{
					rolePanelObj.GetComponent<RolePanelUI>().Hide();
				}
			}
			if (Input.GetKeyDown(KeyCode.Tab))
			{
				foreach (BattleSprite sprite2 in Sprites)
				{
					sprite2.ShowHpMpSpBar();
				}
			}
			if (!Input.GetKeyUp(KeyCode.Tab))
			{
				return;
			}
			foreach (BattleSprite sprite3 in Sprites)
			{
				sprite3.HideHpMpSpBar();
			}
		}

		private void DoBlocksLogic(BattleStatus status, CommonSettings.ObjectCallBack onClearFocusBlock, CommonSettings.ObjectCallBack onFocusBlock, CommonSettings.Int2CallBack onSelectBlock)
		{
			if (Status != status || rolePanelObj.activeSelf || BattleRoleListPanelObj.activeSelf || itemPanelObj.activeSelf || itemDetailPanelObj.activeSelf)
			{
				return;
			}
			if (Input.GetMouseButtonDown(0))
			{
				selectingBlock = true;
			}
			if (Input.GetMouseButtonUp(0))
			{
				if (!selectingBlock)
				{
					return;
				}
				selectingBlock = false;
				if ((bool)currentBlock)
				{
					onClearFocusBlock(currentBlock);
					currentBlock = null;
				}
				Vector3 vector = mainCamera.ScreenToWorldPoint(Input.mousePosition);
				int num = ToLogicX(vector.x);
				int num2 = ToLogicY(vector.y);
				if (num != -1 && num2 != -1 && _blocks[num, num2].GetComponent<BattleBlock>().IsActive)
				{
					ClearAllBlocks();
					onSelectBlock(num, num2);
					return;
				}
			}
			if (!selectingBlock)
			{
				return;
			}
			Vector3 vector2 = mainCamera.ScreenToWorldPoint(Input.mousePosition);
			int num3 = ToLogicX(vector2.x);
			int num4 = ToLogicY(vector2.y);
			if (num3 != -1 && num4 != -1 && _blocks[num3, num4].GetComponent<BattleBlock>().IsActive)
			{
				BattleBlock component = _blocks[num3, num4].GetComponent<BattleBlock>();
				if (!(component == currentBlock))
				{
					if (currentBlock != null)
					{
						onClearFocusBlock(currentBlock);
					}
					onFocusBlock(component);
					currentBlock = component;
				}
			}
			else if (currentBlock != null)
			{
				onClearFocusBlock(currentBlock);
				currentBlock = null;
			}
		}

		private void ClearAllBlocks()
		{
			GameObject[,] blocks = _blocks;
			int length = blocks.GetLength(0);
			int length2 = blocks.GetLength(1);
			for (int i = 0; i < length; i++)
			{
				for (int j = 0; j < length2; j++)
				{
					GameObject gameObject = blocks[i, j];
					BattleBlock component = gameObject.GetComponent<BattleBlock>();
					component.Reset();
				}
			}
		}

		private void SetCurrentBlock(int x, int y)
		{
			BattleBlock component = _blocks[x, y].GetComponent<BattleBlock>();
			if (!(component == currentBlock))
			{
				component.SetFocus(true);
				if (currentBlock != null)
				{
					currentBlock.SetFocus(false);
				}
				currentBlock = component;
			}
		}

		private void InitStatusMap()
		{
			_statusMap.Add(BattleStatus.Starting, OnStatusStarting);
			_statusMap.Add(BattleStatus.WaitingForActivePerson, WaitingForActivePerson);
			_statusMap.Add(BattleStatus.NextActivePersonAction, OnNextActivePersonAction);
			_statusMap.Add(BattleStatus.UISelectMove, OnSelectMove);
			_statusMap.Add(BattleStatus.UISelectAction, OnSelectAction);
			_statusMap.Add(BattleStatus.UISelectSkill, OnSelectSkill);
			_statusMap.Add(BattleStatus.UISelectItem, OnSelectItem);
			_statusMap.Add(BattleStatus.UISelectItemTarget, OnSelectItemTarget);
			_statusMap.Add(BattleStatus.UISelectRest, OnSelectRest);
			_statusMap.Add(BattleStatus.UISelectTarget, delegate
			{
				Suggest("选择施展目标位置[长按显示范围]");
			});
			_statusMap.Add(BattleStatus.Win, OnWin);
			_statusMap.Add(BattleStatus.Lose, OnLose);
		}

		public static int ToScreenX(int x)
		{
			return (x - LuaManager.GetConfigInt("BATTLE_MOVEBLOCK_MARGIN_RIGHT")) * LuaManager.GetConfigInt("BATTLE_MOVEBLOCK_LENGTH");
		}

		public static int ToScreenY(int y)
		{
			return (y - LuaManager.GetConfigInt("BATTLE_MOVEBLOCK_MARGIN_TOP")) * LuaManager.GetConfigInt("BATTLE_MOVEBLOCK_WIDTH");
		}

		public BattleSprite GetSprite(int x, int y)
		{
			foreach (GameObject sprite in _sprites)
			{
				BattleSprite component = sprite.GetComponent<BattleSprite>();
				if (component.X == x && component.Y == y)
				{
					return component;
				}
			}
			return null;
		}

		public static int ToLogicX(float x)
		{
			for (int i = 0; i < MOVEBLOCK_MAX_X; i++)
			{
				if ((float)(ToScreenX(i) - 33) <= x && x <= (float)(ToScreenX(i) + 33))
				{
					return i;
				}
			}
			return -1;
		}

		public static int ToLogicY(float y)
		{
			for (int i = 0; i < MOVEBLOCK_MAX_Y; i++)
			{
				if ((float)(ToScreenY(i) - 33) <= y && y <= (float)(ToScreenY(i) + 33))
				{
					return i;
				}
			}
			return -1;
		}

		private void OnStatusStarting()
		{
			Status = BattleStatus.WaitingForActivePerson;
		}

		private void WaitingForActivePerson()
		{
			BattleTimeRun();
		}

		private void BattleTimeRun()
		{
			if (currentSprite != null)
			{
				currentSprite.IsCurrent = false;
			}
			_battleTimestamp++;
			if (_battleTimestamp > CommonSettings.DEFAULT_MAX_GAME_SPTIME)
			{
				Status = BattleStatus.Lose;
				return;
			}
			TimeLabelObj.GetComponent<Text>().text = string.Format("时间限制 {0}/{1}", _battleTimestamp.ToString(), CommonSettings.DEFAULT_MAX_GAME_SPTIME);
			foreach (BattleSprite sprite in Sprites)
			{
				BuffsEffect(sprite);
			}
			if (_battleTimestamp % 50 == 0)
			{
				MiusSkillCd();
			}
			activePersons.Clear();
			foreach (GameObject sprite2 in _sprites)
			{
				BattleSprite component = sprite2.GetComponent<BattleSprite>();
				component.AddSp();
				if (component.Sp >= 100.0)
				{
					activePersons.Enqueue(component);
				}
			}
			if (activePersons.Count > 0)
			{
				Status = BattleStatus.NextActivePersonAction;
			}
			else
			{
				Status = BattleStatus.WaitingForActivePerson;
			}
		}

		private void MiusSkillCd()
		{
			foreach (BattleSprite sprite in Sprites)
			{
				foreach (SkillInstance skill in sprite.Role.Skills)
				{
					if (skill.CurrentCd > 0)
					{
						skill.CurrentCd--;
					}
					foreach (UniqueSkillInstance uniqueSkill in skill.UniqueSkills)
					{
						if (uniqueSkill.CurrentCd > 0)
						{
							uniqueSkill.CurrentCd--;
						}
					}
				}
				foreach (InternalSkillInstance internalSkill in sprite.Role.InternalSkills)
				{
					foreach (UniqueSkillInstance uniqueSkill2 in internalSkill.UniqueSkills)
					{
						if (uniqueSkill2.CurrentCd > 0)
						{
							uniqueSkill2.CurrentCd--;
						}
					}
				}
				foreach (SpecialSkillInstance specialSkill in sprite.Role.SpecialSkills)
				{
					if (specialSkill.CurrentCd > 0)
					{
						specialSkill.CurrentCd--;
					}
				}
				if (sprite.ItemCd > 0)
				{
					sprite.ItemCd--;
				}
			}
		}

		private void OnNextActivePersonAction()
		{
			if (!JudgeIfBattleEnd())
			{
				return;
			}
			if (activePersons.Count == 0)
			{
				Status = BattleStatus.WaitingForActivePerson;
				return;
			}
			if (currentSprite != null)
			{
				currentSprite.IsCurrent = false;
			}
			currentSprite = activePersons.Dequeue();
			if (currentSprite.Hp <= 0)
			{
				Status = BattleStatus.NextActivePersonAction;
				return;
			}
			currentSprite.IsCurrent = true;
			if (currentSprite.Role.HasTalent("普照") && Tools.ProbabilityTest(0.4))
			{
				Log(currentSprite.Role.Name + "天赋【普照】发动");
				foreach (BattleSprite sprite in Sprites)
				{
					if (sprite.Team == currentSprite.Team && Math.Abs(sprite.X - currentSprite.X) + Math.Abs(sprite.Y - currentSprite.Y) <= 2)
					{
						BuffInstance buffInstance = new BuffInstance();
						buffInstance.buff = new Buff
						{
							Name = "恢复",
							Level = 2
						};
						buffInstance.Owner = sprite;
						buffInstance.LeftRound = 3;
						BuffInstance buffInstance2 = buffInstance;
						BuffInstance buff = sprite.GetBuff("恢复");
						if (buff == null)
						{
							sprite.Buffs.Add(buffInstance2);
						}
						else if (buffInstance2.Level >= buff.Level)
						{
							buff = buffInstance2;
						}
						Log(sprite.Role.Name + "获得增益效果【" + buffInstance2.buff.Name + "】，等级" + buffInstance2.Level);
						sprite.AttackInfo("回复", Color.yellow);
						sprite.Refresh();
					}
				}
			}
			if (currentSprite.Role.HasTalent("无相") && Tools.ProbabilityTest(0.3))
			{
				Log(currentSprite.Role.Name + "天赋【无相】发动");
				int level = currentSprite.Role.Level;
				int num = Tools.GetRandomInt(0, level * 30 + 50);
				if (num + currentSprite.Role.Attributes["hp"] > currentSprite.Role.Attributes["maxhp"])
				{
					num = currentSprite.Role.Attributes["maxhp"] - currentSprite.Role.Attributes["hp"];
				}
				currentSprite.Hp += num;
				currentSprite.AttackInfo("恢复生命" + num, Color.green);
			}
			if (currentSprite.Role.HasTalent("琴胆剑心") && Tools.ProbabilityTest(0.4))
			{
				Log(currentSprite.Role.Name + "天赋【琴胆剑心】发动");
				foreach (BattleSprite sprite2 in Sprites)
				{
					if (sprite2.Team == currentSprite.Team && Math.Abs(sprite2.X - currentSprite.X) + Math.Abs(sprite2.Y - currentSprite.Y) <= 2)
					{
						BuffInstance buffInstance = new BuffInstance();
						buffInstance.buff = new Buff
						{
							Name = "攻击强化",
							Level = 3
						};
						buffInstance.Owner = sprite2;
						buffInstance.LeftRound = 3;
						BuffInstance buffInstance3 = buffInstance;
						BuffInstance buff2 = sprite2.GetBuff("攻击强化");
						if (buff2 == null)
						{
							sprite2.Buffs.Add(buffInstance3);
						}
						else if (buffInstance3.Level >= buff2.Level)
						{
							buff2 = buffInstance3;
						}
						Log(sprite2.Role.Name + "获得增益效果【" + buffInstance3.buff.Name + "】，等级" + buffInstance3.Level);
						sprite2.AttackInfo("攻击强化！", Color.red);
						sprite2.Refresh();
					}
				}
			}
			if (currentSprite.Role.HasTalent("医仙"))
			{
				Log(currentSprite.Role.Name + "天赋【医仙】发动");
				foreach (BattleSprite sprite3 in Sprites)
				{
					if (sprite3.Team == currentSprite.Team && Math.Abs(sprite3.X - currentSprite.X) + Math.Abs(sprite3.Y - currentSprite.Y) <= 4)
					{
						int num2 = (int)((double)currentSprite.Role.Attributes["gengu"] * Tools.GetRandom(1.0, 4.0));
						sprite3.Hp += num2;
						sprite3.AttackInfo("【医仙】 +" + num2 + "HP", Color.green);
						Log(sprite3.Role.Name + "恢复生命值" + num2);
					}
				}
			}
			if (currentSprite.Role.HasTalent("救死扶伤"))
			{
				List<BattleSprite> list = new List<BattleSprite>();
				foreach (BattleSprite sprite4 in Sprites)
				{
					if (sprite4.Team == currentSprite.Team && Math.Abs(sprite4.X - currentSprite.X) + Math.Abs(sprite4.Y - currentSprite.Y) <= 4)
					{
						list.Add(sprite4);
					}
				}
				if (list.Count > 0)
				{
					Log(currentSprite.Role.Name + "天赋【救死扶伤】发动");
					BattleSprite battleSprite = list[Tools.GetRandomInt(0, list.Count) % list.Count];
					int num3 = (int)((double)currentSprite.Role.Attributes["gengu"] * Tools.GetRandom(1.0, 4.0));
					battleSprite.Hp += 2 * num3;
					battleSprite.AttackInfo("【救死扶伤】 +" + num3 + "HP", Color.green);
					Log(battleSprite.Role.Name + "恢复生命值" + num3);
				}
			}
			if (currentSprite.Role.HasTalent("哀歌"))
			{
				Log(currentSprite.Role.Name + "天赋【哀歌】发动");
				foreach (BattleSprite sprite5 in Sprites)
				{
					if (sprite5.Team != currentSprite.Team && Math.Abs(sprite5.X - currentSprite.X) + Math.Abs(sprite5.Y - currentSprite.Y) <= 3)
					{
						BuffInstance buffInstance = new BuffInstance();
						buffInstance.buff = new Buff
						{
							Name = "攻击弱化",
							Level = 2
						};
						buffInstance.Owner = sprite5;
						buffInstance.LeftRound = 3;
						BuffInstance buffInstance4 = buffInstance;
						BuffInstance buff3 = sprite5.GetBuff("攻击弱化");
						if (buff3 == null)
						{
							sprite5.Buffs.Add(buffInstance4);
						}
						else if (buffInstance4.Level >= buff3.Level)
						{
							buff3 = buffInstance4;
						}
						Log(sprite5.Role.Name + "获得减益效果【" + buffInstance4.buff.Name + "】，等级" + buffInstance4.Level);
						sprite5.Refresh();
					}
				}
			}
			if (currentSprite.Role.HasTalent("悲酥清风") && Tools.ProbabilityTest(0.3))
			{
				Log(currentSprite.Role.Name + "天赋【悲酥清风】发动");
				foreach (BattleSprite sprite6 in Sprites)
				{
					if (sprite6.Team != currentSprite.Team && Math.Abs(sprite6.X - currentSprite.X) + Math.Abs(sprite6.Y - currentSprite.Y) <= 3)
					{
						sprite6.AttackInfo("-内力" + (int)((float)sprite6.Mp * 0.5f), Color.blue);
						sprite6.Mp = (int)((float)sprite6.Mp * 0.5f);
						Log(sprite6.Role.Name + "内力减少" + (int)((double)sprite6.Mp * 0.5));
						sprite6.Refresh();
					}
				}
			}
			if (currentSprite.Role.HasTalent("嗜酒如命") && Tools.ProbabilityTest(0.15))
			{
				Log(currentSprite.Role.Name + "天赋【嗜酒如命】发动");
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = new Buff
				{
					Name = "醉酒",
					Level = 2
				};
				buffInstance.Owner = currentSprite;
				buffInstance.LeftRound = 3;
				BuffInstance buffInstance5 = buffInstance;
				BuffInstance buff4 = currentSprite.GetBuff("醉酒");
				if (buff4 == null)
				{
					currentSprite.Buffs.Add(buffInstance5);
				}
				else if (buffInstance5.Level >= buff4.Level)
				{
					buff4 = buffInstance5;
				}
				currentSprite.AttackInfo("进入醉酒状态！", Color.red);
				Log(currentSprite.Role.Name + "获得增益效果【" + buffInstance5.buff.Name + "】，等级" + buffInstance5.Level);
				currentSprite.Refresh();
			}
			if (currentSprite.Role.HasTalent("百穴归一"))
			{
				Log(currentSprite.Role.Name + "天赋【百穴归一】发动");
				currentSprite.Balls++;
				int randomInt = Tools.GetRandomInt(50, 200);
				currentSprite.Mp += randomInt;
				Log(currentSprite.Role.Name + "恢复内力" + randomInt);
				currentSprite.AttackInfo("回内+" + randomInt, Color.blue);
			}
			if (currentSprite.Role.HasTalent("右臂有伤"))
			{
				Log(currentSprite.Role.Name + "天赋【右臂有伤】发动，怒气+1");
				currentSprite.Balls++;
			}
			if (currentSprite.Role.HasTalent("魔神降临") && Tools.ProbabilityTest(0.12))
			{
				Log(currentSprite.Role.Name + "天赋【魔神降临】发动，变身为魔神");
				AudioManager.Instance.PlayEffect("音效.咆哮");
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = new Buff
				{
					Name = "魔神降临",
					Level = 2
				};
				buffInstance.Owner = currentSprite;
				buffInstance.LeftRound = 3;
				BuffInstance buff5 = buffInstance;
				currentSprite.AddBuff(buff5);
				currentSprite.Refresh();
			}
			if (currentSprite.Role.HasTalent("百变千幻") && Tools.ProbabilityTest(0.1))
			{
				Log(currentSprite.Role.Name + "天赋【百变千幻】发动，启动易容术");
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = new Buff
				{
					Name = "易容",
					Level = 2
				};
				buffInstance.Owner = currentSprite;
				buffInstance.LeftRound = 3;
				BuffInstance buff6 = buffInstance;
				currentSprite.AddBuff(buff6);
				currentSprite.Refresh();
			}
			if (currentSprite.Role.HasTalent("倚天屠龙") && Tools.ProbabilityTest(0.2))
			{
				Log(currentSprite.Role.Name + "天赋【倚天屠龙】发动，获得圣战状态");
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = new Buff
				{
					Name = "圣战",
					Level = 2
				};
				buffInstance.Owner = currentSprite;
				buffInstance.LeftRound = 2;
				BuffInstance buff7 = buffInstance;
				currentSprite.AddBuff(buff7);
				currentSprite.Refresh();
			}
			if (currentSprite.Role.HasTalent("光明圣火阵"))
			{
				double num4 = 0.0;
				foreach (BattleSprite sprite7 in Sprites)
				{
					if (sprite7.Team == currentSprite.Team && sprite7 != currentSprite)
					{
						num4 += 0.2;
					}
					if (!Tools.ProbabilityTest(num4))
					{
						continue;
					}
					List<BuffInstance> list2 = new List<BuffInstance>();
					foreach (BuffInstance buff9 in currentSprite.Buffs)
					{
						if (buff9.IsDebuff)
						{
							list2.Add(buff9);
						}
					}
					foreach (BuffInstance item in list2)
					{
						currentSprite.Buffs.Remove(item);
					}
					if (list2.Count > 0)
					{
						Log(currentSprite.Role.Name + "阵法【光明圣火阵】发动，清除负面效果");
						currentSprite.AttackInfo("光明圣火阵,清除负面效果", Color.red);
						currentSprite.Refresh();
					}
				}
			}
			BuffInstance buff8 = currentSprite.GetBuff("晕眩");
			bool flag = false;
			if (buff8 != null)
			{
				flag = true;
			}
			foreach (BuffInstance buff10 in currentSprite.Buffs)
			{
				if (buff10.buff.Name == "醉酒")
				{
					if (Tools.GetRandom(0.0, 1.0) <= 0.2)
					{
						Log(currentSprite.Role.Name + "由于醉酒，无法行动。");
						flag = true;
					}
					else
					{
						Log(currentSprite.Role.Name + "醉酒发动，怒气全满。");
						currentSprite.Balls = 6;
					}
					break;
				}
			}
			if (!flag)
			{
				if (currentSprite.Role.HasTalent("令狐冲的怪病") && Tools.ProbabilityTest(0.4))
				{
					Log(currentSprite.Role.Name + "由于【令狐冲的怪病】无法行动");
					currentSprite.AttackInfo("咳嗽..", Color.red);
					bool flag2 = false;
					foreach (BattleSprite sprite8 in Sprites)
					{
						if (sprite8.Role.Key == "平一指" && sprite8.Team == currentSprite.Team && sprite8.Hp > 1)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						currentSprite.Sp = 0.0;
						Status = BattleStatus.NextActivePersonAction;
						return;
					}
				}
				LuaManager.Call("BATTLE_BeforeRoleAction", this, currentSprite);
				RoleAction(currentSprite);
			}
			else
			{
				currentSprite.AttackInfo("晕眩中..", Color.white);
				currentSprite.Sp = 0.0;
				Status = BattleStatus.NextActivePersonAction;
			}
		}

		private void RoleAction(BattleSprite sp)
		{
			if (Configer.IsAutoBattle || sp.Team != 1 || _battle.ForceAI)
			{
				OnAI(sp);
			}
			else
			{
				OnManual(sp);
			}
			currentSprite.Sp = 0.0;
		}

		private void OnAI(BattleSprite sp)
		{
			Status = BattleStatus.AI;
			_isAI = true;
			_aiResult = _ai.GetAIResult();
			RoleMoveTo(currentSprite, _aiResult.MoveX, _aiResult.MoveY);
		}

		private void OnManual(BattleSprite sp)
		{
			_isAI = false;
			rollbackCurrentX = currentSprite.X;
			rollbackCurrentY = currentSprite.Y;
			rollbackCurrentFace = currentSprite.FaceRight;
			RefreshSkillPanel();
			Status = BattleStatus.UISelectMove;
		}

		public void OnAuttoBattleSet(bool isAuto)
		{
			Configer.IsAutoBattle = isAuto;
			if (isAuto && (Status == BattleStatus.UISelectAction || Status == BattleStatus.UISelectItem || Status == BattleStatus.UISelectItemTarget || Status == BattleStatus.UISelectMove || Status == BattleStatus.UISelectSkill || Status == BattleStatus.UISelectTarget))
			{
				currentSprite.Pos = new Vector2(rollbackCurrentX, rollbackCurrentY);
				currentSprite.FaceRight = rollbackCurrentFace;
				ClearAllBlocks();
				itemMenu.Hide();
				cancelButton.gameObject.SetActive(false);
				messageBox.gameObject.SetActive(false);
				selectMenu.Hide();
				itemDetailPanel.gameObject.SetActive(false);
				spriteActionMenu.gameObject.SetActive(false);
				OnAI(currentSprite);
			}
		}

		public void Suggest(string text)
		{
			if (!_isAI)
			{
				suggestText.text = text;
			}
		}

		private void OnSelectMove()
		{
			cancelButton.gameObject.SetActive(true);
			cancelButton.onClick.AddListener(delegate
			{
				ClearAllBlocks();
				cancelButton.onClick.RemoveAllListeners();
				cancelButton.gameObject.SetActive(false);
				Status = BattleStatus.UISelectAction;
			});
			Suggest("请选择移动到的位置.. [长按高亮选择]");
			ClearAllBlocks();
			List<LocationBlock> moveRange = _ai.GetMoveRange(currentSprite.X, currentSprite.Y);
			foreach (LocationBlock item in moveRange)
			{
				_blocks[item.X, item.Y].GetComponent<BattleBlock>().Status = BattleBlockStatus.HighLightGreen;
				_blocks[item.X, item.Y].GetComponent<BattleBlock>().IsActive = true;
			}
		}

		private bool JudgeIfBattleEnd()
		{
			if (isEnd)
			{
				return false;
			}
			int num = 0;
			int num2 = 0;
			foreach (BattleSprite sprite in Sprites)
			{
				if (sprite.Team == 1)
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
			if (num == 0)
			{
				isEnd = true;
				Status = BattleStatus.Lose;
				return false;
			}
			if (num2 == 0)
			{
				isEnd = true;
				Status = BattleStatus.Win;
				return false;
			}
			return true;
		}

		private void BuffsEffect(BattleSprite sp)
		{
			List<RoundBuffResult> list = sp.RunBuffs();
			foreach (RoundBuffResult item in list)
			{
				if (item.AddHp != 0)
				{
					Log(sp.Role.Name + "由于效果【" + item.buff.buff.Name + "】，生命值" + item.AddHp);
					sp.Hp += item.AddHp;
					if (sp.Hp <= 0)
					{
						sp.Hp = 1;
					}
					sp.AttackInfo((item.AddHp <= 0) ? (string.Empty + item.AddHp) : ("+" + item.AddHp), Color.green);
				}
				if (item.AddMp != 0)
				{
					Log(sp.Role.Name + "由于效果【" + item.buff.buff.Name + "】，内力" + item.AddMp);
					sp.Mp += item.AddMp;
					if (sp.Mp <= 0)
					{
						sp.Mp = 1;
					}
					sp.AttackInfo((item.AddMp <= 0) ? (string.Empty + item.AddMp) : ("+" + item.AddMp), Color.blue);
				}
				if (item.AddBall != 0)
				{
					Log(sp.Role.Name + "由于效果【" + item.buff.buff.Name + "】，怒气" + item.AddBall);
					sp.Balls += item.AddBall;
					if (sp.Balls < 0)
					{
						sp.Balls = 0;
					}
					sp.AttackInfo((item.AddBall <= 0) ? (string.Empty + item.AddBall) : ("+怒气 " + item.AddBall), Color.magenta);
				}
			}
			sp.Refresh();
		}

		private void RefreshSkillPanel()
		{
			SkillSelectPanelObj.GetComponent<BattleSkillSelectPanelUI>().Clear();
			foreach (SkillBox avaliableSkill in currentSprite.Role.GetAvaliableSkills())
			{
				SkillBox cs = avaliableSkill;
				GameObject gameObject = UnityEngine.Object.Instantiate(skillSelectItemObj);
				gameObject.GetComponent<BattleSkillSelectItemUI>().Bind(cs, currentSprite);
				gameObject.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate
				{
					if (cs.Status == SkillStatus.Ok)
					{
						currentSprite.CurrentSkill = cs;
						SkillSelectPanelObj.GetComponent<BattleSkillSelectPanelUI>().SetCurrent(cs);
					}
					else if (cs.Status == SkillStatus.NoBalls)
					{
						ShowPopSuggestText("你没有足够的怒气释放该技能");
					}
					else if (cs.Status == SkillStatus.NoCd)
					{
						ShowPopSuggestText("该技能尚未冷却");
					}
					else if (cs.Status == SkillStatus.NoMp)
					{
						ShowPopSuggestText("你没有足够的内力释放该技能");
					}
					else if (cs.Status == SkillStatus.Seal)
					{
						ShowPopSuggestText("该技能被封印了");
					}
				});
				SkillSelectPanelObj.GetComponent<BattleSkillSelectPanelUI>().AddItem(gameObject);
				if (currentSprite.CurrentSkill == cs)
				{
					SkillSelectPanelObj.GetComponent<BattleSkillSelectPanelUI>().SetCurrent(cs);
				}
			}
		}

		private void OnSelectAction()
		{
			Suggest("选择角色行动...点击查看角色详情");
			currentSprite.Status = BattleSpriteStatus.Standing;
			actionBarRoleHeadObj.GetComponent<Image>().sprite = Resource.GetImage(currentSprite.Role.Head);
			spriteActionMenu.SetActive(true);
			spriteActionMenu.transform.Find("SuggestKeyText").gameObject.SetActive(!CommonSettings.TOUCH_MODE);
			SkillSelectPanelObj.GetComponent<BattleSkillSelectPanelUI>().SetCurrent(currentSprite.CurrentSkill);
		}

		private void OnSelectSkill()
		{
			if (currentSprite.CurrentSkill == null || currentSprite.CurrentSkill.Status != SkillStatus.Ok)
			{
				ShowPopSuggestText("你必须先选择一个可用的技能");
				Status = BattleStatus.UISelectAction;
			}
			else
			{
				Status = BattleStatus.UISelectTarget;
				SelectSkillTarget(currentSprite.CurrentSkill);
			}
		}

		private void SelectSkillTarget(SkillBox skill)
		{
			ClearAllBlocks();
			cancelButton.gameObject.SetActive(true);
			cancelButton.onClick.AddListener(delegate
			{
				selectMenu.Hide();
				ClearAllBlocks();
				cancelButton.onClick.RemoveAllListeners();
				cancelButton.gameObject.SetActive(false);
				Status = BattleStatus.UISelectAction;
			});
			selectingBlock = false;
			BattleSprite battleSprite = currentSprite;
			currentSkill = skill;
			List<LocationBlock> skillCastBlocks = skill.GetSkillCastBlocks(battleSprite.X, battleSprite.Y);
			foreach (LocationBlock item in skillCastBlocks)
			{
				if (item.X >= 0 && item.X < MOVEBLOCK_MAX_X && item.Y >= 0 && item.Y < MOVEBLOCK_MAX_Y)
				{
					BattleBlock component = _blocks[item.X, item.Y].GetComponent<BattleBlock>();
					component.Status = BattleBlockStatus.HighLightRed;
					component.IsActive = true;
					component.RelatedBlocks = skill.GetSkillCoverBlocks(item.X, item.Y, battleSprite.X, battleSprite.Y);
				}
			}
		}

		private void PreCastSkill()
		{
			SkillSuggestPanel.SetActive(false);
			Status = BattleStatus.Acting;
			currentSprite.Status = BattleSpriteStatus.Standing;
			if (skilltarget_x > currentSprite.X)
			{
				currentSprite.FaceRight = true;
			}
			if (skilltarget_x < currentSprite.X)
			{
				currentSprite.FaceRight = false;
			}
			if (currentSkill.SkillType == SkillType.Normal || currentSkill.SkillType == SkillType.Unique)
			{
				Aoyi aoyi = AoyiLogic.ChangeToAoyi(currentSprite, currentSkill);
				if (aoyi != null)
				{
					currentSkill = new AoyiInstance(currentSkill, aoyi);
					if (currentSprite.Role.Female)
					{
						AudioManager.Instance.PlayRandomEffect(LuaManager.GetConfig<string[]>("AOYI_SOUND_FEMALE"));
						AudioManager.Instance.PlayRandomEffect(LuaManager.GetConfig<string[]>("AOYI_EFFECT"));
					}
					else
					{
						AudioManager.Instance.PlayRandomEffect(LuaManager.GetConfig<string[]>("AOYI_SOUND_MALE"));
						AudioManager.Instance.PlayRandomEffect(LuaManager.GetConfig<string[]>("AOYI_EFFECT"));
					}
				}
			}
			if (!string.IsNullOrEmpty(currentSkill.ScreenEffect))
			{
				PlayScreenEffect(currentSprite, currentSkill.ScreenEffect, currentSkill.IsScreenEffectFollowSprite, delegate
				{
					screenEffectCover.SetActive(false);
					currentSprite.transform.SetParent(base.gameObject.transform, false);
					currentSprite.Status = BattleSpriteStatus.Attacking;
					currentSprite.Pos = new Vector2(currentSprite.X, currentSprite.Y);
					currentSprite.Mp -= currentSkill.CostMp;
					currentSprite.AttackInfo(currentSkill.Name, currentSkill.Color);
					Invoke("CastSkill", 0.8f);
					Invoke("ShakeCamera", 0.8f);
				});
			}
			else
			{
				currentSprite.Mp -= currentSkill.CostMp;
				currentSprite.AttackInfo(currentSkill.Name, currentSkill.Color);
				currentSprite.Status = BattleSpriteStatus.Attacking;
				Invoke("CastSkill", 0.8f);
			}
		}

		private void PlayScreenEffect(BattleSprite role, string animationName, bool followSprite, CommonSettings.VoidCallBack callback)
		{
			role.transform.SetParent(screenEffectCover.transform, false);
			if (CommonSettings.MOD_MODE && UserDefinedAnimationManager.instance.HasAnimation(animationName, "effect"))
			{
				GameObject gameObject = UserDefinedAnimationManager.instance.GenerateObject(animationName, "effect");
				SkillAnimation skillAnimation = gameObject.AddComponent<SkillAnimation>();
				if (followSprite)
				{
					skillAnimation.DisplayEffect(role.X, role.Y);
				}
				else
				{
					skillAnimation.DisplayEffectNotFollowSprite();
				}
				gameObject.GetComponent<UserDefinedAnimation>().Play("effect", callback);
				gameObject.transform.SetParent(screenEffectCover.transform, false);
			}
			else
			{
				GameObject gameObject2 = ResourcePool.Get("Effects/" + animationName);
				if (gameObject2 == null)
				{
					Debug.LogError("调用了错误的动画" + animationName);
					gameObject2 = ResourcePool.Get("Effects/jiqi2");
				}
				GameObject gameObject3 = UnityEngine.Object.Instantiate(gameObject2);
				SkillAnimation component = gameObject3.GetComponent<SkillAnimation>();
				if (component == null)
				{
					Debug.LogError("动画" + animationName + "没有设置SkillAnimation组件!");
				}
				if (followSprite)
				{
					component.DisplayEffect(role.X, role.Y);
				}
				else
				{
					component.DisplayEffectNotFollowSprite();
				}
				component.SetCallback(callback);
				gameObject3.transform.SetParent(screenEffectCover.transform, false);
				screenEffectText.GetComponent<Animator>().Play("ScreenEffectTextAnimation");
				aoyiRoleHeadImage.GetComponent<Image>().sprite = Resource.GetImage(role.Role.Head);
				aoyiRoleHeadImage.GetComponent<Animator>().Play("AoyiRoleHeadAnimation");
			}
			screenEffectText.GetComponent<Text>().text = currentSkill.Name;
			screenEffectText.GetComponent<Text>().color = currentSkill.Color;
			screenEffectCover.SetActive(true);
		}

		private void ShakeCamera()
		{
		}

		private void CastSkill()
		{
			int x = skilltarget_x;
			int y = skilltarget_y;
			if (currentSkill.IsAoyi)
			{
				Log(currentSprite.Role.Name + "施展奥义【" + currentSkill.Name + "】");
			}
			else
			{
				Log(currentSprite.Role.Name + "施展【" + currentSkill.Name + "】");
			}
			currentSkill.CastCd();
			int hitNumber = 0;
			List<LocationBlock> skillCoverBlocks = currentSkill.GetSkillCoverBlocks(x, y, currentSprite.X, currentSprite.Y);
			skillCallbackTag = false;
			foreach (LocationBlock item in skillCoverBlocks)
			{
				BattleSprite battleSprite = Attack(currentSprite, currentSkill, item.X, item.Y, delegate
				{
					BattleSprite battleSprite2 = currentSprite;
					SkillBox skillBox = currentSkill;
					if (currentSprite.Role.HasTalent("碎裂的怒吼") && currentSkill.IsAoyi && Tools.ProbabilityTest(0.3))
					{
						currentSprite.AttackInfo("大地，震裂！", Color.yellow);
						Log(currentSprite.Role.Name + "天赋【碎裂的怒吼】发动");
						foreach (BattleSprite sprite in Sprites)
						{
							if (sprite.Team != battleSprite2.Team && Math.Abs(sprite.X - battleSprite2.X) + Math.Abs(sprite.Y - battleSprite2.Y) <= 3)
							{
								BuffInstance buff = new BuffInstance
								{
									buff = new Buff
									{
										Name = "晕眩",
										Level = 0
									},
									Owner = sprite,
									LeftRound = 2
								};
								sprite.AddBuff(buff);
								sprite.Refresh();
								Log(sprite.Role.Name + "晕眩了");
							}
						}
					}
					if (battleSprite2.Role.HasTalent("越女剑") && skillBox.IsAoyi && skillBox.Name == "越女剑法奥义式")
					{
						Log(battleSprite2.Role.Name + "天赋【】发动，全场敌方集气减少");
						foreach (BattleSprite sprite2 in Sprites)
						{
							if (sprite2.Team != battleSprite2.Team)
							{
								double num2 = sprite2.Sp - 20.0;
								if (num2 < 0.0)
								{
									num2 = 0.0;
								}
								sprite2.Sp -= num2;
								sprite2.AttackInfo("集气减少", Color.yellow);
							}
						}
					}
					LuaManager.Call("BATTLE_AfterSkillAnimation", this, currentSprite, skillBox, hitNumber);
					Status = BattleStatus.NextActivePersonAction;
				});
				if (battleSprite != null && battleSprite.Team != currentSprite.Team)
				{
					battleSprite.DisplayHpMpBar();
					hitNumber++;
				}
			}
			currentSprite.DisplayHpMpBar();
			LuaManager.Call("BATTLE_BeforeSkillAnimation", this, currentSprite, currentSkill, hitNumber);
			if (hitNumber > 0)
			{
				double num = 0.5 + (double)((float)currentSprite.Role.fuyuan / 200f) * 0.2;
				if (currentSprite.Role.HasTalent("暴躁"))
				{
					num += 0.15;
				}
				if (Tools.ProbabilityTest(num))
				{
					currentSprite.Balls++;
					if (currentSprite.Role.HasTalent("斗魂"))
					{
						currentSprite.Balls++;
						Log(currentSprite.Role.Name + "天赋【斗魂】发动，怒气增益翻倍");
					}
				}
				bool flag = false;
				if (currentSkill.TryAddExp(15 + currentSprite.Role.AttributesFinal["wuxing"] / 2))
				{
					currentSprite.AttackInfo(currentSkill.Name + "等级升级", Color.green);
					flag = true;
				}
				if (currentSprite.Role.GetEquippedInternalSkill().TryAddExp(15 + currentSprite.Role.AttributesFinal["wuxing"] / 2))
				{
					currentSprite.AttackInfo(currentSprite.Role.GetEquippedInternalSkill().Name + "等级提升", Color.green);
					flag = true;
				}
				if (currentSprite.Role.AddExp(3))
				{
					currentSprite.AttackInfo("角色等级提升", Color.green);
					flag = true;
				}
				if (flag)
				{
				}
				currentSprite.Balls -= currentSkill.CostBall;
			}
			AudioManager.Instance.PlayEffect(currentSkill.Audio);
		}

		public BattleSprite Attack(BattleSprite source, SkillBox skill, int x, int y, CommonSettings.VoidCallBack callback = null)
		{
			BattleSprite sprite = GetSprite(x, y);
			ShowSkillAnimation(skill, x, y, callback);
			if (sprite == null || (source == sprite && !skill.HitSelf))
			{
				return null;
			}
			if (!RuntimeData.Instance.FriendlyFire && source.Team == sprite.Team && !skill.HitSelf)
			{
				return null;
			}
			AttackResult attackResult = AttackLogic.Attack(skill, source, sprite, this);
			if (attackResult.Hp > 0)
			{
				sprite.Status = BattleSpriteStatus.BeAttack;
			}
			if (source.Mp < 0)
			{
				source.Mp = 0;
			}
			if (sprite.Mp < 0)
			{
				sprite.Mp = 0;
			}
			if (source.Hp <= 0)
			{
				Die(source, attackResult);
			}
			if (sprite.Hp <= 0)
			{
				Die(sprite, attackResult);
			}
			List<BattleSprite> list = new List<BattleSprite>();
			foreach (AttackCastInfo item in attackResult.castInfo)
			{
				if (item.type == AttackCastInfoType.ATTACK_TEXT)
				{
					if (item.sprite == null)
					{
						Debug.LogError("spirte == null,info=" + item.info);
					}
					item.sprite.AttackInfo(item.info, item.color);
				}
				else if (item.type == AttackCastInfoType.SMALL_DIALOG)
				{
					if (item.sprite == null)
					{
						Debug.LogError("spirte == null,info=" + item.info);
					}
					if (item.sprite.Hp > 0 && !list.Contains(item.sprite) && Tools.ProbabilityTest(item.property))
					{
						list.Add(item.sprite);
						item.sprite.Say(item.info);
					}
				}
			}
			return sprite;
		}

		public void Log(string msg)
		{
			string text = logPanelObj.transform.Find("SelectPanel").Find("LogText").GetComponent<Text>()
				.text;
			text = msg + "\n" + text;
			logPanelObj.transform.Find("SelectPanel").Find("LogText").GetComponent<Text>()
				.text = text;
		}

		private void Die(BattleSprite sp, AttackResult result)
		{
			if (!LuaManager.Call<bool>("BATTLE_Die", new object[3] { this, sp, result }))
			{
				if (sp.Role.HasTalent("百足之虫") && Tools.ProbabilityTest(0.2))
				{
					Log(sp.Role.Name + "天赋【百足之虫】发动，不死");
					result.AddCastInfo(sp, "只要还有一口气，就不会轻易死去！");
					sp.Hp = 1;
				}
				else if (sp.Role.HasTalent("真气护体") && sp.Role.Attributes["mp"] >= result.Hp * 2 && Tools.ProbabilityTest(0.5))
				{
					Log(sp.Role.Name + "天赋【真气护体】发动，改为减少内力");
					result.AddCastInfo(sp, "猛提起一口真气，又有了几分精神！");
					sp.Hp = 1;
					sp.Mp -= result.Hp * 2;
				}
				else if (sp.Role.HasTalent("无尽斗志") && ((sp.Role.HasTalent("我就是神") && sp.FuhuoCount == 0) || Tools.ProbabilityTest(0.1)))
				{
					Log(sp.Role.Name + "天赋【无尽斗志】发动，原地满血复活了！");
					result.AddCastInfo(sp, "天道不息，斗气不止！（天赋*无尽斗志发动！）");
					sp.Hp = sp.Role.Attributes["maxhp"];
					sp.Mp = sp.Role.Attributes["maxmp"];
					sp.Balls = 6;
					result.AddAttackInfo(sp, "原地满血复活!", Color.red);
					sp.FuhuoCount++;
				}
				else
				{
					sp.Hp = 0;
					sp.Die();
					_sprites.Remove(sp.gameObject);
					Log(sp.Role.Name + "被击败！");
				}
			}
		}

		public void ShowSkillAnimation(SkillBox skill, int x, int y, CommonSettings.VoidCallBack callback = null)
		{
			string animation = skill.Animation;
			if (CommonSettings.MOD_MODE && UserDefinedAnimationManager.instance.HasAnimation(animation, "effect"))
			{
				GameObject gameObject = UserDefinedAnimationManager.instance.GenerateObject(animation, "effect");
				gameObject.AddComponent<SkillAnimation>();
				if (!skillCallbackTag)
				{
					gameObject.GetComponent<UserDefinedAnimation>().Play("effect", callback);
					gameObject.GetComponent<SkillAnimation>().Display(x, y, callback);
					skillCallbackTag = true;
				}
				else
				{
					gameObject.GetComponent<UserDefinedAnimation>().Play("effect");
					gameObject.GetComponent<SkillAnimation>().Display(x, y);
				}
				return;
			}
			GameObject gameObject2 = ResourcePool.Get("Effects/" + animation);
			if (gameObject2 == null)
			{
				Debug.LogError("调用了未定义的动画:" + animation);
				callback();
				return;
			}
			GameObject gameObject3 = UnityEngine.Object.Instantiate(gameObject2);
			if (!skillCallbackTag)
			{
				gameObject3.GetComponent<SkillAnimation>().Display(x, y, callback);
				skillCallbackTag = true;
			}
			else
			{
				gameObject3.GetComponent<SkillAnimation>().Display(x, y);
			}
		}

		private void OnSelectItem()
		{
			Suggest("选择使用的物品");
			itemMenu.Show("选择要使用的物品", RuntimeData.Instance.GetItems(ItemType.Costa), delegate(object ret)
			{
				itemMenu.Hide();
				ItemInstance item = ret as ItemInstance;
				itemDetailPanel.Show(item, ItemDetailMode.Usable, delegate
				{
					currentItem = item;
					if (currentSprite.Role.HasTalent("隔空取物"))
					{
						Status = BattleStatus.UISelectItemTarget;
					}
					else
					{
						OnUseItemTarget(currentItem, currentSprite);
					}
				}, delegate
				{
					Status = BattleStatus.UISelectItem;
				});
			}, delegate
			{
				Status = BattleStatus.UISelectAction;
			});
		}

		public void OnUseItemTarget(ItemInstance item, BattleSprite target)
		{
			if (item == null)
			{
				Debug.LogError("use null item");
				return;
			}
			Status = BattleStatus.Acting;
			Log(currentSprite.Role.Name + "对" + target.Role.Name + "使用了物品【" + item.Name + "】");
			if (target.ItemCd > 0 && !currentSprite.Role.HasTalent("妙手仁心"))
			{
				messageBox.Show("错误", "少侠，你吃药太频繁了会气血失调的！【还需要" + target.ItemCd + "回合】", Color.white, delegate
				{
					Status = BattleStatus.UISelectItem;
				});
				return;
			}
			ItemResult itemResult = item.TryUse(currentSprite.Role, target.Role);
			bool flag = false;
			if (target.GetBuff("重伤") != null)
			{
				itemResult.Hp /= 2;
				itemResult.Mp /= 2;
				if (itemResult.Hp > 0 || itemResult.Mp > 0)
				{
					Log("由于重伤效果,恢复效果减半！");
				}
			}
			if (itemResult.Hp > 0)
			{
				target.Hp += itemResult.Hp;
				Log(target.Role.Name + "生命值恢复" + itemResult.Hp);
				if (itemResult.Hp > 0)
				{
					target.AttackInfo("+" + itemResult.Hp, Color.green);
				}
				flag = true;
			}
			if (itemResult.Mp > 0)
			{
				target.Mp += itemResult.Mp;
				Log(target.Role.Name + "内力恢复" + itemResult.Mp);
				if (itemResult.Mp > 0)
				{
					target.AttackInfo("+" + itemResult.Mp, Color.blue);
				}
				flag = true;
			}
			if (itemResult.Balls > 0)
			{
				target.Role.balls += itemResult.Balls;
				if (target.Role.balls > 6)
				{
					target.Role.balls = 6;
				}
				target.AttackInfo("怒气+" + itemResult.Balls, Color.yellow);
				Log(target.Role.Name + "怒气增加" + itemResult.Balls);
				flag = true;
			}
			if (itemResult.Buffs != null)
			{
				foreach (Buff buff3 in itemResult.Buffs)
				{
					BuffInstance buff = target.GetBuff(buff3.Name);
					BuffInstance buffInstance = new BuffInstance();
					buffInstance.buff = buff3;
					buffInstance.Owner = target;
					buffInstance.Level = buff3.Level;
					buffInstance.LeftRound = buff3.Round;
					BuffInstance buffInstance2 = buffInstance;
					if (buff == null)
					{
						target.AddBuff(buffInstance2);
					}
					else if (buffInstance2.Level >= buff.Level)
					{
						buff.buff = buffInstance2.buff;
						buff.Owner = buffInstance2.Owner;
						buff.Level = buffInstance2.Level;
						buff.LeftRound = buffInstance2.LeftRound;
					}
					target.AttackInfo(buff3.Name + "(" + buff3.Level + ")", Color.red);
					Log(target.Role.Name + "获得效果【" + buff3.Name + "】，等级" + buff3.Level);
				}
				target.Refresh();
			}
			BuffInstance buff2 = target.GetBuff("中毒");
			if (buff2 != null && (itemResult.DescPoisonLevel != 0 || itemResult.DescPoisonDuration != 0))
			{
				buff2.Level -= itemResult.DescPoisonLevel;
				buff2.LeftRound -= itemResult.DescPoisonDuration;
				if (buff2.Level <= 0 || buff2.LeftRound <= 0)
				{
					target.Buffs.Remove(buff2);
				}
				AudioManager.Instance.PlayEffect("音效.恢复3");
				target.Refresh();
			}
			LuaManager.Call("ITEM_OnItemResultRun", itemResult, target, this);
			if (flag)
			{
				AudioManager.Instance.PlayEffect("音效.恢复类物品");
			}
			RuntimeData.Instance.addItem(item, -1);
			if (RuntimeData.Instance.GameMode != "normal")
			{
				target.ItemCd += item.Cooldown;
			}
			Status = BattleStatus.NextActivePersonAction;
		}

		private void OnSelectItemTarget()
		{
			Suggest("选择物品使用对象[长按选择]");
			cancelButton.gameObject.SetActive(true);
			cancelButton.onClick.AddListener(delegate
			{
				ClearAllBlocks();
				cancelButton.onClick.RemoveAllListeners();
				cancelButton.gameObject.SetActive(false);
				Status = BattleStatus.UISelectItem;
			});
			int num = 2;
			List<LocationBlock> list = new List<LocationBlock>();
			for (int num2 = 0; num2 < MOVEBLOCK_MAX_X; num2++)
			{
				for (int num3 = 0; num3 < MOVEBLOCK_MAX_Y; num3++)
				{
					if (Math.Abs(currentSprite.X - num2) + Math.Abs(currentSprite.Y - num3) <= num)
					{
						list.Add(new LocationBlock
						{
							X = num2,
							Y = num3
						});
					}
				}
			}
			foreach (LocationBlock item in list)
			{
				_blocks[item.X, item.Y].GetComponent<BattleBlock>().Status = BattleBlockStatus.HighLightRed;
				_blocks[item.X, item.Y].GetComponent<BattleBlock>().IsActive = true;
			}
		}

		private void OnSelectRest()
		{
			currentSprite.Status = BattleSpriteStatus.Standing;
			Log(currentSprite.Role.Name + "休息。");
			Status = BattleStatus.Acting;
			int num = Math.Max(currentSprite.Role.GetEquippedInternalSkill().Yin, Math.Abs(currentSprite.Role.GetEquippedInternalSkill().Yang));
			float num2 = ((float)num + 100f) / 150f;
			int num3 = (int)(40.0 * (1.0 + 1.5 * (double)(float)currentSprite.Role.Attributes["gengu"] / 100.0) * (double)num2 * Tools.GetRandom(0.1, 2.0));
			if (num3 == 0)
			{
				num3 = 1;
			}
			if (currentSprite.GetBuff("重伤") != null)
			{
				num3 /= 2;
				Log("由于重伤效果,恢复减半");
			}
			int num4 = (int)(60.0 * (double)(1f + 2f * (float)currentSprite.Role.Attributes["gengu"] / 100f) * (double)num2 * Tools.GetRandom(0.2, 2.0));
			if (num4 == 0)
			{
				num4 = 1;
			}
			if (currentSprite.GetBuff("重伤") != null)
			{
				num4 /= 2;
			}
			if (currentSprite.GetBuff("封穴") != null)
			{
				num4 = 0;
				Log(currentSprite.Role.Name + "由于被封穴，无法恢复内力");
			}
			string text = LuaManager.Call<string>("BATTLE_Rest", new object[4] { this, currentSprite, num3, num4 });
			num3 = Convert.ToInt32(text.Split(',')[0]);
			num4 = Convert.ToInt32(text.Split(',')[1]);
			if (num3 + currentSprite.Hp > currentSprite.Role.Attributes["maxhp"])
			{
				num3 = currentSprite.Role.Attributes["maxhp"] - currentSprite.Hp;
				currentSprite.Hp = currentSprite.Role.Attributes["maxhp"];
			}
			else
			{
				currentSprite.Hp += num3;
			}
			if (num3 > 0)
			{
				currentSprite.AttackInfo("+" + num3, Color.green);
				AudioManager.Instance.PlayEffect("音效.休息");
				Log(currentSprite.Role.Name + "回复生命值" + num3);
			}
			if (num4 + currentSprite.Mp > currentSprite.Role.Attributes["maxmp"])
			{
				num4 = currentSprite.Role.Attributes["maxmp"] - currentSprite.Mp;
				currentSprite.Mp = currentSprite.Role.Attributes["maxmp"];
			}
			else
			{
				currentSprite.Mp += num4;
			}
			if (num4 > 0)
			{
				currentSprite.AttackInfo("+" + num4, Color.blue);
				AudioManager.Instance.PlayEffect("音效.休息");
				Log(currentSprite.Role.Name + "回复内力" + num4);
			}
			Status = BattleStatus.NextActivePersonAction;
		}

		public void OnMoveButtonClicked()
		{
			ClearAllBlocks();
			spriteActionMenu.SetActive(false);
			RollBackRolePosition();
			Status = BattleStatus.UISelectMove;
		}

		private void RollBackRolePosition()
		{
			currentSprite.Pos = new Vector2(rollbackCurrentX, rollbackCurrentY);
			currentSprite.FaceRight = rollbackCurrentFace;
		}

		public void OnRestButtonClicked()
		{
			ClearAllBlocks();
			spriteActionMenu.SetActive(false);
			Status = BattleStatus.UISelectRest;
		}

		public void OnAttackButtonClicked()
		{
			ClearAllBlocks();
			spriteActionMenu.SetActive(false);
			Status = BattleStatus.UISelectSkill;
		}

		public void OnItemButtonClicked()
		{
			ClearAllBlocks();
			spriteActionMenu.SetActive(false);
			Status = BattleStatus.UISelectItem;
		}

		private void RoleMoveTo(BattleSprite sprite, int x, int y)
		{
			cancelButton.onClick.RemoveAllListeners();
			cancelButton.gameObject.SetActive(false);
			List<MoveSearchHelper> way = _ai.GetWay(sprite.X, sprite.Y, x, y);
			sprite.Move(way, delegate
			{
				if (_isAI)
				{
					if (_aiResult.skill != null)
					{
						currentSkill = _aiResult.skill;
						skilltarget_x = _aiResult.AttackX;
						skilltarget_y = _aiResult.AttackY;
						Invoke("PreCastSkill", 0.1f);
					}
					else
					{
						Invoke("OnSelectRest", 0.1f);
					}
				}
				else
				{
					Status = BattleStatus.UISelectAction;
				}
			});
		}

		public void ShowCurrentRoleInfo()
		{
			ShowRole(currentSprite.Role);
		}

		public void ShowRole(Role role)
		{
			rolePanelObj.GetComponent<RolePanelUI>().Show(role, null, false);
		}

		private void OnWin()
		{
			ShowBattleResult(true);
		}

		private void OnLose()
		{
			ShowBattleResult(false);
		}

		public void OnBattleResultConfirm()
		{
			if (RuntimeData.Instance.gameEngine.BattleSelectRole_BattleCallback != null)
			{
				RuntimeData.Instance.gameEngine.BattleSelectRole_BattleCallback((!_win) ? "lose" : "win");
			}
		}

		public void ShowBattleResult(bool win)
		{
			if (win)
			{
				battleResultObj.transform.Find("ResultText").GetComponent<Text>().text = "战斗胜利";
				BonusLogic bonusLogic = new BonusLogic(_battle);
				battleResultObj.transform.Find("DetailText").GetComponent<Text>().text = "获得金钱：" + bonusLogic.Money + "           获得经验：" + bonusLogic.Exp + "/每人" + ((bonusLogic.Yuanbao != 0) ? ("          获得元宝:" + bonusLogic.Yuanbao) : string.Empty);
				battleResultObj.transform.Find("ItemMenu").GetComponent<ItemMenu>().Show(string.Empty, bonusLogic.Items, delegate(object ret)
				{
					ItemInstance item = ret as ItemInstance;
					itemDetailPanel.Show(item, ItemDetailMode.Disable);
				});
				battleResultObj.transform.Find("DetailText").gameObject.SetActive(true);
				battleResultObj.transform.Find("ItemMenu").gameObject.SetActive(true);
				bonusLogic.Run();
			}
			else
			{
				battleResultObj.transform.Find("DetailText").gameObject.SetActive(false);
				battleResultObj.transform.Find("ItemMenu").gameObject.SetActive(false);
				battleResultObj.transform.Find("ResultText").GetComponent<Text>().text = "战斗失败";
			}
			_win = win;
			battleResultObj.SetActive(true);
		}

		public void LogButtonClicked()
		{
			isLog = !isLog;
			if (isLog)
			{
				logPanelObj.transform.parent.localPosition = new Vector3(-5f, 0f);
			}
			else
			{
				logPanelObj.transform.parent.localPosition = new Vector3(-255f, 0f);
			}
			logPanelObj.SetActive(isLog);
		}

		public void RoleListButtonClicked()
		{
			BattleRoleListPanelObj.GetComponent<BattleRoleListPanelUI>().Show(Sprites);
		}

		public void ShowPopSuggestText(string text)
		{
			GameObject original = attackInfoPrefab;
			GameObject gameObject = UnityEngine.Object.Instantiate(original);
			gameObject.GetComponent<AttackInfo>().DisplayPopinfo(text, Color.yellow, attackInfoLayer.transform);
		}

		public void ShowCurrentAttackRange()
		{
			ClearAllBlocks();
			if (currentSprite.CurrentSkill.Status == SkillStatus.Ok)
			{
				ShowAttackRange();
			}
		}

		private void ShowAttackRange()
		{
			List<LocationBlock> skillCastBlocks = currentSprite.CurrentSkill.GetSkillCastBlocks(currentSprite.X, currentSprite.Y);
			List<BattleBlock> list = new List<BattleBlock>();
			foreach (LocationBlock item in skillCastBlocks)
			{
				if (item.X < 0 || item.X >= MOVEBLOCK_MAX_X || item.Y < 0 || item.Y >= MOVEBLOCK_MAX_Y)
				{
					continue;
				}
				BattleBlock component = _blocks[item.X, item.Y].GetComponent<BattleBlock>();
				component.Status = BattleBlockStatus.HighLightRed;
				component.IsActive = true;
				list.Add(component);
				component.RelatedBlocks = currentSprite.CurrentSkill.GetSkillCoverBlocks(item.X, item.Y, currentSprite.X, currentSprite.Y);
				foreach (LocationBlock skillCoverBlock in currentSprite.CurrentSkill.GetSkillCoverBlocks(item.X, item.Y, currentSprite.X, currentSprite.Y))
				{
					if (!skillCastBlocks.Contains(skillCoverBlock) && skillCoverBlock.X >= 0 && skillCoverBlock.X < MOVEBLOCK_MAX_X && skillCoverBlock.Y >= 0 && skillCoverBlock.Y < MOVEBLOCK_MAX_Y)
					{
						BattleBlock component2 = _blocks[skillCoverBlock.X, skillCoverBlock.Y].GetComponent<BattleBlock>();
						component2.Status = BattleBlockStatus.HighLightBlue;
						component2.IsActive = false;
					}
				}
			}
		}
	}
}
