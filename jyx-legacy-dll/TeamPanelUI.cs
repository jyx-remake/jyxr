using System.Collections.Generic;
using JyGame;
using UnityEngine;

public class TeamPanelUI : MonoBehaviour
{
	public GameObject TeamRoleItemObj;

	public GameObject ItemPreviewObj;

	private CommonSettings.StringCallBack _callback;

	public SelectMenu selectMenu
	{
		get
		{
			return base.transform.Find("SelectMenu").GetComponent<SelectMenu>();
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void Show(CommonSettings.StringCallBack callback)
	{
		_callback = callback;
		IEnumerable<Role> team = RuntimeData.Instance.Team;
		selectMenu.Clear();
		int num = 0;
		foreach (Role item in team)
		{
			GameObject gameObject = Object.Instantiate(TeamRoleItemObj);
			gameObject.GetComponent<TeamRoleItemUI>().Bind(item, num, this, callback);
			selectMenu.AddItem(gameObject);
			num++;
		}
		selectMenu.Show(delegate
		{
			selectMenu.Hide();
			Hide();
		});
	}

	public void Hide()
	{
		base.gameObject.SetActive(false);
	}

	public void Refresh()
	{
		Show(_callback);
	}
}
