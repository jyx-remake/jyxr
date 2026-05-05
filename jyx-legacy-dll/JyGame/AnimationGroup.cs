using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("group")]
	public class AnimationGroup
	{
		[XmlAttribute]
		public string name;

		[XmlElement("image")]
		public List<AnimationImage> images;
	}
}
