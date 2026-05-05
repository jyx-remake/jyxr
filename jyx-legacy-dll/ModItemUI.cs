using System.Collections.Generic;
using System.IO;
using System.Text;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class ModItemUI : MonoBehaviour
{
	public Image image;

	public Text text;

	public Text desc;

	public Text loadingText;

	public ModDownloadPanelUI downloadPanel;

	public Button startBtn;

	public Button downloadBtn;

	public Button deleteBtn;

	public ConfirmPanel confirmPanel;

	public ModInfo _mod;

	public ModSelectUI _parent;

	private int _currentIndex;

	private List<string> tobedownloadFiles = new List<string>();

	private bool _abortflag;

	private void Start()
	{
		Refresh();
	}

	private void Update()
	{
	}

	private void Refresh()
	{
	}

	public void Bind(ModInfo mod, ModSelectUI parent, bool local)
	{
		_mod = mod;
		_parent = parent;
		string empty = string.Empty;
		if (local)
		{
			startBtn.gameObject.SetActive(true);
			downloadBtn.gameObject.SetActive(false);
			deleteBtn.gameObject.SetActive(true);
			empty = "file:///" + mod.LocalDirPath + "poster.jpg";
		}
		else
		{
			startBtn.gameObject.SetActive(false);
			downloadBtn.gameObject.SetActive(true);
			deleteBtn.gameObject.SetActive(false);
			empty = mod.dir + "poster.jpg";
		}
		StartCoroutine(Tools.DownloadImage(empty, delegate(object sp)
		{
			image.sprite = sp as Sprite;
			loadingText.gameObject.SetActive(false);
		}, delegate
		{
		}, Vector2.zero, 1f));
		text.text = mod.name;
		if (string.IsNullOrEmpty(mod.name))
		{
			text.text = "官方原版";
		}
		else
		{
			text.text = mod.name;
		}
		desc.text = string.Format("作者:{0}  容量:{2}\n版本:{1}  发布时间:{3}\n简介:{4}", mod.author, mod.version, mod.size, mod.date, mod.desc);
	}

	public void OnLoad()
	{
		ModManager.SetCurrentMod(_mod);
		LoadingUI.Load("MainMenu");
	}

	public void OnDelete()
	{
		confirmPanel.Show("确认要删除吗？（该MOD存档也将被删除）", delegate
		{
			File.Delete(_mod.LocalXmlPath);
			Directory.Delete(_mod.LocalDirPath, true);
			_parent.OnLocalMod();
		});
	}

	public void OnDownload()
	{
		tobedownloadFiles.Clear();
		_currentIndex = 0;
		_abortflag = false;
		Tools.getFileContentFromUrl(this, _mod.dir + "index.xml", delegate(string content)
		{
			tobedownloadFiles.Add("index.xml");
			tobedownloadFiles.Add("poster.jpg");
			AssetBundleManager.ModResourcesData modResourcesData = Tools.DeserializeXML<AssetBundleManager.ModResourcesData>(content);
			AssetBundleManager.ModResourceData[] data = modResourcesData.data;
			foreach (AssetBundleManager.ModResourceData modResourceData in data)
			{
				if (!(modResourceData.url == string.Empty) && (Application.isMobilePlatform || !modResourceData.url.EndsWith(".mp3")) && (!Application.isMobilePlatform || !modResourceData.url.EndsWith(".ogg")))
				{
					string text = _mod.LocalDirPath + modResourceData.url;
					if (!File.Exists(text) || !modResourceData.hash.Equals(Tools.GetMD5HashFromFile(text)))
					{
						tobedownloadFiles.Add(modResourceData.url);
					}
				}
			}
			downloadPanel.Bind(this);
			DownloadNext(delegate
			{
				OnDownloadFinished();
			});
		}, delegate
		{
		});
	}

	private void OnDownloadFinished()
	{
		downloadPanel.DownloadFinished();
		if (_parent != null)
		{
			_parent.OnLocalMod();
		}
		using (StreamWriter streamWriter = new StreamWriter(_mod.LocalXmlPath, false, Encoding.UTF8))
		{
			streamWriter.WriteLine("<root>\n");
			streamWriter.Write(Tools.SerializeXML(_mod) + "\n");
			streamWriter.WriteLine("</root>");
		}
	}

	private void DownloadNext(CommonSettings.VoidCallBack callback)
	{
		if (_abortflag)
		{
			return;
		}
		if (_currentIndex >= tobedownloadFiles.Count)
		{
			callback();
			return;
		}
		string text = tobedownloadFiles[_currentIndex];
		if (!Application.isMobilePlatform && text.EndsWith(".mp3"))
		{
			text = text.Replace(".mp3", ".ogg");
		}
		_currentIndex++;
		string text2 = _mod.dir + text;
		string text3 = _mod.LocalDirPath + text;
		string directoryName = Path.GetDirectoryName(text3);
		if (!Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		Debug.Log("downloading " + text2 + " to " + text3);
		downloadPanel.Show((float)_currentIndex / (float)tobedownloadFiles.Count, string.Format("正在下载 {0}", text));
		StartCoroutine(Tools.Download(text2, text3, delegate
		{
			DownloadNext(callback);
		}, delegate
		{
		}));
	}

	public void AbortDownload()
	{
		_abortflag = true;
	}
}
