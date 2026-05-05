using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class RolePanelUI : MonoBehaviour
{
	public GameObject RoleHeadImageObj;

	public GameObject ShuxingPanel;

	public GameObject ZhuangbeiPanel;

	public GameObject JinengPanel;

	public GameObject TianfuPanel;

	public GameObject LiezhuanPanel;

	public GameObject LiezhuanTextObj;

	public GameObject SkillSelectItemObj;

	public GameObject SkillSelectMenuObj;

	public GameObject LeftPointTextObj;

	public GameObject LeftPointButtonObj;

	public GameObject LeftPointPanelObj;

	public GameObject ItemPreviewPanelObj;

	public GameObject TianfuTextObj;

	public GameObject SkillSelectSuggestTextObj;

	public GameObject WuqiButtonObj;

	public GameObject FangjuButtonObj;

	public GameObject ShipinButtonObj;

	public GameObject JingshuButtonObj;

	public GameObject WuxueTextObj;

	public GameObject ItemDetailPanelObj;

	public GameObject ItemMenuObj;

	public GameObject MessageboxObj;

	public GameObject HpMaskObj;

	public GameObject MpMaskObj;

	private CommonSettings.VoidCallBack _cancelCallback;

	private bool _isActive = true;

	public GameObject ItemWuqi;

	public GameObject ItemFangju;

	public GameObject ItemShipin;

	public GameObject ItemJingshu;

	private Role _role;

	private Text nameText
	{
		get
		{
			return base.transform.Find("NameText").GetComponent<Text>();
		}
	}

	private Image roleHeadImage
	{
		get
		{
			return RoleHeadImageObj.GetComponent<Image>();
		}
	}

	private Text roleDetail
	{
		get
		{
			return base.transform.Find("TextRoleDetail").GetComponent<Text>();
		}
	}

	private Text TianfuText
	{
		get
		{
			return TianfuTextObj.GetComponent<Text>();
		}
	}

	private Text WuxueText
	{
		get
		{
			return WuxueTextObj.GetComponent<Text>();
		}
	}

	private SelectMenu SkillSelectMenu
	{
		get
		{
			return SkillSelectMenuObj.GetComponent<SelectMenu>();
		}
	}

	private ItemMenu itemMenu
	{
		get
		{
			return ItemMenuObj.GetComponent<ItemMenu>();
		}
	}

	public void Refresh()
	{
		LeftPointButtonObj.SetActive(_isActive);
		Role role = _role;
		try
		{
			nameText.text = CommonSettings.getRoleName(role.Key);
		}
		catch
		{
			nameText.text = role.Name;
		}
		roleHeadImage.sprite = Resource.GetImage(role.Head);
		string text = string.Format("{0}/{1}", role.exp, role.LevelupExp);
		if (role.Level >= CommonSettings.MAX_LEVEL)
		{
			text = "-/-";
		}
		roleDetail.text = string.Format("等级 {2}\n生命 {4}/{5}\n内力 {6}/{7}\n经验 {3}\n攻击 {0}\n韧性 {1}", (int)role.Attack, (int)role.Defence, role.level, text, role.hp, role.maxhp, role.mp, role.maxmp);
		ShuxingPanel.transform.Find("HpSlider").GetComponent<Slider>().value = (float)role.hp / (float)role.maxhp;
		ShuxingPanel.transform.Find("MpSlider").GetComponent<Slider>().value = (float)role.mp / (float)role.maxmp;
		HpMaskObj.transform.localPosition = new Vector3(HpMaskObj.transform.localPosition.x, (float)role.hp / (float)role.maxhp * 100f - 90f);
		MpMaskObj.transform.localPosition = new Vector3(MpMaskObj.transform.localPosition.x, (float)role.mp / (float)role.maxmp * 100f - 90f);
		ShuxingPanel.transform.Find("TextHp").GetComponent<Text>().text = string.Format("{0}/{1}", role.hp, role.maxhp);
		ShuxingPanel.transform.Find("TextMp").GetComponent<Text>().text = string.Format("{0}/{1}", role.mp, role.maxmp);
		ShuxingPanel.transform.Find("Quanfa").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("quanzhang");
		ShuxingPanel.transform.Find("Jianfa").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("jianfa");
		ShuxingPanel.transform.Find("Daofa").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("daofa");
		ShuxingPanel.transform.Find("Qimen").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("qimen");
		ShuxingPanel.transform.Find("Bili").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("bili");
		ShuxingPanel.transform.Find("Wuxing").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("wuxing");
		ShuxingPanel.transform.Find("Shenfa").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("shenfa");
		ShuxingPanel.transform.Find("Fuyuan").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("fuyuan");
		ShuxingPanel.transform.Find("Gengu").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("gengu");
		ShuxingPanel.transform.Find("Dingli").Find("InfoText").GetComponent<Text>()
			.text = role.GetAttributeString("dingli");
		string text2 = ((role.leftpoint != 0) ? string.Format("<color='green'>{0}</color>", role.leftpoint) : "0");
		LeftPointTextObj.GetComponent<Text>().text = text2;
		RefreshZhuangbei();
		RefreshSkills();
		RefreshTalents();
		string liezhuan = Resource.GetLiezhuan(role);
		if (string.IsNullOrEmpty(liezhuan))
		{
			LiezhuanTextObj.GetComponent<Text>().text = "无";
		}
		else
		{
			LiezhuanTextObj.GetComponent<Text>().text = liezhuan;
		}
	}

	public void Show(Role role, CommonSettings.VoidCallBack cancelCallback = null, bool isActive = true)
	{
		Clear();
		_cancelCallback = cancelCallback;
		_role = role;
		_isActive = isActive;
		base.gameObject.SetActive(true);
		Refresh();
		SetFocusShuxing();
	}

	public void OnEquipWuqi()
	{
		OnFindEquippment(ItemType.Weapon);
	}

	public void OnEquipFangju()
	{
		OnFindEquippment(ItemType.Armor);
	}

	public void OnEquipShipin()
	{
		OnFindEquippment(ItemType.Accessories);
	}

	public void OnEquipJingshu()
	{
		OnFindEquippment(ItemType.Book);
	}

	public void OnLeftPointAddButton()
	{
		LeftPointPanelObj.GetComponent<LeftPointPanelUI>().Show(_role, this);
	}

	public void AddBili()
	{
		if (_role.bili <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0 && !GameEngine.IsMobilePlatform && _isActive)
		{
			_role.bili++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			Refresh();
		}
	}

	public void AddWuxing()
	{
		if (_role.wuxing <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0 && !GameEngine.IsMobilePlatform && _isActive)
		{
			_role.wuxing++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			Refresh();
		}
	}

	public void AddShenfa()
	{
		if (_role.shenfa <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0 && !GameEngine.IsMobilePlatform && _isActive)
		{
			_role.shenfa++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			Refresh();
		}
	}

	public void AddFuyuan()
	{
		if (_role.fuyuan <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0 && !GameEngine.IsMobilePlatform && _isActive)
		{
			_role.fuyuan++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			Refresh();
		}
	}

	public void AddGengu()
	{
		if (_role.gengu <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0 && !GameEngine.IsMobilePlatform && _isActive)
		{
			_role.gengu++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			Refresh();
		}
	}

	public void AddDingli()
	{
		if (_role.dingli <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0 && !GameEngine.IsMobilePlatform && _isActive)
		{
			_role.dingli++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			Refresh();
		}
	}

	private void OnFindEquippment(ItemType type)
	{
		itemMenu.Show("选择你需要装备的物品", RuntimeData.Instance.GetItems(type), delegate(object ret)
		{
			ItemInstance item = ret as ItemInstance;
			itemMenu.Hide();
			ItemDetailPanelObj.GetComponent<ItemSkillDetailPanelUI>().Show(item, ItemDetailMode.Selectable, delegate
			{
				if (item.CanEquip(_role))
				{
					_role.Equipment.Add(item);
					AudioManager.Instance.PlayEffect("音效.装备");
					RuntimeData.Instance.addItem(item, -1);
					ItemDetailPanelObj.SetActive(false);
					Refresh();
				}
				else
				{
					MessageboxObj.GetComponent<MessageBoxUI>().Show("装备选取错误", "你的人物不满足装备条件，需要：\n<color='red'>" + item.EquipCase + "</color>", Color.white, delegate
					{
						ItemDetailPanelObj.SetActive(false);
						itemMenu.gameObject.SetActive(true);
					});
				}
			}, delegate
			{
				ItemDetailPanelObj.SetActive(false);
				itemMenu.gameObject.SetActive(true);
			});
		}, delegate
		{
		}, delegate(object ret)
		{
			ItemInstance itemInstance = ret as ItemInstance;
			return itemInstance.CanEquip(_role);
		});
	}

	private void RefreshZhuangbei()
	{
		SetZhuangbei(ItemWuqi, ItemType.Weapon, WuqiButtonObj);
		SetZhuangbei(ItemFangju, ItemType.Armor, FangjuButtonObj);
		SetZhuangbei(ItemShipin, ItemType.Accessories, ShipinButtonObj);
		SetZhuangbei(ItemJingshu, ItemType.Book, JingshuButtonObj);
	}

	private void SetZhuangbei(GameObject itemObj, ItemType type, GameObject zhuangbeiButton)
	{
		ItemInstance item = _role.GetEquipment(type);
		if (item != null)
		{
			ShowItem(itemObj, item, delegate
			{
				ItemDetailPanelObj.GetComponent<ItemSkillDetailPanelUI>().Show(item, ItemDetailMode.DropEquipped, delegate
				{
					ItemDetailPanelObj.SetActive(false);
					_role.Equipment.Remove(item);
					AudioManager.Instance.PlayEffect("音效.装备");
					RuntimeData.Instance.addItem(item);
					Refresh();
				}, delegate
				{
					ItemDetailPanelObj.SetActive(false);
				});
			});
			zhuangbeiButton.gameObject.SetActive(false);
		}
		else
		{
			itemObj.SetActive(false);
			zhuangbeiButton.gameObject.SetActive(true);
		}
		zhuangbeiButton.GetComponent<Button>().interactable = _isActive;
		itemObj.GetComponent<Button>().interactable = _isActive;
		SkillSelectSuggestTextObj.SetActive(_isActive);
	}

	private void ShowItem(GameObject obj, ItemInstance item, CommonSettings.VoidCallBack callback)
	{
		obj.GetComponent<ItemUI>().Bind(item, 1, callback, null, ItemPreviewPanelObj);
		obj.SetActive(true);
	}

	private void RefreshSkills()
	{
		SkillSelectMenu.Clear();
		foreach (SpecialSkillInstance specialSkill in _role.SpecialSkills)
		{
			AddSkillItem(specialSkill);
		}
		foreach (SkillInstance skill in _role.Skills)
		{
			AddSkillItem(skill);
			foreach (UniqueSkillInstance uniqueSkill in skill.UniqueSkills)
			{
				if (skill.Level >= uniqueSkill.RequireLevel)
				{
					AddSkillItem(uniqueSkill);
				}
			}
		}
		foreach (InternalSkillInstance internalSkill in _role.InternalSkills)
		{
			AddSkillItem(internalSkill);
			foreach (UniqueSkillInstance uniqueSkill2 in internalSkill.UniqueSkills)
			{
				if (internalSkill.Level >= uniqueSkill2.RequireLevel)
				{
					AddSkillItem(uniqueSkill2);
				}
			}
		}
		SkillSelectMenu.Show();
	}

	private void RefreshTalents()
	{
		List<string> list = new List<string>();
		string text = string.Empty;
		int num = 0;
		foreach (string talent in _role.Talents)
		{
			list.Add(talent);
			text += string.Format("<color='red'>{0}</color> <color='blue'>【消耗武学常识:{1}】</color>\n<color='black'>{2}</color>\n\n", talent, Resource.GetTalentCost(talent), Resource.GetTalentDesc(talent));
			num += Resource.GetTalentCost(talent);
		}
		foreach (string equipmentTalent in _role.EquipmentTalents)
		{
			if (!list.Contains(equipmentTalent))
			{
				list.Add(equipmentTalent);
				text += string.Format("<color='yellow'>{0}</color> <color='blue'>【装备/武学被动生效】</color>\n<color='black'>{2}</color>\n\n", equipmentTalent, Resource.GetTalentCost(equipmentTalent), Resource.GetTalentDesc(equipmentTalent));
			}
		}
		foreach (string talent2 in _role.GetEquippedInternalSkill().Talents)
		{
			if (!list.Contains(talent2))
			{
				list.Add(talent2);
				text += string.Format("<color='cyan'>{0}</color> <color='blue'>【内功装备天赋】</color>\n<color='black'>{2}</color>\n\n", talent2, Resource.GetTalentCost(talent2), Resource.GetTalentDesc(talent2));
			}
		}
		WuxueText.text = string.Format("武学常识 {0}/{1}", num, _role.AttributesFinal["wuxue"]);
		TianfuText.text = text;
	}

	private void AddSkillItem(SkillBox s)
	{
		GameObject gameObject = Object.Instantiate(SkillSelectItemObj);
		SkillSelectItemUI component = gameObject.GetComponent<SkillSelectItemUI>();
		component.Bind(s, _isActive, ItemPreviewPanelObj);
		SkillSelectMenu.AddItem(gameObject);
	}

	private void ResetAllTabPanels()
	{
		ZhuangbeiPanel.SetActive(false);
		JinengPanel.SetActive(false);
		TianfuPanel.SetActive(false);
		LiezhuanPanel.SetActive(false);
		ShuxingPanel.SetActive(false);
	}

	private void Clear()
	{
		_role = null;
		ResetAllTabPanels();
	}

	public void Hide()
	{
		base.gameObject.SetActive(false);
		if (_cancelCallback != null)
		{
			_cancelCallback();
		}
	}

	public void OnCloseClick()
	{
		Hide();
	}

	public void SetFocusShuxing()
	{
		ResetAllTabPanels();
		ShuxingPanel.SetActive(true);
	}

	public void SetFocusZhuangbei()
	{
		ResetAllTabPanels();
		ZhuangbeiPanel.SetActive(true);
	}

	public void SetFocusJineng()
	{
		ResetAllTabPanels();
		JinengPanel.SetActive(true);
	}

	public void SetFocusTianfu()
	{
		ResetAllTabPanels();
		TianfuPanel.SetActive(true);
	}

	public void SetFocusLiezhuan()
	{
		ResetAllTabPanels();
		LiezhuanPanel.SetActive(true);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
