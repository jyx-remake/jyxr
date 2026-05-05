using UnityEngine;

namespace JyGame
{
	public class AttackCastInfo
	{
		public string info;

		public float property;

		public Color color;

		public AttackCastInfoType type;

		public BattleSprite sprite;

		public AttackCastInfo(BattleSprite sprite, string info, float property, Color color, AttackCastInfoType type = AttackCastInfoType.ATTACK_TEXT)
		{
			this.sprite = sprite;
			this.info = info;
			this.property = property;
			this.color = color;
			this.type = type;
		}
	}
}
