using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("require")]
	public class Require : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("argvs")]
		public string ArgvsString;

		public override string PK
		{
			get
			{
				return Name + ArgvsString;
			}
		}
	}
}
