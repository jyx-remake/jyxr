using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("trigger")]
	public class Trigger : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("argvs")]
		public string ArgvsString;

		[XmlAttribute("lv")]
		public int Level = -1;

		public override string PK
		{
			get
			{
				string text = Name + "_" + ArgvsString;
				return text + "_" + Level;
			}
		}

		[XmlIgnore]
		public List<string> Argvs
		{
			get
			{
				return new List<string>(ArgvsString.Split(','));
			}
		}

		public override string ToString()
		{
			if (Name == "AddBuff")
			{
				return string.Empty;
			}
			if (Name == "talent")
			{
				string text = Argvs[0];
				return string.Format("天赋 {0}(被动生效)\n{1}", text, ResourceManager.Get<Resource>("天赋." + text).Value.Split('#')[1]);
			}
			if (Name == "eq_talent")
			{
				string text2 = Argvs[0];
				return string.Format("天赋 {0}(装备生效)\n{1}", text2, ResourceManager.Get<Resource>("天赋." + text2).Value.Split('#')[1]);
			}
			string value = ResourceManager.Get<Resource>("ItemTrigger." + Name).Value;
			return string.Format(value, Argvs.ToArray());
		}
	}
}
