using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType]
	public class GameSaveRole
	{
		[XmlAttribute]
		public string key;

		[XmlAttribute]
		public string animation;

		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public string head;

		[XmlAttribute]
		public int maxhp;

		[XmlAttribute]
		public int maxmp;

		[XmlAttribute]
		public int wuxing;

		[XmlAttribute]
		public int shenfa;

		[XmlAttribute]
		public int bili;

		[XmlAttribute]
		public int gengu;

		[XmlAttribute]
		public int fuyuan;

		[XmlAttribute]
		public int dingli;

		[XmlAttribute]
		public int quanzhang;

		[XmlAttribute]
		public int jianfa;

		[XmlAttribute]
		public int daofa;

		[XmlAttribute]
		public int qimen;

		[XmlAttribute]
		public string currentSkillName;

		[XmlAttribute]
		public int exp;

		[XmlAttribute]
		public int female;

		[XmlAttribute]
		public int leftpoint;

		[XmlAttribute]
		public string grow_template;

		[XmlAttribute]
		public int level;

		[XmlAttribute]
		public string talent;

		[XmlElement("sk")]
		public GameSaveRoleSkill[] skills;

		[XmlElement("i")]
		public GameSaveRoleSkill[] internalSkills;

		[XmlElement("sp")]
		public GameSaveRoleSkill[] specialSkills;

		[XmlElement("e")]
		public GameSaveItem[] equippments;

		public Role GenerateRole()
		{
			Role role = new Role();
			role.Key = key;
			role.Animation = animation;
			role.Name = name;
			role.Head = head;
			role.maxhp = maxhp;
			role.hp = maxhp;
			role.maxmp = maxmp;
			role.mp = maxmp;
			role.wuxing = wuxing;
			role.shenfa = shenfa;
			role.bili = bili;
			role.gengu = gengu;
			role.fuyuan = fuyuan;
			role.dingli = dingli;
			role.quanzhang = quanzhang;
			role.jianfa = jianfa;
			role.daofa = daofa;
			role.qimen = qimen;
			role.currentSkillName = currentSkillName;
			role.FemaleValue = female;
			role.leftpoint = leftpoint;
			role.GrowTemplateValue = grow_template;
			role.level = level;
			role.exp = exp;
			role.TalentValue = talent;
			role.Skills = GetSkills();
			role.InternalSkills = GetInternalSkills();
			role.SpecialSkills = GetSpecialSkills();
			if (equippments != null)
			{
				GameSaveItem[] array = equippments;
				foreach (GameSaveItem gameSaveItem in array)
				{
					role.Equipment.Add(gameSaveItem.Generate());
				}
			}
			role.InitBind();
			return role;
		}

		public static GameSaveRole[] Create(List<Role> roles)
		{
			GameSaveRole[] array = new GameSaveRole[roles.Count];
			for (int i = 0; i < roles.Count; i++)
			{
				array[i] = Create(roles[i]);
			}
			return array;
		}

		public static GameSaveRole Create(Role r)
		{
			GameSaveRole gameSaveRole = new GameSaveRole();
			gameSaveRole.key = r.Key;
			gameSaveRole.animation = r.Animation;
			gameSaveRole.name = r.Name;
			gameSaveRole.head = r.Head;
			gameSaveRole.maxhp = r.maxhp;
			gameSaveRole.maxmp = r.maxmp;
			gameSaveRole.wuxing = r.wuxing;
			gameSaveRole.shenfa = r.shenfa;
			gameSaveRole.bili = r.bili;
			gameSaveRole.gengu = r.gengu;
			gameSaveRole.fuyuan = r.fuyuan;
			gameSaveRole.dingli = r.dingli;
			gameSaveRole.quanzhang = r.quanzhang;
			gameSaveRole.jianfa = r.jianfa;
			gameSaveRole.daofa = r.daofa;
			gameSaveRole.qimen = r.qimen;
			gameSaveRole.currentSkillName = r.currentSkillName;
			gameSaveRole.exp = r.exp;
			gameSaveRole.female = r.FemaleValue;
			gameSaveRole.leftpoint = r.leftpoint;
			gameSaveRole.grow_template = r.GrowTemplateValue;
			gameSaveRole.level = r.level;
			gameSaveRole.talent = r.TalentValue;
			if (r.Skills.Count > 0)
			{
				gameSaveRole.skills = new GameSaveRoleSkill[r.Skills.Count];
				for (int i = 0; i < r.Skills.Count; i++)
				{
					gameSaveRole.skills[i] = GameSaveRoleSkill.Create(r.Skills[i]);
				}
			}
			if (r.InternalSkills.Count > 0)
			{
				gameSaveRole.internalSkills = new GameSaveRoleSkill[r.InternalSkills.Count];
				for (int j = 0; j < r.InternalSkills.Count; j++)
				{
					gameSaveRole.internalSkills[j] = GameSaveRoleSkill.Create(r.InternalSkills[j]);
				}
			}
			if (r.SpecialSkills.Count > 0)
			{
				gameSaveRole.specialSkills = new GameSaveRoleSkill[r.SpecialSkills.Count];
				for (int k = 0; k < r.SpecialSkills.Count; k++)
				{
					gameSaveRole.specialSkills[k] = GameSaveRoleSkill.Create(r.SpecialSkills[k]);
				}
			}
			if (r.Equipment.Count > 0)
			{
				gameSaveRole.equippments = new GameSaveItem[r.Equipment.Count];
				for (int l = 0; l < r.Equipment.Count; l++)
				{
					ItemInstance itemInstance = r.Equipment[l];
					GameSaveItem gameSaveItem = new GameSaveItem();
					gameSaveItem.name = itemInstance.Name;
					gameSaveItem.triggers = itemInstance.AdditionTriggers.ToArray();
					gameSaveItem.count = 1;
					GameSaveItem gameSaveItem2 = gameSaveItem;
					gameSaveRole.equippments[l] = gameSaveItem2;
				}
			}
			return gameSaveRole;
		}

		public List<SkillInstance> GetSkills()
		{
			List<SkillInstance> list = new List<SkillInstance>();
			if (skills == null)
			{
				return list;
			}
			GameSaveRoleSkill[] array = skills;
			foreach (GameSaveRoleSkill gameSaveRoleSkill in array)
			{
				list.Add(gameSaveRoleSkill.GenerateSkill());
			}
			return list;
		}

		public List<InternalSkillInstance> GetInternalSkills()
		{
			List<InternalSkillInstance> list = new List<InternalSkillInstance>();
			if (internalSkills == null)
			{
				return list;
			}
			GameSaveRoleSkill[] array = internalSkills;
			foreach (GameSaveRoleSkill gameSaveRoleSkill in array)
			{
				list.Add(gameSaveRoleSkill.GenerateInternalSkill());
			}
			return list;
		}

		public List<SpecialSkillInstance> GetSpecialSkills()
		{
			List<SpecialSkillInstance> list = new List<SpecialSkillInstance>();
			if (specialSkills == null)
			{
				return list;
			}
			GameSaveRoleSkill[] array = specialSkills;
			foreach (GameSaveRoleSkill gameSaveRoleSkill in array)
			{
				list.Add(gameSaveRoleSkill.GenerateSpecialSkill());
			}
			return list;
		}
	}
}
