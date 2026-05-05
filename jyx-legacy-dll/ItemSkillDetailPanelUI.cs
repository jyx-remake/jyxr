using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class ItemSkillDetailPanelUI : MonoBehaviour
{
	private CommonSettings.VoidCallBack _confirmCallback;

	private CommonSettings.VoidCallBack _cancelCallback;

	private CommonSettings.VoidCallBack _confirm2Callback;

	private GameObject confirmButtonObj
	{
		get
		{
			return base.transform.Find("ConfirmButton").gameObject;
		}
	}

	private GameObject confirmButton2Obj
	{
		get
		{
			return base.transform.Find("ConfirmButton2").gameObject;
		}
	}

	private Text confirmButtonText
	{
		get
		{
			return base.transform.Find("ConfirmButton").Find("Text").GetComponent<Text>();
		}
	}

	private Text contentText
	{
		get
		{
			return base.transform.Find("SelectPanel").Find("Text").GetComponent<Text>();
		}
	}

	public void Show(SkillBox skill, CommonSettings.VoidCallBack confirmCallback = null, CommonSettings.VoidCallBack cancelCallback = null)
	{
		_confirmCallback = confirmCallback;
		_cancelCallback = cancelCallback;
		base.gameObject.SetActive(true);
		base.transform.Find("Icon").GetComponent<Image>().sprite = Resource.GetIcon(skill.Icon);
		base.transform.Find("NameText").GetComponent<Text>().text = skill.Name;
		base.transform.Find("NameText").GetComponent<Text>().color = skill.Color;
		base.transform.Find("TypeText").GetComponent<Text>().text = skill.GetSkillTypeChinese();
		confirmButtonObj.SetActive(false);
		confirmButton2Obj.SetActive(false);
		contentText.text = skill.DescriptionInRichtextBlackBg;
	}

	public void Show(ItemInstance item, ItemDetailMode mode, CommonSettings.VoidCallBack confirmCallback = null, CommonSettings.VoidCallBack cancelCallback = null, CommonSettings.VoidCallBack confirm2Callback = null)
	{
		_confirmCallback = confirmCallback;
		_cancelCallback = cancelCallback;
		_confirm2Callback = confirm2Callback;
		base.gameObject.SetActive(true);
		base.transform.Find("Icon").GetComponent<Image>().sprite = Resource.GetImage(item.pic);
		base.transform.Find("NameText").GetComponent<Text>().text = item.Name;
		base.transform.Find("NameText").GetComponent<Text>().color = item.GetColor();
		base.transform.Find("TypeText").GetComponent<Text>().text = item.GetTypeStr() + item.GetLevelStr();
		confirmButton2Obj.SetActive(false);
		switch (mode)
		{
		case ItemDetailMode.Usable:
			confirmButtonObj.SetActive(true);
			confirmButtonText.text = "使用";
			break;
		case ItemDetailMode.DropEquipped:
			confirmButtonObj.SetActive(true);
			confirmButtonText.text = "卸下";
			break;
		case ItemDetailMode.Selectable:
			confirmButtonObj.SetActive(true);
			confirmButtonText.text = "确认";
			break;
		case ItemDetailMode.Equipable:
			confirmButtonObj.SetActive(true);
			confirmButtonText.text = "装备";
			break;
		case ItemDetailMode.Studiable:
			confirmButtonObj.SetActive(true);
			confirmButtonText.text = "修炼";
			break;
		case ItemDetailMode.Disable:
			confirmButtonObj.SetActive(false);
			break;
		case ItemDetailMode.Sellable:
		{
			int num = RuntimeData.Instance.Items[item];
			confirmButtonObj.SetActive(true);
			confirmButtonText.text = "卖出";
			if (num > 1)
			{
				confirmButton2Obj.SetActive(true);
				confirmButton2Obj.transform.Find("Text").GetComponent<Text>().text = "全部卖出";
			}
			else
			{
				confirmButton2Obj.SetActive(false);
			}
			break;
		}
		default:
			Debug.LogError("invalid item panel mode!");
			confirmButtonObj.SetActive(false);
			break;
		}
		contentText.text = item.DescriptionInRichtextBlackEnd;
	}

	public void OnConfirmButtonClicked()
	{
		base.gameObject.SetActive(false);
		if (_confirmCallback != null)
		{
			_confirmCallback();
		}
	}

	public void OnConfirmButton2Cliekd()
	{
		base.gameObject.SetActive(false);
		if (_confirm2Callback != null)
		{
			_confirm2Callback();
		}
	}

	public void OnCancelButtonClicked()
	{
		base.gameObject.SetActive(false);
		if (_cancelCallback != null)
		{
			_cancelCallback();
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
