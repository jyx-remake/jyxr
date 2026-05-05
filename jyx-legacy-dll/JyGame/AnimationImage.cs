using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("image")]
	public class AnimationImage
	{
		[XmlAttribute]
		public int anchorx;

		[XmlAttribute]
		public int anchory;

		[XmlAttribute]
		public int w;

		[XmlAttribute]
		public int h;

		public override string ToString()
		{
			return string.Format("{0}-{1}-{2}-{3}", anchorx, anchory, w, h);
		}
	}
}
