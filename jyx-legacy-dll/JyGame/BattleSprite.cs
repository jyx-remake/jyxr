using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace JyGame
{
	public class BattleSprite : MonoBehaviour
	{
		public GameObject HpSliderObj;

		public GameObject MpSliderObj;

		public GameObject HpTextObj;

		public GameObject MpTextObj;

		public GameObject SpSliderObj;

		public GameObject NameTextObj;

		public GameObject CurrentTagArrowObj;

		public GameObject TooltipTextObj;

		private bool _isCurrent;

		private GameObject spriteObj;

		private bool isUserDefinedAnimation;

		public int AnimatorStateCount = 3;

		private bool _isDisplayingHpMpBar;

		private Queue<TextInfo> _attackInfoQueue = new Queue<TextInfo>();

		public int team;

		public BattleRole battleRole;

		public int FuhuoCount;

		private int _x;

		private int _y;

		private bool _faceRight = true;

		public bool IsDead;

		private int _hp;

		private int _mp;

		private double _sp;

		public int ItemCd;

		public List<BuffInstance> Buffs = new List<BuffInstance>();

		private static string[] statusStrMap = new string[3] { "stand", "move", "attack" };

		private BattleSpriteStatus _status;

		private List<MoveSearchHelper> MoveWay;

		private int moved;

		private CommonSettings.VoidCallBack moveCallback;

		public SkillBox CurrentSkill
		{
			get
			{
				return Role.CurrentSkill;
			}
			set
			{
				Role.CurrentSkill = value;
			}
		}

		public bool IsCurrent
		{
			get
			{
				return _isCurrent;
			}
			set
			{
				_isCurrent = value;
				CurrentTagArrowObj.SetActive(_isCurrent);
			}
		}

		private Slider HpSlider
		{
			get
			{
				return HpSliderObj.GetComponent<Slider>();
			}
		}

		private Slider MpSlider
		{
			get
			{
				return MpSliderObj.GetComponent<Slider>();
			}
		}

		private Slider SpSlider
		{
			get
			{
				return SpSliderObj.GetComponent<Slider>();
			}
		}

		private Text HpText
		{
			get
			{
				return HpTextObj.GetComponent<Text>();
			}
		}

		private Text MpText
		{
			get
			{
				return MpTextObj.GetComponent<Text>();
			}
		}

		private Text NameText
		{
			get
			{
				return NameTextObj.GetComponent<Text>();
			}
		}

		private Animator SpriteAnimator
		{
			get
			{
				return spriteObj.GetComponent<Animator>();
			}
		}

		private UserDefinedAnimation UserdefinedAnimator
		{
			get
			{
				return spriteObj.GetComponent<UserDefinedAnimation>();
			}
		}

		public Role Role
		{
			get
			{
				return battleRole.role;
			}
		}

		public int X
		{
			get
			{
				return _x;
			}
		}

		public int Y
		{
			get
			{
				return _y;
			}
		}

		public bool FaceRight
		{
			get
			{
				return _faceRight;
			}
			set
			{
				_faceRight = value;
				if (!_faceRight)
				{
					spriteObj.transform.rotation = new Quaternion(0f, -180f, 0f, 0f);
				}
				else
				{
					spriteObj.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
				}
			}
		}

		public Vector2 Pos
		{
			get
			{
				return new Vector2(_x, _y);
			}
			set
			{
				_x = (int)value.x;
				_y = (int)value.y;
				base.transform.position = new Vector3(BattleField.ToScreenX(_x), BattleField.ToScreenY(_y), _y - 100);
			}
		}

		public int MoveAbility
		{
			get
			{
				if (GetBuff("定身") != null)
				{
					return 0;
				}
				if (Role.HasTalent("金刚伏魔圈"))
				{
					return 0;
				}
				BuffInstance buff = GetBuff("缓速");
				double num = 0.0;
				if (buff != null)
				{
					num = (double)buff.Level * 1.5;
				}
				int num2 = 2;
				if (Role.AttributesFinal["shenfa"] > 100)
				{
					num2++;
				}
				if (Role.AttributesFinal["shenfa"] > 180)
				{
					num2++;
				}
				if (Role.AttributesFinal["shenfa"] > 250)
				{
					num2++;
				}
				num2 -= (int)num;
				BuffInstance buff2 = GetBuff("轻身");
				if (buff2 != null)
				{
					num2 += buff2.Level + 1;
				}
				if (Role.HasTalent("轻功高超"))
				{
					num2++;
				}
				if (Role.HasTalent("瘸子"))
				{
					num2--;
				}
				if (num2 > 5)
				{
					num2 = 5;
				}
				return (num2 <= 0) ? 1 : num2;
			}
		}

		public string Name
		{
			get
			{
				return Role.Name;
			}
		}

		public int Hp
		{
			get
			{
				return _hp;
			}
			set
			{
				_hp = value;
				if (_hp < 0)
				{
					_hp = 0;
				}
				if (_hp > MaxHp)
				{
					_hp = MaxHp;
				}
				Role.hp = _hp;
				HpSlider.value = (float)_hp / (float)MaxHp;
				HpText.text = _hp.ToString();
			}
		}

		public int MaxHp
		{
			get
			{
				return Role.maxhp;
			}
			set
			{
				Role.maxhp = value;
			}
		}

		public int Mp
		{
			get
			{
				return _mp;
			}
			set
			{
				_mp = value;
				if (_mp > MaxMp)
				{
					_mp = MaxMp;
				}
				if (_mp < 0)
				{
					_mp = 0;
				}
				Role.mp = _mp;
				MpSlider.value = (float)_mp / (float)MaxMp;
				MpText.text = _mp.ToString();
			}
		}

		public int MaxMp
		{
			get
			{
				return Role.maxmp;
			}
			set
			{
				Role.maxmp = value;
			}
		}

		public double Sp
		{
			get
			{
				return _sp;
			}
			set
			{
				_sp = value;
				if (_sp > 100.0)
				{
					_sp = 100.0;
				}
			}
		}

		public int Balls
		{
			get
			{
				return Role.balls;
			}
			set
			{
				Role.balls = value;
				SpSlider.value = value;
			}
		}

		public int Team
		{
			get
			{
				return battleRole.Team;
			}
		}

		public BattleSpriteStatus Status
		{
			get
			{
				return _status;
			}
			set
			{
				_status = value;
				switch (_status)
				{
				case BattleSpriteStatus.Standing:
					if (isUserDefinedAnimation)
					{
						UserdefinedAnimator.Play("stand");
					}
					else
					{
						SpriteAnimator.Play("stand");
					}
					break;
				case BattleSpriteStatus.Moving:
					if (isUserDefinedAnimation)
					{
						UserdefinedAnimator.Play("move");
					}
					else
					{
						SpriteAnimator.Play("move");
					}
					break;
				case BattleSpriteStatus.Attacking:
					if (isUserDefinedAnimation)
					{
						UserdefinedAnimator.Play("attack");
					}
					else
					{
						SpriteAnimator.Play("attack");
					}
					break;
				case BattleSpriteStatus.BeAttack:
					if (isUserDefinedAnimation)
					{
						UserdefinedAnimator.Play("be");
					}
					else
					{
						SpriteAnimator.Play("be");
					}
					break;
				}
			}
		}

		public BattleField ParentBattleField { get; set; }

		public double SpAddSpeed
		{
			get
			{
				if (Role.HasTalent("鬼魅"))
				{
					return 3.5;
				}
				double num = (double)Role.AttributesFinal["shenfa"] / 100.0 + (double)Role.AttributesFinal["gengu"] / 130.0;
				if (num > 2.2)
				{
					num = 2.2;
				}
				if (num < 1.0)
				{
					num = 1.0;
				}
				foreach (Trigger trigger in Role.GetTriggers("sp"))
				{
					num += double.Parse(trigger.Argvs[0]);
				}
				BuffInstance buff = GetBuff("麻痹");
				if (buff != null && buff.Level > 0)
				{
					num -= 0.2 * (double)buff.Level;
				}
				BuffInstance buff2 = GetBuff("神行");
				if (buff2 != null)
				{
					num += (double)buff2.Level * 0.2;
				}
				if (num > 2.5)
				{
					num = 2.5;
				}
				if (num < 0.8)
				{
					num = 0.8;
				}
				BuffInstance buff3 = GetBuff("晕眩");
				if (buff3 != null)
				{
					num = 0.0;
				}
				return num;
			}
		}

		public void ShowRole()
		{
			if (ParentBattleField != null && ParentBattleField.currentSprite != this && ParentBattleField.Status == BattleStatus.UISelectAction)
			{
				ParentBattleField.ShowRole(Role);
				GetComponentInChildren<ToolTipUI>().HideToolTip();
			}
		}

		public void RandomSay(string[] contents)
		{
			Say(contents[Tools.GetRandomInt(0, contents.Length - 1)]);
		}

		public void Say(string content)
		{
			StartCoroutine(Say2(content));
		}

		private IEnumerator Say2(string content)
		{
			GameObject dialogObj = base.transform.Find("Canvas").Find("Dialog").gameObject;
			dialogObj.SetActive(true);
			dialogObj.transform.Find("Text").GetComponent<Text>().text = content;
			dialogObj.transform.Find("Head").GetComponent<Image>().sprite = Resource.GetImage(Role.Head);
			yield return new WaitForSeconds(2f);
			dialogObj.gameObject.SetActive(false);
		}

		public void DisplayHpMpBar()
		{
			if (!_isDisplayingHpMpBar)
			{
				_isDisplayingHpMpBar = true;
				StartCoroutine(DisplayHpMpBarInTime());
			}
		}

		public void ShowHpMpSpBar()
		{
			HpSliderObj.SetActive(true);
			MpSliderObj.SetActive(true);
			SpSliderObj.SetActive(true);
		}

		public void HideHpMpSpBar()
		{
			if (!Configer.IsBattleTipShow)
			{
				HpSliderObj.SetActive(false);
				MpSliderObj.SetActive(false);
				SpSliderObj.SetActive(false);
			}
		}

		private IEnumerator DisplayHpMpBarInTime()
		{
			ShowHpMpSpBar();
			yield return new WaitForSeconds(2f);
			HideHpMpSpBar();
			_isDisplayingHpMpBar = false;
		}

		public static GameObject Create(BattleField field, BattleRole role)
		{
			Role role2 = role.role;
			if (role2 == null)
			{
				Debug.LogError("init battlerole fail, role = null!");
				return null;
			}
			string animation = role.role.GetAnimation();
			bool flag = false;
			GameObject gameObject = null;
			if (CommonSettings.MOD_MODE && UserDefinedAnimationManager.instance.HasAnimation(animation))
			{
				gameObject = UserDefinedAnimationManager.instance.GenerateObject(animation);
				flag = true;
			}
			else
			{
				gameObject = ResourcePool.Get("Animations/" + animation);
			}
			if (gameObject == null)
			{
				Debug.LogError("调用了未定义的动画模型：" + animation);
				gameObject = Resources.Load("Animations/asdg") as GameObject;
			}
			if (gameObject != null)
			{
				GameObject original = Resources.Load("UI/BattleSpritePrefab") as GameObject;
				GameObject gameObject2 = Object.Instantiate(original);
				GameObject gameObject3 = null;
				gameObject3 = ((!flag) ? Object.Instantiate(gameObject) : gameObject);
				gameObject3.name = "sprite";
				BattleSprite component = gameObject2.GetComponent<BattleSprite>();
				component.isUserDefinedAnimation = flag;
				component.spriteObj = gameObject3;
				component.IsCurrent = false;
				gameObject3.transform.SetParent(gameObject2.transform, false);
				gameObject3.transform.localPosition = new Vector3(0f, 0f, 0f);
				component.Bind(field, role);
				component.HideHpMpSpBar();
				role2.Sprite = component;
				return gameObject2;
			}
			return null;
		}

		public void AttackInfo(string text, Color color)
		{
			lock (_attackInfoQueue)
			{
				_attackInfoQueue.Enqueue(new TextInfo(text, color));
				if (_attackInfoQueue.Count == 1)
				{
					Invoke("DoAttackInfo", 0.1f);
				}
			}
		}

		private void DoAttackInfo()
		{
			TextInfo textInfo;
			lock (_attackInfoQueue)
			{
				if (_attackInfoQueue.Count == 0)
				{
					return;
				}
				textInfo = _attackInfoQueue.Dequeue();
			}
			GameObject attackInfoPrefab = ParentBattleField.attackInfoPrefab;
			GameObject gameObject = Object.Instantiate(attackInfoPrefab);
			gameObject.GetComponent<AttackInfo>().Display(X, Y, textInfo.text, textInfo.color, ParentBattleField.attackInfoLayer.transform, delegate
			{
			});
			Invoke("DoAttackInfo", 0.4f);
		}

		public void Bind(BattleField battleField, BattleRole role)
		{
			ParentBattleField = battleField;
			base.transform.parent = battleField.transform;
			Pos = new Vector2(role.X, role.Y);
			FaceRight = role.FaceRight;
			battleRole = role;
			Hp = role.role.hp;
			Mp = role.role.mp;
			NameText.text = role.role.Name;
			if (role.Team == 1)
			{
				NameText.color = Color.yellow;
			}
			else
			{
				NameText.color = Color.red;
			}
		}

		public void SetPos(int x, int y)
		{
			Pos = new Vector2(x, y);
		}

		public bool HasBuff(string buffName)
		{
			return GetBuff(buffName) != null;
		}

		public void DeleteBuff(string buffName)
		{
			BuffInstance buffInstance = null;
			foreach (BuffInstance buff in Buffs)
			{
				if (buff.buff.Name == buffName)
				{
					buffInstance = buff;
					break;
				}
			}
			if (buffInstance != null)
			{
				Buffs.Remove(buffInstance);
			}
		}

		public void Addbuff(string name, int level, int round)
		{
			BuffInstance buffInstance = new BuffInstance();
			buffInstance.buff = new Buff
			{
				Name = name
			};
			buffInstance.Level = level;
			buffInstance.LeftRound = round;
			buffInstance.Owner = this;
			BuffInstance buff = buffInstance;
			AddBuff(buff);
		}

		public void AddBuff(BuffInstance buff)
		{
			if (GetBuff(buff.buff.Name) == null)
			{
				AttackInfo(buff.buff.Name, Color.white);
			}
			DeleteBuff(buff.buff.Name);
			if (!Role.HasTalent("心眼通明") || !(buff.buff.Name == "致盲"))
			{
				Buffs.Add(buff);
			}
		}

		public BuffInstance GetBuff(string name)
		{
			foreach (BuffInstance buff in Buffs)
			{
				if (buff.buff.Name == name)
				{
					return buff;
				}
			}
			return null;
		}

		public List<RoundBuffResult> RunBuffs()
		{
			List<RoundBuffResult> list = new List<RoundBuffResult>();
			List<BuffInstance> list2 = new List<BuffInstance>();
			foreach (BuffInstance buff in Buffs)
			{
				buff.TimeStamp++;
				if (buff.TimeStamp >= 50)
				{
					buff.TimeStamp = 0;
					list.Add(buff.RoundEffect());
					buff.LeftRound--;
					if (Role.HasTalent("清心") && buff.IsDebuff && Tools.ProbabilityTest(0.5))
					{
						buff.LeftRound = 0;
					}
					if (Role.HasTalent("清风") && buff.IsDebuff && Tools.ProbabilityTest(0.015 * (double)Role.Level))
					{
						buff.LeftRound = 0;
					}
					if (buff.LeftRound <= 0)
					{
						list2.Add(buff);
					}
				}
			}
			foreach (BuffInstance item in list2)
			{
				Buffs.Remove(item);
			}
			return list;
		}

		public void Refresh()
		{
			Text component = base.transform.Find("Canvas").Find("BuffText").GetComponent<Text>();
			string text = string.Empty;
			foreach (BuffInstance buff in Buffs)
			{
				text = ((!buff.IsDebuff) ? (text + string.Format("<color='yellow'>{0} {1}\n</color>", buff.buff.Name, buff.Level)) : (text + string.Format("<color='red'>{0} {1}\n</color>", buff.buff.Name, buff.Level)));
			}
			component.text = text;
			TooltipTextObj.GetComponent<Text>().text = string.Format("{0}\n生命{1}/{2}\n内力{3}/{4}\n{5}", Role.Name, Role.hp, Role.maxhp, Role.mp, Role.maxmp, text);
		}

		public void AddSp()
		{
			Sp += SpAddSpeed;
		}

		public void SkillCdRecover()
		{
			foreach (SkillInstance skill in Role.Skills)
			{
				skill.CurrentCd = 0;
				foreach (UniqueSkillInstance uniqueSkill in skill.UniqueSkills)
				{
					uniqueSkill.CurrentCd = 0;
				}
			}
			foreach (InternalSkillInstance internalSkill in Role.InternalSkills)
			{
				foreach (UniqueSkillInstance uniqueSkill2 in internalSkill.UniqueSkills)
				{
					uniqueSkill2.CurrentCd = 0;
				}
			}
			foreach (SpecialSkillInstance specialSkill in Role.SpecialSkills)
			{
				specialSkill.CurrentCd = 0;
			}
		}

		public void Move(List<MoveSearchHelper> way, CommonSettings.VoidCallBack callback)
		{
			if (way.Count == 0)
			{
				callback();
				return;
			}
			Status = BattleSpriteStatus.Moving;
			moveCallback = callback;
			MoveWay = way;
			moved = 0;
			Move(moved);
		}

		private void Move(int count)
		{
			if (moved >= MoveWay.Count)
			{
				moved = 1;
				base.transform.DOKill();
				moveCallback();
				return;
			}
			int x = MoveWay[moved].X;
			int y = MoveWay[moved].Y;
			if (x == X && y == Y)
			{
				moved++;
				Move(moved);
				return;
			}
			if (x < X)
			{
				FaceRight = false;
			}
			if (x > X)
			{
				FaceRight = true;
			}
			MoveTo(x, y);
		}

		public void MoveTo(int x, int y)
		{
			Vector3 vector = new Vector3(BattleField.ToScreenX(x), BattleField.ToScreenY(y), -1f);
			Tween t = null;
			if (x == X)
			{
				t = base.transform.DOMoveY(BattleField.ToScreenY(y), 0.3f);
			}
			else if (y == Y)
			{
				t = base.transform.DOMoveX(BattleField.ToScreenX(x), 0.3f);
			}
			t.SetUpdate(false);
			t.SetEase(Ease.Linear);
			t.OnComplete(delegate
			{
				moved++;
				Pos = new Vector2(x, y);
				if (moved < MoveWay.Count)
				{
					Move(moved);
				}
				else
				{
					moved = 1;
					base.transform.DOKill();
					moveCallback();
				}
			});
			t.Play();
		}

		public void Shake()
		{
			base.transform.DOShakePosition(0.5f, 4f);
		}

		public void Die()
		{
			IsDead = true;
			if (isUserDefinedAnimation)
			{
				spriteObj.transform.Find("sprite").GetComponent<SpriteRenderer>().material.DOFade(0f, 1.5f).OnComplete(delegate
				{
					Object.Destroy(base.gameObject);
				});
			}
			else
			{
				spriteObj.GetComponent<Renderer>().material.DOFade(0f, 1.5f).OnComplete(delegate
				{
					Object.Destroy(base.gameObject);
				});
			}
		}
	}
}
