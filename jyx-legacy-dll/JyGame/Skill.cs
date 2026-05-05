using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("skill")]
	public class Skill : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("tiaohe")]
		public int TiaoheValue = -1;

		[XmlAttribute("type")]
		public int Type;

		private int _coverType = -1;

		[XmlAttribute("coversize")]
		public int CoverSize;

		[XmlAttribute("castsize")]
		public int CastSize;

		[XmlAttribute("suit")]
		public float Suit;

		[XmlAttribute("hard")]
		public float Hard;

		[XmlAttribute("info")]
		public string Info;

		[XmlAttribute("audio")]
		public string Audio;

		[XmlAttribute("basepower")]
		public float BasePower;

		[XmlAttribute("step")]
		public float Step;

		[XmlAttribute("animation")]
		public string Animation;

		[XmlAttribute("cd")]
		public int Cd;

		[XmlAttribute]
		public string icon = string.Empty;

		[XmlAttribute("buff")]
		public string buff;

		[XmlElement("level")]
		public List<SkillLevelInfo> Levels = new List<SkillLevelInfo>();

		[XmlElement("trigger")]
		public List<Trigger> Triggers = new List<Trigger>();

		[XmlElement("unique")]
		public List<UniqueSkill> UniqueSkills = new List<UniqueSkill>();

		public override string PK
		{
			get
			{
				return Name;
			}
		}

		public bool Tiaohe
		{
			get
			{
				return TiaoheValue == 1;
			}
		}

		[XmlAttribute("covertype")]
		public int CoverType
		{
			get
			{
				return (_coverType != -1) ? _coverType : ((int)CommonSettings.GetDefaultCoverType(Type));
			}
			set
			{
				_coverType = value;
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

		public string SuitInfo
		{
			get
			{
				if (Tiaohe)
				{
					return "阴阳调和";
				}
				if (Suit == 0f)
				{
					return "无适性";
				}
				if (Suit > 0f)
				{
					return string.Format("阳{0}%", Suit * 100f);
				}
				if (Suit < 0f)
				{
					return string.Format("阴{0}%", (0f - Suit) * 100f);
				}
				return "错误适性";
			}
		}

		private SkillLevelInfo GetSkillLevelInfo(int level)
		{
			foreach (SkillLevelInfo level2 in Levels)
			{
				if (level2.Level == level)
				{
					return level2;
				}
			}
			return null;
		}

		public float GetPower(int level)
		{
			SkillLevelInfo skillLevelInfo = GetSkillLevelInfo(level);
			if (skillLevelInfo != null)
			{
				return skillLevelInfo.Power;
			}
			return BasePower + (float)(level - 1) * Step;
		}

		public int GetCoverSize(int level)
		{
			SkillLevelInfo skillLevelInfo = GetSkillLevelInfo(level);
			if (skillLevelInfo != null && skillLevelInfo.CoverSize > 0)
			{
				return skillLevelInfo.CoverSize;
			}
			if (CoverSize > 0)
			{
				return CoverSize;
			}
			float dSize = new SkillCoverTypeHelper((SkillCoverType)CoverType).dSize;
			return (level > 10) ? ((int)(1f + dSize * 10f)) : ((int)(1f + dSize * (float)level));
		}

		public SkillCoverType GetCoverType(int level)
		{
			SkillLevelInfo skillLevelInfo = GetSkillLevelInfo(level);
			if (skillLevelInfo != null)
			{
				return (SkillCoverType)skillLevelInfo.CoverType;
			}
			return (SkillCoverType)CoverType;
		}

		public int GetCastSize(int level)
		{
			SkillLevelInfo skillLevelInfo = GetSkillLevelInfo(level);
			if (skillLevelInfo != null && skillLevelInfo.CastSize > 0)
			{
				return skillLevelInfo.CastSize;
			}
			if (CastSize > 0)
			{
				return CastSize;
			}
			SkillCoverTypeHelper skillCoverTypeHelper = new SkillCoverTypeHelper(GetCoverType(level));
			int baseCastSize = skillCoverTypeHelper.baseCastSize;
			float dCastSize = skillCoverTypeHelper.dCastSize;
			return (level > 10) ? ((int)((float)baseCastSize + dCastSize * 10f)) : ((int)((float)baseCastSize + dCastSize * (float)level));
		}

		public string GetAnimationName(int level)
		{
			SkillLevelInfo skillLevelInfo = GetSkillLevelInfo(level);
			if (skillLevelInfo != null)
			{
				return skillLevelInfo.Animation;
			}
			return Animation;
		}

		public int GetCooldown(int level)
		{
			SkillLevelInfo skillLevelInfo = GetSkillLevelInfo(level);
			if (skillLevelInfo != null)
			{
				return skillLevelInfo.Cd;
			}
			return Cd;
		}

		public int GetLevelupExp(int currentLevel)
		{
			return (int)((float)currentLevel / 4f * (Hard + 1f) / 4f * 15f * 8f);
		}

		public override void InitBind()
		{
			foreach (UniqueSkill uniqueSkill in UniqueSkills)
			{
				ResourceManager.Add<UniqueSkill>(uniqueSkill.PK, uniqueSkill);
			}
		}
	}
}
