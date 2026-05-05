using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("animation")]
	public class AnimationNode : BasePojo
	{
		[XmlAttribute]
		public string name;

		[XmlElement("group")]
		public List<AnimationGroup> groups;

		public override string PK
		{
			get
			{
				return name;
			}
		}

		public AnimationGroup GetGroup(string name)
		{
			foreach (AnimationGroup group in groups)
			{
				if (group.name == name)
				{
					return group;
				}
			}
			return null;
		}
	}
}
