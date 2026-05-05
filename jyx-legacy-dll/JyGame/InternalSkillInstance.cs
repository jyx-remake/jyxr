using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	[XmlType("internal_skill")]
	public class InternalSkillInstance : SkillBox
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

		public int Yin
		{
			get
			{
				return InternalSkill.Yin * Level / 10;
			}
		}

		public int Yang
		{
			get
			{
				int num = InternalSkill.Yang * Level / 10;
				if (base.Owner != null && base.Owner.HasTalent("至刚至阳"))
				{
					num = (int)((double)num * 1.3);
				}
				return num;
			}
		}

		public float Attack
		{
			get
			{
				float num = 1f;
				if (base.Owner != null)
				{
					foreach (Trigger trigger in base.Owner.GetTriggers("powerup_internalskill"))
					{
						if (trigger.Argvs[0] == InternalSkill.Name)
						{
							num += (float)((double)int.Parse(trigger.Argvs[1]) / 100.0);
						}
					}
				}
				return (float)Level / 10f * InternalSkill.Attack * num;
			}
		}

		public float Critical
		{
			get
			{
				if (Level < 10)
				{
					return (float)Level / 10f * InternalSkill.Critical;
				}
				return InternalSkill.Critical;
			}
		}

		public float Defence
		{
			get
			{
				float num = 1f;
				if (base.Owner != null)
				{
					foreach (Trigger trigger in base.Owner.GetTriggers("powerup_internalskill"))
					{
						if (trigger.Argvs[0] == InternalSkill.Name)
						{
							num += (float)((double)int.Parse(trigger.Argvs[1]) / 100.0);
						}
					}
				}
				return (float)Level / 10f * InternalSkill.Defence * num;
			}
		}

		public float Hard
		{
			get
			{
				return InternalSkill.Hard;
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
				return InternalSkill.icon;
			}
		}

		public override Color Color
		{
			get
			{
				return Color.magenta;
			}
		}

		public override int Cd
		{
			get
			{
				return 0;
			}
		}

		public override int Type
		{
			get
			{
				return 4;
			}
		}

		public override int CostMp
		{
			get
			{
				return (int)Hard * Level * 4;
			}
		}

		public override float Power
		{
			get
			{
				return Attack * 13f;
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
				return 0;
			}
		}

		public override int Size
		{
			get
			{
				return 0;
			}
		}

		public override SkillCoverType CoverType
		{
			get
			{
				return SkillCoverType.NORMAL;
			}
		}

		public override string Animation
		{
			get
			{
				return null;
			}
		}

		public override SkillType SkillType
		{
			get
			{
				return SkillType.Internal;
			}
		}

		public override int Level
		{
			get
			{
				return level;
			}
		}

		[XmlIgnore]
		public override string DescriptionInRichtext
		{
			get
			{
				string empty = string.Empty;
				empty += InternalSkill.Info;
				empty += "\n";
				empty += string.Format("<color='black'>等级 {0}/{1}</color>\n", Level, MaxLevel);
				empty += string.Format("<color='black'>经验 {0}/{1}</color>\n", Exp, LevelupExp);
				empty += string.Format("<color='red'>+攻击 {0}%</color>\n", Attack * 100f);
				empty += string.Format("<color='green'>+防御 {0}%</color>\n", Defence * 100f);
				empty += string.Format("<color='yellow'>+爆发 {0}%</color>\n", Critical * 100f);
				empty += string.Format("<color='cyan'>阴适性 {0}</color>\n", Yin);
				empty += string.Format("<color='yellow'>阳适性 {0}</color>\n", Yang);
				if (InternalSkill.Triggers.Count > 0)
				{
					empty += "\n 被动增益：\n";
					foreach (Trigger trigger in InternalSkill.Triggers)
					{
						empty = ((Level >= trigger.Level) ? (empty + string.Format("<color='green'>(√)({1}级解锁){0}</color>", trigger.ToString(), trigger.Level)) : ((trigger.Level > MaxLevel) ? (empty + string.Format("<color='red'>(×)({0}级解锁)???</color>", trigger.Level)) : (empty + string.Format("<color='red'>(×)({1}级解锁){0}</color>", trigger.ToString(), trigger.Level))));
						empty += "\n";
					}
				}
				return empty;
			}
		}

		[XmlIgnore]
		public int LevelupExp
		{
			get
			{
				return GetLevelupExp(Level);
			}
		}

		public InternalSkill InternalSkill
		{
			get
			{
				return ResourceManager.Get<InternalSkill>(name);
			}
		}

		[XmlIgnore]
		public IEnumerable<string> Talents
		{
			get
			{
				foreach (Trigger t in InternalSkill.Triggers)
				{
					if (t.Name == "eq_talent" && Level >= t.Level && IsUsed)
					{
						yield return t.Argvs[0];
					}
					if (t.Name == "talent" && Level >= t.Level)
					{
						yield return t.Argvs[0];
					}
				}
			}
		}

		public InternalSkillInstance()
		{
			equipped = 0;
		}

		public void RefreshUniquSkills()
		{
			UniqueSkills.Clear();
			foreach (UniqueSkill uniqueSkill in InternalSkill.UniqueSkills)
			{
				UniqueSkills.Add(new UniqueSkillInstance(uniqueSkill, this));
			}
		}

		public int GetLevelupExp(int currentLevel)
		{
			return (int)(((float)currentLevel + 4f) / 4f * (Hard + 4f) / 4f * 40f);
		}

		public bool HasTalent(string talent)
		{
			return Talents.Contains(talent);
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
			Exp += exp;
			bool result = false;
			while (Exp >= LevelupExp)
			{
				if (Level < MaxLevel)
				{
					Exp -= LevelupExp;
					level++;
					if (level > CommonSettings.MAX_INTERNALSKILL_LEVEL)
					{
						level = CommonSettings.MAX_INTERNALSKILL_LEVEL;
					}
					result = true;
					continue;
				}
				Exp = LevelupExp;
				break;
			}
			return result;
		}

		public void SetLevel(int level)
		{
			this.level = level;
		}
	}
}
