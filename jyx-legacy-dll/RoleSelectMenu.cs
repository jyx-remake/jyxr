using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class RoleSelectMenu : MonoBehaviour
{
	public RoleSelectMenuMode Mode;

	public GameObject RoleItemPrefab;

	public GameObject FriendToBeSelectedCountObj;

	public GameObject FriendToBeSelectedCurrentCountObj;

	public GameObject RoleSelectItemObj;

	public GameObject SelectMenuObj;

	private CommonSettings.StringCallBack _callback;

	private CommonSettings.VoidCallBack _cancelCallback;

	private Battle _battle;

	private List<string> _mustKeys = new List<string>();

	private List<string> _forbiddenKeys = new List<string>();

	private List<string> _selectedKeys = new List<string>();

	private static int roleIndex;

	public Text FriendToBeSelectedCount
	{
		get
		{
			return FriendToBeSelectedCountObj.GetComponent<Text>();
		}
	}

	public Text FriendToBeSelectedCurrentCount
	{
		get
		{
			return FriendToBeSelectedCurrentCountObj.GetComponent<Text>();
		}
	}

	private SelectMenu selectMenu
	{
		get
		{
			return SelectMenuObj.GetComponent<SelectMenu>();
		}
	}

	public List<BattleRole> FriendToBeFillRoles
	{
		get
		{
			List<BattleRole> list = new List<BattleRole>();
			foreach (BattleRole role in _battle.Roles)
			{
				BattleRole battleRole = role;
				if (battleRole.Team == 1 && string.IsNullOrEmpty(battleRole.PredefinedKey))
				{
					list.Add(battleRole);
				}
			}
			return list;
		}
	}

	public void Show(IEnumerable<Role> roles, CommonSettings.StringCallBack callback, CommonSettings.JudgeCallback isRoleActiveCallback = null)
	{
		base.gameObject.SetActive(true);
		_callback = callback;
		selectMenu.Clear();
		foreach (Role role in roles)
		{
			AddRoleItem(role, isRoleActiveCallback);
		}
		if (Mode == RoleSelectMenuMode.RoleSelectMode)
		{
			FriendToBeSelectedCountObj.SetActive(false);
			FriendToBeSelectedCurrentCountObj.SetActive(false);
			base.transform.Find("ConfrmPanel").gameObject.SetActive(false);
		}
		selectMenu.Show(delegate
		{
			base.transform.parent.gameObject.SetActive(false);
		});
	}

	public void Show(Battle battle, List<string> forbiddenKeys, CommonSettings.VoidCallBack cancelCallback = null, CommonSettings.JudgeCallback isRoleActiveCallback = null)
	{
		base.gameObject.SetActive(true);
		selectMenu.Clear();
		_battle = battle;
		if (battle.mustKeys != null)
		{
			_mustKeys = battle.mustKeys;
		}
		if (forbiddenKeys != null)
		{
			_forbiddenKeys = forbiddenKeys;
		}
		_selectedKeys = new List<string>();
		_cancelCallback = cancelCallback;
		base.gameObject.SetActive(true);
		foreach (Role item in RuntimeData.Instance.Team)
		{
			AddRoleItem(item, isRoleActiveCallback);
		}
		if (Mode == RoleSelectMenuMode.BattleSelectMode)
		{
			FriendToBeSelectedCountObj.SetActive(true);
			FriendToBeSelectedCurrentCountObj.SetActive(true);
			base.transform.Find("ConfrmPanel").gameObject.SetActive(true);
			FriendToBeSelectedCount.text = "可选择" + FriendToBeFillRoles.Count + "人";
			FriendToBeSelectedCurrentCount.text = "已选择" + _selectedKeys.Count + "人";
			if (FriendToBeFillRoles.Count == 0)
			{
				CofirmButtonClicked();
				return;
			}
		}
		selectMenu.Show();
	}

	public void Hide()
	{
		base.gameObject.SetActive(false);
	}

	public void AddRoleItem(Role role, CommonSettings.JudgeCallback isRoleActiveCallback)
	{
		string roleKey = role.Key;
		GameObject item = Object.Instantiate(RoleSelectItemObj);
		item.gameObject.SetActive(true);
		item.transform.Find("Text").GetComponent<Text>().text = CommonSettings.getRoleName(roleKey);
		item.transform.Find("IconImage").GetComponent<Image>().sprite = Resource.GetImage(role.Head);
		if (isRoleActiveCallback != null && !isRoleActiveCallback(role))
		{
			item.transform.Find("IconImage").GetComponent<Image>().color = Color.black;
		}
		if (_forbiddenKeys.Contains(roleKey))
		{
			item.transform.Find("StatusCross").gameObject.SetActive(true);
		}
		else if (_mustKeys.Contains(roleKey))
		{
			item.transform.Find("StatusSelected").gameObject.SetActive(true);
			_selectedKeys.Add(roleKey);
		}
		else if (Mode == RoleSelectMenuMode.BattleSelectMode)
		{
			item.GetComponent<Button>().onClick.AddListener(delegate
			{
				if (!_forbiddenKeys.Contains(roleKey))
				{
					if (_selectedKeys.Contains(roleKey) && !_mustKeys.Contains(roleKey))
					{
						item.transform.Find("StatusSelected").gameObject.SetActive(false);
						_selectedKeys.Remove(roleKey);
					}
					else if (_selectedKeys.Count < FriendToBeFillRoles.Count)
					{
						item.transform.Find("StatusSelected").gameObject.SetActive(true);
						_selectedKeys.Add(roleKey);
					}
					FriendToBeSelectedCurrentCount.text = "已选择" + _selectedKeys.Count + "人";
				}
			});
		}
		else if (Mode == RoleSelectMenuMode.RoleSelectMode)
		{
			item.GetComponent<Button>().onClick.AddListener(delegate
			{
				item.GetComponent<Button>().onClick.RemoveAllListeners();
				if (_callback != null)
				{
					_callback(roleKey);
				}
			});
		}
		selectMenu.AddItem(item);
	}

	public void CancelButtonClicked()
	{
		if (_cancelCallback != null)
		{
			_cancelCallback();
		}
	}

	public void CofirmButtonClicked()
	{
		if (_selectedKeys.Count == 0 && FriendToBeFillRoles.Count > 0)
		{
			return;
		}
		if (RuntimeData.Instance.gameEngine.battleType == BattleType.Trial)
		{
			RuntimeData.Instance.gameEngine.CurrentInTrail = _selectedKeys[0];
		}
		for (int i = 0; i < _selectedKeys.Count; i++)
		{
			RuntimeData.Instance.gameEngine.BattleSelectRole_CurrentForbbidenKeys.Add(_selectedKeys[i]);
		}
		Battle battle = _battle.Clone();
		battle.Roles.Clear();
		foreach (BattleRole role in _battle.Roles)
		{
			BattleRole battleRole = role;
			if (battleRole.role != null)
			{
				battleRole.role.Reset();
			}
			if (!string.IsNullOrEmpty(battleRole.PredefinedKey))
			{
				if (battleRole.Team == 1)
				{
					battleRole.role = RuntimeData.Instance.GetTeamRole(battleRole.PredefinedKey);
				}
				else
				{
					battleRole.role = ResourceManager.Get<Role>(battleRole.PredefinedKey).Clone();
					battleRole.role.addRandomTalentAndWeapons();
				}
				battle.Roles.Add(battleRole);
			}
		}
		List<BattleRole> friendToBeFillRoles = FriendToBeFillRoles;
		for (int j = 0; j < _selectedKeys.Count && j + 1 <= FriendToBeFillRoles.Count; j++)
		{
			BattleRole battleRole2 = new BattleRole();
			battleRole2.PredefinedKey = null;
			battleRole2.IsPlayerPickedRole = true;
			battleRole2.role = RuntimeData.Instance.GetTeamRole(_selectedKeys[j]);
			battleRole2.X = friendToBeFillRoles[j].X;
			battleRole2.Y = friendToBeFillRoles[j].Y;
			battleRole2.Face = friendToBeFillRoles[j].Face;
			battle.Roles.Add(battleRole2);
		}
		if (battle.randomBattleRoles != null)
		{
			foreach (BattleRole randomRole in battle.randomBattleRoles.randomRoles)
			{
				battle.Roles.Add(GenerateRandomRole(randomRole));
			}
		}
		if (battle.Key.StartsWith("arena_"))
		{
			int arenaHardLevel = RuntimeData.Instance.gameEngine.ArenaHardLevel;
			IEnumerable<Role> all = ResourceManager.GetAll<Role>();
			List<string> list = new List<string>();
			foreach (Role item in all)
			{
				if (item.Arena)
				{
					if (arenaHardLevel < 5 && item.Level <= arenaHardLevel * 5 && item.Level > (arenaHardLevel - 1) * 5)
					{
						list.Add(item.Key);
					}
					else if (arenaHardLevel == 5 && item.Level >= 25 && item.Level < 30)
					{
						list.Add(item.Key);
					}
					else if (arenaHardLevel == 6 && item.Level >= 30)
					{
						list.Add(item.Key);
					}
				}
			}
			foreach (BattleRole role2 in battle.Roles)
			{
				if (role2.Team != 1)
				{
					string key = list[Tools.GetRandomInt(0, list.Count - 1)];
					role2.role = ResourceManager.Get<Role>(key).Clone();
				}
			}
		}
		if (RuntimeData.Instance.gameEngine.battleType == BattleType.Zhenlongqiju)
		{
			int zhenlongqijuLevel = ModData.ZhenlongqijuLevel;
			foreach (BattleRole role3 in battle.Roles)
			{
				if (role3.Team != 1)
				{
					ZhenlongqijuLogic.PowerupRole(role3.role, zhenlongqijuLevel);
				}
			}
		}
		else
		{
			foreach (BattleRole role4 in battle.Roles)
			{
				if (!role4.IsPlayerPickedRole)
				{
					role4.role.maxhp = (int)((double)role4.role.maxhp * (1.0 + CommonSettings.ZHOUMU_HP_ADD * (double)(RuntimeData.Instance.Round - 1)));
					role4.role.maxmp = (int)((double)role4.role.maxmp * (1.0 + CommonSettings.ZHOUMU_MP_ADD * (double)(RuntimeData.Instance.Round - 1)));
					role4.role.addRoundSkillLevel();
					role4.role.Reset();
				}
			}
		}
		RuntimeData.Instance.gameEngine.BattleSelectRole_GeneratedBattle = battle;
		LoadingUI.LoadingLevel = "Battle";
		Application.LoadLevel("Loading");
	}

	public BattleRole GenerateRandomRole(BattleRole r)
	{
		if (r.IsBoss)
		{
			return GenerateRandomBoss(r);
		}
		int level = r.Level;
		string text = r.Name;
		string animation = r.Animation;
		List<Skill> list = new List<Skill>();
		list.Clear();
		int[] array = new int[4] { 0, 5, 7, 10 };
		int[] array2 = new int[4] { 4, 6, 9, 100 };
		foreach (Skill item in ResourceManager.GetAll<Skill>())
		{
			if (item.Hard >= (float)array[level] && item.Hard <= (float)array2[level])
			{
				list.Add(item);
			}
		}
		List<Role> list2 = new List<Role>();
		list2.Clear();
		list2.Add(ResourceManager.Get<Role>("小混混"));
		list2.Add(ResourceManager.Get<Role>("小混混2"));
		list2.Add(ResourceManager.Get<Role>("小混混3"));
		list2.Add(ResourceManager.Get<Role>("小混混4"));
		list2.Add(ResourceManager.Get<Role>("无量剑弟子"));
		list2.Add(ResourceManager.Get<Role>("全真派入门弟子"));
		list2.Add(ResourceManager.Get<Role>("童姥使者"));
		list2.Add(ResourceManager.Get<Role>("明教徒"));
		list2.Add(ResourceManager.Get<Role>("峨眉弟子"));
		list2.Add(ResourceManager.Get<Role>("青城弟子"));
		list2.Add(ResourceManager.Get<Role>("全真派弟子"));
		list2.Add(ResourceManager.Get<Role>("天龙门弟子"));
		list2.Add(ResourceManager.Get<Role>("丐帮弟子"));
		list2.Add(ResourceManager.Get<Role>("五毒教弟子"));
		int[] array3 = new int[4] { 30, 50, 70, 90 };
		int num = array3[level];
		int index = Tools.GetRandomInt(0, list2.Count - 1) % list2.Count;
		Role role = list2[index].Clone();
		int num2 = 1;
		switch (level)
		{
		case 0:
			num2 = Tools.GetRandomInt(1, 5);
			break;
		case 1:
			num2 = Tools.GetRandomInt(6, 10);
			break;
		case 2:
			num2 = Tools.GetRandomInt(11, 15);
			break;
		case 3:
			num2 = Tools.GetRandomInt(16, 20);
			break;
		default:
			num2 = 20;
			break;
		}
		role.Name = text;
		role.Key = text + roleIndex;
		roleIndex++;
		role.level = num2;
		role.Exp = 0;
		role.maxhp = num2 * 70;
		role.hp = num2 * 70;
		role.maxmp = num2 * 70;
		role.mp = num2 * 70;
		role.bili = num;
		role.gengu = num;
		role.fuyuan = num;
		role.shenfa = num;
		role.dingli = num;
		role.wuxing = num;
		role.quanzhang = num;
		role.jianfa = num;
		role.daofa = num;
		role.qimen = num;
		if (!string.IsNullOrEmpty(animation))
		{
			role.Animation = animation;
		}
		role.SpecialSkills.Clear();
		role.Skills.Clear();
		role.InternalSkills.Clear();
		SkillInstance skillInstance = new SkillInstance();
		skillInstance.name = list[Tools.GetRandomInt(0, list.Count - 1) % list.Count].Name;
		skillInstance.level = Tools.GetRandomInt(1, 6);
		skillInstance.Owner = role;
		SkillInstance skillInstance2 = skillInstance;
		skillInstance2.RefreshUniquSkills();
		role.Skills.Add(skillInstance2);
		InternalSkillInstance internalSkillInstance = new InternalSkillInstance();
		internalSkillInstance.name = "基本内功";
		internalSkillInstance.level = 10;
		internalSkillInstance.equipped = 1;
		internalSkillInstance.Owner = role;
		InternalSkillInstance internalSkillInstance2 = internalSkillInstance;
		internalSkillInstance2.RefreshUniquSkills();
		role.InternalSkills.Add(internalSkillInstance2);
		role.addRandomTalentAndWeapons();
		foreach (SkillInstance skill in role.Skills)
		{
			skill.CurrentCd = 0;
		}
		BattleRole battleRole = new BattleRole();
		battleRole.PredefinedKey = null;
		battleRole.role = role;
		battleRole.X = r.X;
		battleRole.Y = r.Y;
		battleRole.Face = r.Face;
		battleRole.Team = 2;
		return battleRole;
	}

	public BattleRole GenerateRandomBoss(BattleRole r)
	{
		int level = r.Level;
		List<Role> list = new List<Role>();
		list.Clear();
		foreach (Role item in ResourceManager.GetAll<Role>())
		{
			if (item.Arena)
			{
				if (level < 5 && level * 5 < item.Level && (level + 1) * 5 >= item.Level)
				{
					list.Add(item);
				}
				if (level >= 5 && item.Level > level * 5)
				{
					list.Add(item);
				}
			}
		}
		Role role = list[Tools.GetRandomInt(0, list.Count) % list.Count].Clone();
		role.addRandomTalentAndWeapons();
		foreach (SkillInstance skill in role.Skills)
		{
			skill.CurrentCd = 0;
		}
		BattleRole battleRole = new BattleRole();
		battleRole.PredefinedKey = null;
		battleRole.role = role;
		battleRole.X = r.X;
		battleRole.Y = r.Y;
		battleRole.Face = r.Face;
		battleRole.Team = 2;
		return battleRole;
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
