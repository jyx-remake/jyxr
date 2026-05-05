using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("event")]
	public class MapEvent
	{
		[XmlAttribute]
		public string type;

		[XmlAttribute]
		public string value;

		[XmlAttribute]
		public string image;

		[XmlAttribute("probability")]
		public int probability = 100;

		[XmlAttribute]
		public int lv = -1;

		[XmlAttribute]
		public string description;

		[XmlAttribute("repeat")]
		public string repeatValue;

		[XmlElement("condition")]
		public List<Condition> Conditions;

		public bool IsRepeatOnce
		{
			get
			{
				return repeatValue == "once";
			}
		}

		[XmlIgnore]
		public bool IsActive
		{
			get
			{
				if (IsRepeatOnce && RuntimeData.Instance.IsStoryFinished(value))
				{
					return false;
				}
				if (!Tools.ProbabilityTest((double)probability / 100.0))
				{
					return false;
				}
				foreach (Condition condition in Conditions)
				{
					if (!condition.IsTrue)
					{
						return false;
					}
				}
				return true;
			}
		}
	}
}
