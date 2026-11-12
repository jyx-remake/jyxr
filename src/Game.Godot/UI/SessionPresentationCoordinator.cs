using Game.Application;
using Game.Core.Story;
using Game.Godot.Map;

namespace Game.Godot.UI;

internal sealed class SessionPresentationCoordinator(UIRoot ui, DetailPanelHost details) : IDisposable
{
	private GameSession? _session;
	private SessionEvents? _sessionEvents;
	private readonly List<IDisposable> _sessionSubscriptions = [];

	public void ResetAfterLoad()
	{
		ui.SetStoryPresentationActive(false);
		ui.CloseMainPanel();
		ui.ResetTransientPanels();
		details.Close();

		var currentMapId = Game.State.Location.CurrentMapId;
		if (!string.IsNullOrWhiteSpace(currentMapId))
		{
			World.Instance.RestoreCurrentMap();
		}

		ui.RefreshHud();
	}

	public void PresentStoryActivity(bool active)
	{
		if (World.Instance.CurrentScene is MapScreen map)
		{
			map.SetStoryPresentationActive(active);
		}
	}

	public void Bind(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);

		if (ReferenceEquals(_sessionEvents, session.Events))
		{
			ui.RefreshHud();
			return;
		}

		if (_sessionEvents is not null)
		{
			Dispose();
		}

		_session = session;
		_sessionEvents = session.Events;
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<MapChangedEvent>(OnMapChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<MapReturnedEvent>(OnMapReturned));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ClockChangedEvent>(OnClockChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<AdventureStateChangedEvent>(OnAdventureStateChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ItemAcquiredEvent>(OnItemAcquired));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ToastRequestedEvent>(OnToastRequested));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<CharacterLeveledUpEvent>(OnCharacterLeveledUp));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<SaveLoadedEvent>(OnSaveLoaded));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ExecutionAbortedEvent>(_ => ResetAfterLoad()));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ProfileChangedEvent>(OnProfileChanged));
		_sessionSubscriptions.Add(_sessionEvents.Subscribe<ProfileLoadedEvent>(OnProfileLoaded));
		ui.RefreshHud();
	}

	private void OnMapChanged(MapChangedEvent _) => ui.RefreshHud();

	private void OnMapReturned(MapReturnedEvent returned)
	{
		if (GameFlow.IsMainMenuActive || string.IsNullOrWhiteSpace(returned.MapId)) return;
		World.Instance.RefreshCurrentMap();
		var music = Game.ContentRepository.GetMap(returned.MapId).Musics;
		if (music.Count == 0) Game.Audio.StopBgm();
		else Game.Audio.PlayBgm(music);
		ui.RefreshHud();
	}

	private void OnClockChanged(ClockChangedEvent _) => ui.RefreshHud();

	private void OnCurrencyChanged(CurrencyChangedEvent _) => ui.RefreshHud();

	private void OnAdventureStateChanged(AdventureStateChangedEvent _) => ui.RefreshHud();

	private void OnItemAcquired(ItemAcquiredEvent sessionEvent)
	{
		var quantitySuffix = sessionEvent.Quantity > 1
			? $" x{sessionEvent.Quantity}"
			: string.Empty;
		ui.ShowToast($"获得物品【{sessionEvent.ItemName}】{quantitySuffix}", soundEffectId: "音效.升级");
	}

	private void OnToastRequested(ToastRequestedEvent sessionEvent) =>
		ui.ShowToast(sessionEvent.Message, sessionEvent.Tone, sessionEvent.SoundEffectId);

	private void OnCharacterLeveledUp(CharacterLeveledUpEvent sessionEvent)
	{
		var characterName = _session!.State.Party.TryGetMember(sessionEvent.CharacterId, out var character) && character is not null
			? character.Name
			: sessionEvent.CharacterId;
		ui.ShowToast($"【{characterName}】升到{sessionEvent.NewLevel}级", ToastTone.Important);
	}

	private void OnSaveLoaded(SaveLoadedEvent _)
	{
		ui.ResetTransientPanels();
		details.Close();
		ui.RefreshHud();
	}

	private void OnAchievementUnlocked(AchievementUnlockedEvent sessionEvent)
	{
		ui.ShowToast($"获得称号【{sessionEvent.AchievementId}】", ToastTone.Important);
	}

	private void OnProfileChanged(ProfileChangedEvent _)
	{
		ui.RefreshHud();
	}

	private void OnProfileLoaded(ProfileLoadedEvent _) => ui.RefreshHud();

	public void Dispose()
	{
		foreach (var subscription in _sessionSubscriptions)
		{
			subscription.Dispose();
		}

		_sessionSubscriptions.Clear();
		_sessionEvents = null;
		_session = null;
	}
}
