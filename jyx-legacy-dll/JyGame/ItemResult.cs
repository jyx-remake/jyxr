using System.Collections.Generic;
using XLua;


namespace JyGame
{
	public class ItemResult
	{
		public int Hp;

		public int Mp;

		public int DescPoisonLevel;

		public int DescPoisonDuration;

		public int Balls;

		public int MaxHp;

		public int MaxMp;

		public string UpgradeSkill = string.Empty;

		public string UpgradeInternalSkill = string.Empty;

		public IEnumerable<Buff> Buffs;

		public LuaTable data = LuaTool.CreateLuaTable();
	}
}
