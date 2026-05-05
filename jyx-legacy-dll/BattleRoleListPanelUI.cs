using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class BattleRoleListPanelUI : MonoBehaviour
{
	public GameObject RoleItemObj;

	public GameObject SelectMenuObj;

	public GameObject RolePanelObj;

	public void Show(IEnumerable<BattleSprite> sprites)
	{
		SelectMenu component = SelectMenuObj.GetComponent<SelectMenu>();
		component.Clear();
		foreach (BattleSprite sprite in sprites)
		{
			Role role = sprite.Role;
			GameObject gameObject = Object.Instantiate(RoleItemObj);
			gameObject.transform.Find("Head").GetComponent<Image>().sprite = Resource.GetImage(sprite.Role.Head);
			gameObject.transform.Find("Text").GetComponent<Text>().text = sprite.Role.Name;
			gameObject.transform.Find("Text").GetComponent<Text>().color = ((sprite.Team != 1) ? Color.red : Color.yellow);
			gameObject.GetComponent<Button>().onClick.AddListener(delegate
			{
				RolePanelObj.GetComponent<RolePanelUI>().Show(role, null, false);
			});
			component.AddItem(gameObject);
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
