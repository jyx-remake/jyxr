using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class SystemPanelUI : MonoBehaviour
{
	public GameObject SavePanelObj;

	public GameObject consolePanelObj;

	public GameObject confirmPanelObj;

	private SavePanelUI SavePanel
	{
		get
		{
			return SavePanelObj.GetComponent<SavePanelUI>();
		}
	}

	public void CancelButtonClicked()
	{
		base.gameObject.SetActive(false);
	}

	public void Save()
	{
		base.gameObject.SetActive(false);
		SavePanel.Show(SavePanelMode.SAVE);
	}

	public void Load()
	{
		base.gameObject.SetActive(false);
		SavePanel.Show(SavePanelMode.LOAD);
	}

	public void BackToMenu()
	{
		confirmPanelObj.GetComponent<ConfirmPanel>().Show("提示，若当前没有存档将丢失目前进度，确认吗？", delegate
		{
			Application.LoadLevel("MainMenu");
		});
	}

	public void OnAutoBattle()
	{
		bool isOn = base.transform.Find("Toggles").Find("AutoBattleToggle").GetComponent<Toggle>()
			.isOn;
		Configer.IsAutoBattle = isOn;
	}

	public void OnAutoSave()
	{
		bool isOn = base.transform.Find("Toggles").Find("AutoSaveToggle").GetComponent<Toggle>()
			.isOn;
		Configer.IsAutoSave = isOn;
	}

	public void OnMusic()
	{
		bool isOn = base.transform.Find("Toggles").Find("MusicToggle").GetComponent<Toggle>()
			.isOn;
		Configer.IsMusicOn = isOn;
	}

	public void OnEffect()
	{
		bool isOn = base.transform.Find("Toggles").Find("EffectToggle").GetComponent<Toggle>()
			.isOn;
		Configer.IsEffectOn = isOn;
	}

	public void OnScaleBigMap()
	{
		bool isOn = base.transform.Find("Toggles").Find("ScaleBigMapToggle").GetComponent<Toggle>()
			.isOn;
		Configer.IsBigmapFullScreen = isOn;
	}

	public void OnBattleTip()
	{
		bool isOn = base.transform.Find("Toggles").Find("BattleTipToggle").GetComponent<Toggle>()
			.isOn;
		Configer.IsBattleTipShow = isOn;
	}

	private void Refresh()
	{
		base.transform.Find("Toggles").Find("AutoBattleToggle").GetComponent<Toggle>()
			.isOn = Configer.IsAutoBattle;
		base.transform.Find("Toggles").Find("MusicToggle").GetComponent<Toggle>()
			.isOn = Configer.IsMusicOn;
		base.transform.Find("Toggles").Find("EffectToggle").GetComponent<Toggle>()
			.isOn = Configer.IsEffectOn;
		base.transform.Find("Toggles").Find("AutoSaveToggle").GetComponent<Toggle>()
			.isOn = Configer.IsAutoSave;
		base.transform.Find("Toggles").Find("ScaleBigMapToggle").GetComponent<Toggle>()
			.isOn = Configer.IsBigmapFullScreen;
		base.transform.Find("Toggles").Find("BattleTipToggle").GetComponent<Toggle>()
			.isOn = Configer.IsBattleTipShow;
	}

	public void Show()
	{
		Refresh();
		base.gameObject.SetActive(true);
	}

	private void Start()
	{
		consolePanelObj.gameObject.SetActive(CommonSettings.DEBUG_CONSOLE);
		base.transform.Find("_hotKeySuggestText").gameObject.SetActive(!CommonSettings.TOUCH_MODE);
		base.transform.Find("Toggles").Find("ScaleBigMapToggle").gameObject.SetActive(CommonSettings.TOUCH_MODE);
	}

	private void Update()
	{
	}
}
