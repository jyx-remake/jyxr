using UnityEngine;
using UnityEngine.EventSystems;

public class HardIconScript : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	public void OnPointerDown(PointerEventData data)
	{
		ShowSuggestInfo();
	}

	public void OnPointerUp(PointerEventData data)
	{
		HideSuggestInfo();
	}

	public void OnPointerEnter(PointerEventData data)
	{
		ShowSuggestInfo();
	}

	public void OnPointerExit(PointerEventData data)
	{
		HideSuggestInfo();
	}

	public void HideSuggestInfo()
	{
		base.transform.Find("_bg").gameObject.SetActive(false);
		base.transform.Find("NickText").gameObject.SetActive(false);
		base.transform.Find("ZhoumuText").gameObject.SetActive(false);
	}

	public void ShowSuggestInfo()
	{
		base.transform.Find("_bg").gameObject.SetActive(true);
		base.transform.Find("NickText").gameObject.SetActive(true);
		base.transform.Find("ZhoumuText").gameObject.SetActive(true);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
