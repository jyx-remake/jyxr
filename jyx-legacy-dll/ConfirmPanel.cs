using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPanel : MonoBehaviour
{
	private CommonSettings.VoidCallBack callback;

	public void Show(string info, CommonSettings.VoidCallBack confirmCallback)
	{
		base.transform.Find("Text").GetComponent<Text>().text = info;
		base.gameObject.SetActive(true);
		callback = confirmCallback;
	}

	public void Confirm()
	{
		if (callback != null)
		{
			callback();
		}
		base.gameObject.SetActive(false);
	}

	public void Cancel()
	{
		base.gameObject.SetActive(false);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
