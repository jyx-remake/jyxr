namespace JyGame
{
	public class AttributeHelper
	{
		private Role owner;

		private RoleGrowTemplate template;

		public int this[string key]
		{
			get
			{
				if (owner != null)
				{
					switch (key)
					{
					case "hp":
						return owner.hp;
					case "maxhp":
						return owner.maxhp;
					case "mp":
						return owner.mp;
					case "maxmp":
						return owner.maxmp;
					case "gengu":
						return owner.gengu;
					case "bili":
						return owner.bili;
					case "fuyuan":
						return owner.fuyuan;
					case "shenfa":
						return owner.shenfa;
					case "dingli":
						return owner.dingli;
					case "wuxing":
						return owner.wuxing;
					case "quanzhang":
						return owner.quanzhang;
					case "jianfa":
						return owner.jianfa;
					case "daofa":
						return owner.daofa;
					case "qimen":
						return owner.qimen;
					case "female":
						return owner.FemaleValue;
					case "wuxue":
						return owner.wuxue;
					default:
						return -1;
					}
				}
				if (template != null)
				{
					switch (key)
					{
					case "hp":
						return template.hp;
					case "mp":
						return template.mp;
					case "gengu":
						return template.gengu;
					case "bili":
						return template.bili;
					case "fuyuan":
						return template.fuyuan;
					case "shenfa":
						return template.shenfa;
					case "dingli":
						return template.dingli;
					case "wuxing":
						return template.wuxing;
					case "quanzhang":
						return template.quanzhang;
					case "jianfa":
						return template.jianfa;
					case "daofa":
						return template.daofa;
					case "qimen":
						return template.qimen;
					case "wuxue":
						return template.wuxue;
					default:
						return -1;
					}
				}
				return -1;
			}
		}

		public AttributeHelper(Role Owner)
		{
			owner = Owner;
		}

		public AttributeHelper(RoleGrowTemplate template)
		{
			this.template = template;
		}
	}
}
