using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("keyvalue")]
	public class GameSaveKeyValues
	{
		[XmlAttribute("k")]
		public string key;

		[XmlAttribute("v")]
		public string value;
	}
}
