using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	[XmlType("item")]
	public class ItemInstance : BasePojo
	{
		[XmlIgnore]
		private string _name;

		[XmlElement("addition_trigger")]
		public List<Trigger> AdditionTriggers = new List<Trigger>();

		[XmlIgnore]
		private Item _item;

		public override string PK
		{
			get
			{
				string text = type + "_" + Name;
				foreach (Trigger additionTrigger in AdditionTriggers)
				{
					text = text + "_" + additionTrigger.PK;
				}
				return text;
			}
		}

		[XmlAttribute("name")]
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
				_item = Item.GetItem(value);
			}
		}

		[XmlIgnore]
		public string desc
		{
			get
			{
				return item.desc;
			}
		}

		[XmlIgnore]
		public string talent
		{
			get
			{
				return item.talent;
			}
		}

		[XmlIgnore]
		public string pic
		{
			get
			{
				return item.pic;
			}
		}

		[XmlIgnore]
		public int level
		{
			get
			{
				return item.level;
			}
		}

		[XmlIgnore]
		public int price
		{
			get
			{
				return item.price;
			}
		}

		[XmlIgnore]
		public bool drop
		{
			get
			{
				return item.drop;
			}
		}

		[XmlIgnore]
		public string[] Talents
		{
			get
			{
				return talent.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
			}
		}

		[XmlIgnore]
		public string CanzhangSkill
		{
			get
			{
				return item.CanzhangSkill;
			}
		}

		[XmlIgnore]
		public List<Require> Requires
		{
			get
			{
				return item.Requires;
			}
		}

		[XmlIgnore]
		public List<Trigger> Triggers
		{
			get
			{
				return item.Triggers;
			}
		}

		[XmlIgnore]
		public int Cooldown
		{
			get
			{
				return item.Cooldown;
			}
		}

		[XmlIgnore]
		public IEnumerable<Trigger> AllTriggers
		{
			get
			{
				foreach (Trigger trigger in Triggers)
				{
					yield return trigger;
				}
				foreach (Trigger additionTrigger in AdditionTriggers)
				{
					yield return additionTrigger;
				}
			}
		}

		public string EquipCase
		{
			get
			{
				if (Requires == null)
				{
					return string.Empty;
				}
				string text = string.Empty;
				Dictionary<string, int> itemRequireAttrs = getItemRequireAttrs();
				foreach (string key in itemRequireAttrs.Keys)
				{
					string arg = CommonSettings.AttributeToChinese(key);
					text += string.Format("{0}>{1} ", arg, itemRequireAttrs[key]);
				}
				List<string> itemRequireTalents = getItemRequireTalents();
				foreach (string item in itemRequireTalents)
				{
					text += string.Format("具有天赋【" + item + "】 ");
				}
				return text.TrimEnd();
			}
		}

		[XmlIgnore]
		public string DescriptionInRichtext
		{
			get
			{
				string empty = string.Empty;
				empty = empty + "<color='black'>" + desc + "</color>";
				string equipCase = EquipCase;
				if (!equipCase.Equals(string.Empty))
				{
					empty = empty + "\n\n<color='red'>装备要求:\n" + equipCase + "</color>";
				}
				if (Triggers.Count > 0)
				{
					empty += "\n\n<color='green'>物品效果:\n";
					foreach (Trigger trigger in Triggers)
					{
						empty = empty + trigger.ToString() + "\n";
					}
					empty += "</color>";
				}
				if (Talents.Length > 0)
				{
					string[] talents = Talents;
					foreach (string name in talents)
					{
						empty = empty + "<color='blue'>" + Resource.GetTalentInfo(name, false) + "</color>\n";
					}
				}
				if (AdditionTriggers.Count > 0)
				{
					empty += "\n\n<color='green'>附加效果:\n";
					foreach (Trigger additionTrigger in AdditionTriggers)
					{
						empty = empty + additionTrigger.ToString() + "\n";
					}
					empty += "</color>";
				}
				if (Cooldown > 0 && RuntimeData.Instance.GameMode != "normal")
				{
					empty = empty + "\n冷却回合数 " + Cooldown;
				}
				return empty;
			}
		}

		[XmlIgnore]
		public string DescriptionInRichtextBlackEnd
		{
			get
			{
				return DescriptionInRichtext.Replace("black", "white").Replace("blue", "cyan").Replace("green", "yellow");
			}
		}

		[XmlIgnore]
		public int type
		{
			get
			{
				return item.type;
			}
		}

		[XmlIgnore]
		public ItemType Type
		{
			get
			{
				return (ItemType)item.type;
			}
		}

		[XmlIgnore]
		public Item item
		{
			get
			{
				if (_item == null)
				{
					_item = Item.GetItem(Name);
				}
				return _item;
			}
		}

		public Color GetColor()
		{
			if (AdditionTriggers.Count >= 4)
			{
				return Color.magenta;
			}
			if (AdditionTriggers.Count == 3)
			{
				return Color.yellow;
			}
			if (AdditionTriggers.Count == 2)
			{
				return Color.green;
			}
			if (AdditionTriggers.Count == 1)
			{
				return Color.blue;
			}
			if (Name.EndsWith("残章"))
			{
				return Color.blue;
			}
			if (type == 8)
			{
				return Color.red;
			}
			if (type == 7)
			{
				return Color.magenta;
			}
			return Color.white;
		}

		public string GetTypeStr()
		{
			switch (Type)
			{
			case ItemType.Costa:
				return "战场消耗品";
			case ItemType.Accessories:
				return "装备：饰品";
			case ItemType.Book:
				return "武学经书";
			case ItemType.Canzhang:
				return "武学残章";
			case ItemType.Armor:
				return "装备：防具";
			case ItemType.Weapon:
				return "装备：武器";
			case ItemType.Mission:
				return "任务物品";
			case ItemType.Special:
				return "特殊物品";
			case ItemType.SpeicalSkillBook:
				return "消耗品：特技书";
			case ItemType.TalentBook:
				return "消耗品：天赋书";
			case ItemType.Upgrade:
				return "消耗品：永久增强性物品";
			default:
				return string.Empty;
			}
		}

		public string GetLevelStr()
		{
			if (Type != ItemType.Weapon && Type != ItemType.Armor && Type != ItemType.Accessories)
			{
				return string.Empty;
			}
			if (AdditionTriggers.Count == 0)
			{
				return "【普通】";
			}
			if (AdditionTriggers.Count == 1)
			{
				return "【高级】";
			}
			if (AdditionTriggers.Count == 2)
			{
				return "【稀有】";
			}
			if (AdditionTriggers.Count == 3)
			{
				return "【神器】";
			}
			return "【史诗】";
		}

		public bool HasTalent(string talent)
		{
			string[] array = talent.Split('#');
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (text.Equals(talent))
				{
					return true;
				}
			}
			foreach (Trigger trigger in Triggers)
			{
				if (trigger.Name == "talent" && trigger.Argvs[0] == talent)
				{
					return true;
				}
			}
			foreach (Trigger additionTrigger in AdditionTriggers)
			{
				if (additionTrigger.Name == "talent" && additionTrigger.Argvs[0] == talent)
				{
					return true;
				}
			}
			return false;
		}

		public override string ToString()
		{
			string equipCase = EquipCase;
			string text = Name + "\n" + desc + "\n";
			if (!equipCase.Equals(string.Empty))
			{
				text = text + " \n装备条件 " + equipCase;
			}
			if (Triggers.Count > 0)
			{
				text += "\n";
				foreach (Trigger trigger in Triggers)
				{
					text = string.Concat(text, trigger, " ");
				}
				text = text.TrimEnd();
			}
			string[] array = talent.Split('#');
			if (array.Length > 0)
			{
				text += "\n天赋 ";
				string[] array2 = array;
				foreach (string text2 in array2)
				{
					string[] array3 = ResourceManager.Get<Resource>("天赋." + text2).Value.Split('#');
					string empty = string.Empty;
					empty = ((array3.Length != 1) ? array3[1] : array3[0]);
					text = text + empty + "\n";
				}
				text = text.TrimEnd();
			}
			if (Cooldown > 0 && RuntimeData.Instance.GameMode != "normal")
			{
				text = text + "\n冷却回合数 " + Cooldown;
			}
			return text;
		}

		public void AddRandomTriggers(int number)
		{
			if (Type == ItemType.Weapon || Type == ItemType.Armor || Type == ItemType.Accessories)
			{
				for (int i = 0; i < number; i++)
				{
					AddRandomTrigger();
				}
			}
		}

		public void AddRandomTrigger()
		{
			Trigger trigger = GenerateRandomTrigger();
			if (trigger != null)
			{
				AdditionTriggers.Add(trigger);
			}
		}

		public Trigger GenerateRandomTrigger()
		{
			if (Type != ItemType.Weapon && Type != ItemType.Armor && Type != ItemType.Accessories)
			{
				return null;
			}
			List<ITTrigger> list = new List<ITTrigger>();
			int num = 0;
			foreach (ItemTrigger item in ResourceManager.GetAll<ItemTrigger>())
			{
				if ((level < item.MinLevel || level > item.MaxLevel) && !Name.Equals(item.Name))
				{
					continue;
				}
				foreach (ITTrigger trigger2 in item.Triggers)
				{
					num += trigger2.Weight;
					list.Add(trigger2);
				}
			}
			Trigger trigger;
			while (true)
			{
				trigger = null;
				ITTrigger iTTrigger = null;
				foreach (ITTrigger item2 in list)
				{
					double p = (double)item2.Weight / (double)num;
					if (Tools.ProbabilityTest(p))
					{
						trigger = item2.GenerateItemTrigger(level);
						iTTrigger = item2;
						break;
					}
				}
				if (trigger == null)
				{
					continue;
				}
				bool flag = true;
				foreach (Trigger additionTrigger in AdditionTriggers)
				{
					if (trigger.Name == additionTrigger.Name && !iTTrigger.HasPool)
					{
						flag = false;
						break;
					}
					if (trigger.Name == additionTrigger.Name && iTTrigger.HasPool && trigger.Argvs[0] == additionTrigger.Argvs[0])
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			return trigger;
		}

		public Dictionary<string, int> getItemRequireAttrs()
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			dictionary.Clear();
			foreach (Require require in Requires)
			{
				if (CommonSettings.RoleAttributeList.Contains(require.Name))
				{
					dictionary.Add(require.Name, int.Parse(require.ArgvsString));
				}
			}
			return dictionary;
		}

		public List<string> getItemRequireTalents()
		{
			List<string> list = new List<string>();
			foreach (Require require in Requires)
			{
				if (require.Name == "talent")
				{
					list.Add(require.ArgvsString);
				}
			}
			return list;
		}

		public bool CanEquip(Role r)
		{
			if (Requires == null)
			{
				return false;
			}
			Dictionary<string, int> itemRequireAttrs = getItemRequireAttrs();
			string[] roleAttributeList = CommonSettings.RoleAttributeList;
			foreach (string key in roleAttributeList)
			{
				if (itemRequireAttrs.ContainsKey(key) && r.AttributesFinal[key] < itemRequireAttrs[key])
				{
					return false;
				}
			}
			List<string> itemRequireTalents = getItemRequireTalents();
			foreach (string item in itemRequireTalents)
			{
				if (!r.HasTalent(item))
				{
					return false;
				}
			}
			return true;
		}

		public ItemSkill GetItemSkill()
		{
			foreach (Trigger trigger in Triggers)
			{
				if (trigger.Name.Equals("skill") || trigger.Name.Equals("internalskill") || trigger.Name.Equals("specialskill") || trigger.Name.Equals("talent"))
				{
					ItemSkill itemSkill = new ItemSkill();
					itemSkill.IsInternal = trigger.Name.Equals("internalskill");
					itemSkill.SkillName = trigger.Argvs[0];
					itemSkill.MaxLevel = ((trigger.Argvs.Count <= 1) ? 1 : int.Parse(trigger.Argvs[1]));
					return itemSkill;
				}
			}
			return null;
		}

		public ItemResult TryUse(Role source, Role target)
		{
			ItemResult itemResult = new ItemResult();
			foreach (Trigger trigger in item.Triggers)
			{
				switch (trigger.Name)
				{
				case "AddHp":
				{
					int num4 = int.Parse(trigger.Argvs[0]);
					itemResult.Hp += num4;
					continue;
				}
				case "RecoverHp":
				{
					int num3 = (int)((double)target.maxhp * ((double)int.Parse(trigger.Argvs[0]) / 100.0));
					itemResult.Hp += num3;
					continue;
				}
				case "AddMp":
				{
					int num2 = int.Parse(trigger.Argvs[0]);
					itemResult.Mp += num2;
					continue;
				}
				case "RecoverMp":
				{
					int num = (int)((double)target.maxmp * ((double)int.Parse(trigger.Argvs[0]) / 100.0));
					itemResult.Mp += num;
					continue;
				}
				case "解毒":
					itemResult.DescPoisonLevel = int.Parse(trigger.Argvs[0]);
					itemResult.DescPoisonDuration = int.Parse(trigger.Argvs[1]);
					continue;
				case "Balls":
					itemResult.Balls = int.Parse(trigger.Argvs[0]);
					continue;
				case "AddMaxHp":
					itemResult.MaxHp = int.Parse(trigger.Argvs[0]);
					continue;
				case "AddMaxMp":
					itemResult.MaxMp = int.Parse(trigger.Argvs[0]);
					continue;
				case "AddBuff":
					itemResult.Buffs = Buff.Parse(trigger.Argvs[0]);
					continue;
				}
				LuaManager.Call("ITEM_OnTryUse", source, target, trigger, itemResult);
				Debug.Log("error item trigger " + trigger.Name);
			}
			return itemResult;
		}

		public override bool Equals(object obj)
		{
			if (obj is ItemInstance)
			{
				return PK == (obj as ItemInstance).PK;
			}
			return false;
		}

		public override int GetHashCode()
		{
			int num = 0;
			string pK = PK;
			foreach (char c in pK)
			{
				num += c;
			}
			return num;
		}

		public static ItemInstance Generate(string name, bool setRandomTriggers = false)
		{
			return Item.GetItem(name).Generate(setRandomTriggers);
		}
	}
}
