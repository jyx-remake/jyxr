using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("internal_skill")]
	public class InternalSkill : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("info")]
		public string Info;

		[XmlAttribute("yin")]
		public int Yin;

		[XmlAttribute("yang")]
		public int Yang;

		[XmlAttribute("attack")]
		public float Attack;

		[XmlAttribute("critical")]
		public float Critical;

		[XmlAttribute("defence")]
		public float Defence;

		[XmlAttribute("hard")]
		public float Hard;

		[XmlAttribute]
		public string icon = string.Empty;

		[XmlElement("trigger")]
		public List<Trigger> Triggers = new List<Trigger>();

		[XmlElement("unique")]
		public List<UniqueSkill> UniqueSkills = new List<UniqueSkill>();

		public override string PK
		{
			get
			{
				return Name;
			}
		}

		public override void InitBind()
		{
			foreach (UniqueSkill uniqueSkill in UniqueSkills)
			{
				ResourceManager.Add<UniqueSkill>(uniqueSkill.PK, uniqueSkill);
			}
		}
	}
}
