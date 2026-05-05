using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	[XmlType("resource")]
	public class Resource : BasePojo
	{
		[XmlAttribute("key")]
		public string Key;

		[XmlAttribute("value")]
		public string Value;

		[XmlAttribute("icon")]
		public string Icon;

		private static List<string> suggestTips = new List<string>();

		private static bool suggestTipInited = false;

		public override string PK
		{
			get
			{
				return Key;
			}
		}

		public static Sprite GetZhujueHead()
		{
			return GetImage(RuntimeData.Instance.Team[0].Head);
		}

		public static string GetRandomSuggestTip()
		{
			if (!suggestTipInited)
			{
				foreach (Resource item in ResourceManager.GetAll<Resource>())
				{
					if (item.Key.StartsWith("小贴士"))
					{
						suggestTips.Add(item.Value);
					}
				}
				suggestTipInited = true;
			}
			if (suggestTips.Count == 0)
			{
				suggestTipInited = false;
				return string.Empty;
			}
			return suggestTips[Tools.GetRandomInt(0, suggestTips.Count - 1)];
		}

		public static Sprite GetIcon(string iconName)
		{
			if (CommonSettings.MOD_MODE)
			{
				Sprite sprite = ModEditorResourceManager.GetSprite("Icons/" + iconName);
				if (sprite != null)
				{
					return sprite;
				}
			}
			return Resources.Load<Sprite>("Icons/" + iconName);
		}

		public static Sprite GetBattleBg(string key)
		{
			if (CommonSettings.MOD_MODE)
			{
				Sprite sprite = ModEditorResourceManager.GetSprite("BattleBg/" + key);
				if (sprite != null)
				{
					return sprite;
				}
			}
			return AssetBundleManager.GetBattleBg(key);
		}

		public static Sprite GetImage(string key, bool forceLoadFromResource = false)
		{
			Resource resource = ResourceManager.Get<Resource>(key);
			if (resource == null)
			{
				return null;
			}
			string text = resource.Value.Split('.')[0];
			if (CommonSettings.MOD_MODE && !forceLoadFromResource)
			{
				Sprite sprite = ModEditorResourceManager.GetSprite(text);
				if (sprite != null)
				{
					return sprite;
				}
			}
			if (key.StartsWith("地图."))
			{
				return AssetBundleManager.GetMap(text.Split('/').Last());
			}
			return Resources.Load<Sprite>(text);
		}

		private IEnumerator LoadTextureFromURL(string url, Action<Texture2D> cb)
		{
			WWW www = new WWW(url);
			yield return www;
			if (string.IsNullOrEmpty(www.error))
			{
				cb(www.texture);
				www.Dispose();
			}
		}

		public static void GetMusic(string key, Action<AudioClip> callback)
		{
			Resource resource = ResourceManager.Get<Resource>(key);
			if (resource == null)
			{
				Debug.Log("load invalid music, key=" + key);
				callback(null);
				return;
			}
			string text = resource.Value.Split('.')[0];
			string url = text.Split('/').Last();
			if (CommonSettings.MOD_MODE && ModEditorResourceManager.audios.ContainsKey(text))
			{
				ModEditorResourceManager.GetAudioClip(text, delegate(AudioClip acc)
				{
					callback(acc);
				});
			}
			else
			{
				AudioClip audio = AssetBundleManager.GetAudio(url);
				callback(audio);
			}
		}

		public static string GetLiezhuan(Role role)
		{
			Resource resource = ((role.Key == "主角") ? ResourceManager.Get<Resource>("人物.主角") : ((!(role.Key == "女主")) ? ResourceManager.Get<Resource>("人物." + role.Name) : ResourceManager.Get<Resource>("人物.女主")));
			if (resource == null)
			{
				return null;
			}
			return resource.Value;
		}

		public static string GetTalentDesc(string talent)
		{
			Resource resource = ResourceManager.Get<Resource>("天赋." + talent);
			if (resource == null)
			{
				return null;
			}
			return resource.Value.Split('#')[1];
		}

		public static int GetTalentCost(string talent)
		{
			Resource resource = ResourceManager.Get<Resource>("天赋." + talent);
			if (resource == null)
			{
				return 0;
			}
			return int.Parse(resource.Value.Split('#')[0]);
		}

		public static byte[] GetBytes(string path, bool loadFromAssetBundle = true)
		{
			if (loadFromAssetBundle)
			{
				return null;
			}
			return Resources.Load<TextAsset>(path).bytes;
		}

		public static string GetTalentInfo(string name, bool displayCost = true)
		{
			string talentDesc = GetTalentDesc(name);
			if (displayCost)
			{
				return string.Format("【{0}】(消耗武学常识:{1}){2}", name, GetTalentCost(name), talentDesc);
			}
			return string.Format("【{0}】{1}", name, talentDesc);
		}
	}
}
