using System.Collections;
using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
	public static string LoadingLevel = "MainMenu";

	public GameObject ProgressSliderObj;

	public GameObject ProgressTextObj;

	public GameObject DetailTextObj;

	public GameObject BackgroundObj;

	public GameObject SuggestTipObj;

	public GameObject JumpButtonObj;

	private static List<string> _randomMaps = new List<string>();

	private static List<string> _suggestTips = new List<string>();

	public static bool IsResourceLoaded = false;

	private AsyncOperation async;

	private bool loadingAssetBundle;

	public Slider ProgressSlider
	{
		get
		{
			return ProgressSliderObj.GetComponent<Slider>();
		}
	}

	public Text ProgressText
	{
		get
		{
			return ProgressTextObj.GetComponent<Text>();
		}
	}

	public Text DetailText
	{
		get
		{
			return DetailTextObj.GetComponent<Text>();
		}
	}

	public static void Load(string sceneName)
	{
		LoadingLevel = sceneName;
		Application.LoadLevel("Loading");
	}

	private void Start()
	{
		UserDefinedAnimationManager.instance._parent = this;
		ShowTipsAndBg();
		if (LoadingLevel == "Battle")
		{
			Battle battleSelectRole_GeneratedBattle = RuntimeData.Instance.gameEngine.BattleSelectRole_GeneratedBattle;
			StartCoroutine(ResourcePool.Load(battleSelectRole_GeneratedBattle, delegate
			{
				StartCoroutine(LoadScene());
			}));
		}
		else if (LoadingLevel == "MainMenu")
		{
			if (!AssetBundleManager.IsInited)
			{
				LoadAssetBundles(delegate
				{
					if (!IsResourceLoaded)
					{
						loadingAssetBundle = false;
						ResourceManager.LoadResource<Resource>("resource_suggesttips.xml", "root/resource");
						ResourceManager.LoadResource<Resource>("resource.xml", "root/resource");
						IsResourceLoaded = true;
						if (ResourceManager.GetAll<Resource>() != null)
						{
							foreach (Resource item in ResourceManager.GetAll<Resource>())
							{
								if (item.Key.StartsWith("小贴士."))
								{
									_suggestTips.Add(item.Value);
								}
								if (item.Key.StartsWith("地图."))
								{
									_randomMaps.Add(item.Key);
								}
							}
						}
					}
					StartCoroutine(ResourceManager.Init2(delegate
					{
						StartCoroutine(LoadScene());
					}));
					ShowTipsAndBg();
				});
			}
			else
			{
				StartCoroutine(LoadSceneWithProgress());
			}
		}
		else
		{
			StartCoroutine(LoadSceneWithProgress());
		}
	}

	private void ShowTipsAndBg()
	{
		if (_randomMaps.Count > 0)
		{
			BackgroundObj.GetComponent<Image>().sprite = Resource.GetImage(_randomMaps[Tools.GetRandomInt(0, _randomMaps.Count - 1)]);
		}
		if (_suggestTips.Count > 0)
		{
			SuggestTipObj.GetComponent<Text>().text = _suggestTips[Tools.GetRandomInt(0, _suggestTips.Count - 1)];
			SuggestTipObj.SetActive(true);
		}
		else
		{
			SuggestTipObj.SetActive(false);
		}
	}

	private IEnumerator LoadScene()
	{
		async = Application.LoadLevelAsync(LoadingLevel);
		yield return async;
	}

	private IEnumerator LoadSceneWithProgress()
	{
		DetailText.text = "正在努力为您加载..";
		AsyncOperation op = Application.LoadLevelAsync(LoadingLevel);
		while (!op.isDone)
		{
			ProgressSlider.value = op.progress;
			ProgressText.text = string.Format("{0:F0}%", op.progress * 100f);
			yield return 0;
		}
	}

	private void LoadAssetBundles(CommonSettings.VoidCallBack callback)
	{
		if (!CommonSettings.MOD_MODE)
		{
			loadingAssetBundle = true;
		}
		AssetBundleManager.Init(this, callback);
	}

	private void Update()
	{
		float num = 0f;
		if (loadingAssetBundle)
		{
			if (AssetBundleManager.IsFailed)
			{
				DetailText.text = "<color='red'>加载资源包错误，请检查网络...</color>";
				return;
			}
			DetailText.text = "正在载入" + AssetBundleManager.CurrentLoadingAssetsInfo + "资源包...（首次进入游戏可能比较慢，请耐心等待）";
			num = AssetBundleManager.CurrentWWW.progress;
			ProgressText.text = string.Format("{0:F0}%", num * 100f);
			ProgressSlider.value = num;
		}
		else if (LoadingLevel == "MainMenu")
		{
			num = ResourceManager.progress;
			DetailText.text = ResourceManager.detail;
			ProgressText.text = string.Format("{0:F0}%", num * 100f);
			ProgressSlider.value = num;
		}
		else if (LoadingLevel == "Battle")
		{
			num = ResourcePool.GetLoadProgress();
			DetailText.text = "正在载入战斗...";
			ProgressText.text = string.Format("{0:F0}%", num * 100f);
			ProgressSlider.value = num;
		}
	}

	public void Show(float progress, string msg)
	{
		DetailText.text = msg;
		ProgressText.text = string.Format("{0:F0}%", progress * 100f);
		ProgressSlider.value = progress;
	}
}
