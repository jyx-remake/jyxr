using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class EventConfirmPanel : MonoBehaviour
{
	private MapEvent _evt;

	private string _name;

	private int _timeCost;

	public void Show(Sprite image, MapEvent evt, string name, string desc, int timeCost)
	{
		_timeCost = timeCost;
		base.transform.Find("Image").GetComponent<Image>().sprite = image;
		base.transform.Find("TitleText").GetComponent<Text>().text = name;
		base.transform.Find("DescText").GetComponent<Text>().text = desc + string.Format("<color='red'>\n消耗时间:{0}</color>", CommonSettings.HourToChineseTime(timeCost));
		if (evt.lv != -1)
		{
			string text = "战斗难度： ";
			for (int i = 0; i < evt.lv / 5 + 1; i++)
			{
				text += "★";
			}
			base.transform.Find("LvText").GetComponent<Text>().text = text;
		}
		else
		{
			base.transform.Find("LvText").GetComponent<Text>().text = string.Empty;
		}
		base.gameObject.SetActive(true);
		_evt = evt;
		_name = name;
	}

	public void ButtonOkClicked()
	{
		base.gameObject.SetActive(false);
		if (_evt.value != RuntimeData.Instance.CurrentBigMap)
		{
			RuntimeData.Instance.SetLocation(RuntimeData.Instance.CurrentBigMap, _name);
		}
		RuntimeData.Instance.Date = RuntimeData.Instance.Date.AddHours(_timeCost);
		RuntimeData.Instance.gameEngine.SwitchGameScene(_evt.type, _evt.value);
		MapLocationUI.currentLocationUI = null;
		MapRoleUI.currentRoleUI = null;
	}

	public void ButtonCancelClicked()
	{
		base.gameObject.SetActive(false);
		MapLocationUI.currentLocationUI = null;
		MapRoleUI.currentRoleUI = null;
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
