using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("story")]
	public class Story : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlElement("action")]
		public List<StoryAction> Actions = new List<StoryAction>();

		[XmlElement("result")]
		public List<StoryResult> Results = new List<StoryResult>();

		public override string PK
		{
			get
			{
				return Name;
			}
		}
	}
}
