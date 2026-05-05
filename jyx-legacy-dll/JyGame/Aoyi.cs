using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("aoyi")]
	public class Aoyi : BasePojo
	{
		private string _pk;

		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute]
		public string start;

		[XmlAttribute]
		public int level;

		[XmlAttribute]
		public float probability;

		[XmlAttribute]
		public string buff;

		[XmlAttribute]
		public string animation;

		[XmlAttribute]
		public float addPower;

		[XmlElement("condition")]
		public List<AoyiCondition> Conditions = new List<AoyiCondition>();

		public override string PK
		{
			get
			{
				return _pk;
			}
		}

		[XmlIgnore]
		public IEnumerable<Buff> Buffs
		{
			get
			{
				return Buff.Parse(buff);
			}
		}

		public Aoyi()
		{
			_pk = Guid.NewGuid().ToString();
		}

		public float GetStartSkillHard()
		{
			Skill skill = ResourceManager.Get<Skill>(start);
			if (skill != null)
			{
				return skill.Hard;
			}
			UniqueSkill uniqueSkill = ResourceManager.Get<UniqueSkill>(start);
			if (uniqueSkill != null)
			{
				return uniqueSkill.Hard;
			}
			return 100f;
		}
	}
}
