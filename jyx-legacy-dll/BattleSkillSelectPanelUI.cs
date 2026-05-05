using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class BattleSkillSelectPanelUI : MonoBehaviour
{
	public GameObject ParentBattleField;

	public GameObject CurrentSkillImage;

	public Transform selectContent
	{
		get
		{
			return base.transform.Find("SelectPanel").Find("SelectContent").transform;
		}
	}

	private ScrollRect scrollRect
	{
		get
		{
			return base.transform.Find("SelectPanel").GetComponent<ScrollRect>();
		}
	}

	public void Clear()
	{
		foreach (Transform item in selectContent)
		{
			Object.Destroy(item.gameObject);
		}
		selectContent.DetachChildren();
	}

	public void AddItem(GameObject item)
	{
		item.transform.SetParent(selectContent);
	}

	public void SetCurrent(SkillBox cs)
	{
		foreach (Transform item in selectContent)
		{
			BattleSkillSelectItemUI component = item.GetComponent<BattleSkillSelectItemUI>();
			if (component != null)
			{
				if (component.isOn && component.Skill != cs)
				{
					component.isOn = false;
				}
				if (!component.isOn && component.Skill == cs)
				{
					component.isOn = true;
				}
			}
		}
		CurrentSkillImage.GetComponent<CurrentSkillImageUI>().Bind(cs);
		ParentBattleField.GetComponent<BattleField>().ShowCurrentAttackRange();
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
