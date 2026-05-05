using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	[XmlType("skill")]
	public class SkillInstance : SkillBox
	{
		private string _name;

		[XmlIgnore]
		public List<UniqueSkillInstance> UniqueSkills = new List<UniqueSkillInstance>();

		[XmlAttribute]
		public int level;

		[XmlAttribute]
		public int Exp;

		[XmlAttribute]
		public string name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		public override string Name
		{
			get
			{
				return name;
			}
		}

		public override string Icon
		{
			get
			{
				return Skill.icon;
			}
		}

		public override Color Color
		{
			get
			{
				return Color.white;
			}
		}

		public override int Cd
		{
			get
			{
				return Skill.GetCooldown(level);
			}
		}

		public override int Type
		{
			get
			{
				return Skill.Type;
			}
		}

		public override int CostMp
		{
			get
			{
				return new SkillCoverTypeHelper(CoverType).CostMp(Power, Size);
			}
		}

		public override int CostBall
		{
			get
			{
				return 0;
			}
		}

		public override int CastSize
		{
			get
			{
				return Skill.GetCastSize(level);
			}
		}

		public override string Animation
		{
			get
			{
				return Skill.GetAnimationName(level);
			}
		}

		public override SkillType SkillType
		{
			get
			{
				return SkillType.Normal;
			}
		}

		public override int Level
		{
			get
			{
				return level;
			}
		}

		public override bool Tiaohe
		{
			get
			{
				return Skill.Tiaohe;
			}
		}

		public override float Suit
		{
			get
			{
				return Skill.Suit;
			}
		}

		public override string Audio
		{
			get
			{
				return Skill.Audio;
			}
		}

		[XmlIgnore]
		public override string DescriptionInRichtext
		{
			get
			{
				string empty = string.Empty;
				empty += Skill.Info;
				empty += "\n";
				empty += string.Format("<color='black'>等级 {0}/{1}</color>\n", Level, MaxLevel);
				empty += string.Format("<color='black'>经验 {0}/{1}</color>\n", Exp, LevelupExp);
				empty += string.Format("<color='red'>威力 {0}</color>\n", Power);
				empty += string.Format("<color='black'>覆盖类型 {0}</color>\n", GetSkillCoverTypeChinese());
				empty += string.Format("<color='black'>覆盖范围 {0}</color>\n", Size);
				empty += string.Format("<color='black'>施展范围 {0}</color>\n", CastSize);
				empty += string.Format("<color='cyan'>消耗内力 {0}</color>\n", CostMp);
				empty = ((CurrentCd != 0) ? (empty + string.Format("<color='red'>技能CD {0}/{1}</color>\n", CurrentCd, Cd)) : (empty + string.Format("<color='green'>技能CD {0}/{1}</color>\n", CurrentCd, Cd)));
				if (Tiaohe)
				{
					empty += string.Format("<color='green'>适性:阴阳调和</color>\n");
				}
				else if (Suit == 0f)
				{
					empty += string.Format("<color='black'>适性:无</color>\n");
				}
				else if (Suit > 0f)
				{
					empty += string.Format("<color='yellow'>适性:阳{0}%</color>\n", Skill.Suit * 100f);
				}
				else if (Suit < 0f)
				{
					empty += string.Format("<color='cyan'>适性:阴{0}%</color>\n", (0f - Skill.Suit) * 100f);
				}
				empty += GetBuffsString();
				if (Skill.Triggers.Count > 0)
				{
					empty += "\n\n 被动增益：\n";
					foreach (Trigger trigger in Skill.Triggers)
					{
						empty = ((Level >= trigger.Level) ? (empty + string.Format("<color='green'>(√)({1}级解锁){0}</color>", trigger.ToString(), trigger.Level)) : ((trigger.Level > MaxLevel) ? (empty + string.Format("<color='red'>(×)({0}级解锁)???</color>", trigger.Level)) : (empty + string.Format("<color='red'>(×)({1}级解锁){0}</color>", trigger.ToString(), trigger.Level))));
						empty += "\n";
					}
				}
				return empty;
			}
		}

		[XmlIgnore]
		public override IEnumerable<Buff> Buffs
		{
			get
			{
				return Skill.Buffs;
			}
		}

		public override float Power
		{
			get
			{
				int num = 0;
				if (base.Owner != null)
				{
					foreach (Trigger trigger in base.Owner.GetTriggers("powerup_skill"))
					{
						if (trigger.Argvs[0] == Name)
						{
							num += int.Parse(trigger.Argvs[1]);
						}
					}
				}
				return (float)Math.Round((double)Skill.GetPower(level) * (1.0 + (double)(float)num / 100.0), 2);
			}
		}

		public override int Size
		{
			get
			{
				return Skill.GetCoverSize(level);
			}
		}

		public override SkillCoverType CoverType
		{
			get
			{
				return Skill.GetCoverType(level);
			}
		}

		public Skill Skill
		{
			get
			{
				return ResourceManager.Get<Skill>(name);
			}
		}

		[XmlIgnore]
		public int LevelupExp
		{
			get
			{
				return Skill.GetLevelupExp(Level);
			}
		}

		[XmlIgnore]
		public int PreLevelupExp
		{
			get
			{
				return Skill.GetLevelupExp(Level - 1);
			}
		}

		public void RefreshUniquSkills()
		{
			UniqueSkills.Clear();
			foreach (UniqueSkill uniqueSkill in Skill.UniqueSkills)
			{
				UniqueSkills.Add(new UniqueSkillInstance(uniqueSkill, this));
			}
		}

		public override bool TryAddExp(int exp)
		{
			exp += base.Owner.AttributesFinal["wuxing"] / 30;
			if (base.Owner.HasTalent("武学奇才"))
			{
				Exp += exp * 2;
			}
			else
			{
				Exp += exp;
			}
			bool result = false;
			while (Exp >= LevelupExp)
			{
				if (Level < MaxLevel)
				{
					Exp -= LevelupExp;
					level++;
					if (level > CommonSettings.MAX_SKILL_LEVEL)
					{
						level = CommonSettings.MAX_SKILL_LEVEL;
					}
					result = true;
					continue;
				}
				Exp = LevelupExp;
				break;
			}
			return result;
		}
	}
}
