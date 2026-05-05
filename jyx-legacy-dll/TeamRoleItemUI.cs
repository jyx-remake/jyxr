using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class TeamRoleItemUI : MonoBehaviour
{
	public GameObject ItemDetailPanelObj;

	public GameObject ZhuangbeiPanel;

	public GameObject itemMenuObj;

	public GameObject messageBoxObj;

	private Role _role;

	private int _index = -1;

	private TeamPanelUI _parent;

	private CommonSettings.StringCallBack _callback;

	private Image headImage
	{
		get
		{
			return base.transform.Find("_bgColor").Find("HeadImage").GetComponent<Image>();
		}
	}

	private Text nameText
	{
		get
		{
			return base.transform.Find("NameText").GetComponent<Text>();
		}
	}

	private Text infoText
	{
		get
		{
			return base.transform.Find("InfoText").GetComponent<Text>();
		}
	}

	private ItemUI ItemWuqi
	{
		get
		{
			return base.transform.Find("ZhuangbeiPanel").Find("_bg1").Find("ItemWuqi")
				.GetComponent<ItemUI>();
		}
	}

	private ItemUI ItemFangju
	{
		get
		{
			return base.transform.Find("ZhuangbeiPanel").Find("_bg2").Find("ItemFangju")
				.GetComponent<ItemUI>();
		}
	}

	private ItemUI ItemShipin
	{
		get
		{
			return base.transform.Find("ZhuangbeiPanel").Find("_bg3").Find("ItemShipin")
				.GetComponent<ItemUI>();
		}
	}

	private ItemUI ItemJingshu
	{
		get
		{
			return base.transform.Find("ZhuangbeiPanel").Find("_bg4").Find("ItemJingshu")
				.GetComponent<ItemUI>();
		}
	}

	private Button upButton
	{
		get
		{
			return base.transform.Find("UpButton").GetComponent<Button>();
		}
	}

	private Button downButton
	{
		get
		{
			return base.transform.Find("DownButton").GetComponent<Button>();
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void OnSelect()
	{
		if (_callback != null)
		{
			_callback(_role.Key);
		}
	}

	public void Bind(Role role, int index, TeamPanelUI parent, CommonSettings.StringCallBack callback)
	{
		_role = role;
		_index = index;
		_parent = parent;
		_callback = callback;
		Refresh();
	}

	public void Refresh()
	{
		nameText.text = _role.Name;
		infoText.text = string.Format("等级:{0}\n攻:{1}\n韧:{2}", _role.Level, (int)_role.Attack, (int)_role.Defence);
		headImage.sprite = Resource.GetImage(_role.Head);
		RefreshZhuangbei();
		if (_index == 0)
		{
			upButton.gameObject.SetActive(false);
			downButton.gameObject.SetActive(false);
		}
		if (_index == 1)
		{
			upButton.gameObject.SetActive(false);
		}
		if (_index == RuntimeData.Instance.Team.Count - 1)
		{
			downButton.gameObject.SetActive(false);
		}
		if (_role.Female)
		{
			base.transform.Find("MaleTag").gameObject.SetActive(false);
			base.transform.Find("FemaleTag").gameObject.SetActive(true);
		}
		else
		{
			base.transform.Find("MaleTag").gameObject.SetActive(true);
			base.transform.Find("FemaleTag").gameObject.SetActive(false);
		}
	}

	public void OnUpButtonClicked()
	{
		Role value = RuntimeData.Instance.Team[_index - 1];
		RuntimeData.Instance.Team[_index - 1] = _role;
		RuntimeData.Instance.Team[_index] = value;
		_parent.Refresh();
	}

	public void OnDownButtonClicked()
	{
		Role value = RuntimeData.Instance.Team[_index + 1];
		RuntimeData.Instance.Team[_index + 1] = _role;
		RuntimeData.Instance.Team[_index] = value;
		_parent.Refresh();
	}

	private void RefreshZhuangbei()
	{
		SetZhuangbei(ItemWuqi, ItemType.Weapon, "ButtonEquipWuqi");
		SetZhuangbei(ItemFangju, ItemType.Armor, "ButtonEquipFangju");
		SetZhuangbei(ItemShipin, ItemType.Accessories, "ButtonEquipShipin");
		SetZhuangbei(ItemJingshu, ItemType.Book, "ButtonEquipJingshu");
	}

	private GameObject GetEquipGameObject(string path)
	{
		for (int i = 1; i <= 4; i++)
		{
			Transform transform = ZhuangbeiPanel.transform.Find("_bg" + i).Find(path);
			if (transform != null)
			{
				return transform.gameObject;
			}
		}
		return null;
	}

	private void SetZhuangbei(ItemUI itemObj, ItemType type, string EquipButtonName)
	{
		ItemInstance item = _role.GetEquipment(type);
		if (item != null)
		{
			itemObj.Bind(item, 1, delegate
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
			}, null, _parent.ItemPreviewObj);
			GetEquipGameObject(EquipButtonName).SetActive(false);
			itemObj.gameObject.SetActive(true);
		}
		else
		{
			itemObj.gameObject.SetActive(false);
			GetEquipGameObject(EquipButtonName).SetActive(true);
		}
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

	private void OnFindEquippment(ItemType type)
	{
		ItemMenu itemMenu = itemMenuObj.GetComponent<ItemMenu>();
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
					messageBoxObj.GetComponent<MessageBoxUI>().Show("装备选取错误", "你的人物不满足装备条件，需要：\n<color='red'>" + item.EquipCase + "</color>", Color.white, delegate
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
}
