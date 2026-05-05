using System.Collections.Generic;

namespace JyGame
{
	public class Buff
	{
		public static string[] BuffNames = LuaManager.GetConfig<string[]>("BUFF_LIST");

		public static string[] DebuffNames = LuaManager.GetConfig<string[]>("DEBUFF_LIST");

		public string Name;

		public int Level;

		public int Round = -1;

		public int Property = -1;

		public bool IsDebuff
		{
			get
			{
				string[] buffNames = BuffNames;
				foreach (string value in buffNames)
				{
					if (Name.Equals(value))
					{
						return false;
					}
				}
				return true;
			}
		}

		public static string BuffsToString(List<Buff> buffs)
		{
			if (buffs.Count > 0)
			{
				string text = string.Empty;
				foreach (Buff buff in buffs)
				{
					text += string.Format("#{0}.{1}.{2}.{3}", buff.Name, buff.Level, buff.Round, buff.Property);
				}
				return text.TrimStart('#');
			}
			return string.Empty;
		}

		public static IEnumerable<Buff> Parse(string content)
		{
			if (string.IsNullOrEmpty(content))
			{
				yield break;
			}
			string[] tmp = content.Split('#');
			string[] array = tmp;
			foreach (string s in array)
			{
				string[] paras = s.Split('.');
				string name = paras[0];
				int level = 1;
				int round = 3;
				int property = -1;
				if (paras.Length > 1)
				{
					level = int.Parse(paras[1]);
				}
				if (paras.Length > 2)
				{
					round = int.Parse(paras[2]);
				}
				if (paras.Length > 3)
				{
					property = int.Parse(paras[3]);
				}
				yield return new Buff
				{
					Name = name,
					Level = level,
					Round = round,
					Property = property
				};
			}
		}
	}
}
