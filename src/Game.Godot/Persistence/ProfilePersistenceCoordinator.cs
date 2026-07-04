using Game.Application;
using Godot;

namespace Game.Godot.Persistence;

public sealed class ProfilePersistenceCoordinator
{
	private readonly Node _deferredOwner;
	private readonly LocalProfileStore _profileStore;
	private bool _isDirty;
	private bool _isScheduled;

	public ProfilePersistenceCoordinator(Node deferredOwner, LocalProfileStore? profileStore = null)
	{
		ArgumentNullException.ThrowIfNull(deferredOwner);
		_deferredOwner = deferredOwner;
		_profileStore = profileStore ?? new LocalProfileStore();
	}

	public void RequestSave()
	{
		_isDirty = true;
		if (_isScheduled)
		{
			return;
		}

		_isScheduled = true;
		_ = FlushOnNextFrameAsync();
	}

	public void FlushNow()
	{
		if (!_isDirty && !_isScheduled)
		{
			return;
		}

		_isDirty = false;
		_isScheduled = false;
		try
		{
			_profileStore.SaveCurrentProfile();
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Persisting global profile failed.", exception);
		}
	}

	private async Task FlushOnNextFrameAsync()
	{
		if (GodotObject.IsInstanceValid(_deferredOwner) && _deferredOwner.IsInsideTree())
		{
			await _deferredOwner.ToSignal(_deferredOwner.GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		FlushNow();
	}
}
