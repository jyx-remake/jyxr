using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("globalsave")]
	public class GlobalSave : BasePojo
	{
		[XmlElement("n")]
		public string[] Nicks;

		[XmlElement("kv")]
		public GameSaveKeyValues[] KeyValues;

		[XmlElement("s")]
		public GlobalSkillMaxLevel[] SkillMaxLevels;

		[XmlIgnore]
		public override string PK
		{
			get
			{
				return "globalsave";
			}
		}
	}
}
