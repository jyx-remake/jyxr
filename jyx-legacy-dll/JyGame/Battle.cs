using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("battle")]
	public class Battle : BasePojo
	{
		[XmlAttribute("key")]
		public string Key;

		[XmlAttribute("map")]
		public string Map;

		[XmlAttribute("mapkey")]
		public string MapKey;

		[XmlAttribute("music")]
		public string Music;

		[XmlAttribute("must")]
		public string MustStr;

		[XmlAttribute("forceAI")]
		public bool ForceAI;

		[XmlArray("roles")]
		[XmlArrayItem(typeof(BattleRole))]
		public List<BattleRole> Roles;

		[XmlArray("story")]
		[XmlArrayItem(typeof(StoryAction))]
		public List<StoryAction> StoryActions;

		[XmlElement("random")]
		public RandomBattleRole randomBattleRoles;

		[XmlAttribute("bonus")]
		public bool Bonus = true;

		public override string PK
		{
			get
			{
				return Key;
			}
		}

		public List<string> mustKeys
		{
			get
			{
				if (MustStr != null)
				{
					return new List<string>(MustStr.Split('#'));
				}
				return null;
			}
		}

		[XmlIgnore]
		public bool IsArena
		{
			get
			{
				return Key.StartsWith("arena");
			}
		}

		public Battle Clone()
		{
			return BasePojo.Create<Battle>(Xml);
		}
	}
}
