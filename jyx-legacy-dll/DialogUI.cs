using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class DialogUI : MonoBehaviour
{
	private CommonSettings.VoidCallBack _callback;

	private float _timeCount;

	public void Show(string roleKey, string msg, CommonSettings.VoidCallBack callback = null)
	{
		_callback = callback;
		StoryAction storyAction = new StoryAction();
		storyAction.type = "DIALOG";
		storyAction.value = roleKey + "#" + msg;
		Show(storyAction);
	}

	public void Show(StoryAction action, CommonSettings.VoidCallBack callback = null)
	{
		_callback = callback;
		_timeCount = 0f;
		base.gameObject.SetActive(true);
		string[] array = action.value.Split('#');
		string roleKey = array[0];
		string roleName = CommonSettings.getRoleName(roleKey);
		string text = array[1];
		base.transform.Find("NameText").GetComponent<Text>().text = roleName;
		text = text.Replace("$MALE$", RuntimeData.Instance.maleName).Replace("$FEMALE$", RuntimeData.Instance.femaleName).Replace("$ZHENLONG_LEVEL$", (ModData.ZhenlongqijuLevel + 1).ToString());
		text = text.Replace("[[red:", "<color='red'>").Replace("[[yellow:", "<color='yellow'>").Replace("]]", "</color>");
		base.transform.Find("ContentText").GetComponent<Text>().text = text;
		base.transform.Find("_mask").Find("HeadImage").GetComponent<Image>()
			.sprite = Resource.GetImage(CommonSettings.getRoleHead(roleKey));
	}

	public void OnClicked()
	{
		base.gameObject.SetActive(false);
		RuntimeData.Instance.mapUI.ExecuteNextStoryAction(_callback);
	}

	public void OnJump()
	{
		base.gameObject.SetActive(false);
		RuntimeData.Instance.mapUI.JumpDialogs(_callback);
	}

	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.Space))
		{
			_timeCount += Time.deltaTime;
			if (_timeCount > 0.3f)
			{
				OnClicked();
			}
		}
		else if (Input.GetKeyUp(KeyCode.Space))
		{
			OnClicked();
		}
		else if (Input.GetMouseButtonDown(0))
		{
			OnClicked();
		}
	}
}
