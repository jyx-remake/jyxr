using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	public class AoyiInstance : SkillBox
	{
		private SkillBox _parent;

		private Aoyi _aoyi;

		public override string Name
		{
			get
			{
				return _aoyi.Name;
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
				return 0;
			}
		}

		public override int CastSize
		{
			get
			{
				return _parent.CastSize;
			}
		}

		public override int Size
		{
			get
			{
				return _parent.Size;
			}
		}

		public override SkillCoverType CoverType
		{
			get
			{
				return _parent.CoverType;
			}
		}

		public override string Animation
		{
			get
			{
				return _parent.Animation;
			}
		}

		public override SkillType SkillType
		{
			get
			{
				return SkillType.Aoyi;
			}
		}

		public override int Level
		{
			get
			{
				return _parent.Level;
			}
		}

		public override int MaxLevel
		{
			get
			{
				return _parent.MaxLevel;
			}
		}

		public override bool Tiaohe
		{
			get
			{
				return _parent.Tiaohe;
			}
		}

		public override float Power
		{
			get
			{
				int num = 0;
				Trigger trigger = null;
				try
				{
					if (base.Owner != null)
					{
						foreach (Trigger trigger2 in base.Owner.GetTriggers("powerup_aoyi"))
						{
							if (trigger2.Argvs[0] == Name)
							{
								trigger = trigger2;
								num += int.Parse(trigger2.Argvs[1]);
							}
						}
					}
				}
				catch
				{
					Debug.LogError("奥义数据错误:" + Name + "," + trigger.Argvs);
				}
				return (float)((double)(_parent.Power + _aoyi.addPower) * (1.0 + (double)(float)num / 100.0));
			}
		}

		public override float Suit
		{
			get
			{
				return _parent.Suit;
			}
		}

		[XmlIgnore]
		public override IEnumerable<Buff> Buffs
		{
			get
			{
				return _aoyi.Buffs;
			}
		}

		public override string Audio
		{
			get
			{
				return _parent.Audio;
			}
		}

		public AoyiInstance(SkillBox parent, Aoyi aoyi)
		{
			_parent = parent;
			_aoyi = aoyi;
			base.Owner = _parent.Owner;
			ScreenEffect = _aoyi.animation;
		}

		public override bool TryAddExp(int exp)
		{
			return _parent.TryAddExp(exp);
		}
	}
}
