using UnityEngine;
using UnityEngine.UI;

public class ItemPreviewPanelUI : MonoBehaviour
{
	public void Show(string text)
	{
		base.gameObject.SetActive(true);
		base.transform.Find("Text").GetComponent<Text>().text = text;
		Vector3 vector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		base.transform.localPosition = new Vector3(vector.x + 80f, vector.y);
		if (base.transform.localPosition.x > 200f)
		{
			base.transform.localPosition -= new Vector3(500f, 0f);
		}
		base.transform.localPosition = new Vector3(base.transform.localPosition.x, 84f);
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
