using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class MenpaiSelectUI : MonoBehaviour
{
	public GameObject HeadImageObj;

	public GameObject MenpaiNameTextObj;

	public GameObject MenpaiInfoTextObj;

	public GameObject MenpaiDescTextObj;

	public GameObject BackgroundObj;

	private List<Menpai> Menpais = new List<Menpai>();

	private int currentIndex;

	public void OnPrevButtonClicked()
	{
		currentIndex--;
		if (currentIndex < 0)
		{
			currentIndex = Menpais.Count - 1;
		}
		Refresh();
	}

	public void OnNextButtonClicked()
	{
		currentIndex++;
		if (currentIndex >= Menpais.Count)
		{
			currentIndex = 0;
		}
		Refresh();
	}

	public void OnConfirmButtonClicked()
	{
		Menpai menpai = Menpais[currentIndex];
		RuntimeData.Instance.gameEngine.SwitchGameScene("story", menpai.story);
	}

	private void Start()
	{
		if (!RuntimeData.Instance.IsInited)
		{
			RuntimeData.Instance.Init();
		}
		AudioManager.Instance.Play("音乐.开场");
		Menpais.Clear();
		foreach (Menpai item in ResourceManager.GetAll<Menpai>())
		{
			Menpais.Add(item);
		}
		currentIndex = 0;
		Refresh();
	}

	private void Refresh()
	{
		Menpai menpai = Menpais[currentIndex];
		if (string.IsNullOrEmpty(menpai.Pic))
		{
			if (RuntimeData.Instance.Team.Count > 0)
			{
				HeadImageObj.GetComponent<Image>().sprite = Resource.GetZhujueHead();
			}
			else
			{
				HeadImageObj.GetComponent<Image>().sprite = null;
			}
		}
		else
		{
			HeadImageObj.GetComponent<Image>().sprite = Resource.GetImage(menpai.Pic);
		}
		BackgroundObj.GetComponent<Image>().sprite = Resource.GetImage(menpai.Background);
		MenpaiNameTextObj.GetComponent<Text>().text = menpai.Name;
		MenpaiInfoTextObj.GetComponent<Text>().text = string.Format("师父：<color='red'>{0}</color>\n顶级武学：<color='yellow'>{1}</color>\n主修：<color='magenta'>{2}</color>\n门派特点：<color='cyan'>{3}</color>", menpai.shifu, menpai.wuxue, menpai.zhuxiu, menpai.tedian);
		MenpaiDescTextObj.GetComponent<Text>().text = menpai.info;
	}

	private void Update()
	{
	}
}
