using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class RoleStatePanelUI : MonoBehaviour
{
	public GameObject RoleHeadObj;

	public void Refresh()
	{
		if (RuntimeData.Instance.Team.Count > 0)
		{
			RoleHeadObj.GetComponent<Image>().sprite = Resource.GetZhujueHead();
		}
		base.transform.Find("HardIcon").GetComponent<HardIconScript>().HideSuggestInfo();
		base.transform.Find("HardIcon").Find("NickText").GetComponent<Text>()
			.text = RuntimeData.Instance.CurrentNick;
		Text component = base.transform.Find("HardIcon").Find("ZhoumuText").GetComponent<Text>();
		if (RuntimeData.Instance.GameMode == "normal")
		{
			component.text = "简单:周目" + RuntimeData.Instance.Round;
			component.color = Color.white;
		}
		else if (RuntimeData.Instance.GameMode == "hard")
		{
			component.text = "进阶:周目" + RuntimeData.Instance.Round;
			component.color = Color.yellow;
		}
		else if (RuntimeData.Instance.GameMode == "crazy")
		{
			if (RuntimeData.Instance.AutoSaveOnly)
			{
				component.text = "无悔:周目" + RuntimeData.Instance.Round;
				component.color = Color.magenta;
			}
			else
			{
				component.text = "炼狱:周目" + RuntimeData.Instance.Round;
				component.color = Color.red;
			}
		}
	}

	private void Start()
	{
		Refresh();
	}

	private void Update()
	{
	}
}
