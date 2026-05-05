using System;
using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class AchievementPanelUI : MonoBehaviour
{
	public GameObject TabJianghuLilian;

	public GameObject TabChengjiu;

	public GameObject TabWuxue;

	public GameObject JianghuLilianTextObj;

	public GameObject ChengjiuTextObj;

	public GameObject ChengjiuWanchengTextObj;

	public GameObject SkillSelectItemObj;

	public GameObject SkillDetailPanelObj;

	private WuxueFilter _filter;

	public void OnJianghulilianClicked()
	{
		HideAll();
		TabJianghuLilian.SetActive(true);
	}

	public void OnChengjiuClicked()
	{
		HideAll();
		TabChengjiu.SetActive(true);
	}

	public void OnWuxueClicked()
	{
		HideAll();
		TabWuxue.SetActive(true);
	}

	public void CancelClicked()
	{
		base.gameObject.SetActive(false);
	}

	private void HideAll()
	{
		TabJianghuLilian.SetActive(false);
		TabChengjiu.SetActive(false);
		TabWuxue.SetActive(false);
	}

	public void Show()
	{
		base.gameObject.SetActive(true);
		Refresh();
		OnJianghulilianClicked();
	}

	public void SetWuxueFilter(int filterCode)
	{
		_filter = (WuxueFilter)filterCode;
		RefreshWuxue();
	}

	private void RefreshWuxue()
	{
		SelectMenu component = TabWuxue.transform.Find("SelectMenu").GetComponent<SelectMenu>();
		component.Clear();
		foreach (KeyValuePair<string, int> skillMaxLevel in ModData.SkillMaxLevels)
		{
			string skillName = skillMaxLevel.Key;
			int maxLevel = skillMaxLevel.Value;
			Skill skill = ResourceManager.Get<Skill>(skillName);
			InternalSkill internalSkill = null;
			if (skill == null)
			{
				internalSkill = ResourceManager.Get<InternalSkill>(skillName);
			}
			if (skill != null)
			{
				if ((_filter != WuxueFilter.QUAN || skill.Type == 0) && (_filter != WuxueFilter.JIAN || skill.Type == 1) && (_filter != WuxueFilter.DAO || skill.Type == 2) && (_filter != WuxueFilter.QIMEN || skill.Type == 3) && _filter != WuxueFilter.NEIGONG)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(SkillSelectItemObj);
					gameObject.transform.Find("NameText").GetComponent<Text>().text = skillName;
					gameObject.transform.Find("IconImage").GetComponent<Image>().sprite = Resource.GetIcon(skill.icon);
					gameObject.transform.Find("LevelText").GetComponent<Text>().text = string.Format("{0}", maxLevel);
					gameObject.GetComponent<Button>().onClick.AddListener(delegate
					{
						SkillInstance skillInstance = new SkillInstance
						{
							name = skillName,
							level = maxLevel
						};
						skillInstance.RefreshUniquSkills();
						SkillDetailPanelObj.GetComponent<ItemSkillDetailPanelUI>().Show(skillInstance);
					});
					component.AddItem(gameObject);
				}
			}
			else if (internalSkill != null && (_filter == WuxueFilter.ALL || _filter == WuxueFilter.NEIGONG))
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(SkillSelectItemObj);
				gameObject2.transform.Find("NameText").GetComponent<Text>().text = skillName;
				gameObject2.transform.Find("IconImage").GetComponent<Image>().sprite = Resource.GetIcon(internalSkill.icon);
				gameObject2.transform.Find("LevelText").GetComponent<Text>().text = string.Format("{0}", maxLevel);
				gameObject2.GetComponent<Button>().onClick.AddListener(delegate
				{
					InternalSkillInstance internalSkillInstance = new InternalSkillInstance
					{
						name = skillName,
						level = maxLevel
					};
					internalSkillInstance.RefreshUniquSkills();
					SkillDetailPanelObj.GetComponent<ItemSkillDetailPanelUI>().Show(internalSkillInstance);
				});
				component.AddItem(gameObject2);
			}
		}
	}

	private void Refresh()
	{
		string text = LuaManager.Call<string>("GameEngine_jianghuContent", new object[1] { this });
		Text component = JianghuLilianTextObj.GetComponent<Text>();
		component.text = text;
		string text2 = string.Empty;
		int num = 0;
		int num2 = 0;
		foreach (Resource item in ResourceManager.GetAll<Resource>())
		{
			if (item.Key.StartsWith("nick."))
			{
				num++;
				string text3 = item.Key.Replace("nick.", string.Empty);
				string value = item.Value;
				if (ModData.Nicks.Contains(text3))
				{
					text2 += string.Format("<color='green'>{0}:{1}</color>\n", text3, value);
					num2++;
				}
				else
				{
					text2 += string.Format("<color='red'>{0}:（未获得）</color>\n", text3);
				}
			}
		}
		Text component2 = ChengjiuTextObj.GetComponent<Text>();
		component2.text = text2;
		double num3 = 0.0;
		if (num != 0)
		{
			num3 = (float)num2 / (float)num;
		}
		ChengjiuWanchengTextObj.GetComponent<Text>().text = string.Format("完成度：{0}%", Math.Round(num3 * 100.0, 2));
		RefreshWuxue();
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
