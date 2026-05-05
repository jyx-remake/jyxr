using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("grow_template")]
	public class RoleGrowTemplate : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute]
		public int hp;

		[XmlAttribute]
		public int mp;

		[XmlAttribute]
		public int wuxing;

		[XmlAttribute]
		public int shenfa;

		[XmlAttribute]
		public int bili;

		[XmlAttribute]
		public int gengu;

		[XmlAttribute]
		public int fuyuan;

		[XmlAttribute]
		public int dingli;

		[XmlAttribute]
		public int quanzhang;

		[XmlAttribute]
		public int jianfa;

		[XmlAttribute]
		public int daofa;

		[XmlAttribute]
		public int qimen;

		[XmlAttribute]
		public int wuxue;

		[XmlIgnore]
		public AttributeHelper Attributes;

		public override string PK
		{
			get
			{
				return Name;
			}
		}

		public override void InitBind()
		{
			Attributes = new AttributeHelper(this);
		}
	}
}
