using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("tower")]
	public class Tower : BasePojo
	{
		[XmlAttribute("key")]
		public string Key;

		[XmlAttribute("desc")]
		public string Desc;

		[XmlElement("map")]
		public List<TowerMap> Maps = new List<TowerMap>();

		[XmlElement("condition")]
		public List<Condition> Conditions = new List<Condition>();

		public override string PK
		{
			get
			{
				return Key;
			}
		}
	}
}
