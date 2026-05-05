using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("location")]
	public class MapLocation : MapRole
	{
		[XmlAttribute]
		public int x;

		[XmlAttribute]
		public int y;

		[XmlIgnore]
		public int X
		{
			get
			{
				return x * 1140 * 2 / 800 + 18;
			}
		}

		[XmlIgnore]
		public int Y
		{
			get
			{
				return -y * 640 * 2 / 600 - 8;
			}
		}

		public string getName()
		{
			if (name.Equals("女主"))
			{
				return RuntimeData.Instance.femaleName;
			}
			return name;
		}

		public string GetImageKey()
		{
			foreach (MapEvent @event in Events)
			{
				if (!string.IsNullOrEmpty(@event.image) && @event.type == "map")
				{
					return @event.image;
				}
			}
			return string.Empty;
		}
	}
}
