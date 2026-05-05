using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("item")]
	public class TowerItem
	{
		[XmlAttribute("key")]
		public string Key;

		[XmlAttribute("number")]
		public int Number = -1;

		[XmlAttribute("probability")]
		public float Probability = 1f;
	}
}
