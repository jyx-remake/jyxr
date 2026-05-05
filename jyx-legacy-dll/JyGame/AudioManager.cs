using UnityEngine;

namespace JyGame
{
	public class AudioManager : MonoBehaviour
	{
		public AudioSource audioMgr;

		public AudioSource[] effectChannels;

		private string currentMusicKey = string.Empty;

		private static AudioManager instance;

		private static int effectChannelIndex;

		public static AudioManager Instance
		{
			get
			{
				if (!RuntimeData.Instance.IsInited)
				{
					RuntimeData.Instance.Init();
				}
				if (instance == null)
				{
					GameObject original = Resources.Load<GameObject>("UI/AudioManagerObj");
					GameObject gameObject = Object.Instantiate(original);
					instance = gameObject.GetComponent<AudioManager>();
				}
				return instance;
			}
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Object.Destroy(base.gameObject);
			}
			else
			{
				instance = this;
			}
			Object.DontDestroyOnLoad(base.gameObject);
		}

		private void Start()
		{
			Mute(!Configer.IsMusicOn);
			MuteEffect(!Configer.IsEffectOn);
		}

		private void Update()
		{
			if (audioMgr.clip != null && !audioMgr.isPlaying && audioMgr.clip.isReadyToPlay)
			{
				audioMgr.Play();
			}
		}

		public void Play(string key)
		{
			if (currentMusicKey.Equals(key))
			{
				return;
			}
			Resource.GetMusic(key, delegate(AudioClip ac)
			{
				if (!(ac == null))
				{
					audioMgr.clip = ac;
					currentMusicKey = key;
				}
			});
		}

		public void PlayEffect(string key)
		{
			effectChannelIndex++;
			if (effectChannelIndex >= effectChannels.Length)
			{
				effectChannelIndex = 0;
			}
			Resource.GetMusic(key, delegate(AudioClip ac)
			{
				if (!(ac == null))
				{
					effectChannels[effectChannelIndex].clip = ac;
					effectChannels[effectChannelIndex].Play();
				}
			});
		}

		public void PlayRandomEffect(string[] paths)
		{
			PlayEffect(paths[Tools.GetRandomInt(0, paths.Length - 1)]);
		}

		public void Stop()
		{
			audioMgr.Stop();
			currentMusicKey = string.Empty;
			Debug.Log("Stop background music");
		}

		public void Mute(bool isMute)
		{
			audioMgr.mute = isMute;
		}

		public void MuteEffect(bool isMute)
		{
			AudioSource[] array = effectChannels;
			foreach (AudioSource audioSource in array)
			{
				audioSource.mute = isMute;
			}
		}
	}
}
