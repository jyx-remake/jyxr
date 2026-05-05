using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("map")]
	public class TowerMap
	{
		[XmlAttribute("key")]
		public string Key;

		[XmlAttribute("index")]
		public int Index;

		[XmlElement("item")]
		public List<TowerItem> Items = new List<TowerItem>();

		[XmlElement("nick")]
		public List<TowerNick> Nicks = new List<TowerNick>();
	}
}
