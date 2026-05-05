using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("item_trigger")]
	public class ItemTrigger : BasePojo
	{
		private string _pk;

		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("minlevel")]
		public int MinLevel = -1;

		[XmlAttribute("maxlevel")]
		public int MaxLevel = -1;

		[XmlElement("trigger")]
		public List<ITTrigger> Triggers;

		public override string PK
		{
			get
			{
				return _pk;
			}
		}

		public ItemTrigger()
		{
			_pk = Guid.NewGuid().ToString();
		}
	}
}
