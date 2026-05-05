using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("level")]
	public class SkillLevelInfo
	{
		[XmlAttribute("level")]
		public int Level;

		[XmlAttribute("covertype")]
		public int CoverType;

		[XmlAttribute("coversize")]
		public int CoverSize;

		[XmlAttribute("castsize")]
		public int CastSize;

		[XmlAttribute("power")]
		public float Power;

		[XmlAttribute("animation")]
		public string Animation;

		[XmlAttribute("cd")]
		public int Cd;
	}
}
