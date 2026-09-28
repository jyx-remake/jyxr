using Game.Godot.Persistence;
using Game.Application;

namespace Game.Godot.Settings;

public sealed class UserSettingsService
{
	private readonly DeferredPersistence<UserSettingsRecord> _persistence;

	public UserSettingsService(LocalUserSettingsStore store, UserSettingsRecord initialSettings)
	{
		ArgumentNullException.ThrowIfNull(store);
		Current = initialSettings ?? throw new ArgumentNullException(nameof(initialSettings));
		_persistence = new(initialSettings, settings => store.Save(settings));
	}

	public UserSettingsRecord Current { get; private set; }

	public void ApplyCurrent() => UserSettingsApplier.Apply(Current);
	public void Flush() => _persistence.Flush();
	public void FlushIfDue() => _persistence.FlushIfDue();

	public void Update(Func<UserSettingsRecord, UserSettingsRecord> update)
	{
		ArgumentNullException.ThrowIfNull(update);

		var previous = Current;
		var updated = update(previous) ?? throw new InvalidOperationException("User settings update returned null.");
		if (updated.Version != UserSettingsRecord.CurrentVersion)
		{
			throw new InvalidOperationException(
				$"User settings version must be {UserSettingsRecord.CurrentVersion}, but was {updated.Version}.");
		}

		if (updated == previous)
		{
			return;
		}

		try
		{
			UserSettingsApplier.Apply(updated, previous);
			Current = updated;
			_persistence.Update(updated);
		}
		catch
		{
			UserSettingsApplier.Apply(previous);
			throw;
		}
	}
}
