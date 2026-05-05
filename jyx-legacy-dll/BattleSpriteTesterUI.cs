using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class BattleSpriteTesterUI : MonoBehaviour
{
	public GameObject SpeedSlider;

	public void Stand()
	{
		Animator[] array = Object.FindObjectsOfType<Animator>();
		Animator[] array2 = array;
		foreach (Animator animator in array2)
		{
			animator.Play("stand");
		}
		UserDefinedAnimation[] array3 = Object.FindObjectsOfType<UserDefinedAnimation>();
		UserDefinedAnimation[] array4 = array3;
		foreach (UserDefinedAnimation userDefinedAnimation in array4)
		{
			userDefinedAnimation.Play("stand");
		}
	}

	public void Move()
	{
		Animator[] array = Object.FindObjectsOfType<Animator>();
		Animator[] array2 = array;
		foreach (Animator animator in array2)
		{
			animator.Play("move");
		}
		UserDefinedAnimation[] array3 = Object.FindObjectsOfType<UserDefinedAnimation>();
		UserDefinedAnimation[] array4 = array3;
		foreach (UserDefinedAnimation userDefinedAnimation in array4)
		{
			userDefinedAnimation.Play("move");
		}
	}

	public void Attack()
	{
		Animator[] array = Object.FindObjectsOfType<Animator>();
		Animator[] array2 = array;
		foreach (Animator animator in array2)
		{
			animator.Play("attack");
		}
		UserDefinedAnimation[] array3 = Object.FindObjectsOfType<UserDefinedAnimation>();
		UserDefinedAnimation[] array4 = array3;
		foreach (UserDefinedAnimation userDefinedAnimation in array4)
		{
			userDefinedAnimation.Play("attack");
		}
	}

	public void BeAttack()
	{
		Animator[] array = Object.FindObjectsOfType<Animator>();
		Animator[] array2 = array;
		foreach (Animator animator in array2)
		{
			animator.Play("be");
		}
		UserDefinedAnimation[] array3 = Object.FindObjectsOfType<UserDefinedAnimation>();
		UserDefinedAnimation[] array4 = array3;
		foreach (UserDefinedAnimation userDefinedAnimation in array4)
		{
			userDefinedAnimation.Play("be");
		}
	}

	public int GetStateNumberFromPrefab(Animator animator)
	{
		return 4;
	}

	public void OnSpeedChange()
	{
		float value = SpeedSlider.GetComponent<Slider>().value;
		Time.timeScale = value;
	}

	public void TestUserDefinedAnimations()
	{
		UserDefinedAnimationManager.instance._parent = this;
		UserDefinedAnimationManager.instance.Init(delegate
		{
			GameObject gameObject = UserDefinedAnimationManager.instance.GenerateObject("caoyuan");
			gameObject.name = "user_defined_obj";
			gameObject.transform.SetParent(base.transform);
			GameObject gameObject2 = UserDefinedAnimationManager.instance.GenerateObject("test", "effect");
			SkillAnimation skillAnimation = gameObject2.AddComponent<SkillAnimation>();
			skillAnimation.SetCallback(delegate
			{
				Debug.Log("on effect callback");
			});
			gameObject2.GetComponent<UserDefinedAnimation>().Play("effect", delegate
			{
				Debug.Log("on effect callback2");
			});
			skillAnimation.DisplayEffectNotFollowSprite();
			gameObject2.transform.SetParent(base.transform);
		});
	}

	private void Start()
	{
		if (CommonSettings.MOD_MODE)
		{
			TestUserDefinedAnimations();
		}
	}

	private void Update()
	{
	}
}
