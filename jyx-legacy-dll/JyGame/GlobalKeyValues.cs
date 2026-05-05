using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("keyvalue")]
	public class GlobalKeyValues
	{
		[XmlAttribute("k")]
		public string key;

		[XmlAttribute("v")]
		public string value;
	}
}
