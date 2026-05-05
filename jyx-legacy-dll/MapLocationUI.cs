using JyGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapLocationUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public Text locationText;

	public Image locationImage;

	public GameObject storyTag;

	public GameObject mapPointer;

	public static MapLocationUI currentLocationUI;

	private MapUI _mapUI;

	private int _timeCost;

	private MapLocation _location;

	private MapEvent evt;

	private Sprite image;

	private string desc;

	private string locationName;

	public GameObject SuggestPanel;

	public void LocationClicked()
	{
		if (CommonSettings.TOUCH_MODE)
		{
			if (evt != null && currentLocationUI != this)
			{
				_mapUI.ShowEventConfirmPanel(image, evt, locationName, desc, _timeCost);
				currentLocationUI = this;
			}
			else if (evt != null)
			{
				Excute();
			}
		}
		else
		{
			Excute();
		}
	}

	public void Excute()
	{
		_mapUI.HideEventConfirmPanel();
		if (evt != null)
		{
			if (evt.value != RuntimeData.Instance.CurrentBigMap)
			{
				RuntimeData.Instance.SetLocation(RuntimeData.Instance.CurrentBigMap, locationName);
			}
			RuntimeData.Instance.Date = RuntimeData.Instance.Date.AddHours(_timeCost);
			RuntimeData.Instance.gameEngine.SwitchGameScene(evt.type, evt.value);
			currentLocationUI = null;
			MapRoleUI.currentRoleUI = null;
		}
	}

	public void Bind(MapUI mapUI, MapLocation location, int timeCost)
	{
		_mapUI = mapUI;
		_timeCost = timeCost;
		SuggestPanel = mapUI.SuggestPanelObj;
		evt = location.GetActiveEvent();
		bool flag = evt != null;
		locationName = location.getName();
		desc = location.description;
		string text = string.Empty;
		bool flag2 = false;
		if (evt != null)
		{
			if (!string.IsNullOrEmpty(evt.description))
			{
				desc = evt.description;
			}
			text = (string.IsNullOrEmpty(evt.image) ? location.GetImageKey() : evt.image);
			if (evt.IsRepeatOnce)
			{
				flag2 = true;
			}
		}
		_location = location;
		base.transform.localPosition = new Vector3(location.X, location.Y);
		base.transform.localScale = new Vector3(1f, 1f, 1f);
		locationText.text = locationName;
		float num = (float)CommonSettings.timeOpacity[RuntimeData.Instance.Date.Hour / 2];
		if ((double)num < 0.7)
		{
			locationText.color = Color.black;
		}
		else
		{
			locationText.color = Color.black;
		}
		if (text != string.Empty)
		{
			image = Resource.GetImage(text);
			if (image == null && CommonSettings.MOD_MODE)
			{
				image = Resource.GetImage(text, true);
			}
			locationImage.sprite = image;
			if (text.Contains("town.city."))
			{
				locationImage.SetNativeSize();
			}
		}
		if (evt == null || evt.type.Equals("map"))
		{
		}
		if (evt == null && location.name.Equals("帐篷"))
		{
			image = Resource.GetImage("town.zhangpeng_gray", true);
			locationImage.sprite = image;
		}
		if (!flag2)
		{
			storyTag.SetActive(false);
		}
		if (location.getName().Equals(RuntimeData.Instance.GetLocation(RuntimeData.Instance.CurrentBigMap)))
		{
			mapPointer.SetActive(true);
			mapPointer.GetComponent<Image>().sprite = Resource.GetZhujueHead();
		}
		else
		{
			mapPointer.SetActive(false);
		}
		if (RuntimeData.Instance.hasTask())
		{
			Color color = locationImage.color;
			locationImage.color = new Color(color.r, color.g, color.b, 0.3f);
			if (!RuntimeData.Instance.isLocationInTask(location.name))
			{
				Object.Destroy(locationImage.GetComponent<Outline>());
				Object.Destroy(locationText.GetComponent<Outline>());
				evt = new MapEvent();
				evt.type = "story";
				evt.value = "original_新手任务无法进入";
				evt.repeatValue = string.Empty;
				evt.probability = 100;
			}
			storyTag.SetActive(false);
		}
		if (RuntimeData.Instance.isLocationInTask(location.name))
		{
			Color color2 = locationImage.color;
			locationImage.color = new Color(color2.r, color2.g, color2.b, 1f);
			storyTag.SetActive(true);
		}
		if (text != null && text.Contains("town.city.") && !RuntimeData.Instance.isLocationInTask(location.name))
		{
			Color color3 = locationImage.color;
			locationImage.color = new Color(color3.r, color3.g, color3.b, 0.1f);
			Object.Destroy(locationImage.GetComponent<Outline>());
		}
	}

	public void OnPointerEnter(PointerEventData data)
	{
		if (evt != null && !CommonSettings.TOUCH_MODE)
		{
			SuggestPanel.GetComponent<SuggestPanelUI>().Show(image, locationName, desc, _timeCost);
		}
	}

	public void OnPointerExit(PointerEventData data)
	{
		if (!CommonSettings.TOUCH_MODE)
		{
			SuggestPanel.GetComponent<SuggestPanelUI>().Hide();
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
