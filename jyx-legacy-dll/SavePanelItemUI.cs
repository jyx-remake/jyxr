using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class SavePanelItemUI : MonoBehaviour
{
	private CommonSettings.VoidCallBack _callback;

	public void Bind(string title, string saveContent, CommonSettings.VoidCallBack callback)
	{
		_callback = callback;
		if (string.IsNullOrEmpty(saveContent))
		{
			base.transform.Find("Image").GetComponent<Image>().sprite = null;
			base.transform.Find("Image").gameObject.SetActive(false);
			base.transform.Find("Text").GetComponent<Text>().text = "<color='white'>空存档</color>";
		}
		else
		{
			GameSave gameSave = BasePojo.Create<GameSave>(saveContent);
			if (gameSave == null)
			{
				base.transform.Find("Image").GetComponent<Image>().sprite = null;
				base.transform.Find("Image").gameObject.SetActive(false);
				base.transform.Find("Text").GetComponent<Text>().text = "<color='red'>损坏的存档</color>";
			}
			else
			{
				base.transform.Find("Image").GetComponent<Image>().sprite = Resource.GetImage(gameSave.Roles[0].head);
				base.transform.Find("Text").GetComponent<Text>().text = gameSave.ToString();
			}
		}
		base.transform.Find("Text").GetComponent<Text>().text = "<color='cyan'>" + title + "</color>\n" + base.transform.Find("Text").GetComponent<Text>().text;
	}

	public void OnClicked()
	{
		if (_callback != null)
		{
			_callback();
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
