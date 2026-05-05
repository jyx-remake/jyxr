using System;

namespace JyGame
{
	public class BuffInstance
	{
		public BattleSprite Owner;

		public Buff buff;

		private int _level = -1;

		public int LeftRound;

		public int TimeStamp;

		public string Name
		{
			get
			{
				return buff.Name;
			}
		}

		public int Level
		{
			get
			{
				return (_level != -1) ? _level : buff.Level;
			}
			set
			{
				_level = value;
			}
		}

		public bool IsDebuff
		{
			get
			{
				return buff.IsDebuff;
			}
		}

		public override string ToString()
		{
			return buff.Name + " ";
		}

		public string Info()
		{
			string empty = string.Empty;
			if (buff.Level > 0)
			{
				string text = empty;
				empty = text + "程度:" + Level + "\n";
			}
			else
			{
				empty += "程度:\n";
			}
			return empty + "持续时间:" + LeftRound + "回合";
		}

		public RoundBuffResult RoundEffect()
		{
			RoundBuffResult roundBuffResult = new RoundBuffResult();
			roundBuffResult.buff = this;
			switch (buff.Name)
			{
			case "中毒":
			{
				int num3 = Math.Min(Owner.Role.dingli, 200);
				int num4 = (int)((double)(35 * Level) * (1.0 - (double)(num3 / 200) * 0.5) * Tools.GetRandom(0.5, 1.0));
				if (num4 <= 0)
				{
					num4 = 1;
				}
				if (Owner.Hp - num4 < 0)
				{
					num4 = Owner.Hp - 1;
					Owner.Hp = 1;
				}
				if (Owner.Role.HasTalent("毒素抗性"))
				{
					num4 /= 2;
				}
				roundBuffResult.AddHp = -num4;
				break;
			}
			case "恢复":
			{
				int num = (int)((double)(Owner.Role.gengu / 3 * Level) * (1.0 + Tools.GetRandom(0.0, 0.5)));
				if (Owner.Hp + num > Owner.MaxHp)
				{
					num = Owner.MaxHp - Owner.Hp;
				}
				Owner.Hp += num;
				roundBuffResult.AddHp = num;
				break;
			}
			case "内伤":
			{
				int num2 = (int)((double)((150 - Owner.Role.dingli) / 4 * Level) * (1.0 + Tools.GetRandom(0.0, 0.5)));
				if (Owner.Mp - num2 < 0)
				{
					num2 = Owner.Mp;
				}
				Owner.Mp -= num2;
				roundBuffResult.AddMp = -num2;
				break;
			}
			case "集气":
				if (Tools.ProbabilityTest(0.15 + 0.2 * (double)Level))
				{
					Owner.Balls++;
					roundBuffResult.AddBall = 1;
				}
				break;
			default:
				LuaManager.Call("BUFF_OnRoundBuff", this, roundBuffResult);
				break;
			}
			return roundBuffResult;
		}
	}
}
