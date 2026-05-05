using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("role")]
	public class Role : BasePojo
	{
		[XmlAttribute("key")]
		public string Key;

		[XmlAttribute("animation")]
		public string Animation;

		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("head")]
		public string Head;

		[XmlAttribute]
		public int hp;

		[XmlAttribute]
		public int maxhp;

		[XmlAttribute]
		public int mp;

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
		public string currentSkillName = string.Empty;

		private int _level;

		[XmlAttribute]
		public int exp;

		[XmlAttribute("arena")]
		public string ArenaValue;

		[XmlAttribute("female")]
		public int FemaleValue;

		[XmlIgnore]
		public List<string> Talents = new List<string>();

		[XmlAttribute]
		public int leftpoint;

		[XmlAttribute("grow_template")]
		public string GrowTemplateValue = "default";

		[XmlArrayItem(typeof(SkillInstance))]
		[XmlArray("skills")]
		public List<SkillInstance> Skills = new List<SkillInstance>();

		[XmlArrayItem(typeof(InternalSkillInstance))]
		[XmlArray("internal_skills")]
		public List<InternalSkillInstance> InternalSkills = new List<InternalSkillInstance>();

		[XmlArrayItem(typeof(SpecialSkillInstance))]
		[XmlArray("special_skills")]
		public List<SpecialSkillInstance> SpecialSkills = new List<SpecialSkillInstance>();

		[XmlIgnore]
		public int balls;

		[XmlArray("items")]
		[XmlArrayItem(typeof(ItemInstance))]
		public List<ItemInstance> Equipment = new List<ItemInstance>();

		[XmlIgnore]
		public AttributeFinalHelper AttributesFinal;

		[XmlIgnore]
		public AttributeHelper Attributes;

		[XmlIgnore]
		public BattleSprite Sprite;

		[XmlIgnore]
		public override string PK
		{
			get
			{
				return Key;
			}
		}

		[XmlIgnore]
		public SkillBox CurrentSkill
		{
			get
			{
				SkillBox skillBox = null;
				foreach (SkillBox avaliableSkill in GetAvaliableSkills())
				{
					if (skillBox == null)
					{
						skillBox = avaliableSkill;
					}
					if (string.IsNullOrEmpty(currentSkillName) && avaliableSkill.SkillType == SkillType.Normal)
					{
						return avaliableSkill;
					}
					if (avaliableSkill.Name == currentSkillName)
					{
						return avaliableSkill;
					}
				}
				return skillBox;
			}
			set
			{
				currentSkillName = value.Name;
			}
		}

		[XmlIgnore]
		public int wuxue
		{
			get
			{
				return 20 + Level * GrowTemplate.Attributes["wuxue"];
			}
		}

		[XmlAttribute]
		public int level
		{
			get
			{
				return _level;
			}
			set
			{
				_level = value;
				exp = PrevLevelupExp;
			}
		}

		public int Level
		{
			get
			{
				return level;
			}
		}

		[XmlIgnore]
		public int Exp
		{
			get
			{
				return exp;
			}
			set
			{
				exp = value;
			}
		}

		public bool Arena
		{
			get
			{
				return ArenaValue != "no";
			}
		}

		public bool Female
		{
			get
			{
				return FemaleValue == 1;
			}
		}

		[XmlAttribute("talent")]
		public string TalentValue
		{
			get
			{
				return string.Join("#", Talents.ToArray());
			}
			set
			{
				Talents.Clear();
				string[] array = value.Split('#');
				foreach (string text in array)
				{
					if (!string.IsNullOrEmpty(text))
					{
						Talents.Add(text);
					}
				}
			}
		}

		[XmlIgnore]
		public RoleGrowTemplate GrowTemplate
		{
			get
			{
				RoleGrowTemplate roleGrowTemplate = ResourceManager.Get<RoleGrowTemplate>(GrowTemplateValue);
				if (roleGrowTemplate == null)
				{
					roleGrowTemplate = ResourceManager.Get<RoleGrowTemplate>("default");
				}
				return roleGrowTemplate;
			}
		}

		[XmlIgnore]
		public int LevelupExp
		{
			get
			{
				return CommonSettings.LevelupExp(Level);
			}
		}

		[XmlIgnore]
		public int PrevLevelupExp
		{
			get
			{
				return CommonSettings.LevelupExp(Level - 1);
			}
		}

		[XmlIgnore]
		public IEnumerable<string> EquipmentTalents
		{
			get
			{
				List<string> visitedTalent = new List<string>();
				foreach (Trigger t in GetTriggers("talent"))
				{
					string talentName = t.Argvs[0];
					if (t.Name == "talent" && !visitedTalent.Contains(talentName))
					{
						yield return talentName;
						visitedTalent.Add(talentName);
					}
				}
				foreach (ItemInstance t2 in Equipment)
				{
					if (t2 == null)
					{
						continue;
					}
					string[] talents = t2.Talents;
					foreach (string talentName2 in talents)
					{
						if (!visitedTalent.Contains(talentName2))
						{
							yield return talentName2;
							visitedTalent.Add(talentName2);
						}
					}
				}
			}
		}

		[XmlIgnore]
		public bool Animal
		{
			get
			{
				return Attributes["female"] == -1;
			}
		}

		[XmlIgnore]
		public double Defence
		{
			get
			{
				double num = 150.0 + (10.0 + (double)AttributesFinal["dingli"] / 40.0 + (double)AttributesFinal["gengu"] / 70.0) * 8.0 * (double)(1f + GetEquippedInternalSkill().Defence);
				foreach (Trigger trigger in GetTriggers("defence"))
				{
					num += double.Parse(trigger.Argvs[0]);
				}
				double num2 = AttackLogic.defenceDescAttack(num);
				return (double)maxhp / (1.0 - num2) / 30.0;
			}
		}

		[XmlIgnore]
		public double Attack
		{
			get
			{
				double num = 1.0;
				num *= 4.0 + (double)AttributesFinal["bili"] / 120.0;
				num *= 2.0 + (double)GetMaxSkillTypeValue() / 200.0;
				num *= (double)(1f + GetEquippedInternalSkill().Attack);
				foreach (Trigger trigger in GetTriggers("attack"))
				{
					num += double.Parse(trigger.Argvs[0]) / 35.0;
				}
				double num2 = (double)AttributesFinal["fuyuan"] / 50.0 / 20.0 * (double)(1f + GetEquippedInternalSkill().Critical) + (double)CriticalProbabilityAdd();
				if (num2 > 1.0)
				{
					num2 = 1.0;
				}
				int num3 = 0;
				foreach (Trigger trigger2 in GetTriggers("critical"))
				{
					num3 += int.Parse(trigger2.Argvs[0]);
				}
				num *= 1.0 + num2 * (1.5 + (double)num3 / 100.0);
				double num4 = 0.0;
				foreach (SkillInstance skill in Skills)
				{
					if ((double)skill.Power > num4)
					{
						num4 = skill.Power;
					}
				}
				if (num4 == 0.0)
				{
					foreach (UniqueSkillInstance uniqueSkill in GetEquippedInternalSkill().UniqueSkills)
					{
						if ((double)uniqueSkill.Power > num4)
						{
							num4 = uniqueSkill.Power;
						}
					}
				}
				return num * num4;
			}
		}

		public Role()
		{
			AttributesFinal = new AttributeFinalHelper(this);
			Attributes = new AttributeHelper(this);
		}

		public static Role Generate(string key)
		{
			return ResourceManager.Get<Role>(key).Clone();
		}

		public override void InitBind()
		{
			foreach (SkillInstance skill in Skills)
			{
				skill.Owner = this;
				skill.RefreshUniquSkills();
				foreach (UniqueSkillInstance uniqueSkill in skill.UniqueSkills)
				{
					uniqueSkill.Owner = this;
				}
			}
			foreach (InternalSkillInstance internalSkill in InternalSkills)
			{
				internalSkill.Owner = this;
				internalSkill.RefreshUniquSkills();
				foreach (UniqueSkillInstance uniqueSkill2 in internalSkill.UniqueSkills)
				{
					uniqueSkill2.Owner = this;
				}
			}
			foreach (SpecialSkillInstance specialSkill in SpecialSkills)
			{
				specialSkill.Owner = this;
			}
			hp = maxhp;
			mp = maxmp;
		}

		public string GetAnimation()
		{
			using (IEnumerator<Trigger> enumerator = GetTriggers("animation").GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					Trigger current = enumerator.Current;
					return current.Argvs[1];
				}
			}
			return Animation;
		}

		public ItemInstance GetEquipment(ItemType type)
		{
			return GetEquipment((int)type);
		}

		public ItemInstance GetEquipment(int type)
		{
			if (Equipment == null)
			{
				return null;
			}
			foreach (ItemInstance item in Equipment)
			{
				if (item.type == type)
				{
					return item;
				}
			}
			return null;
		}

		public Role Clone()
		{
			return BasePojo.Create<Role>(Xml);
		}

		public void Reset(bool recover = true)
		{
			if (recover)
			{
				hp = Attributes["maxhp"];
				mp = Attributes["maxmp"];
			}
			else
			{
				if (Attributes["hp"] <= 0)
				{
					hp = 1;
				}
				if (Attributes["mp"] <= 0)
				{
					mp = 0;
				}
			}
			balls = 0;
			SkillCdRecover();
		}

		public void SkillCdRecover()
		{
			foreach (SkillInstance skill in Skills)
			{
				skill.CurrentCd = 0;
				foreach (UniqueSkillInstance uniqueSkill in skill.UniqueSkills)
				{
					uniqueSkill.CurrentCd = 0;
				}
			}
			foreach (InternalSkillInstance internalSkill in InternalSkills)
			{
				foreach (UniqueSkillInstance uniqueSkill2 in internalSkill.UniqueSkills)
				{
					uniqueSkill2.CurrentCd = 0;
				}
			}
			foreach (SpecialSkillInstance specialSkill in SpecialSkills)
			{
				specialSkill.CurrentCd = 0;
			}
		}

		public void AddTalent(string talent)
		{
			Talents.Add(talent);
		}

		public bool RemoveTalent(string talent)
		{
			if (Talents.Contains(talent))
			{
				Talents.Remove(talent);
				return true;
			}
			return false;
		}

		public bool HasTalent(string talent)
		{
			if (Talents.Contains(talent))
			{
				return true;
			}
			if (EquipmentTalents.Contains(talent))
			{
				return true;
			}
			InternalSkillInstance equippedInternalSkill = GetEquippedInternalSkill();
			if (equippedInternalSkill != null && equippedInternalSkill.HasTalent(talent))
			{
				return true;
			}
			return false;
		}

		public InternalSkillInstance GetEquippedInternalSkill()
		{
			foreach (InternalSkillInstance internalSkill in InternalSkills)
			{
				if (internalSkill.IsUsed)
				{
					return internalSkill;
				}
			}
			return null;
		}

		public void SetEquippedInternalSkill(InternalSkillInstance skill)
		{
			foreach (InternalSkillInstance internalSkill in InternalSkills)
			{
				internalSkill.equipped = 0;
			}
			skill.equipped = 1;
		}

		public IEnumerable<SkillBox> GetAvaliableSkills()
		{
			foreach (SpecialSkillInstance ss in SpecialSkills)
			{
				if (ss.IsUsed)
				{
					yield return ss;
				}
			}
			foreach (SkillInstance ss2 in Skills)
			{
				if (!ss2.IsUsed)
				{
					continue;
				}
				yield return ss2;
				foreach (UniqueSkillInstance us in ss2.UniqueSkills)
				{
					if (ss2.level >= us.UniqueSkill.RequireLevel)
					{
						yield return us;
					}
				}
			}
			InternalSkillInstance EquippedInternalSkill = GetEquippedInternalSkill();
			if (EquippedInternalSkill == null)
			{
				yield break;
			}
			foreach (UniqueSkillInstance us2 in EquippedInternalSkill.UniqueSkills)
			{
				if (EquippedInternalSkill.level >= us2.UniqueSkill.RequireLevel)
				{
					yield return us2;
				}
			}
		}

		public IEnumerable<Trigger> GetTriggers(string name)
		{
			foreach (ItemInstance item in Equipment)
			{
				foreach (Trigger t in item.AllTriggers)
				{
					if (t.Name == name)
					{
						yield return t;
					}
				}
			}
			foreach (SkillInstance s in Skills)
			{
				foreach (Trigger t2 in s.Skill.Triggers)
				{
					if (s.level >= t2.Level && t2.Name == name)
					{
						yield return t2;
					}
				}
			}
			foreach (InternalSkillInstance s2 in InternalSkills)
			{
				foreach (Trigger t3 in s2.InternalSkill.Triggers)
				{
					if (s2.level >= t3.Level && t3.Name == name)
					{
						yield return t3;
					}
				}
			}
		}

		public IEnumerable<Trigger> GetAllTriggers()
		{
			foreach (ItemInstance item in Equipment)
			{
				foreach (Trigger allTrigger in item.AllTriggers)
				{
					yield return allTrigger;
				}
			}
			foreach (SkillInstance s in Skills)
			{
				foreach (Trigger t in s.Skill.Triggers)
				{
					if (s.level >= t.Level)
					{
						yield return t;
					}
				}
			}
			foreach (InternalSkillInstance s2 in InternalSkills)
			{
				foreach (Trigger t2 in s2.InternalSkill.Triggers)
				{
					if (s2.level >= t2.Level)
					{
						yield return t2;
					}
				}
			}
		}

		public int GetAdditionAttribute(string attribute)
		{
			string text = CommonSettings.AttributeToChinese(attribute);
			int num = 0;
			foreach (Trigger trigger in GetTriggers("attribute"))
			{
				if (trigger.Argvs[0] == text)
				{
					num += int.Parse(trigger.Argvs[1]);
				}
			}
			if (HasTalent("武学奇才"))
			{
				num *= 2;
			}
			return num;
		}

		public int GetSkillTypeValue()
		{
			return AttributesFinal["quanzhang"] + AttributesFinal["jianfa"] + AttributesFinal["daofa"] + AttributesFinal["qimen"];
		}

		public int GetMaxSkillTypeValue()
		{
			int[] source = new int[4]
			{
				AttributesFinal["quanzhang"],
				AttributesFinal["jianfa"],
				AttributesFinal["daofa"],
				AttributesFinal["qimen"]
			};
			return source.Max();
		}

		public string GetAttributeString(string attr)
		{
			return string.Format("{0}(+{1})", Attributes[attr], GetAdditionAttribute(attr));
		}

		public float CriticalProbabilityAdd()
		{
			float num = 0f;
			foreach (Trigger trigger in GetTriggers("criticalp"))
			{
				num += (float)((double)float.Parse(trigger.Argvs[0]) / 100.0);
			}
			foreach (Trigger trigger2 in GetTriggers("attack"))
			{
				num += (float)((double)int.Parse(trigger2.Argvs[1]) / 100.0);
			}
			return num;
		}

		public bool AddExp(int add)
		{
			Exp += add;
			ItemInstance equipment = GetEquipment(4);
			if (equipment != null)
			{
				ItemSkill itemSkill = equipment.GetItemSkill();
				if (itemSkill.IsInternal)
				{
					bool flag = false;
					foreach (InternalSkillInstance internalSkill in InternalSkills)
					{
						if (internalSkill.Name.Equals(itemSkill.SkillName))
						{
							flag = true;
						}
						if (internalSkill.Name.Equals(itemSkill.SkillName) && itemSkill.MaxLevel >= internalSkill.Level)
						{
							internalSkill.TryAddExp(add);
							flag = true;
						}
					}
					if (!flag && InternalSkills.Count < CommonSettings.MAX_INTERNALSKILL_COUNT)
					{
						InternalSkillInstance internalSkillInstance = new InternalSkillInstance();
						internalSkillInstance.name = itemSkill.SkillName;
						internalSkillInstance.level = 1;
						internalSkillInstance.equipped = 0;
						internalSkillInstance.Owner = this;
						InternalSkillInstance internalSkillInstance2 = internalSkillInstance;
						internalSkillInstance2.RefreshUniquSkills();
						InternalSkills.Add(internalSkillInstance2);
						internalSkillInstance2.TryAddExp(add);
					}
				}
				else
				{
					bool flag2 = false;
					foreach (SkillInstance skill in Skills)
					{
						if (skill.Name.Equals(itemSkill.SkillName))
						{
							flag2 = true;
						}
						if (skill.Skill.Name.Equals(itemSkill.SkillName) && itemSkill.MaxLevel >= skill.Level)
						{
							skill.TryAddExp(add);
							flag2 = true;
						}
					}
					if (!flag2 && Skills.Count < CommonSettings.MAX_SKILL_COUNT)
					{
						SkillInstance skillInstance = new SkillInstance();
						skillInstance.name = itemSkill.SkillName;
						skillInstance.level = 1;
						skillInstance.Owner = this;
						SkillInstance skillInstance2 = skillInstance;
						skillInstance2.RefreshUniquSkills();
						Skills.Add(skillInstance2);
						skillInstance2.TryAddExp(add);
					}
				}
			}
			bool result = false;
			if (Level >= CommonSettings.MAX_LEVEL)
			{
				Exp = 0;
			}
			while (Exp > LevelupExp && Level < CommonSettings.MAX_LEVEL)
			{
				level++;
				leftpoint += 2;
				maxhp += GrowTemplate.Attributes["hp"];
				if (maxhp > CommonSettings.MAX_HPMP)
				{
					maxhp = CommonSettings.MAX_HPMP;
				}
				maxmp += GrowTemplate.Attributes["mp"];
				if (maxmp > CommonSettings.MAX_HPMP)
				{
					maxmp = CommonSettings.MAX_HPMP;
				}
				if (Attributes["bili"] < CommonSettings.MAX_ATTRIBUTE)
				{
					bili += GrowTemplate.Attributes["bili"];
				}
				if (Attributes["fuyuan"] < CommonSettings.MAX_ATTRIBUTE)
				{
					fuyuan += GrowTemplate.Attributes["fuyuan"];
				}
				if (Attributes["gengu"] < CommonSettings.MAX_ATTRIBUTE)
				{
					gengu += GrowTemplate.Attributes["gengu"];
				}
				if (Attributes["dingli"] < CommonSettings.MAX_ATTRIBUTE)
				{
					dingli += GrowTemplate.Attributes["dingli"];
				}
				if (Attributes["shenfa"] < CommonSettings.MAX_ATTRIBUTE)
				{
					shenfa += GrowTemplate.Attributes["shenfa"];
				}
				if (Attributes["wuxing"] < CommonSettings.MAX_ATTRIBUTE)
				{
					wuxing += GrowTemplate.Attributes["wuxing"];
				}
				if (Attributes["quanzhang"] < CommonSettings.MAX_ATTRIBUTE)
				{
					quanzhang += GrowTemplate.Attributes["quanzhang"];
				}
				if (Attributes["jianfa"] < CommonSettings.MAX_ATTRIBUTE)
				{
					jianfa += GrowTemplate.Attributes["jianfa"];
				}
				if (Attributes["daofa"] < CommonSettings.MAX_ATTRIBUTE)
				{
					daofa += GrowTemplate.Attributes["daofa"];
				}
				if (Attributes["qimen"] < CommonSettings.MAX_ATTRIBUTE)
				{
					qimen += GrowTemplate.Attributes["qimen"];
				}
				if (bili > 300)
				{
					bili = 300;
				}
				if (fuyuan > 300)
				{
					fuyuan = 300;
				}
				if (gengu > 300)
				{
					gengu = 300;
				}
				if (dingli > 300)
				{
					dingli = 300;
				}
				if (shenfa > 300)
				{
					shenfa = 300;
				}
				if (wuxing > 300)
				{
					wuxing = 300;
				}
				if (quanzhang > 300)
				{
					quanzhang = 300;
				}
				if (jianfa > 300)
				{
					jianfa = 300;
				}
				if (daofa > 300)
				{
					daofa = 300;
				}
				if (qimen > 300)
				{
					qimen = 300;
				}
				result = true;
			}
			return result;
		}

		public bool CanLearnTalent(string t, ref int need)
		{
			int talentCost = Resource.GetTalentCost(t);
			int num = Attributes["wuxue"];
			int totalWuxueCost = GetTotalWuxueCost();
			need = talentCost;
			return talentCost + totalWuxueCost <= num;
		}

		public int GetTotalWuxueCost()
		{
			int num = 0;
			foreach (string talent in Talents)
			{
				num += Resource.GetTalentCost(talent);
			}
			return num;
		}

		public void addRoundSkillLevel()
		{
			int round = RuntimeData.Instance.Round;
			int num = round / LuaManager.GetConfigInt("NPC_SKILL_LEVEL_ADD_BY_ZHOUMU");
			if (num <= 0)
			{
				return;
			}
			foreach (SkillInstance skill in Skills)
			{
				skill.level += num;
				if (skill.Level > CommonSettings.MAX_SKILL_LEVEL)
				{
					skill.level = CommonSettings.MAX_SKILL_LEVEL;
				}
			}
			foreach (InternalSkillInstance internalSkill in InternalSkills)
			{
				internalSkill.level += num;
				if (internalSkill.Level > CommonSettings.MAX_INTERNALSKILL_LEVEL)
				{
					internalSkill.level = CommonSettings.MAX_INTERNALSKILL_LEVEL;
				}
			}
		}

		public void addRandomTalentAndWeapons()
		{
			addRandomTalent();
		}

		private void addRandomTalent()
		{
			string empty = string.Empty;
			int num = 0;
			if (RuntimeData.Instance.GameMode == "hard")
			{
				num = 1;
			}
			if (RuntimeData.Instance.GameMode == "crazy")
			{
				num = 3;
			}
			if (RuntimeData.Instance.GameMode == "crazy")
			{
				string enemyRandomTalentListCrazyAttack = CommonSettings.GetEnemyRandomTalentListCrazyAttack();
				string enemyRandomTalentListCrazyDefence = CommonSettings.GetEnemyRandomTalentListCrazyDefence();
				string enemyRandomTalentListCrazyOther = CommonSettings.GetEnemyRandomTalentListCrazyOther();
				Talents.Add(enemyRandomTalentListCrazyAttack);
				Talents.Add(enemyRandomTalentListCrazyDefence);
				Talents.Add(enemyRandomTalentListCrazyOther);
				return;
			}
			for (int i = 0; i < num; i++)
			{
				do
				{
					empty = CommonSettings.GetEnemyRandomTalent(Female);
				}
				while (HasTalent(empty));
				Talents.Add(empty);
			}
		}
	}
}
