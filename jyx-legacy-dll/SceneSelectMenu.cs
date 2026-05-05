using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class SceneSelectMenu : MonoBehaviour
{
	public SceneSelectMenuMode Mode;

	public GameObject TowerItemObj;

	public GameObject SelectMenuObj;

	private CommonSettings.StringCallBack _callback;

	public string currentSelection;

	private SelectMenu selectMenu
	{
		get
		{
			return SelectMenuObj.GetComponent<SelectMenu>();
		}
	}

	public void Show(IEnumerable<Tower> towers, CommonSettings.StringCallBack callback)
	{
		base.gameObject.SetActive(true);
		_callback = callback;
		selectMenu.Clear();
		foreach (Tower tower2 in towers)
		{
			Tower tower = tower2;
			string key = tower.Key;
			AddTowerItem(key);
		}
		Mode = SceneSelectMenuMode.TowerSelectMode;
		selectMenu.ShowWithSpacing(100f, delegate
		{
			base.transform.parent.gameObject.SetActive(false);
		});
	}

	public void Hide()
	{
		base.gameObject.SetActive(false);
	}

	public void AddTowerItem(string towerKey)
	{
		GameObject item = Object.Instantiate(TowerItemObj);
		item.gameObject.SetActive(true);
		item.transform.Find("Text").GetComponent<Text>().text = towerKey;
		item.GetComponent<Button>().onClick.AddListener(delegate
		{
			foreach (Transform item2 in selectMenu.selectContent)
			{
				item2.Find("TextSelectedSign").gameObject.SetActive(false);
			}
			item.transform.Find("TextSelectedSign").gameObject.SetActive(true);
			currentSelection = towerKey;
		});
		selectMenu.AddItem(item);
	}

	public void CofirmButtonClicked()
	{
		_callback(currentSelection);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
