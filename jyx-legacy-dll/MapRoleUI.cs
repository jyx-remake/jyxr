using JyGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapRoleUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public const int MAP_ROLE_TIMECOST = 2;

	public static MapRoleUI currentRoleUI;

	private Sprite _sprite;

	private MapUI _mapUI;

	private MapEvent _evt;

	private MapRole _role;

	private string desc;

	private string mapRoleName;

	private GameObject ToolTipPanel
	{
		get
		{
			return base.transform.Find("ToolTipPanel").gameObject;
		}
	}

	public void OnMapRoleClicked()
	{
		if (CommonSettings.TOUCH_MODE)
		{
			if (_evt != null && currentRoleUI != this)
			{
				_mapUI.ShowEventConfirmPanel(_sprite, _evt, mapRoleName, desc, 2);
				currentRoleUI = this;
			}
			else if (_evt != null)
			{
				Execute();
			}
		}
		else
		{
			Execute();
		}
	}

	private void Execute()
	{
		_mapUI.HideEventConfirmPanel();
		if (_evt.value != RuntimeData.Instance.CurrentBigMap)
		{
			RuntimeData.Instance.SetLocation(RuntimeData.Instance.CurrentBigMap, mapRoleName);
		}
		RuntimeData.Instance.Date = RuntimeData.Instance.Date.AddHours(2.0);
		RuntimeData.Instance.gameEngine.SwitchGameScene(_evt.type, _evt.value);
		MapLocationUI.currentLocationUI = null;
		currentRoleUI = null;
	}

	public void Bind(MapUI mapUI, MapRole role, int index, MapEvent evt)
	{
		HideToolTip();
		_evt = evt;
		_role = role;
		_mapUI = mapUI;
		mapRoleName = role.Name;
		desc = role.description;
		string text = ((!(role.pic == "无") && !string.IsNullOrEmpty(role.pic)) ? role.pic : string.Empty);
		if (string.IsNullOrEmpty(text))
		{
			Role role2 = ResourceManager.Get<Role>(role.roleKey);
			if (role2 != null && !string.IsNullOrEmpty(role2.Head))
			{
				text = role2.Head;
			}
		}
		bool active = false;
		if (_evt != null)
		{
			if (!string.IsNullOrEmpty(_evt.description))
			{
				desc = _evt.description;
			}
			if (!string.IsNullOrEmpty(_evt.image))
			{
				text = _evt.image;
			}
			if (_evt.IsRepeatOnce)
			{
				active = true;
			}
		}
		_sprite = Resource.GetImage(text);
		base.transform.Find("_mask").Find("HeadImage").GetComponent<Image>()
			.sprite = _sprite;
		if (text.StartsWith("地图"))
		{
			base.transform.Find("_mask").Find("HeadImage").transform.localScale = new Vector3(1.7777778f, 1f);
		}
		base.transform.localPosition = new Vector3(-398 + index * 200, 90f, 0f);
		base.transform.Find("StoryTag").gameObject.SetActive(active);
		base.transform.Find("NameText").GetComponent<Text>().text = mapRoleName;
		if (RuntimeData.Instance.hasTask())
		{
			base.transform.Find("StoryTag").gameObject.SetActive(false);
		}
		if (RuntimeData.Instance.isLocationInTask(role.Name))
		{
			base.transform.Find("StoryTag").gameObject.SetActive(true);
		}
	}

	public void OnPointerEnter(PointerEventData data)
	{
		if (!CommonSettings.TOUCH_MODE)
		{
			ShowToolTip();
		}
	}

	public void OnPointerExit(PointerEventData data)
	{
		if (!CommonSettings.TOUCH_MODE)
		{
			HideToolTip();
		}
	}

	private void ShowToolTip()
	{
		ToolTipPanel.SetActive(true);
		ToolTipPanel.transform.Find("Text").GetComponent<Text>().text = desc;
	}

	private void HideToolTip()
	{
		ToolTipPanel.SetActive(false);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
