using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("unique")]
	public class UniqueSkill : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("info")]
		public string Info;

		[XmlAttribute("hard")]
		public float Hard;

		[XmlAttribute("covertype")]
		public int CoverType;

		[XmlAttribute("coversize")]
		public int CoverSize;

		[XmlAttribute("castsize")]
		public int CastSize;

		[XmlAttribute("poweradd")]
		public float PowerAdd;

		[XmlAttribute("requirelv")]
		public int RequireLevel;

		[XmlAttribute("animation")]
		public string Animation;

		[XmlAttribute("cd")]
		public int CastCd;

		[XmlAttribute("costball")]
		public int CostBall;

		[XmlAttribute("audio")]
		public string Audio;

		[XmlAttribute("buff")]
		public string BuffsValue;

		[XmlAttribute]
		public string icon = string.Empty;

		public override string PK
		{
			get
			{
				return Name;
			}
		}

		[XmlIgnore]
		public IEnumerable<Buff> Buffs
		{
			get
			{
				return Buff.Parse(BuffsValue);
			}
		}
	}
}
