using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("user_defined_animations")]
	public class UserDefinedAnimationsData
	{
		[XmlElement("animation")]
		public UserDefinedAnimtionData[] animations;
	}
}
