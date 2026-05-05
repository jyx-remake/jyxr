namespace JyGame
{
	public class AttributeFinalHelper
	{
		private Role owner;

		public int this[string key]
		{
			get
			{
				int num = owner.Attributes[key] + owner.GetAdditionAttribute(key);
				if (key == "jianfa" && owner.HasTalent("浪子剑客"))
				{
					num = (int)((double)num * 1.2);
				}
				if (key == "quanzhang" && owner.HasTalent("神拳无敌"))
				{
					num = (int)((double)num * 1.1);
				}
				if (key == "quanzhang" && owner.HasTalent("拳掌增益"))
				{
					num = (int)((double)num * 1.05);
				}
				if (key == "jianfa" && owner.HasTalent("剑法增益"))
				{
					num = (int)((double)num * 1.05);
				}
				if (key == "daofa" && owner.HasTalent("刀法增益"))
				{
					num = (int)((double)num * 1.05);
				}
				if (key == "qimen" && owner.HasTalent("奇门增益"))
				{
					num = (int)((double)num * 1.05);
				}
				return num;
			}
		}

		public AttributeFinalHelper(Role Owner)
		{
			owner = Owner;
		}
	}
}
