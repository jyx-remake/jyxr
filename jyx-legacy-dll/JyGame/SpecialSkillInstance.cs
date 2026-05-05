using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	[XmlType("special_skill")]
	public class SpecialSkillInstance : SkillBox
	{
		[XmlAttribute]
		public string name;

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
				return SpecialSkill.icon;
			}
		}

		public override Color Color
		{
			get
			{
				return new Color(0.337f, 0.584f, 1f);
			}
		}

		public override int Cd
		{
			get
			{
				return SpecialSkill.Cd;
			}
		}

		public override int Type
		{
			get
			{
				return -1;
			}
		}

		public override int CostMp
		{
			get
			{
				return SpecialSkill.CostMp;
			}
		}

		public override int CostBall
		{
			get
			{
				return SpecialSkill.CostBall;
			}
		}

		public override int CastSize
		{
			get
			{
				return SpecialSkill.CastSize;
			}
		}

		public override int Size
		{
			get
			{
				return SpecialSkill.CoverSize;
			}
		}

		public override bool HitSelf
		{
			get
			{
				return SpecialSkill.HitSelf;
			}
		}

		public override SkillCoverType CoverType
		{
			get
			{
				return (SkillCoverType)SpecialSkill.CoverType;
			}
		}

		public override string Animation
		{
			get
			{
				return SpecialSkill.Animation;
			}
		}

		public override SkillType SkillType
		{
			get
			{
				return SkillType.Special;
			}
		}

		public override string Audio
		{
			get
			{
				return SpecialSkill.Audio;
			}
		}

		public override bool IsUsed
		{
			get
			{
				return base.IsUsed;
			}
		}

		public override string DescriptionInRichtext
		{
			get
			{
				string empty = string.Empty;
				empty += SpecialSkill.Info;
				empty += "\n";
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
				return SpecialSkill.Buffs;
			}
		}

		public SpecialSkill SpecialSkill
		{
			get
			{
				return ResourceManager.Get<SpecialSkill>(name);
			}
		}
	}
}
