using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class SuggestPanelUI : MonoBehaviour
{
	public void Show(Sprite image, string title, string text, int timeCost)
	{
		base.transform.Find("TitleText").GetComponent<Text>().text = title;
		string text2 = string.Empty;
		if (timeCost > 0)
		{
			text2 = string.Format("\n<color='red'>\n消耗时间:{0}</color>", CommonSettings.HourToChineseTime(timeCost));
		}
		base.transform.Find("Text").GetComponent<Text>().text = text + text2;
		if (image == null)
		{
			base.transform.Find("Image").gameObject.SetActive(false);
		}
		else
		{
			base.transform.Find("Image").gameObject.SetActive(true);
			base.transform.Find("Image").GetComponent<Image>().sprite = image;
		}
		Vector3 vector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		base.transform.localPosition = new Vector3(vector.x + 30f, vector.y);
		if (base.transform.localPosition.x > 200f)
		{
			base.transform.localPosition -= new Vector3(430f, 0f);
		}
		if (base.transform.localPosition.y < -150f)
		{
			base.transform.localPosition += new Vector3(0f, 135f);
		}
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
