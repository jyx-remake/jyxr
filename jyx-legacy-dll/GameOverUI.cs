using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
	public GameObject headImageObj;

	public GameObject deadTextObj;

	public void MenuClicked()
	{
		int param = ModData.GetParam("last_save_index");
		if (param == 0)
		{
			Application.LoadLevel("MainMenu");
			return;
		}
		string empty = string.Empty;
		empty = ((!Configer.IsAutoSave) ? string.Format("save{0}", param - 1) : "autosave");
		if (empty != string.Empty)
		{
			string save = SaveManager.GetSave(empty);
			RuntimeData.Instance.Load(save);
			RuntimeData.Instance.gameEngine.SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
		}
	}

	private void Start()
	{
		ModData.ParamAdd("dead", 1);
		AudioManager.Instance.Play("音乐.游戏失败");
		headImageObj.GetComponent<Image>().sprite = Resource.GetZhujueHead();
		deadTextObj.GetComponent<Text>().text = string.Format("您在这个世界已经累计死亡了{0}次", ModData.GetParam("dead"));
	}

	private void Update()
	{
	}
}
