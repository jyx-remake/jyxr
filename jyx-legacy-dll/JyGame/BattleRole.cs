using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("role")]
	public class BattleRole
	{
		[XmlAttribute("key")]
		public string PredefinedKey;

		[XmlIgnore]
		public bool IsPlayerPickedRole;

		[XmlIgnore]
		private Role _role;

		[XmlAttribute("team")]
		public int Team = 1;

		[XmlAttribute("x")]
		public int X;

		[XmlAttribute("y")]
		public int Y;

		[XmlAttribute("face")]
		public int Face = 1;

		[XmlAttribute("level")]
		public int Level;

		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("animation")]
		public string Animation;

		[XmlAttribute("boss")]
		public bool IsBoss;

		[XmlIgnore]
		public int random_level = -1;

		[XmlIgnore]
		public string random_name;

		[XmlIgnore]
		public Role role
		{
			get
			{
				if (_role == null)
				{
					if (!string.IsNullOrEmpty(PredefinedKey))
					{
						_role = ResourceManager.Get<Role>(PredefinedKey).Clone();
						return _role;
					}
					return null;
				}
				return _role;
			}
			set
			{
				_role = value;
			}
		}

		public bool FaceRight
		{
			get
			{
				return Face == 1;
			}
		}

		public bool IsRandom
		{
			get
			{
				return (random_level != -1) ? true : false;
			}
		}
	}
}
