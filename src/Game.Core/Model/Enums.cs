using System.Text.Json.Serialization;

namespace Game.Core.Model;

public enum CharacterGender
{
    [JsonStringEnumMemberName("male")]
    Male,
    [JsonStringEnumMemberName("female")]
    Female,
    [JsonStringEnumMemberName("neutral")]
    Neutral,
    [JsonStringEnumMemberName("animal")]
    Animal,
    [JsonStringEnumMemberName("eunuch")]
    Eunuch,
}

public enum WeaponType
{
    [JsonStringEnumMemberName("quanzhang")]
    Quanzhang,
    [JsonStringEnumMemberName("jianfa")]
    Jianfa,
    [JsonStringEnumMemberName("daofa")]
    Daofa,
    [JsonStringEnumMemberName("qimen")]
    Qimen,
    [JsonStringEnumMemberName("internal_skill")]
    InternalSkill,
    [JsonStringEnumMemberName("unknown")]
    Unknown
}

public enum ItemType
{
    [JsonStringEnumMemberName("consumable")]
    Consumable,
    [JsonStringEnumMemberName("equipment")]
    Equipment,
    [JsonStringEnumMemberName("skill_book")]
    SkillBook,
    [JsonStringEnumMemberName("quest_item")]
    QuestItem,
    [JsonStringEnumMemberName("special_skill_book")]
    SpecialSkillBook,
    [JsonStringEnumMemberName("talent_book")]
    TalentBook,
    [JsonStringEnumMemberName("booster")]
    Booster,
    [JsonStringEnumMemberName("utility")]
    Utility,
}

public enum EquipmentSlotType
{
    [JsonStringEnumMemberName("weapon")]
    Weapon,
    [JsonStringEnumMemberName("armor")]
    Armor,
    [JsonStringEnumMemberName("accessory")]
    Accessory,
}

public enum StatType
{
    [JsonStringEnumMemberName("bili")]
    Bili,
    [JsonStringEnumMemberName("dingli")]
    Dingli,
    [JsonStringEnumMemberName("fuyuan")]
    Fuyuan,
    [JsonStringEnumMemberName("gengu")]
    Gengu,
    [JsonStringEnumMemberName("jianfa")]
    Jianfa,
    [JsonStringEnumMemberName("daofa")]
    Daofa,
    [JsonStringEnumMemberName("quanzhang")]
    Quanzhang,
    [JsonStringEnumMemberName("qimen")]
    Qimen,
    [JsonStringEnumMemberName("shenfa")]
    Shenfa,
    [JsonStringEnumMemberName("wuxing")]
    Wuxing,
    [JsonStringEnumMemberName("wuxue")]
    Wuxue,
    [JsonStringEnumMemberName("max_hp")]
    MaxHp,
    [JsonStringEnumMemberName("max_mp")]
    MaxMp,
    [JsonStringEnumMemberName("attack")]
    Attack,
    [JsonStringEnumMemberName("defence")]
    Defence,
    [JsonStringEnumMemberName("accuracy")]
    Accuracy,
    [JsonStringEnumMemberName("crit_chance")]
    CritChance,
    [JsonStringEnumMemberName("crit_mult")]
    CritMult,
    [JsonStringEnumMemberName("anti_crit_chance")]
    AntiCritChance,
    [JsonStringEnumMemberName("lifesteal")]
    Lifesteal,
    [JsonStringEnumMemberName("anti_debuff")]
    AntiDebuff,
    [JsonStringEnumMemberName("speed")]
    Speed,
    [JsonStringEnumMemberName("movement")]
    Movement,
}
