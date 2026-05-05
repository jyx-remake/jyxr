using System.Xml.Serialization;

namespace JyGame
{
	[XmlType]
	public class GameSaveRoleSkill
	{
		[XmlAttribute]
		public int equipped;

		[XmlAttribute]
		public int level;

		[XmlAttribute]
		public int exp;

		[XmlAttribute]
		public string name;

		public SkillInstance GenerateSkill()
		{
			SkillInstance skillInstance = new SkillInstance();
			skillInstance.equipped = equipped;
			skillInstance.level = level;
			skillInstance.Exp = exp;
			skillInstance.name = name;
			skillInstance.RefreshUniquSkills();
			return skillInstance;
		}

		public InternalSkillInstance GenerateInternalSkill()
		{
			InternalSkillInstance internalSkillInstance = new InternalSkillInstance();
			internalSkillInstance.equipped = equipped;
			internalSkillInstance.level = level;
			internalSkillInstance.Exp = exp;
			internalSkillInstance.name = name;
			internalSkillInstance.RefreshUniquSkills();
			return internalSkillInstance;
		}

		public SpecialSkillInstance GenerateSpecialSkill()
		{
			SpecialSkillInstance specialSkillInstance = new SpecialSkillInstance();
			specialSkillInstance.equipped = equipped;
			specialSkillInstance.name = name;
			return specialSkillInstance;
		}

		public static GameSaveRoleSkill Create(SkillInstance s)
		{
			GameSaveRoleSkill gameSaveRoleSkill = new GameSaveRoleSkill();
			gameSaveRoleSkill.name = s.Name;
			gameSaveRoleSkill.level = s.level;
			gameSaveRoleSkill.exp = s.Exp;
			gameSaveRoleSkill.equipped = s.equipped;
			return gameSaveRoleSkill;
		}

		public static GameSaveRoleSkill Create(InternalSkillInstance s)
		{
			GameSaveRoleSkill gameSaveRoleSkill = new GameSaveRoleSkill();
			gameSaveRoleSkill.name = s.Name;
			gameSaveRoleSkill.level = s.level;
			gameSaveRoleSkill.exp = s.Exp;
			gameSaveRoleSkill.equipped = s.equipped;
			return gameSaveRoleSkill;
		}

		public static GameSaveRoleSkill Create(SpecialSkillInstance s)
		{
			GameSaveRoleSkill gameSaveRoleSkill = new GameSaveRoleSkill();
			gameSaveRoleSkill.name = s.Name;
			gameSaveRoleSkill.equipped = s.equipped;
			return gameSaveRoleSkill;
		}
	}
}
