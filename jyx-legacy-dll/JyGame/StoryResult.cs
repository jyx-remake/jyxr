using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("result")]
	public class StoryResult
	{
		[XmlAttribute]
		public string ret;

		[XmlAttribute]
		public string type;

		[XmlAttribute]
		public string value;

		[XmlElement("condition")]
		public List<Condition> Conditions;
	}
}
