using JyGame;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	public GameObject TooltipObj;

	public GameObject[] OtherTooltipObjs;

	public bool IsTouchEnable = true;

	public void OnPointerDown(PointerEventData data)
	{
		if (CommonSettings.TOUCH_MODE && IsTouchEnable)
		{
			ShowToolTip();
		}
	}

	public void OnPointerUp(PointerEventData data)
	{
		if (CommonSettings.TOUCH_MODE && IsTouchEnable)
		{
			HideToolTip();
		}
	}

	public void OnPointerEnter(PointerEventData data)
	{
		if (!CommonSettings.DEBUG_FORCE_MOBILE_MODE)
		{
			ShowToolTip();
		}
	}

	public void OnPointerExit(PointerEventData data)
	{
		if (!CommonSettings.DEBUG_FORCE_MOBILE_MODE)
		{
			HideToolTip();
		}
	}

	public void ShowToolTip()
	{
		if (TooltipObj == null)
		{
			base.transform.Find("_tooltip").gameObject.SetActive(true);
			return;
		}
		TooltipObj.SetActive(true);
		if (OtherTooltipObjs != null && OtherTooltipObjs.Length != 0)
		{
			GameObject[] otherTooltipObjs = OtherTooltipObjs;
			foreach (GameObject gameObject in otherTooltipObjs)
			{
				gameObject.SetActive(true);
			}
		}
	}

	public void HideToolTip()
	{
		if (TooltipObj == null)
		{
			base.transform.Find("_tooltip").gameObject.SetActive(false);
			return;
		}
		TooltipObj.SetActive(false);
		if (OtherTooltipObjs != null && OtherTooltipObjs.Length != 0)
		{
			GameObject[] otherTooltipObjs = OtherTooltipObjs;
			foreach (GameObject gameObject in otherTooltipObjs)
			{
				gameObject.SetActive(false);
			}
		}
	}

	private void Start()
	{
		HideToolTip();
	}

	private void Update()
	{
	}
}
