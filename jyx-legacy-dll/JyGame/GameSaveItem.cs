using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("item")]
	public class GameSaveItem
	{
		[XmlAttribute("n")]
		public string name;

		[XmlElement("t")]
		public Trigger[] triggers;

		[XmlAttribute("c")]
		public int count;

		public ItemInstance Generate()
		{
			ItemInstance itemInstance = new ItemInstance();
			itemInstance.Name = name;
			itemInstance.AdditionTriggers = ((triggers != null) ? triggers.ToList() : new List<Trigger>());
			itemInstance.InitBind();
			return itemInstance;
		}
	}
}
