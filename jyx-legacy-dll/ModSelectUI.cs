using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class ModSelectUI : MonoBehaviour
{
	public SelectMenu selectMenu;

	public ModItemUI modItem;

	public Text downloadingText;

	public GameObject gonggaoPanel;

	public Text gonggaoText;

	public Text versionText;

	public Button closeBtn;

	private bool isLocal;

	private void Start()
	{
		Refresh();
		OnGonggao();
		closeBtn.gameObject.SetActive(AssetBundleManager.IsInited);
	}

	private void Update()
	{
	}

	public List<ModInfo> LoadXml(string xml)
	{
		List<ModInfo> list = new List<ModInfo>();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		foreach (XmlNode item2 in xmlDocument.SelectNodes("root/mod"))
		{
			ModInfo item = BasePojo.Create<ModInfo>(item2.OuterXml);
			list.Add(item);
		}
		foreach (XmlNode item3 in xmlDocument.SelectNodes("root/gonggao"))
		{
			GonggaoInfo gonggaoInfo = BasePojo.Create<GonggaoInfo>(item3.OuterXml);
			gonggaoText.text = gonggaoInfo.text;
		}
		return list;
	}

	public ModInfo LoadSingleModInfo(string xml)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		IEnumerator enumerator = xmlDocument.SelectNodes("root/mod").GetEnumerator();
		try
		{
			if (enumerator.MoveNext())
			{
				XmlNode xmlNode = (XmlNode)enumerator.Current;
				return BasePojo.Create<ModInfo>(xmlNode.OuterXml);
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
		return null;
	}

	public void NoMod()
	{
		ModManager.SetCurrentMod(null);
		LoadingUI.Load("MainMenu");
	}

	public void Refresh()
	{
		isLocal = false;
		versionText.text = string.Format("客户端版本:V1.1.0.6");
		downloadingText.gameObject.SetActive(true);
		downloadingText.text = "从服务器获取MOD列表中...";
		Tools.getFileContentFromUrl(this, "http://www.hanjiasongshu.com/jygamemod/list.xml", delegate(string content)
		{
			downloadingText.gameObject.SetActive(false);
			List<ModInfo> list = (ModManager.mods = LoadXml(content));
			if (!isLocal)
			{
				selectMenu.Clear();
				foreach (ModInfo item in list)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(modItem.gameObject);
					gameObject.GetComponent<ModItemUI>().Bind(item, this, false);
					if (File.Exists(item.LocalXmlPath))
					{
						gameObject.GetComponent<ModItemUI>().downloadBtn.transform.Find("Text").GetComponent<Text>().text = "更新";
						gameObject.GetComponent<ModItemUI>().downloadBtn.GetComponent<Image>().color = Color.green;
					}
					selectMenu.AddItem(gameObject);
				}
			}
		}, delegate
		{
			downloadingText.text = "获取失败，请检查网络";
		});
	}

	public void RefreshLocal()
	{
		isLocal = true;
		selectMenu.Clear();
		string path = CommonSettings.persistentDataPath + "/modcache/";
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		FileInfo[] files = new DirectoryInfo(path).GetFiles("*.xml");
		foreach (FileInfo fileInfo in files)
		{
			using (StreamReader streamReader = new StreamReader(fileInfo.FullName))
			{
				ModInfo modInfo = LoadSingleModInfo(streamReader.ReadToEnd());
				if (modInfo != null)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(modItem.gameObject);
					gameObject.GetComponent<ModItemUI>().Bind(modInfo, this, true);
					selectMenu.AddItem(gameObject);
				}
			}
		}
	}

	public void OnLocalMod()
	{
		selectMenu.gameObject.SetActive(true);
		gonggaoPanel.SetActive(false);
		RefreshLocal();
	}

	public void OnOnlineMod()
	{
		selectMenu.gameObject.SetActive(true);
		gonggaoPanel.SetActive(false);
		Refresh();
	}

	public void OnGonggao()
	{
		selectMenu.gameObject.SetActive(false);
		gonggaoPanel.SetActive(true);
	}

	public void OnClose()
	{
		LoadingUI.Load("MainMenu");
	}
}
