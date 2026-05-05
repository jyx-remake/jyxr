using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("task")]
	public class Task : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("description")]
		public string Desc;

		[XmlElement("location")]
		public List<TaskLocation> Locations;

		[XmlElement("finish")]
		public List<TaskFinish> Finishes;

		[XmlElement("result")]
		public StoryResult Result;

		public override string PK
		{
			get
			{
				return Name;
			}
		}
	}
}
