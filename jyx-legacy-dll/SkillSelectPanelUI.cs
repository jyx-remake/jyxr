using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectPanelUI : MonoBehaviour
{
	public GameObject SkillSelectItemObj;

	private CommonSettings.ObjectCallBack _callback;

	public SelectMenu selectMenu
	{
		get
		{
			return base.transform.Find("SelectMenu").GetComponent<SelectMenu>();
		}
	}

	public void Show(IEnumerable<SkillBox> skills, CommonSettings.ObjectCallBack callback)
	{
		_callback = callback;
		selectMenu.Clear();
		foreach (SkillBox skill in skills)
		{
			AddSkillItem(skill);
		}
		base.gameObject.SetActive(true);
	}

	private void AddSkillItem(SkillBox s)
	{
		SkillBox skill = s;
		GameObject gameObject = Object.Instantiate(SkillSelectItemObj);
		gameObject.GetComponent<SkillSelectItemUI>().Bind(s);
		gameObject.GetComponent<SkillSelectItemUI>().SetMode(SelectItemMode.NONE);
		gameObject.transform.Find("IsUseToggle").gameObject.SetActive(false);
		gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
		gameObject.GetComponent<Button>().onClick.AddListener(delegate
		{
			base.gameObject.SetActive(false);
			if (_callback != null)
			{
				_callback(skill);
			}
		});
		selectMenu.AddItem(gameObject);
	}

	public void OnCancelClickd()
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
