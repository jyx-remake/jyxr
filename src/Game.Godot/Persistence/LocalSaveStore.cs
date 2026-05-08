using System.Linq;
using System.Text.Json;
using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;
using Godot;

namespace Game.Godot.Persistence;

public enum LocalSaveReadFailureReason
{
	None = 0,
	MissingFile,
	InvalidFormat,
	EnvelopeVersionMismatch,
	SaveVersionMismatch,
}

public sealed class LocalSaveStore
{
	public const int SlotCount = 4;
	private const string SavePathFormat = "user://saves/save-slot-{0}.json";

	public string SaveCurrentSession(int slotIndex)
	{
		ValidateSlotIndex(slotIndex);
		var envelope = new LocalSaveEnvelope(
			LocalSaveEnvelope.CurrentVersion,
			Game.SaveGameService.CreateSave(),
			DateTimeOffset.UtcNow);

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

	public bool TryLoad(int slotIndex, out LocalSaveEnvelope? envelope, out LocalSaveReadFailureReason failureReason)
	{
		var absolutePath = ResolveAbsolutePath(slotIndex);
		if (!TryReadEnvelope(slotIndex, out envelope, out failureReason))
		{
			return false;
		}

		Game.Logger.Info($"Loaded slot {slotIndex} from '{absolutePath}'.");
		return true;
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
			return new LocalSaveSlotSummary(slotIndex);
		}

		if (!TryReadEnvelope(slotIndex, out var envelope, out var failureReason) || envelope is null)
		{
			return new LocalSaveSlotSummary(slotIndex, HasSave: true, FailureReason: failureReason);
		}

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
		string.Format(SavePathFormat, slotIndex);

	private static string ResolveAbsolutePath(int slotIndex) => ProjectSettings.GlobalizePath(ResolveSavePath(slotIndex));

	private static bool TryReadEnvelope(int slotIndex, out LocalSaveEnvelope? envelope, out LocalSaveReadFailureReason failureReason)
	{
		ValidateSlotIndex(slotIndex);
		var absolutePath = ResolveAbsolutePath(slotIndex);
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
				Game.Logger.Warning($"Save slot {slotIndex} could not be deserialized: {absolutePath}");
				return Fail(out envelope, out failureReason, LocalSaveReadFailureReason.InvalidFormat);
			}

			if (rawEnvelope.Version is < 1 or > LocalSaveEnvelope.CurrentVersion)
			{
				Game.Logger.Warning(
					$"Save slot {slotIndex} envelope version mismatch: {rawEnvelope.Version}, supported 1..{LocalSaveEnvelope.CurrentVersion}.");
				return Fail(out envelope, out failureReason, LocalSaveReadFailureReason.EnvelopeVersionMismatch);
			}

			if (rawEnvelope.SaveGame.Version != SaveGame.CurrentVersion)
			{
				Game.Logger.Warning(
					$"Save slot {slotIndex} save version mismatch: {rawEnvelope.SaveGame.Version}, supported {SaveGame.CurrentVersion}.");
				return Fail(out envelope, out failureReason, LocalSaveReadFailureReason.SaveVersionMismatch);
			}

			envelope = rawEnvelope;
			failureReason = LocalSaveReadFailureReason.None;
			return true;
		}
		catch (Exception exception)
		{
			Game.Logger.Warning($"Save slot {slotIndex} read failed: {absolutePath}. {exception.Message}");
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

public sealed record LocalSaveSlotSummary(
	int SlotIndex,
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
