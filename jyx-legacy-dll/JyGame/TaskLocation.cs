using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("location")]
	public class TaskLocation
	{
		[XmlAttribute("name")]
		public string name;
	}
}
