using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("finish")]
	public class TaskFinish
	{
		[XmlElement("condition")]
		public List<Condition> Conditions;
	}
}
