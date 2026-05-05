using JyGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSelectItemUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	public GameObject DetailPanelObj;

	private SkillBox _skill;

	private SelectItemMode _mode;

	private GameObject _previewPanel;

	private ItemSkillDetailPanelUI DetailPanel
	{
		get
		{
			return DetailPanelObj.GetComponent<ItemSkillDetailPanelUI>();
		}
	}

	public SkillBox GetSkill()
	{
		return _skill;
	}

	public void SetMode(SelectItemMode mode)
	{
		_mode = mode;
	}

	public void OnPointerEnter(PointerEventData data)
	{
		if (!CommonSettings.TOUCH_MODE && _previewPanel != null)
		{
			_previewPanel.GetComponent<ItemPreviewPanelUI>().Show(_skill.Name + "\n" + _skill.DescriptionInRichtextBlackBg);
		}
	}

	public void OnPointerExit(PointerEventData data)
	{
		if (!CommonSettings.TOUCH_MODE && _previewPanel != null)
		{
			_previewPanel.GetComponent<ItemPreviewPanelUI>().Hide();
		}
	}

	public void OnPointerDown(PointerEventData data)
	{
		if (_previewPanel != null)
		{
			_previewPanel.GetComponent<ItemPreviewPanelUI>().Show(_skill.Name + "\n" + _skill.DescriptionInRichtextBlackBg);
		}
	}

	public void OnPointerUp(PointerEventData data)
	{
		if (_previewPanel != null)
		{
			_previewPanel.GetComponent<ItemPreviewPanelUI>().Hide();
		}
	}

	public void Bind(SkillBox skill, bool isActive = true, GameObject previewPanel = null)
	{
		_skill = skill;
		_previewPanel = previewPanel;
		if (skill.IsUnique)
		{
			if ((skill as UniqueSkillInstance)._parent is InternalSkillInstance)
			{
				base.transform.Find("NameText").GetComponent<Text>().text = string.Format("<color='magenta'>{0}</color>", (skill as UniqueSkillInstance)._parent.Name);
			}
			else
			{
				base.transform.Find("NameText").GetComponent<Text>().text = string.Format("<color='white'>{0}</color>", (skill as UniqueSkillInstance)._parent.Name);
			}
			Text component = base.transform.Find("NameText").GetComponent<Text>();
			component.text = component.text + "\n" + skill.Name.Replace(".", string.Empty).Replace((skill as UniqueSkillInstance)._parent.Name, string.Empty);
			base.transform.Find("NameText").GetComponent<Text>().color = skill.Color;
		}
		else
		{
			base.transform.Find("NameText").GetComponent<Text>().text = skill.Name;
			base.transform.Find("NameText").GetComponent<Text>().color = skill.Color;
		}
		if (skill.IsSpecial || skill.IsUnique)
		{
			base.transform.Find("LevelText").GetComponent<Text>().text = string.Empty;
			if (skill.IsUnique)
			{
				base.transform.Find("IsUseToggle").gameObject.SetActive(false);
			}
		}
		else
		{
			base.transform.Find("LevelText").GetComponent<Text>().text = string.Format("{0}", skill.Level);
		}
		base.transform.Find("IconImage").GetComponent<Image>().sprite = Resource.GetIcon(skill.Icon);
		IsOn(skill.IsUsed);
		base.transform.Find("IsUseToggle").GetComponent<Toggle>().interactable = isActive;
	}

	public void IsOn(bool isOn)
	{
		base.transform.Find("IsUseToggle").GetComponent<Toggle>().isOn = isOn;
	}

	public void OnToggleValuedChanged()
	{
		SkillBox skill = _skill;
		bool isOn = base.transform.Find("IsUseToggle").GetComponent<Toggle>().isOn;
		if (skill.IsInternal)
		{
			if (!isOn)
			{
				if (skill == skill.Owner.GetEquippedInternalSkill())
				{
					IsOn(true);
				}
				return;
			}
			skill.Owner.SetEquippedInternalSkill(skill as InternalSkillInstance);
			if (base.transform.parent != null)
			{
				SkillSelectItemUI[] componentsInChildren = base.transform.parent.GetComponentsInChildren<SkillSelectItemUI>();
				foreach (SkillSelectItemUI skillSelectItemUI in componentsInChildren)
				{
					if (skillSelectItemUI.GetSkill().IsInternal && skillSelectItemUI.GetSkill() != skill)
					{
						skillSelectItemUI.IsOn(false);
					}
				}
			}
			RolePanelUI componentInParent = GetComponentInParent<RolePanelUI>();
			if (componentInParent != null)
			{
				componentInParent.Refresh();
			}
		}
		else
		{
			skill.equipped = (isOn ? 1 : 0);
			IsOn(isOn);
		}
	}

	public void OnSkillInfoClicked()
	{
		if (_mode == SelectItemMode.SEEDETAIL)
		{
			DetailPanel.Show(_skill);
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
