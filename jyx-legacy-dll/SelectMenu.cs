using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class SelectMenu : MonoBehaviour
{
	private const SelectMenuMode Mode = SelectMenuMode.Grid;

	private CommonSettings.VoidCallBack _cancelCallback;

	public Transform selectContent
	{
		get
		{
			return base.transform.Find("SelectPanel").Find("SelectContent").transform;
		}
	}

	private ScrollRect scrollRect
	{
		get
		{
			return base.transform.Find("SelectPanel").GetComponent<ScrollRect>();
		}
	}

	public void Clear()
	{
		foreach (Transform item in selectContent)
		{
			Object.Destroy(item.gameObject);
		}
		selectContent.DetachChildren();
	}

	public void ShowWithSpacing(float spacing, CommonSettings.VoidCallBack cancelCallback = null)
	{
		selectContent.GetComponent<VerticalLayoutGroup>().spacing = spacing;
		Show(cancelCallback);
	}

	public void Show(CommonSettings.VoidCallBack cancelCallback = null)
	{
		_cancelCallback = cancelCallback;
		base.gameObject.SetActive(true);
		if (cancelCallback != null)
		{
			base.transform.Find("CancelButton").gameObject.SetActive(true);
		}
	}

	public void Hide()
	{
		base.gameObject.SetActive(false);
	}

	public void AddItem(GameObject item)
	{
		item.transform.SetParent(selectContent);
	}

	public void CancelButtonClicked()
	{
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
