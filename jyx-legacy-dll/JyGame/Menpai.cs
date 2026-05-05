using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("menpai")]
	public class Menpai : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("bg")]
		public string Background;

		[XmlAttribute("pic")]
		public string Pic;

		[XmlAttribute]
		public string story;

		[XmlAttribute]
		public string shifu;

		[XmlAttribute]
		public string wuxue;

		[XmlAttribute]
		public string zhuxiu;

		[XmlAttribute]
		public string tedian;

		[XmlAttribute]
		public string info;

		public override string PK
		{
			get
			{
				return Name;
			}
		}
	}
}
