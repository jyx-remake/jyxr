using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class MessageBoxUI : MonoBehaviour
{
	private CommonSettings.VoidCallBack _callback;

	public void Show(string title, string text, Color color, CommonSettings.VoidCallBack callback = null, string confirmText = "确认")
	{
		base.gameObject.SetActive(true);
		base.transform.Find("TitleText").GetComponent<Text>().text = title;
		base.transform.Find("Text").GetComponent<Text>().text = text;
		base.transform.Find("Text").GetComponent<Text>().color = color;
		base.transform.Find("Button").Find("Text").GetComponent<Text>()
			.text = confirmText;
		_callback = callback;
	}

	public void OnConfirmed()
	{
		base.gameObject.SetActive(false);
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
