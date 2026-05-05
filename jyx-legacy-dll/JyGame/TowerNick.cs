using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("nick")]
	public class TowerNick
	{
		[XmlAttribute("name")]
		public string Name;
	}
}
