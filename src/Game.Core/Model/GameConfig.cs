namespace Game.Core.Model;

public sealed class GameConfig
{
	public string MainMenuMusic { get; init; } = "音乐.武侠回忆";
	public string MainMenuBackground { get; init; } = "地图.桃花岛";
	public double RoundEnemyAttackAddRatio { get; init; } = 0.1d;
	public double RoundEnemyDefenceAddRatio { get; init; } = 0.08d;
	public double RoundEnemyHpAddRatio { get; init; } = 0.15d;
	public double RoundEnemyMpAddRatio { get; init; } = 0.15d;
	public int RoundsPerMaxSkillLevelIncrease { get; init; } = 2;
	public int RoundsPerNpcSkillLevelIncrease { get; init; } = 2;
	public int DefaultMaxBattleSpTime { get; init; } = 3000;
	public bool ConsoleEnabled { get; init; } = true;
	public int ChestBaseCapacity { get; init; } = 16;
	public int ChestPerRoundCapacity { get; init; } = 6;
	public int MaxExternalSkillCount { get; init; } = 12;
	public int MaxInternalSkillCount { get; init; } = 8;
	public int MaxAttribute { get; init; } = 300;
	public int MaxExternalSkillLevel { get; init; } = 20;
	public int MaxInternalSkillLevel { get; init; } = 20;
	public int MaxHpMp { get; init; } = 10000;
	public int MaxHpMpPerRound { get; init; } = 1000;
	public int MaxLevel { get; init; } = 30;
	public int BattlePlayerTeam { get; init; } = 1;
	public double BattleGoldDropChance { get; init; } = 0.005d;
	public double OrdinaryBattleDropChance { get; init; } = 0.1d;
	public string InitialStorySegmentId { get; init; } = "开局答题";

	public List<string> RandomBattleMusics { get; init; } = [
		"战斗音乐.云狐之战", "战斗音乐.暮云出击", "战斗音乐.山谷行进", "战斗音乐.山谷行进2",
		"战斗音乐.2", "战斗音乐.3", "战斗音乐.4", "战斗音乐.5",
		"音乐.天龙八部.紧张感3", "音乐.天龙八部.紧张感4",
	];
	public List<string> EnemyRandomTalentIds { get; init; } = [
		"飘然", "斗魂", "哀歌", "奋战", "百足之虫", "真气护体", "暴躁", "金钟罩",
		"诸般封印", "刀封印", "剑封印", "奇门封印", "拳掌封印", "自我主义", "大小姐",
		"破甲", "好色", "瘸子", "白内障", "左手剑", "右臂有伤", "拳掌增益", "剑法增益",
		"刀法增益", "奇门增益", "锐眼",
	];
	public List<string> EnemyRandomTalentCrazy1Ids { get; init; } = [
		"百足之虫", "真气护体", "金钟罩", "苦命儿", "老江湖", "暴躁", "灵心慧质",
		"精打细算", "白内障", "右臂有伤", "神经病", "鲁莽",
	];
	public List<string> EnemyRandomTalentCrazy2Ids { get; init; } = [
		"斗魂", "奋战", "暴躁", "自我主义", "破甲", "铁拳无双", "素心神剑", "左右互搏",
		"博览群书", "阴谋家", "琴胆剑心", "追魂", "铁口直断", "左手剑", "拳掌增益",
		"剑法增益", "刀法增益", "奇门增益", "锐眼",
	];
	public List<string> EnemyRandomTalentCrazy3Ids { get; init; } = [
		"刀封印", "剑封印", "奇门封印", "拳掌封印", "清心", "哀歌", "幽居", "金刚",
		"嗜血狂魔", "清风", "御风", "轻功高超", "瘸子",
	];
	public List<string> LegendFemaleVoiceSfxIds { get; init; } = [
		"音效.女", "音效.女2", "音效.女3", "音效.女4",
	];
	public List<string> LegendMaleVoiceSfxIds { get; init; } = [
		"音效.男", "音效.男2", "音效.男3", "音效.男4", "音效.男5", "音效.男-哼",
	];
	public List<string> LegendEffectSfxIds { get; init; } = [
		"音效.内功攻击4", "音效.打雷", "音效.奥义1", "音效.奥义2",
		"音效.奥义3", "音效.奥义4", "音效.奥义5", "音效.奥义6",
	];
	public List<string> ZhenlongWeaponRewardIds { get; init; } = [
		"真.天龙宝剑", "玄铁剑", "冷月宝刀", "被诅咒的木刀", "真.倚天剑",
		"真.屠龙刀", "真.打狗棒", "真.灭仙爪", "打狗棒",
	];
	public List<string> ZhenlongArmorRewardIds { get; init; } = [
		"黄金重甲", "幽梦衣", "霓裳羽衣", "岳飞的重铠", "千变魔女的披风",
		"华裳", "乌蚕衣", "三清袍",
	];
	public List<string> ZhenlongAccessoryRewardIds { get; init; } = [
		"橙色灯戒", "铂金戒指", "蓝宝戒指", "水晶护符", "魔神信物", "神奇戒指",
	];
	public List<string> InitialPartyCharacterIds { get; init; } = ["主角"];
}
