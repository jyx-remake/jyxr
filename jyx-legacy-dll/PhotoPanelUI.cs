using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class PhotoPanelUI : MonoBehaviour
{
	public void Show()
	{
		Map currentMap = base.transform.parent.parent.GetComponent<MapUI>().CurrentMap;
		GetComponent<Image>().sprite = Resource.GetImage(currentMap.Pic);
		base.gameObject.SetActive(true);
	}

	public void Hide()
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
