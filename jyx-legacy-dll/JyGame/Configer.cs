using UnityEngine;

namespace JyGame
{
	public class Configer
	{
		public static bool IsBeta;

		private static bool _isAutoBattle;

		private static bool _isAutoSave;

		private static bool _isSpeedUp;

		private static bool _isMusicOn;

		private static bool _isEffectOn;

		private static bool _isBigMapFullScreen;

		private static bool _isBattleTipShow;

		public static bool IsAutoBattle
		{
			get
			{
				return _isAutoBattle;
			}
			set
			{
				_isAutoBattle = value;
				UpdateKey("config.auto_battle", value);
			}
		}

		public static bool IsAutoSave
		{
			get
			{
				if (RuntimeData.Instance.AutoSaveOnly)
				{
					return true;
				}
				return _isAutoSave;
			}
			set
			{
				_isAutoSave = value;
				UpdateKey("config.auto_save", value);
			}
		}

		public static bool IsSpeedUp
		{
			get
			{
				return _isSpeedUp;
			}
			set
			{
				_isSpeedUp = value;
				UpdateKey("config.speed_up", _isSpeedUp);
			}
		}

		public static bool IsMusicOn
		{
			get
			{
				return _isMusicOn;
			}
			set
			{
				_isMusicOn = value;
				AudioManager.Instance.Mute(!value);
				UpdateKey("config.mute", !value);
			}
		}

		public static bool IsEffectOn
		{
			get
			{
				return _isEffectOn;
			}
			set
			{
				_isEffectOn = value;
				AudioManager.Instance.MuteEffect(!value);
				UpdateKey("config.effect_mute", !value);
			}
		}

		public static bool IsBigmapFullScreen
		{
			get
			{
				if (!CommonSettings.TOUCH_MODE)
				{
					return true;
				}
				return _isBigMapFullScreen;
			}
			set
			{
				_isBigMapFullScreen = value;
				if (RuntimeData.Instance.mapUI != null)
				{
					RuntimeData.Instance.mapUI.OnMapScaleChanged();
				}
				UpdateKey("config.bigmap_fullscreen", value);
			}
		}

		public static bool IsBattleTipShow
		{
			get
			{
				return _isBattleTipShow;
			}
			set
			{
				_isBattleTipShow = value;
				UpdateKey("config.battle_tip", value);
			}
		}

		static Configer()
		{
			_isMusicOn = true;
			_isEffectOn = true;
			IsAutoBattle = PlayerPrefs.HasKey("config.auto_battle");
			IsSpeedUp = PlayerPrefs.HasKey("config.speed_up");
			IsMusicOn = !PlayerPrefs.HasKey("config.mute");
			IsEffectOn = !PlayerPrefs.HasKey("config.effect_mute");
			IsAutoSave = PlayerPrefs.HasKey("config.auto_save");
			IsBigmapFullScreen = PlayerPrefs.HasKey("config.bigmap_fullscreen");
			IsBattleTipShow = PlayerPrefs.HasKey("config.battle_tip");
		}

		private static void UpdateKey(string key, bool value)
		{
			if (value)
			{
				PlayerPrefs.SetInt(key, 1);
				PlayerPrefs.Save();
			}
			else
			{
				PlayerPrefs.DeleteKey(key);
				PlayerPrefs.Save();
			}
		}
	}
}
