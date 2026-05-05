using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JyGame
{
	public class ResourcePool
	{
		private static int _totalCount = 0;

		private static int _loadedCount = 0;

		public static Dictionary<string, GameObject> _values = new Dictionary<string, GameObject>();

		public static void Init()
		{
			Clear();
		}

		public static void Clear()
		{
			_values.Clear();
			_totalCount = 0;
			_loadedCount = 0;
		}

		public static float GetLoadProgress()
		{
			if (_totalCount == 0)
			{
				return 1f;
			}
			return (float)_loadedCount / (float)_totalCount;
		}

		public static GameObject Get(string key)
		{
			if (_values.ContainsKey(key))
			{
				return _values[key];
			}
			return Resources.Load<GameObject>(key);
		}

		public static IEnumerable<GameObject> GetAll<T>()
		{
			return _values.Values;
		}

		public static IEnumerator Load(Battle battle, CommonSettings.VoidCallBack callback)
		{
			Clear();
			List<string> tobeload = new List<string>();
			foreach (BattleRole role in battle.Roles)
			{
				string animationName = role.role.Animation;
				string roleSpriteUrl = "Animations/" + animationName;
				if (!tobeload.Contains(roleSpriteUrl))
				{
					tobeload.Add(roleSpriteUrl);
				}
				foreach (SkillInstance s in role.role.Skills)
				{
					string skillSpriteUrl = "Effects/" + s.Animation;
					if (!tobeload.Contains(skillSpriteUrl))
					{
						tobeload.Add(skillSpriteUrl);
					}
					foreach (Aoyi aoyi in ResourceManager.GetAll<Aoyi>())
					{
						if (aoyi.start == s.Name)
						{
							string aoyiSpriteUrl = "Effects/" + aoyi.animation;
							if (!tobeload.Contains(aoyiSpriteUrl))
							{
								tobeload.Add(aoyiSpriteUrl);
							}
						}
					}
					foreach (UniqueSkillInstance us in s.UniqueSkills)
					{
						string uskillSpriteUrl = "Effects/" + us.Animation;
						if (!tobeload.Contains(uskillSpriteUrl))
						{
							tobeload.Add(uskillSpriteUrl);
						}
					}
				}
				foreach (SpecialSkillInstance s2 in role.role.SpecialSkills)
				{
					string skillSpriteUrl2 = "Effects/" + s2.Animation;
					if (!tobeload.Contains(skillSpriteUrl2))
					{
						tobeload.Add(skillSpriteUrl2);
					}
				}
				foreach (UniqueSkillInstance s3 in role.role.GetEquippedInternalSkill().UniqueSkills)
				{
					string skillSpriteUrl3 = "Effects/" + s3.Animation;
					if (!tobeload.Contains(skillSpriteUrl3))
					{
						tobeload.Add(skillSpriteUrl3);
					}
				}
			}
			_totalCount = tobeload.Count;
			_loadedCount = 0;
			foreach (string key in tobeload)
			{
				GameObject obj = Resources.Load<GameObject>(key);
				if (obj == null)
				{
					Debug.LogWarning("Resources.Load key is null:" + key);
					_loadedCount++;
					yield return 0;
				}
				else
				{
					_values.Add(key, obj);
					_loadedCount++;
					yield return 0;
				}
			}
			if (callback != null)
			{
				callback();
			}
			yield return 0;
		}

		private static void GenerateResourcePool(Battle battle)
		{
		}
	}
}
