using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOverScript : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public void OnPointerEnter(PointerEventData data)
	{
		Debug.Log("mouse enter");
	}

	public void OnPointerExit(PointerEventData data)
	{
		Debug.Log("mouse exit");
	}

	public void OnClicked()
	{
		Debug.Log("clicked");
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
