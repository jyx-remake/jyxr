using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	public class UniqueSkillInstance : SkillBox
	{
		public SkillBox _parent;

		public UniqueSkill _skill;

		public override string Name
		{
			get
			{
				return _skill.Name;
			}
		}

		public override string Icon
		{
			get
			{
				return _parent.Icon;
			}
		}

		public override Color Color
		{
			get
			{
				return Color.red;
			}
		}

		public override int Cd
		{
			get
			{
				return UniqueSkill.CastCd;
			}
		}

		public override int Type
		{
			get
			{
				return _parent.Type;
			}
		}

		public override int CostMp
		{
			get
			{
				return _parent.CostMp;
			}
		}

		public override int CostBall
		{
			get
			{
				return UniqueSkill.CostBall;
			}
		}

		public override int CastSize
		{
			get
			{
				return UniqueSkill.CastSize;
			}
		}

		public override int Size
		{
			get
			{
				return UniqueSkill.CoverSize;
			}
		}

		public override SkillCoverType CoverType
		{
			get
			{
				return (SkillCoverType)UniqueSkill.CoverType;
			}
		}

		public override string Animation
		{
			get
			{
				return UniqueSkill.Animation;
			}
		}

		public override SkillType SkillType
		{
			get
			{
				return SkillType.Unique;
			}
		}

		public override int Level
		{
			get
			{
				return _parent.Level;
			}
		}

		public override float Suit
		{
			get
			{
				return _parent.Suit;
			}
		}

		public override float Power
		{
			get
			{
				int num = 0;
				if (base.Owner != null)
				{
					foreach (Trigger trigger in base.Owner.GetTriggers("powerup_uniqueskill"))
					{
						if (trigger.Argvs[0] == Name)
						{
							num += int.Parse(trigger.Argvs[1]);
						}
					}
				}
				return (float)((double)(_parent.Power + UniqueSkill.PowerAdd) * (1.0 + (double)(float)num / 100.0));
			}
		}

		public override string Audio
		{
			get
			{
				if (!string.IsNullOrEmpty(UniqueSkill.Audio))
				{
					return UniqueSkill.Audio;
				}
				return _parent.Audio;
			}
		}

		[XmlIgnore]
		public override string DescriptionInRichtext
		{
			get
			{
				string empty = string.Empty;
				empty += string.Format("<color='white'>所属武学</color> <color='red'>{0}</color>\n", _parent.Name);
				empty += string.Format("<color='white'>绝技解锁等级</color> <color='red'>{0}</color>\n\n", RequireLevel);
				empty = empty + UniqueSkill.Info + "\n";
				float num = UniqueSkill.PowerAdd + _parent.Power;
				empty += string.Format("<color='red'>威力 {0}</color>\n", num);
				empty += string.Format("<color='black'>覆盖类型 {0}</color>\n", GetSkillCoverTypeChinese());
				empty += string.Format("<color='black'>覆盖范围 {0}</color>\n", Size);
				empty += string.Format("<color='black'>施展范围 {0}</color>\n", CastSize);
				empty += string.Format("<color='cyan'>消耗内力 {0}</color>\n", CostMp);
				empty += string.Format("<color='yellow'>消耗集气 {0}</color>\n", CostBall);
				empty = ((CurrentCd != 0) ? (empty + string.Format("<color='red'>技能CD {0}/{1}</color>\n", CurrentCd, Cd)) : (empty + string.Format("<color='green'>技能CD {0}/{1}</color>\n", CurrentCd, Cd)));
				return empty + GetBuffsString();
			}
		}

		[XmlIgnore]
		public override IEnumerable<Buff> Buffs
		{
			get
			{
				return UniqueSkill.Buffs;
			}
		}

		public override bool Tiaohe
		{
			get
			{
				return _parent.Tiaohe;
			}
		}

		public UniqueSkill UniqueSkill
		{
			get
			{
				return _skill;
			}
		}

		[XmlIgnore]
		public int RequireLevel
		{
			get
			{
				return UniqueSkill.RequireLevel;
			}
		}

		public UniqueSkillInstance(UniqueSkill skill, SkillBox parent)
		{
			base.Owner = parent.Owner;
			_parent = parent;
			_skill = skill;
		}

		public override bool TryAddExp(int exp)
		{
			return _parent.TryAddExp(exp);
		}
	}
}
