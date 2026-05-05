using System.Collections;
using JyGame;
using Umeng;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	public GameObject savePanelObj;

	public GameObject musicPanelObj;

	public GameObject logoObj;

	public GameObject infoTextObj;

	public GameObject versionTextObj;

	public Text modText;

	public GameObject confirmButtonObj;

	public GameObject uiCanvas;

	public GameObject versionCheckCanvas;

	public GameObject buttonShareObj;

	public GameObject messageBoxObj;

	public GameObject dotImageObj;

	public GameObject shareTextObj;

	public GameObject copyRightTextObj;

	public Image backgroundImage;

	private static bool startFlag;

	public GameObject ClearAllConfirmPanelObj;

	private int _clearAllCount;

	private static bool _mod_editor_checked;

	public static bool Touched;

	public GameObject sharePanelObj;

	public void OnNewGame()
	{
		RuntimeData.Instance.Init();
		LoadingUI.Load("RollRole");
	}

	public void OnLoad()
	{
		savePanelObj.GetComponent<SavePanelUI>().Show(SavePanelMode.LOAD);
	}

	public void OnMusic()
	{
		musicPanelObj.GetComponent<MusicPanelUI>().Show();
	}

	public void OnDeveloper()
	{
		RuntimeData.Instance.gameEngine.SwitchGameScene("story", "aboutus_main");
	}

	public void OnMod()
	{
		Application.LoadLevel("ModSelect");
	}

	public void OnGameFinButton()
	{
		Application.LoadLevel("MainMenu");
	}

	public void OnDebugClearAll()
	{
		_clearAllCount++;
		if (_clearAllCount > 10)
		{
			ClearAllConfirmPanelObj.SetActive(true);
		}
	}

	public void OnConfirmClear()
	{
		ModData.ClearAll();
		ClearAllConfirmPanelObj.SetActive(false);
	}

	public void OnCancelClear()
	{
		ClearAllConfirmPanelObj.SetActive(false);
		_clearAllCount = 0;
	}

	public void OnIOSPinglunClicked()
	{
		Tools.openURL("itms-apps://ax.itunes.apple.com/WebObjects/MZStore.woa/wa/viewContentsUserReviews?type=Purple+Software&id=1021093037");
	}

	private void Start()
	{
		LuaManager.Reload();
		if (CommonSettings.MOD_MODE)
		{
			if (!_mod_editor_checked && CommonSettings.CHECK_SCRIPT_RESOURCE)
			{
				ResourceChecker.CheckAll(new FileLogger());
				_mod_editor_checked = true;
			}
			copyRightTextObj.GetComponent<Text>().text = "版权声明：金庸群侠传X（MOD版）所有非官方发布的MOD，MOD作者负有全部责任。涉及的游戏内容、素材、发布行为，均与汉家松鼠工作室无关。";
			modText.text = string.Format("MOD版本:{0}(V{1})", ModData.CurrentMod.name, ModData.CurrentMod.version);
		}
		else
		{
			modText.text = string.Empty;
		}
		GameObject.Find("ButtonIOSPinglun").SetActive(false);
		shareTextObj.GetComponent<Text>().text = "分享/下载";
		if (!startFlag)
		{
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				Analytics.StartWithAppKeyAndChannelId("5598d48c67e58e429f0015f4", "IPhone");
			}
			else if (Application.platform == RuntimePlatform.Android)
			{
				Analytics.StartWithAppKeyAndChannelId("5593b1ab67e58e880a000d12", "Android");
			}
			startFlag = true;
		}
		uiCanvas.gameObject.SetActive(false);
		versionCheckCanvas.gameObject.SetActive(true);
		confirmButtonObj.gameObject.SetActive(false);
		versionTextObj.GetComponent<Text>().text = string.Format("客户端版本: V{0}", "1.1.0.6");
		dotImageObj.SetActive(false);
		ShowMainMenu();
	}

	private void ShowMainMenu()
	{
		uiCanvas.gameObject.SetActive(true);
		versionCheckCanvas.gameObject.SetActive(false);
		if (!RuntimeData.Instance.IsInited)
		{
			RuntimeData.Instance.Init();
		}
		string config = LuaManager.GetConfig("MAINMENU_BG");
		if (!string.IsNullOrEmpty(config) && Resource.GetImage(config) != null)
		{
			backgroundImage.sprite = Resource.GetImage(config);
		}
		string config2 = LuaManager.GetConfig("MAINMENU_MUSIC");
		AudioManager.Instance.Play(config2);
	}

	private void JudgeVersion(string versionInfo)
	{
		if (versionInfo.Contains("1.1.0.6"))
		{
			ShowMainMenu();
		}
		else
		{
			infoTextObj.GetComponent<Text>().text = "当前版本已过期，请更新版本。\n安卓设备需要重新下载并安装APK，网页版需要刷新页面。（存档不会删除）\n详情请访问游戏官网 <color='red'>http://www.jy-x.com</color>";
		}
	}

	private IEnumerator TouchServer(CommonSettings.StringCallBack callback)
	{
		string uuid = SystemInfo.operatingSystem + "_" + SystemInfo.deviceUniqueIdentifier + "_" + SystemInfo.deviceName + "_1.1.0.6";
		uuid = uuid.Replace(" ", string.Empty);
		string url = "http://www.jy-x.com/jygame/touchserver/touch.php?id=" + WWW.EscapeURL(uuid);
		Debug.Log(url);
		WWW www = new WWW(url);
		Touched = true;
		yield return www;
		if (string.IsNullOrEmpty(www.error))
		{
			callback(www.text);
			www.Dispose();
		}
		else
		{
			infoTextObj.GetComponent<Text>().text = "网络连接出错，点击重试";
			confirmButtonObj.gameObject.SetActive(true);
			StartCoroutine(TouchServer(JudgeVersion));
		}
	}

	public void ErrorConfirmButtonClicked()
	{
		confirmButtonObj.gameObject.SetActive(false);
		infoTextObj.GetComponent<Text>().text = "正在校验版本，请稍后....";
	}

	public void OnFourmClicked()
	{
		Tools.openURL("http://tieba.baidu.com/f?ie=utf-8&kw=%E6%B1%89%E5%AE%B6%E6%9D%BE%E9%BC%A0");
	}

	public void OnHomepageClicked()
	{
		Tools.openURL("http://www.jy-x.com");
	}

	private void Update()
	{
	}

	public void Share()
	{
		sharePanelObj.SetActive(true);
	}

	public void CloseSharePanel()
	{
		sharePanelObj.SetActive(false);
	}
}
