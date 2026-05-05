using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("condition")]
	public class Condition
	{
		[XmlAttribute]
		public string type;

		[XmlAttribute]
		public string value;

		[XmlAttribute]
		public int number = -1;

		[XmlIgnore]
		public bool IsTrue
		{
			get
			{
				return TriggerLogic.judge(this);
			}
		}
	}
}
