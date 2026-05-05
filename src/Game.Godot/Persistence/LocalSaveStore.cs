using System.Linq;
using System.Text.Json;
using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;
using Godot;

namespace Game.Godot.Persistence;

public sealed class LocalSaveStore
{
	public const int SlotCount = 4;
	public const string DefaultSavePath = "user://saves/quicksave.json";
	private const string AdditionalSlotPathFormat = "user://saves/save-slot-{0}.json";

	public string SaveCurrentSession(int slotIndex)
	{
		ValidateSlotIndex(slotIndex);
		var envelope = new LocalSaveEnvelope(
			LocalSaveEnvelope.CurrentVersion,
			Game.SaveGameService.CreateSave(),
			DateTimeOffset.UtcNow,
			null);

		var absolutePath = ResolveAbsolutePath(slotIndex);
		var directoryPath = Path.GetDirectoryName(absolutePath);
		if (string.IsNullOrWhiteSpace(directoryPath))
		{
			throw new InvalidOperationException($"Invalid save path: {ResolveSavePath(slotIndex)}");
		}

		Directory.CreateDirectory(directoryPath);
		var json = JsonSerializer.Serialize(envelope, GameJson.Default);
		File.WriteAllText(absolutePath, json);
		Game.Logger.Info($"Saved slot {slotIndex} to '{absolutePath}'.");
		return absolutePath;
	}

	public LocalSaveEnvelope Load(int slotIndex)
	{
		var absolutePath = ResolveAbsolutePath(slotIndex);
		var envelope = ReadEnvelope(slotIndex);
		Game.Logger.Info($"Loaded slot {slotIndex} from '{absolutePath}'.");
		return envelope;
	}

	public bool DeleteSave(int slotIndex)
	{
		ValidateSlotIndex(slotIndex);
		var absolutePath = ResolveAbsolutePath(slotIndex);
		if (!File.Exists(absolutePath))
		{
			return false;
		}

		File.Delete(absolutePath);
		Game.Logger.Info($"Deleted slot {slotIndex} at '{absolutePath}'.");
		return true;
	}

	public bool HasSave(int slotIndex)
	{
		ValidateSlotIndex(slotIndex);
		var absolutePath = ResolveAbsolutePath(slotIndex);
		return File.Exists(absolutePath);
	}

	public IReadOnlyList<LocalSaveSlotSummary> GetSlotSummaries() =>
		Enumerable.Range(1, SlotCount)
			.Select(GetSlotSummary)
			.ToList();

	public LocalSaveSlotSummary GetSlotSummary(int slotIndex)
	{
		if (!HasSave(slotIndex))
		{
			return LocalSaveSlotSummary.Empty(slotIndex);
		}

		var envelope = ReadEnvelope(slotIndex);
		return BuildSummary(slotIndex, envelope);
	}

	private static LocalSaveSlotSummary BuildSummary(int slotIndex, LocalSaveEnvelope envelope)
	{
		var saveGame = envelope.SaveGame;
		var leaderId = saveGame.Party.MemberIds.FirstOrDefault();
		var leader = leaderId is null
			? saveGame.Characters.FirstOrDefault()
			: saveGame.Characters.FirstOrDefault(character => string.Equals(character.Id, leaderId, StringComparison.Ordinal))
			  ?? saveGame.Characters.FirstOrDefault();

		return new LocalSaveSlotSummary(
			slotIndex,
			true,
			leader?.Name,
			leader?.Portrait,
			saveGame.Party.MemberIds.Count,
			saveGame.Adventure.Round,
			saveGame.Adventure.Difficulty,
			saveGame.Clock,
			saveGame.Location.CurrentMapId,
			envelope.SavedAtUtc);
	}

	private static void ValidateSlotIndex(int slotIndex)
	{
		if (slotIndex < 1 || slotIndex > SlotCount)
		{
			throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, $"存档槽必须在 1 到 {SlotCount} 之间。");
		}
	}

	private static string ResolveSavePath(int slotIndex) =>
		slotIndex == 1
			? DefaultSavePath
			: string.Format(AdditionalSlotPathFormat, slotIndex);

	private static string ResolveAbsolutePath(int slotIndex) => ProjectSettings.GlobalizePath(ResolveSavePath(slotIndex));

	private static LocalSaveEnvelope ReadEnvelope(int slotIndex)
	{
		ValidateSlotIndex(slotIndex);
		var absolutePath = ResolveAbsolutePath(slotIndex);
		if (!File.Exists(absolutePath))
		{
			throw new InvalidOperationException($"未找到存档文件：{absolutePath}");
		}

		var json = File.ReadAllText(absolutePath);
		var envelope = JsonSerializer.Deserialize<LocalSaveEnvelope>(json, GameJson.Default)
			?? throw new InvalidOperationException("存档文件解析失败。");
		ValidateEnvelope(envelope);
		return envelope;
	}

	private static void ValidateEnvelope(LocalSaveEnvelope envelope)
	{
		if (envelope.Version is < 1 or > LocalSaveEnvelope.CurrentVersion)
		{
			throw new InvalidOperationException(
				$"存档封包版本不匹配：{envelope.Version}，当前支持 1 到 {LocalSaveEnvelope.CurrentVersion}。");
		}

		if (envelope.SaveGame.Version != SaveGame.CurrentVersion)
		{
			throw new InvalidOperationException(
				$"存档版本不匹配：{envelope.SaveGame.Version}，当前支持 {SaveGame.CurrentVersion}。");
		}

		if (envelope.Profile is not null && envelope.Profile.Version != GameProfileRecord.CurrentVersion)
		{
			throw new InvalidOperationException(
				$"档案版本不匹配：{envelope.Profile.Version}，当前支持 {GameProfileRecord.CurrentVersion}。");
		}
	}
}

public sealed record LocalSaveSlotSummary(
	int SlotIndex,
	bool HasSave,
	string? LeaderName,
	string? LeaderPortrait,
	int PartyMemberCount,
	int Round,
	GameDifficulty Difficulty,
	ClockRecord? Clock,
	string? CurrentMapId,
	DateTimeOffset? SavedAtUtc)
{
	public static LocalSaveSlotSummary Empty(int slotIndex) =>
		new(
			slotIndex,
			false,
			null,
			null,
			0,
			1,
			GameDifficulty.Normal,
			null,
			null,
			null);
}

public sealed record LocalSaveEnvelope(
	int Version,
	SaveGame SaveGame,
	DateTimeOffset SavedAtUtc,
	GameProfileRecord? Profile = null)
{
	public const int CurrentVersion = 2;
}
