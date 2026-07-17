using System.Linq;
using System.Text.Json;
using Game.Application;
using Game.Application.Mods;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;

namespace Game.Godot.Persistence;

public enum LocalSaveReadFailureReason
{
	None = 0,
	MissingFile,
	InvalidFormat,
	EnvelopeVersionMismatch,
	SaveVersionMismatch,
}

public enum LocalSaveKind
{
	Auto,
	Quick,
	Manual,
}

public sealed record LocalSaveId
{
	private LocalSaveId(LocalSaveKind kind, int slotIndex)
	{
		Kind = kind;
		SlotIndex = slotIndex;
	}

	public static LocalSaveId Auto { get; } = new(LocalSaveKind.Auto, 0);
	public static LocalSaveId Quick { get; } = new(LocalSaveKind.Quick, 0);

	public LocalSaveKind Kind { get; }
	public int SlotIndex { get; }
	public string Title => Kind switch
	{
		LocalSaveKind.Auto => "自动存档",
		LocalSaveKind.Quick => "快速存档",
		LocalSaveKind.Manual => $"存档{SlotIndex}",
		_ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unsupported save kind."),
	};

	public static LocalSaveId Manual(int slotIndex)
	{
		LocalSaveStore.ValidateSlotIndex(slotIndex);
		return new LocalSaveId(LocalSaveKind.Manual, slotIndex);
	}
}

public sealed class LocalSaveStore
{
	public const int SlotCount = 20;
	private readonly ModStoragePaths? _storagePaths;

	public LocalSaveStore(ModStoragePaths? storagePaths = null)
	{
		_storagePaths = storagePaths;
	}

	public string SaveCurrentSession(LocalSaveId saveId) =>
		SaveCurrentSession(ResolveSavePath(saveId), GetLogName(saveId));

	private static string SaveCurrentSession(string savePath, string logName)
	{
		var envelope = new LocalSaveEnvelope(
			LocalSaveEnvelope.CurrentVersion,
			Game.SaveGameService.CreateSave(),
			DateTimeOffset.UtcNow);

		var absolutePath = savePath;
		var directoryPath = Path.GetDirectoryName(absolutePath);
		if (string.IsNullOrWhiteSpace(directoryPath))
		{
			throw new InvalidOperationException($"Invalid save path: {savePath}");
		}

		Directory.CreateDirectory(directoryPath);
		var json = JsonSerializer.Serialize(envelope, GameJson.Default);
		File.WriteAllText(absolutePath, json);
		Game.Logger.Info($"Saved {logName} to '{absolutePath}'.");
		return absolutePath;
	}

	public bool TryLoad(LocalSaveId saveId, out LocalSaveEnvelope? envelope, out LocalSaveReadFailureReason failureReason)
	{
		var absolutePath = ResolveSavePath(saveId);
		if (!TryReadEnvelope(absolutePath, out envelope, out failureReason))
		{
			return false;
		}

		Game.Logger.Info($"Loaded {GetLogName(saveId)} from '{absolutePath}'.");
		return true;
	}

	public bool Delete(LocalSaveId saveId)
	{
		var absolutePath = ResolveSavePath(saveId);
		if (!File.Exists(absolutePath))
		{
			return false;
		}

		File.Delete(absolutePath);
		Game.Logger.Info($"Deleted {GetLogName(saveId)} at '{absolutePath}'.");
		return true;
	}

	public bool Exists(LocalSaveId saveId)
	{
		return File.Exists(ResolveSavePath(saveId));
	}

	public LocalSaveSummary GetSummary(LocalSaveId saveId)
	{
		if (!Exists(saveId))
		{
			return new LocalSaveSummary(saveId);
		}

		if (!TryReadEnvelope(ResolveSavePath(saveId), out var envelope, out var failureReason) || envelope is null)
		{
			return new LocalSaveSummary(saveId, HasSave: true, FailureReason: failureReason);
		}

		return BuildSummary(saveId, envelope);
	}

	private static LocalSaveSummary BuildSummary(LocalSaveId saveId, LocalSaveEnvelope envelope)
	{
		var saveGame = envelope.SaveGame;
		var leaderId = saveGame.Party.MemberIds.FirstOrDefault();
		var leader = leaderId is null
			? saveGame.Characters.FirstOrDefault()
			: saveGame.Characters.FirstOrDefault(character => string.Equals(character.Id, leaderId, StringComparison.Ordinal))
			  ?? saveGame.Characters.FirstOrDefault();

		return new LocalSaveSummary(
			saveId,
			HasSave: true,
			LeaderName: leader?.Name,
			LeaderPortrait: leader?.Portrait,
			PartyMemberCount: saveGame.Party.MemberIds.Count,
			Round: saveGame.Adventure.Round,
			Difficulty: saveGame.Adventure.Difficulty,
			Clock: saveGame.Clock,
			CurrentMapId: saveGame.Location.CurrentMapId,
			SavedAtUtc: envelope.SavedAtUtc);
	}

	internal static void ValidateSlotIndex(int slotIndex)
	{
		if (slotIndex < 1 || slotIndex > SlotCount)
		{
			throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, $"存档槽必须在 1 到 {SlotCount} 之间。");
		}
	}

	private string ResolveSavePath(LocalSaveId saveId) => saveId.Kind switch
	{
		LocalSaveKind.Auto => StoragePaths.AutoSavePath,
		LocalSaveKind.Quick => StoragePaths.QuickSavePath,
		LocalSaveKind.Manual => StoragePaths.GetSaveSlotPath(saveId.SlotIndex),
		_ => throw new ArgumentOutOfRangeException(nameof(saveId), saveId, "Unsupported save kind."),
	};

	private static string GetLogName(LocalSaveId saveId) => saveId.Kind switch
	{
		LocalSaveKind.Auto => "autosave",
		LocalSaveKind.Quick => "quicksave",
		LocalSaveKind.Manual => $"slot {saveId.SlotIndex}",
		_ => throw new ArgumentOutOfRangeException(nameof(saveId), saveId, "Unsupported save kind."),
	};

	private ModStoragePaths StoragePaths => _storagePaths ?? Game.ActiveMod.StoragePaths;

	private static bool TryReadEnvelope(string savePath, out LocalSaveEnvelope? envelope, out LocalSaveReadFailureReason failureReason)
	{
		var absolutePath = savePath;
		if (!File.Exists(absolutePath))
		{
			return Fail(out envelope, out failureReason, LocalSaveReadFailureReason.MissingFile);
		}

		try
		{
			var json = File.ReadAllText(absolutePath);
			var rawEnvelope = JsonSerializer.Deserialize<LocalSaveEnvelope>(json, GameJson.Default);
			if (rawEnvelope is null)
			{
				Game.Logger.Warning($"Save file could not be deserialized: {absolutePath}");
				return Fail(out envelope, out failureReason, LocalSaveReadFailureReason.InvalidFormat);
			}

			if (rawEnvelope.Version is < 1 or > LocalSaveEnvelope.CurrentVersion)
			{
				Game.Logger.Warning(
					$"Save file envelope version mismatch: {rawEnvelope.Version}, supported 1..{LocalSaveEnvelope.CurrentVersion}. Path: {absolutePath}");
				return Fail(out envelope, out failureReason, LocalSaveReadFailureReason.EnvelopeVersionMismatch);
			}

			if (rawEnvelope.SaveGame.Version is < SaveGame.MinSupportedVersion or > SaveGame.CurrentVersion)
			{
				Game.Logger.Warning(
					$"Save file save version mismatch: {rawEnvelope.SaveGame.Version}, supported {SaveGame.MinSupportedVersion}..{SaveGame.CurrentVersion}. Path: {absolutePath}");
				return Fail(out envelope, out failureReason, LocalSaveReadFailureReason.SaveVersionMismatch);
			}

			envelope = rawEnvelope;
			failureReason = LocalSaveReadFailureReason.None;
			return true;
		}
		catch (Exception exception)
		{
			Game.Logger.Warning($"Save file read failed: {absolutePath}. {exception.Message}");
			return Fail(out envelope, out failureReason, LocalSaveReadFailureReason.InvalidFormat);
		}
	}

	private static bool Fail(
		out LocalSaveEnvelope? envelope,
		out LocalSaveReadFailureReason failureReason,
		LocalSaveReadFailureReason reason)
	{
		envelope = null;
		failureReason = reason;
		return false;
	}
}

public sealed record LocalSaveSummary(
	LocalSaveId SaveId,
	bool HasSave = false,
	string? LeaderName = null,
	string? LeaderPortrait = null,
	int PartyMemberCount = 0,
	int Round = 1,
	GameDifficulty Difficulty = GameDifficulty.Normal,
	ClockRecord? Clock = null,
	string? CurrentMapId = null,
	DateTimeOffset? SavedAtUtc = null,
	LocalSaveReadFailureReason FailureReason = LocalSaveReadFailureReason.None)
{
	public bool CanLoad => HasSave && FailureReason == LocalSaveReadFailureReason.None;
}

public sealed record LocalSaveEnvelope(
	int Version,
	SaveGame SaveGame,
	DateTimeOffset SavedAtUtc)
{
	public const int CurrentVersion = 2;
}
