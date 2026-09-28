using Game.Application;
using Game.Godot.Resources;
using Godot;

namespace Game.Godot.Audio;

public partial class AudioManager : Node
{
	private const string SfxBusName = "SFX";

	public static AudioManager Instance { get; private set; } = null!;

	private AudioStreamPlayer _bgmPlayer = null!;
	private AudioStreamPlayer _sfxPlayer = null!;
	private AudioStreamPlaybackPolyphonic _sfxPlayback = null!;
	private string? _currentBgmReference;
	private string[] _bgmPlaylist = [];
	private int _lastPlaylistIndex = -1;
	private int _bgmSuspensionCount;
	private bool _bgmPausedBeforeSuspension;

	public override void _Ready()
	{
		_bgmPlayer = GetNode<AudioStreamPlayer>("%BgmPlayer");
		_sfxPlayer = GetNode<AudioStreamPlayer>("%SfxPlayer");
		_bgmPlayer.Finished += OnBgmPlayerFinished;
		InitializeSfxPlayback();
		Instance = this;
	}

	public void PlayBgm(string? reference)
	{
		if (string.IsNullOrWhiteSpace(reference))
		{
			return;
		}

		_bgmPlaylist = [];
		_lastPlaylistIndex = -1;
		PlayResolvedBgm(reference.Trim());
	}

	public void PlayBgm(IReadOnlyList<string> references)
	{
		ArgumentNullException.ThrowIfNull(references);

		var normalizedReferences = references
			.Where(static reference => !string.IsNullOrWhiteSpace(reference))
			.Select(static reference => reference.Trim())
			.ToArray();

		if (normalizedReferences.Length == 0)
		{
			return;
		}

		if (normalizedReferences.Length == 1)
		{
			PlayBgm(normalizedReferences[0]);
			return;
		}

		_bgmPlaylist = normalizedReferences;
		PlayPlaylistIndex(PickNextPlaylistIndex());
	}

	public void StopBgm()
	{
		_bgmPlaylist = [];
		_lastPlaylistIndex = -1;
		_currentBgmReference = null;
		_bgmPlayer.Stop();
		_bgmPlayer.Stream = null;
	}

	public IDisposable SuspendBgm()
	{
		if (_bgmSuspensionCount == 0)
		{
			_bgmPausedBeforeSuspension = _bgmPlayer.StreamPaused;
		}

		_bgmSuspensionCount += 1;
		_bgmPlayer.StreamPaused = true;
		return new BgmSuspension(this);
	}

	public void PlaySfx(string? reference)
	{
		if (string.IsNullOrWhiteSpace(reference))
		{
			return;
		}

		var stream = AssetResolver.LoadAudio(reference);
		if (stream is null)
		{
			return;
		}

		var playbackId = _sfxPlayback.PlayStream(stream, bus: SfxBusName);
		if (playbackId == AudioStreamPlaybackPolyphonic.InvalidId)
		{
			Game.Logger.Warning($"SFX polyphony exhausted, dropped sound: {reference}");
		}
	}

	private void OnBgmPlayerFinished()
	{
		if (_bgmPlaylist.Length > 0)
		{
			PlayPlaylistIndex(PickNextPlaylistIndex());
			return;
		}

		if (!string.IsNullOrWhiteSpace(_currentBgmReference))
		{
			PlayResolvedBgm(_currentBgmReference);
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

	private void PlayResolvedBgm(string reference)
	{
		if (_currentBgmReference == reference && _bgmPlayer.Playing)
		{
			return;
		}

		var stream = AssetResolver.LoadAudio(reference);
		if (stream is null)
		{
			return;
		}

		_currentBgmReference = reference;
		_bgmPlayer.Stream = stream;
		_bgmPlayer.Play();
		_bgmPlayer.StreamPaused = _bgmSuspensionCount > 0;
		Game.Logger.Info($"Playing BGM: {reference}");
	}

	private void ResumeBgm(BgmSuspension suspension)
	{
		if (!suspension.TryDispose())
		{
			return;
		}

		_bgmSuspensionCount -= 1;
		if (_bgmSuspensionCount == 0)
		{
			_bgmPlayer.StreamPaused = _bgmPausedBeforeSuspension;
		}
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

	private sealed class BgmSuspension(AudioManager owner) : IDisposable
	{
		private bool _isDisposed;

		public void Dispose() => owner.ResumeBgm(this);

		public bool TryDispose()
		{
			if (_isDisposed)
			{
				return false;
			}

			_isDisposed = true;
			return true;
		}
	}
}
