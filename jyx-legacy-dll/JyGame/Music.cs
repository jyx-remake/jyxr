using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("music")]
	public class Music
	{
		[XmlAttribute("name")]
		public string Name;
	}
}
