using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace JyGame
{
	public class ResourceManager
	{
		private static bool _inited = false;

		private static string _detail = string.Empty;

		private static float _progress = 0f;

		private static List<string> visitedUri = new List<string>();

		private static Dictionary<Type, Dictionary<string, object>> _values = new Dictionary<Type, Dictionary<string, object>>();

		public static string detail
		{
			get
			{
				return _detail;
			}
			set
			{
				_detail = value;
			}
		}

		public static float progress
		{
			get
			{
				return _progress;
			}
			set
			{
				_progress = value;
			}
		}

		public static void ResetInitFlag()
		{
			_inited = false;
		}

		public static void Init()
		{
			if (!_inited)
			{
				Clear();
				LoadResource<Resource>("resource.xml", "root/resource");
				LoadResource<Battle>("battles.xml", "root/battle");
				LoadResource<Skill>("skills.xml", "root/skill");
				LoadResource<InternalSkill>("internal_skills.xml", "root/internal_skill");
				LoadResource<SpecialSkill>("special_skills.xml", "root/special_skill");
				LoadResource<Role>("roles.xml", "root/role");
				LoadResource<Aoyi>("aoyis.xml", "root/aoyi");
				LoadResource<Story>("storys.xml", "root/story");
				LoadResource<Story>("storysPY.xml", "root/story");
				LoadResource<Story>("storysCG.xml", "root/story");
				LoadResource<Map>("maps.xml", "root/map");
				LoadResource<Item>("items.xml", "root/item");
				LoadResource<ItemTrigger>("item_triggers.xml", "root/item_trigger");
				LoadResource<GlobalTrigger>("globaltrigger.xml", "root/trigger");
				LoadResource<Tower>("towers.xml", "root/tower");
				LoadResource<RoleGrowTemplate>("grow_templates.xml", "root/grow_template");
				LoadResource<AnimationNode>("animations.xml", "root/animation");
				LoadResource<Shop>("shops.xml", "root/shop");
				LoadResource<Menpai>("menpai.xml", "root/menpai");
				LoadResource<Task>("newbie.xml", "root/task");
				_inited = true;
				LuaManager.Call("ROOT_onInitedResources");
			}
		}

		public static IEnumerator Init2(CommonSettings.VoidCallBack callback)
		{
			if (_inited)
			{
				yield return 0;
			}
			Clear();
			LoadResource<Resource>("resource.xml", "root/resource");
			detail = "正在加载战斗设定..";
			progress = 0f;
			LoadResource<Battle>("battles.xml", "root/battle");
			yield return 0;
			detail = "正在加载技能设定..";
			progress = 0.1f;
			LoadResource<Skill>("skills.xml", "root/skill");
			yield return 0;
			detail = "正在加载内功技能设定..";
			progress = 0.2f;
			LoadResource<InternalSkill>("internal_skills.xml", "root/internal_skill");
			yield return 0;
			progress = 0.25f;
			detail = "正在加载特殊技能设定..";
			LoadResource<SpecialSkill>("special_skills.xml", "root/special_skill");
			yield return 0;
			detail = "正在加载角色设定..";
			progress = 0.3f;
			LoadResource<Role>("roles.xml", "root/role");
			yield return 0;
			detail = "正在加载奥义设定..";
			progress = 0.35f;
			LoadResource<Aoyi>("aoyis.xml", "root/aoyi");
			yield return 0;
			detail = "正在加载剧本设定..";
			progress = 0.5f;
			LoadResource<Story>("storys.xml", "root/story");
			LoadResource<Story>("storysPY.xml", "root/story");
			LoadResource<Story>("storysCG.xml", "root/story");
			yield return 0;
			detail = "正在加载地图设定..";
			progress = 0.7f;
			LoadResource<Map>("maps.xml", "root/map");
			yield return 0;
			detail = "正在加载物品设定..";
			progress = 0.9f;
			LoadResource<Item>("items.xml", "root/item");
			yield return 0;
			detail = "正在加载物品属性设定..";
			progress = 0.93f;
			LoadResource<ItemTrigger>("item_triggers.xml", "root/item_trigger");
			yield return 0;
			detail = "正在加载触发器设定..";
			progress = 0.95f;
			LoadResource<GlobalTrigger>("globaltrigger.xml", "root/trigger");
			yield return 0;
			detail = "正在加载天关设定..";
			progress = 0.96f;
			LoadResource<Tower>("towers.xml", "root/tower");
			yield return 0;
			detail = "正在加载角色模板..";
			progress = 0.98f;
			LoadResource<RoleGrowTemplate>("grow_templates.xml", "root/grow_template");
			yield return 0;
			detail = "正在加载商店设定..";
			progress = 0.99f;
			LoadResource<Shop>("shops.xml", "root/shop");
			yield return 0;
			LoadResource<AnimationNode>("animations.xml", "root/animation");
			LoadResource<Menpai>("menpai.xml", "root/menpai");
			LoadResource<Task>("newbie.xml", "root/task");
			_inited = true;
			LuaManager.Call("ROOT_onInitedResources");
			if (callback != null)
			{
				callback();
			}
			yield return 0;
		}

		public static T Get<T>(string key)
		{
			foreach (Type key2 in _values.Keys)
			{
				if (typeof(T) == key2 && _values[key2].ContainsKey(key))
				{
					return (T)_values[key2][key];
				}
			}
			return default(T);
		}

		public static IEnumerable<T> GetAll<T>()
		{
			if (_values.ContainsKey(typeof(T)))
			{
				return _values[typeof(T)].Values.Cast<T>();
			}
			return null;
		}

		public static T GetRandom<T>()
		{
			return (T)_values[typeof(T)].Values.ToList()[Tools.GetRandomInt(0, _values[typeof(T)].Count - 1)];
		}

		public static T GetRandomInCondition<T>(CommonSettings.JudgeCallback judgeCallback, int retryTime = 100)
		{
			int num = 0;
			T random;
			do
			{
				num++;
				if (num > retryTime)
				{
					return default(T);
				}
				random = GetRandom<T>();
			}
			while (!judgeCallback(random));
			return random;
		}

		public static void LoadResource<T>(string uri, string nodepath) where T : BasePojo
		{
			if (visitedUri.Contains(uri))
			{
				return;
			}
			visitedUri.Add(uri);
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				if (CommonSettings.SECURE_XML)
				{
					string path = "Scripts/Secure/" + uri.Split('.')[0];
					string input = Resources.Load(path).ToString();
					string xml = SaveManager.crcm(input);
					xmlDocument.LoadXml(xml);
				}
				else if (CommonSettings.MOD_MODE)
				{
					string path2 = "Scripts/" + uri.Split('.')[0];
					string xml2 = ModEditorResourceManager.GetXml(path2);
					if (GlobalData.CurrentMod.enc)
					{
						xmlDocument.LoadXml(SaveManager.crcm(xml2));
					}
					else
					{
						xmlDocument.LoadXml(xml2);
					}
				}
				else if (Application.platform == RuntimePlatform.WindowsEditor)
				{
					string path3 = Application.dataPath + "/AssetBundleSource/Editor/Scripts/" + uri;
					using (StreamReader streamReader = new StreamReader(path3))
					{
						xmlDocument.LoadXml(streamReader.ReadToEnd());
					}
				}
				else
				{
					xmlDocument.LoadXml(AssetBundleManager.GetXml(uri.Split('.')[0]));
				}
				Dictionary<string, object> dictionary = null;
				dictionary = ((!_values.ContainsKey(typeof(T))) ? new Dictionary<string, object>() : _values[typeof(T)]);
				foreach (XmlNode item in xmlDocument.SelectNodes(nodepath))
				{
					T value = BasePojo.Create<T>(item.OuterXml);
					if (dictionary.ContainsKey(value.PK))
					{
						Debug.LogError("重复key:" + value.PK + ",xml=" + uri);
					}
					else
					{
						dictionary.Add(value.PK, value);
					}
				}
				if (!_values.ContainsKey(typeof(T)))
				{
					_values.Add(typeof(T), dictionary);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("xml载入错误:" + uri);
				Debug.LogError(ex.ToString());
			}
		}

		private static void Clear()
		{
			_values.Clear();
			visitedUri.Clear();
		}

		public static void Add<T>(string pk, object obj)
		{
			Dictionary<string, object> dictionary = null;
			dictionary = ((!_values.ContainsKey(typeof(T))) ? new Dictionary<string, object>() : _values[typeof(T)]);
			dictionary[pk] = obj;
			if (!_values.ContainsKey(typeof(T)))
			{
				_values.Add(typeof(T), dictionary);
			}
		}
	}
}
