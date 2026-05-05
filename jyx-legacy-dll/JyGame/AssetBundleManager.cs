using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	public class AssetBundleManager
	{
		[XmlType("root")]
		public class ModResourcesData
		{
			[XmlElement("data")]
			public ModResourceData[] data;
		}

		[XmlType]
		public class ModResourceData
		{
			[XmlAttribute]
			public string url;

			[XmlAttribute]
			public string hash;

			[XmlAttribute]
			public int w;

			[XmlAttribute]
			public int h;
		}

		public static AssetBundle MapAssets = null;

		public static AssetBundle BattleAssets = null;

		public static AssetBundle AudioAssets = null;

		public static AssetBundle XmlAssets = null;

		public static AssetBundle CGAssets = null;

		public static WWW CurrentWWW = null;

		public static string CurrentLoadingAssetsInfo = string.Empty;

		public static bool IsInited = false;

		public static bool IsFailed = false;

		public static ModResourcesData data;

		private static MonoBehaviour _parent;

		private static CommonSettings.VoidCallBack _callback;

		private static List<FileInfo> _tobeLoadResources = new List<FileInfo>();

		private static List<string> _tobeLoadFiles = new List<string>();

		private static string[] avaliableExtensions = new string[5] { ".jpg", ".png", ".xml", ".ogg", ".mp3" };

		private static int _currentLoadResource = 0;

		public static string baseUrl
		{
			get
			{
				if (Application.platform == RuntimePlatform.Android)
				{
					return "jar:file://" + Application.dataPath + "!/assets/";
				}
				if (Application.platform == RuntimePlatform.IPhonePlayer)
				{
					return "file://" + Application.dataPath + "/Raw/";
				}
				if (Application.platform == RuntimePlatform.WindowsPlayer || Application.isEditor)
				{
					return "file://" + Application.dataPath + "/StreamingAssets/";
				}
				if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
					return "StreamingAssets/";
				}
				return "file://" + Application.dataPath + "/StreamingAssets/";
			}
		}

		public static void Init(MonoBehaviour parent, CommonSettings.VoidCallBack callback)
		{
			_parent = parent;
			_callback = callback;
			Clear();
			if (CommonSettings.MOD_MODE)
			{
				LoadUserDefinedAnimations(delegate
				{
					LoadModResources(delegate
					{
						ResourceManager.detail = "正在载入本地资源..";
						LoadAssetBundles();
					});
				});
			}
			else
			{
				LoadAssetBundles();
			}
		}

		private static void LoadAssetBundles()
		{
			MonoBehaviour parent = _parent;
			CommonSettings.VoidCallBack callback = _callback;
			parent.StartCoroutine(InitAssetBundle("maps", "地图", delegate
			{
				parent.StartCoroutine(InitAssetBundle("battlebg", "战斗场景", delegate
				{
					parent.StartCoroutine(InitAssetBundle("audios", "音乐音效", delegate
					{
						parent.StartCoroutine(InitAssetBundle("cgs", "CG", delegate
						{
							parent.StartCoroutine(InitAssetBundle("xml", "脚本", delegate
							{
								IsInited = true;
								callback();
							}, delegate
							{
								IsFailed = true;
							}));
						}));
					}));
				}));
			}));
		}

		private static void Clear()
		{
			_tobeLoadResources.Clear();
			_tobeLoadFiles.Clear();
			_currentLoadResource = 0;
			ModEditorResourceManager.Clear();
			if (MapAssets != null)
			{
				MapAssets.Unload(false);
			}
			if (BattleAssets != null)
			{
				BattleAssets.Unload(false);
			}
			if (AudioAssets != null)
			{
				AudioAssets.Unload(false);
			}
			if (XmlAssets != null)
			{
				XmlAssets.Unload(false);
			}
			if (CGAssets != null)
			{
				CGAssets.Unload(false);
			}
		}

		public static void LoadModResources(CommonSettings.VoidCallBack callback)
		{
			string url = ModManager.ModBaseUrl + "/index.xml";
			Tools.getFileContentFromUrl(_parent, url, delegate(string str)
			{
				ModResourcesData modResourcesData = Tools.DeserializeXML<ModResourcesData>(str);
				if (modResourcesData != null)
				{
					data = modResourcesData;
				}
				ModResourceData[] array = data.data;
				foreach (ModResourceData modResourceData in array)
				{
					if (!modResourceData.url.StartsWith("Animations/"))
					{
						_tobeLoadFiles.Add(modResourceData.url);
					}
				}
				LoadNextModResource(callback);
			});
		}

		public static void LoadUserDefinedAnimations(CommonSettings.VoidCallBack callback)
		{
			ResourceManager.progress = 0f;
			ResourceManager.detail = "正在载入MOD定义动画...";
			UserDefinedAnimationManager.instance.Init(callback);
		}

		public static void LoadNextModResource(CommonSettings.VoidCallBack callback)
		{
			if (_currentLoadResource >= _tobeLoadFiles.Count)
			{
				callback();
				return;
			}
			string text = _tobeLoadFiles[_currentLoadResource];
			if (_tobeLoadFiles.Count > 0)
			{
				ResourceManager.progress = (float)_currentLoadResource / (float)_tobeLoadFiles.Count;
				ResourceManager.detail = "正在载入MOD资源 " + text;
			}
			if (text.EndsWith(".png") || text.EndsWith(".jpg"))
			{
				ModEditorResourceManager.AddSprite(text.Split('.')[0], text);
				_currentLoadResource++;
				LoadNextModResource(callback);
			}
			else if (text.EndsWith(".ogg") || text.EndsWith(".mp3"))
			{
				if (!Application.isMobilePlatform)
				{
					text = text.Replace(".mp3", ".ogg");
				}
				ModEditorResourceManager.AddAudio(text.Split('.')[0], text);
				_currentLoadResource++;
				LoadNextModResource(callback);
			}
			else if (text.EndsWith(".xml"))
			{
				_parent.StartCoroutine(ModEditorResourceManager.LoadXml(_parent.GetComponent<LoadingUI>(), text, delegate
				{
					LoadNextModResource(callback);
				}));
				_currentLoadResource++;
			}
			else
			{
				_currentLoadResource++;
				LoadNextModResource(callback);
			}
		}

		private static IEnumerator InitAssetBundle(string name, string info, CommonSettings.VoidCallBack callback, CommonSettings.VoidCallBack failCallback = null)
		{
			string path = Path.Combine(baseUrl, name);
			bool flag = name == "xml";
			Debug.Log("downloading " + path);
			WWW www = (CurrentWWW = WWW.LoadFromCacheOrDownload(path, 0));
			CurrentLoadingAssetsInfo = info;
			yield return www;
			if (!string.IsNullOrEmpty(www.error))
			{
				Debug.LogError(www.error);
				if (failCallback != null)
				{
					failCallback();
				}
				yield break;
			}
			switch (name)
			{
			case "maps":
				MapAssets = www.assetBundle;
				break;
			case "battlebg":
				BattleAssets = www.assetBundle;
				break;
			case "audios":
				AudioAssets = www.assetBundle;
				break;
			case "xml":
				XmlAssets = www.assetBundle;
				break;
			case "cgs":
				CGAssets = www.assetBundle;
				break;
			}
			if (callback != null)
			{
				callback();
			}
		}

		public static Sprite GetMap(string url)
		{
			if (MapAssets != null)
			{
				return MapAssets.LoadAsset<Sprite>(url);
			}
			return null;
		}

		public static Sprite GetBattleBg(string url)
		{
			if (BattleAssets != null)
			{
				return BattleAssets.LoadAsset<Sprite>(url);
			}
			return null;
		}

		public static AudioClip GetAudio(string url)
		{
			if (AudioAssets != null)
			{
				return AudioAssets.LoadAsset<AudioClip>(url);
			}
			return null;
		}

		public static Sprite GetCG(string url)
		{
			if (CGAssets != null)
			{
				return CGAssets.LoadAsset<Sprite>(url);
			}
			return null;
		}

		public static string GetXml(string url)
		{
			if (XmlAssets != null)
			{
				return (XmlAssets.LoadAsset(url) as TextAsset).text;
			}
			return null;
		}
	}
}
