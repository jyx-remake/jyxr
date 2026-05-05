using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	public abstract class SkillBox
	{
		[XmlAttribute]
		public int equipped = 1;

		[XmlIgnore]
		public int CurrentCd;

		[XmlIgnore]
		public string ScreenEffect;

		[XmlIgnore]
		public bool IsScreenEffectFollowSprite;

		[XmlIgnore]
		public virtual bool IsUsed
		{
			get
			{
				return equipped == 1;
			}
		}

		public abstract string Name { get; }

		public abstract Color Color { get; }

		public abstract SkillType SkillType { get; }

		[XmlIgnore]
		public abstract string Icon { get; }

		[XmlIgnore]
		public Sprite IconSprite
		{
			get
			{
				return Resource.GetIcon(Icon);
			}
		}

		[XmlIgnore]
		public Role Owner { get; set; }

		public abstract int CostBall { get; }

		public abstract int CostMp { get; }

		public abstract int Cd { get; }

		public abstract int Type { get; }

		public abstract int CastSize { get; }

		public abstract SkillCoverType CoverType { get; }

		public abstract int Size { get; }

		public abstract string Animation { get; }

		public virtual int Level
		{
			get
			{
				return 1;
			}
		}

		public virtual int MaxLevel
		{
			get
			{
				int num = Math.Max(Level, ModData.GetSkillMaxLevel(Name));
				if (num > CommonSettings.MAX_SKILL_LEVEL)
				{
					return CommonSettings.MAX_SKILL_LEVEL;
				}
				return num;
			}
		}

		public virtual bool HitSelf
		{
			get
			{
				return false;
			}
		}

		public virtual string Audio
		{
			get
			{
				return string.Empty;
			}
		}

		public bool IsAoyi
		{
			get
			{
				return SkillType == SkillType.Aoyi;
			}
		}

		public bool IsUnique
		{
			get
			{
				return SkillType == SkillType.Unique;
			}
		}

		public bool IsInternal
		{
			get
			{
				return SkillType == SkillType.Internal;
			}
		}

		public bool IsSpecial
		{
			get
			{
				return SkillType == SkillType.Special;
			}
		}

		[XmlIgnore]
		public virtual IEnumerable<Buff> Buffs
		{
			get
			{
				yield break;
			}
		}

		public virtual bool Tiaohe
		{
			get
			{
				return false;
			}
		}

		public virtual float Suit
		{
			get
			{
				return 0f;
			}
		}

		public virtual float Power
		{
			get
			{
				return 0f;
			}
		}

		public virtual SkillStatus Status
		{
			get
			{
				if (Owner == null)
				{
					return SkillStatus.Error;
				}
				if (Owner.balls < CostBall)
				{
					return SkillStatus.NoBalls;
				}
				if (CurrentCd > 0)
				{
					return SkillStatus.NoCd;
				}
				if (CostMp > Owner.mp)
				{
					return SkillStatus.NoMp;
				}
				BattleSprite sprite = Owner.Sprite;
				if (sprite != null && !IsSpecial && ((sprite.GetBuff("诸般封印") != null && !IsUnique) || (sprite.GetBuff("拳掌封印") != null && Type == 0) || (sprite.GetBuff("剑封印") != null && Type == 1) || (sprite.GetBuff("刀封印") != null && Type == 2) || (sprite.GetBuff("奇门封印") != null && Type == 3)))
				{
					return SkillStatus.Seal;
				}
				return SkillStatus.Ok;
			}
		}

		[XmlIgnore]
		public virtual string DescriptionInRichtext
		{
			get
			{
				return string.Empty;
			}
		}

		[XmlIgnore]
		public string DescriptionInRichtextBlackBg
		{
			get
			{
				return DescriptionInRichtext.Replace("black", "white");
			}
		}

		public string GetSkillTypeChinese()
		{
			switch (SkillType)
			{
			case SkillType.Normal:
				return "外功";
			case SkillType.Internal:
				return "内功";
			case SkillType.Unique:
				return "绝技";
			case SkillType.Special:
				return "特殊技能";
			case SkillType.Aoyi:
				return "奥义";
			default:
				return "错误类型";
			}
		}

		public string GetBuffsString()
		{
			string text = string.Empty;
			foreach (Buff buff in Buffs)
			{
				text += string.Format("<color='yellow'>特效:{0}({1})</color>", buff.Name, buff.Level);
				if (buff.Round > 0)
				{
					text += string.Format(" <color='yellow'>持续{0}回合</color>", buff.Round);
				}
				if (buff.Property == 100)
				{
					text += string.Format(" <color='red'>必定命中</color>");
				}
				else if (buff.Property > 0)
				{
					text += string.Format(" <color='yellow'>命中概率:{0}%</color>", buff.Property);
				}
				text += "\n";
			}
			return text;
		}

		public string GetSkillCoverTypeChinese()
		{
			return SkillCoverTypeHelper.GetSkillTypeChinese((int)CoverType);
		}

		public virtual void CastCd()
		{
			CurrentCd += Cd;
		}

		public string GetStatusString()
		{
			switch (Status)
			{
			case SkillStatus.Error:
				return "内部错误";
			case SkillStatus.NoBalls:
				return "怒气不足";
			case SkillStatus.NoCd:
				return "冷却中";
			case SkillStatus.NoMp:
				return "内力不足";
			case SkillStatus.Seal:
				return "被封印";
			case SkillStatus.Ok:
				return "正常";
			default:
				return "内部错误";
			}
		}

		public virtual List<LocationBlock> GetSkillCastBlocks(int x, int y)
		{
			List<LocationBlock> list = new List<LocationBlock>();
			if (CoverType == SkillCoverType.LINE || CoverType == SkillCoverType.FAN || CoverType == SkillCoverType.FRONT)
			{
				list.Add(new LocationBlock
				{
					X = x + 1,
					Y = y
				});
				list.Add(new LocationBlock
				{
					X = x,
					Y = y + 1
				});
				list.Add(new LocationBlock
				{
					X = x + -1,
					Y = y
				});
				list.Add(new LocationBlock
				{
					X = x,
					Y = y - 1
				});
				return list;
			}
			int castSize = GetCastSize();
			for (int i = -castSize; i <= castSize; i++)
			{
				for (int j = -castSize; j <= castSize; j++)
				{
					if ((CoverType != SkillCoverType.NORMAL || HitSelf || i != 0 || j != 0) && Math.Abs(i) + Math.Abs(j) <= castSize)
					{
						list.Add(new LocationBlock
						{
							X = x + i,
							Y = y + j
						});
					}
				}
			}
			return list;
		}

		public virtual List<LocationBlock> GetSkillCoverBlocks(int x, int y, int spx, int spy)
		{
			return new SkillCoverTypeHelper(CoverType).GetSkillCoverBlocks(x, y, spx, spy, GetSize());
		}

		private int GetSize()
		{
			int num = Size;
			if (Owner == null || Owner.Sprite == null)
			{
				Debug.Log("error, skill owner or owner.sprite ==null");
				return num;
			}
			BuffInstance buff = Owner.Sprite.GetBuff("致盲");
			if (buff != null && !Owner.HasTalent("心眼通明"))
			{
				int num2 = num;
				num -= (int)((double)buff.Level * 1.5);
				if (num <= 0)
				{
					num = ((num2 > 0) ? 1 : 0);
				}
			}
			if (Owner.HasTalent("寸长寸强"))
			{
				num++;
			}
			return LuaManager.CallWithIntReturn("SKILL_getSize", this, Owner, num);
		}

		private int GetCastSize()
		{
			int num = CastSize;
			if (Owner.HasTalent("吴钩霜雪") && Name == "太玄神功")
			{
				num += 3;
			}
			if (Owner.HasTalent("金刚伏魔圈") && Name == "日月神鞭")
			{
				return 10;
			}
			if (num != 0)
			{
				BuffInstance buff = Owner.Sprite.GetBuff("致盲");
				if (buff != null && !Owner.HasTalent("心眼通明"))
				{
					num -= (int)((double)buff.Level * 1.5);
					if (num <= 0)
					{
						num = 1;
					}
				}
			}
			return LuaManager.CallWithIntReturn("SKILL_getCastSize", this, Owner, num);
		}

		public virtual bool TryAddExp(int exp)
		{
			return false;
		}
	}
}
