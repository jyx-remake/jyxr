using System.Collections;
using JyGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleSkillSelectItemUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler
{
	public SkillBox Skill;

	public GameObject ToolTipObj;

	public bool isOn
	{
		get
		{
			return base.transform.Find("Toggle").GetComponent<Toggle>().isOn;
		}
		set
		{
			base.transform.Find("Toggle").GetComponent<Toggle>().isOn = value;
		}
	}

	public void Bind(SkillBox skill, BattleSprite currentSprite)
	{
		Skill = skill;
		base.transform.Find("IconImage").GetComponent<Image>().sprite = Resource.GetIcon(skill.Icon);
		if (skill.SkillType == SkillType.Unique)
		{
			base.transform.Find("Text").GetComponent<Text>().color = (skill as UniqueSkillInstance)._parent.Color;
			base.transform.Find("Text").GetComponent<Text>().text = (skill as UniqueSkillInstance)._parent.Name;
			base.transform.Find("TextUnique").GetComponent<Text>().color = skill.Color;
			base.transform.Find("TextUnique").GetComponent<Text>().text = skill.Name.Replace(".", string.Empty).Replace((skill as UniqueSkillInstance)._parent.Name, string.Empty);
		}
		else
		{
			base.transform.Find("Text").GetComponent<Text>().color = skill.Color;
			base.transform.Find("Text").GetComponent<Text>().text = skill.Name;
			base.transform.Find("TextUnique").gameObject.SetActive(false);
		}
		base.transform.Find("Toggle").GetComponent<Toggle>().isOn = currentSprite.CurrentSkill == skill;
		if (skill.Status == SkillStatus.NoBalls || skill.Status == SkillStatus.NoMp || skill.Status == SkillStatus.Error)
		{
			base.transform.Find("IconImage").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
		}
		if (skill.Status == SkillStatus.NoCd && skill.CurrentCd != 0 && skill.Cd != 0)
		{
			base.transform.Find("IconImage").Find("CoolDownImage").GetComponent<Image>()
				.fillAmount = (float)skill.CurrentCd / (float)skill.Cd;
		}
		base.transform.Find("IconImage").Find("SealTag").gameObject.SetActive(skill.Status == SkillStatus.Seal);
	}

	public void OnPointerEnter(PointerEventData data)
	{
		RefreshToolTip();
		ToolTipObj.SetActive(true);
	}

	public void OnPointerExit(PointerEventData data)
	{
		ToolTipObj.SetActive(false);
	}

	public void OnPointerDown(PointerEventData data)
	{
		RefreshToolTip();
		StartCoroutine(ShowToolTip());
	}

	public IEnumerator ShowToolTip()
	{
		ToolTipObj.SetActive(true);
		yield return new WaitForSeconds(2f);
		ToolTipObj.SetActive(false);
		yield return 0;
	}

	private void RefreshToolTip()
	{
		base.transform.GetComponent<ToolTipUI>().TooltipObj.transform.Find("Text").GetComponent<Text>().text = Skill.Name + "\n" + Skill.DescriptionInRichtextBlackBg;
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
