namespace Game.Core.Model;

public sealed class GameConfig
{
	public int ChestBaseCapacity { get; init; }= 16;
	public int ChestPerRoundCapacity { get; init; } = 6;
	public int MaxExternalSkillCount { get; init; } = 10;
	public int MaxInternalSkillCount { get; init; } = 5;
	public int MaxHpMp { get; init; } = 10000;
	public int MaxHpMpPerRound { get; init; } = 1000;
	public int MaxLevel { get; init; } = 30;
	public int BattlePlayerTeam { get; init; } = 1;
	public double BattleGoldDropChance { get; init; } = 0.005d;
	public string InitialStorySegmentId { get; init; } = "开局答题";

	public List<string> RandomBattleMusics { get; init; } = [
		"战斗音乐.云狐之战", "战斗音乐.暮云出击", "战斗音乐.山谷行进", "战斗音乐.山谷行进2",
		"战斗音乐.2", "战斗音乐.3", "战斗音乐.4", "战斗音乐.5",
		"音乐.天龙八部.紧张感3", "音乐.天龙八部.紧张感4",
	];
	public List<string> InitialPartyCharacterIds { get; init; } = ["主角"];
}
