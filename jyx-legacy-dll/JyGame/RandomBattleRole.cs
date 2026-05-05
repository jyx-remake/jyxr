using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("random")]
	public class RandomBattleRole
	{
		[XmlElement("role")]
		public List<BattleRole> randomRoles = new List<BattleRole>();
	}
}
