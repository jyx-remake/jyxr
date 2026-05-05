using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("condition")]
	public class AoyiCondition
	{
		[XmlAttribute]
		public string type;

		[XmlAttribute]
		public string value;

		[XmlAttribute]
		public string levelValue;

		[XmlIgnore]
		public int level
		{
			get
			{
				return (levelValue != null) ? int.Parse(levelValue) : 0;
			}
		}
	}
}
