using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("maprole")]
	public class MapRole
	{
		[XmlAttribute]
		public string description;

		[XmlElement("event")]
		public List<MapEvent> Events;

		[XmlAttribute]
		public string pic;

		[XmlAttribute]
		public string name;

		[XmlIgnore]
		public bool IsActive
		{
			get
			{
				return GetActiveEvent() != null;
			}
		}

		[XmlIgnore]
		public string Name
		{
			get
			{
				Role role = ResourceManager.Get<Role>(name);
				if (role != null)
				{
					return CommonSettings.getRoleName(name);
				}
				return name.TrimEnd('1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
			}
			set
			{
				name = value;
			}
		}

		[XmlIgnore]
		public string roleKey
		{
			get
			{
				return name;
			}
		}

		public MapEvent GetActiveEvent()
		{
			foreach (MapEvent @event in Events)
			{
				if (@event.IsActive)
				{
					return @event;
				}
			}
			return null;
		}
	}
}
