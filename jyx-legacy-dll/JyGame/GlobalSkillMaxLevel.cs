using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("skillmaxlevel")]
	public class GlobalSkillMaxLevel
	{
		[XmlAttribute("k")]
		public string key;

		[XmlAttribute("v")]
		public int value;
	}
}
