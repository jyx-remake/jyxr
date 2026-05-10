using Game.Application;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Audio;

public partial class AudioManager : Node
{
	private const string SfxBusName = "SFX";

	public static AudioManager Instance { get; private set; } = null!;

	private AudioStreamPlayer _bgmPlayer = null!;
	private AudioStreamPlayer _sfxPlayer = null!;
	private AudioStreamPlaybackPolyphonic _sfxPlayback = null!;
	private string? _currentBgmResourceId;
	private string[] _bgmPlaylist = [];
	private int _lastPlaylistIndex = -1;

	public override void _Ready()
	{
		_bgmPlayer = GetNode<AudioStreamPlayer>("%BgmPlayer");
		_sfxPlayer = GetNode<AudioStreamPlayer>("%SfxPlayer");
		_bgmPlayer.Finished += OnBgmPlayerFinished;
		InitializeSfxPlayback();
		Instance = this;
	}

	public void PlayBgm(string? resourceId)
	{
		if (string.IsNullOrWhiteSpace(resourceId))
		{
			return;
		}

		_bgmPlaylist = [];
		_lastPlaylistIndex = -1;
		PlayResolvedBgm(resourceId.Trim());
	}

	public void PlayBgm(IReadOnlyList<string> resourceIds)
	{
		ArgumentNullException.ThrowIfNull(resourceIds);

		var normalizedIds = resourceIds
			.Where(static id => !string.IsNullOrWhiteSpace(id))
			.Select(static id => id.Trim())
			.ToArray();

		if (normalizedIds.Length == 0)
		{
			return;
		}

		if (normalizedIds.Length == 1)
		{
			PlayBgm(normalizedIds[0]);
			return;
		}

		_bgmPlaylist = normalizedIds;
		PlayPlaylistIndex(PickNextPlaylistIndex());
	}

	public void StopBgm()
	{
		_bgmPlaylist = [];
		_lastPlaylistIndex = -1;
		_currentBgmResourceId = null;
		_bgmPlayer.Stop();
		_bgmPlayer.Stream = null;
	}

	public void PlaySfx(string? resourceId)
	{
		if (string.IsNullOrWhiteSpace(resourceId))
		{
			return;
		}

		var stream = AssetResolver.LoadAudioResource(resourceId);
		if (stream is null)
		{
			return;
		}

		var playbackId = _sfxPlayback.PlayStream(stream, bus: SfxBusName);
		if (playbackId == AudioStreamPlaybackPolyphonic.InvalidId)
		{
			Game.Logger.Warning($"SFX polyphony exhausted, dropped sound: {resourceId}");
		}
	}

	private void OnBgmPlayerFinished()
	{
		if (_bgmPlaylist.Length > 0)
		{
			PlayPlaylistIndex(PickNextPlaylistIndex());
			return;
		}

		if (!string.IsNullOrWhiteSpace(_currentBgmResourceId))
		{
			PlayResolvedBgm(_currentBgmResourceId);
		}
	}

	private void PlayPlaylistIndex(int index)
	{
		_lastPlaylistIndex = index;
		PlayResolvedBgm(_bgmPlaylist[index]);
	}

	private int PickNextPlaylistIndex()
	{
		if (_bgmPlaylist.Length == 1)
		{
			return 0;
		}

		var nextIndex = Random.Shared.Next(_bgmPlaylist.Length);
		if (nextIndex == _lastPlaylistIndex)
		{
			nextIndex = (nextIndex + 1) % _bgmPlaylist.Length;
		}

		return nextIndex;
	}

	private void PlayResolvedBgm(string resourceId)
	{
		if (_currentBgmResourceId == resourceId && _bgmPlayer.Playing)
		{
			return;
		}

		var stream = AssetResolver.LoadAudioResource(resourceId);
		if (stream is null)
		{
			return;
		}

		_currentBgmResourceId = resourceId;
		_bgmPlayer.Stream = stream;
		_bgmPlayer.Play();
		Game.Logger.Info($"Playing BGM: {resourceId}");
	}

	private void InitializeSfxPlayback()
	{
		if (_sfxPlayer.Stream is not AudioStreamPolyphonic)
		{
			throw new InvalidOperationException("SfxPlayer must use an AudioStreamPolyphonic stream.");
		}

		_sfxPlayer.Play();
		_sfxPlayback = _sfxPlayer.GetStreamPlayback() as AudioStreamPlaybackPolyphonic
			?? throw new InvalidOperationException("SfxPlayer playback is not AudioStreamPlaybackPolyphonic.");
	}
}
