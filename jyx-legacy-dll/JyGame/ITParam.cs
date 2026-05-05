using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("param")]
	public class ITParam
	{
		[XmlAttribute("min")]
		public int Min = -1;

		[XmlAttribute("max")]
		public int Max = -1;

		[XmlAttribute("pool")]
		public string Pool = string.Empty;
	}
}
