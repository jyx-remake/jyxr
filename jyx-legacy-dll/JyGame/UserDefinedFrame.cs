using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	[XmlType]
	public class UserDefinedFrame
	{
		[XmlAttribute]
		public string tag;

		[XmlAttribute]
		public float x;

		[XmlAttribute]
		public float y;

		[XmlAttribute]
		public string file;

		[XmlAttribute]
		public float scale;

		[XmlIgnore]
		public Sprite sprite;
	}
}
