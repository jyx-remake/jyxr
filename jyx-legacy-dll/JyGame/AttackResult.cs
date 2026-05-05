using System.Collections.Generic;
using UnityEngine;

namespace JyGame
{
	public class AttackResult
	{
		public int Hp;

		public int Mp;

		public int costBall;

		public bool Critical;

		public int repeatTime = 1;

		public List<Buff> Debuff = new List<Buff>();

		public List<Buff> Buff = new List<Buff>();

		public int targetX;

		public int targetY;

		public List<AttackCastInfo> castInfo = new List<AttackCastInfo>();

		public void AddCastInfo(BattleSprite sprite, string info, float property = 1f)
		{
			castInfo.Add(new AttackCastInfo(sprite, info, property, Color.white, AttackCastInfoType.SMALL_DIALOG));
		}

		public void AddCastInfo(BattleSprite sprite, string[] infos, float property = 1f)
		{
			foreach (string info in infos)
			{
				castInfo.Add(new AttackCastInfo(sprite, info, property / (float)infos.Length, Color.white, AttackCastInfoType.SMALL_DIALOG));
			}
		}

		public void AddAttackInfo(BattleSprite sprite, string info, Color color)
		{
			castInfo.Add(new AttackCastInfo(sprite, info, 1f, color));
		}
	}
}
