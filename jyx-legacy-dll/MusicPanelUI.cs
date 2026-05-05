using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class MusicPanelUI : MonoBehaviour
{
	public GameObject selectMenu;

	public GameObject selectItemPrefab;

	public void Show()
	{
		SelectMenu component = selectMenu.GetComponent<SelectMenu>();
		component.Clear();
		foreach (Resource item in ResourceManager.GetAll<Resource>())
		{
			if (item.Key.Contains("音乐."))
			{
				string key = item.Key;
				GameObject gameObject = Object.Instantiate(selectItemPrefab);
				gameObject.transform.Find("Text").GetComponent<Text>().text = item.Key.Replace("音乐.", string.Empty);
				gameObject.GetComponent<Button>().onClick.AddListener(delegate
				{
					AudioManager.Instance.Play(key);
				});
				component.AddItem(gameObject);
			}
		}
		component.Show(delegate
		{
			base.gameObject.SetActive(false);
		});
		base.gameObject.SetActive(true);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
