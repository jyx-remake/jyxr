using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("trigger")]
	public class ITTrigger
	{
		[XmlAttribute("name")]
		public string Name = string.Empty;

		[XmlAttribute("w")]
		public int Weight = 100;

		[XmlElement("param")]
		public List<ITParam> Params = new List<ITParam>();

		public bool HasPool
		{
			get
			{
				foreach (ITParam item in Params)
				{
					if (item.Pool != string.Empty)
					{
						return true;
					}
				}
				return false;
			}
		}

		public Trigger GenerateItemTrigger(int itemLevel)
		{
			Trigger trigger = new Trigger();
			trigger.Name = Name;
			int round = RuntimeData.Instance.Round;
			if (Name == "attribute")
			{
				string[] array = new string[10] { "搏击格斗", "使剑技巧", "耍刀技巧", "奇门兵器", "根骨", "臂力", "福缘", "身法", "定力", "悟性" };
				string arg = array[Tools.GetRandomInt(0, array.Length - 1)];
				int a = (int)((double)itemLevel * ((double)(round + 3) / 4.0)) + itemLevel;
				int b = (int)((double)itemLevel * ((double)(round + 3) / 3.0)) + itemLevel * 2;
				trigger.ArgvsString = string.Format("{0},{1}", arg, Tools.GetRandomInt(a, b));
				return trigger;
			}
			if (Name == "powerup_skill")
			{
				Skill skill = null;
				do
				{
					skill = ResourceManager.GetRandom<Skill>();
				}
				while ((float)(itemLevel + 4) < skill.Hard || skill.Hard + 1f < (float)itemLevel);
				int a2 = (int)(3f * ((float)(round + 3) / (float)((double)skill.Hard / 2.0 + 1.0)));
				int num = (int)(15f * ((float)(round + 3) / (float)((double)skill.Hard / 2.0 + 1.0)));
				if (skill.Hard < 6f)
				{
					num += round * 15;
				}
				trigger.ArgvsString = string.Format("{0},{1}", skill.Name, Tools.GetRandomInt(a2, num));
				return trigger;
			}
			if (Name == "powerup_internalskill")
			{
				InternalSkill internalSkill = null;
				do
				{
					internalSkill = ResourceManager.GetRandom<InternalSkill>();
				}
				while ((float)(itemLevel + 4) < internalSkill.Hard || internalSkill.Hard + 1f < (float)itemLevel);
				int a3 = (int)(2f * ((float)(round + 3) / (float)((double)internalSkill.Hard / 2.0 + 1.0)));
				int num2 = (int)(10f * ((float)(round + 3) / (float)((double)internalSkill.Hard / 2.0 + 1.0)));
				if (internalSkill.Hard < 6f)
				{
					num2 += round * 10;
				}
				trigger.ArgvsString = string.Format("{0},{1}", internalSkill.Name, Tools.GetRandomInt(a3, num2));
				return trigger;
			}
			if (Name == "powerup_aoyi")
			{
				Aoyi aoyi = null;
				float num3 = 100f;
				do
				{
					aoyi = ResourceManager.GetRandom<Aoyi>();
					num3 = aoyi.GetStartSkillHard();
				}
				while ((float)(itemLevel + 4) < num3 || num3 + 1f < (float)itemLevel);
				int a4 = (int)(15f * ((float)(round + 3) / (float)((double)num3 / 2.0 + 1.0)));
				int num4 = (int)(30f * ((float)(round + 3) / (float)((double)num3 / 2.0 + 1.0)));
				if (num3 < 6f)
				{
					num4 += round * 15;
				}
				trigger.ArgvsString = string.Format("{0},{1},{2}", aoyi.Name, Tools.GetRandomInt(a4, num4), Tools.GetRandomInt(0, 10));
				return trigger;
			}
			if (Name == "powerup_uniqueskill")
			{
				UniqueSkill uniqueSkill = null;
				do
				{
					uniqueSkill = ResourceManager.GetRandom<UniqueSkill>();
				}
				while ((float)(itemLevel + 4) < uniqueSkill.Hard || uniqueSkill.Hard + 1f < (float)itemLevel);
				int a5 = (int)(10f * ((float)(round + 3) / (float)((double)uniqueSkill.Hard / 2.0 + 1.0)));
				int num5 = (int)(25f * ((float)(round + 3) / (float)((double)uniqueSkill.Hard / 2.0 + 1.0)));
				if (uniqueSkill.Hard < 6f)
				{
					num5 += round * 15;
				}
				trigger.ArgvsString = string.Format("{0},{1}", uniqueSkill.Name, Tools.GetRandomInt(a5, num5));
				return trigger;
			}
			if (Name == "attack")
			{
				int a6 = itemLevel * 1 * (1 + round * 2);
				int b2 = itemLevel * 2 * (1 + round * 2);
				trigger.ArgvsString = string.Format("{0},{1}", Tools.GetRandomInt(a6, b2), Tools.GetRandomInt(1, itemLevel));
				return trigger;
			}
			if (Name == "powerup_quanzhang" || Name == "powerup_qimen" || Name == "powerup_daofa" || Name == "powerup_jianfa")
			{
				int a7 = (int)((double)itemLevel * 0.2 * (double)(1 + round * 2));
				int b3 = (int)((double)itemLevel * 0.4 * (double)(1 + round * 2));
				trigger.ArgvsString = string.Format("{0}", Tools.GetRandomInt(a7, b3));
				return trigger;
			}
			if (Name == "criticalp")
			{
				float num6 = 0.5f;
				float num7 = itemLevel;
				trigger.ArgvsString = Math.Round(Tools.GetRandom(num6, num7), 2).ToString();
				return trigger;
			}
			List<string> list = new List<string>();
			list.Clear();
			for (int i = 0; i < Params.Count; i++)
			{
				ITParam iTParam = Params[i];
				if (iTParam.Min != -1)
				{
					list.Add(Tools.GetRandomInt(iTParam.Min, iTParam.Max).ToString());
				}
				else if (iTParam.Pool != string.Empty)
				{
					string[] array2 = iTParam.Pool.Split(',');
					string item = array2[Tools.GetRandomInt(0, array2.Length - 1)];
					list.Add(item);
				}
			}
			trigger.ArgvsString = string.Empty;
			for (int j = 0; j < list.Count; j++)
			{
				trigger.ArgvsString = trigger.ArgvsString + list[j] + ",";
			}
			trigger.ArgvsString.TrimEnd(',');
			return trigger;
		}
	}
}
