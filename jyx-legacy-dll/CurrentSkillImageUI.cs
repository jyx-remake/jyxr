using JyGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CurrentSkillImageUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerDownHandler
{
	private SkillBox _currentSkill;

	public void Bind(SkillBox skill)
	{
		_currentSkill = skill;
		GetComponent<Image>().sprite = skill.IconSprite;
		if (skill.SkillType == SkillType.Unique)
		{
			base.transform.Find("Text").GetComponent<Text>().color = (skill as UniqueSkillInstance)._parent.Color;
			base.transform.Find("Text").GetComponent<Text>().text = (skill as UniqueSkillInstance)._parent.Name;
			base.transform.Find("TextUnique").GetComponent<Text>().color = skill.Color;
			base.transform.Find("TextUnique").GetComponent<Text>().text = skill.Name.Replace(".", string.Empty).Replace((skill as UniqueSkillInstance)._parent.Name, string.Empty);
			base.transform.Find("TextUnique").gameObject.SetActive(true);
		}
		else
		{
			base.transform.Find("Text").GetComponent<Text>().color = skill.Color;
			base.transform.Find("Text").GetComponent<Text>().text = skill.Name;
			base.transform.Find("TextUnique").gameObject.SetActive(false);
		}
	}

	public void OnPointerEnter(PointerEventData data)
	{
		RefreshToolTip();
	}

	public void OnPointerDown(PointerEventData data)
	{
		RefreshToolTip();
	}

	private void RefreshToolTip()
	{
		if (_currentSkill != null)
		{
			base.transform.GetComponent<ToolTipUI>().TooltipObj.transform.Find("Text").GetComponent<Text>().text = _currentSkill.Name + "\n" + _currentSkill.DescriptionInRichtextBlackBg;
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
