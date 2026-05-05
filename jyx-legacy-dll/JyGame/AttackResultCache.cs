using System.Collections.Generic;

namespace JyGame
{
	public class AttackResultCache
	{
		private BattleSprite source;

		private BattleField field;

		private Dictionary<SkillBox, Dictionary<BattleSprite, AttackResult>> cache = new Dictionary<SkillBox, Dictionary<BattleSprite, AttackResult>>();

		public AttackResultCache(BattleSprite source, BattleField field)
		{
			this.source = source;
			this.field = field;
		}

		public AttackResult GetAttackResult(SkillBox skill, BattleSprite target)
		{
			if (cache.ContainsKey(skill) && cache[skill].ContainsKey(target))
			{
				return cache[skill][target];
			}
			AttackResult attackResult = AttackLogic.GetAttackResult(skill, source, target, field);
			if (!cache.ContainsKey(skill))
			{
				cache.Add(skill, new Dictionary<BattleSprite, AttackResult>());
			}
			cache[skill].Add(target, attackResult);
			return attackResult;
		}
	}
}
