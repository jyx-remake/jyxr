using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JyGame
{
	public class UserDefinedAnimationManager
	{
		private static UserDefinedAnimationManager _instance;

		public UserDefinedAnimationsData data;

		public MonoBehaviour _parent;

		private CommonSettings.VoidCallBack _callback;

		private List<UserDefinedFrame> _spriteFiles = new List<UserDefinedFrame>();

		private int _loadIndex;

		public static UserDefinedAnimationManager instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new UserDefinedAnimationManager();
				}
				return _instance;
			}
			private set
			{
				_instance = value;
			}
		}

		public void Init(CommonSettings.VoidCallBack callback)
		{
			if (!CommonSettings.MOD_MODE)
			{
				return;
			}
			_callback = callback;
			_spriteFiles.Clear();
			Tools.getFileContentFromUrl(_parent, ModManager.ModBaseUrl + "Animations/animations.xml", delegate(string content)
			{
				UserDefinedAnimationsData userDefinedAnimationsData = Tools.DeserializeXML<UserDefinedAnimationsData>(content);
				if (userDefinedAnimationsData != null)
				{
					data = userDefinedAnimationsData;
				}
				LoadSprites();
			}, delegate
			{
				callback();
			});
		}

		public bool HasAnimation(string name, string type = "role")
		{
			if (data == null || data.animations == null)
			{
				return false;
			}
			UserDefinedAnimtionData[] animations = data.animations;
			foreach (UserDefinedAnimtionData userDefinedAnimtionData in animations)
			{
				if (userDefinedAnimtionData.name == name && userDefinedAnimtionData.type == type)
				{
					return true;
				}
			}
			return false;
		}

		public GameObject GenerateObject(string name, string type = "role")
		{
			UserDefinedAnimtionData[] animations = data.animations;
			foreach (UserDefinedAnimtionData userDefinedAnimtionData in animations)
			{
				if (userDefinedAnimtionData.name == name && userDefinedAnimtionData.type == type)
				{
					GameObject original = Resources.Load<GameObject>("UI/UserDefineAnimation");
					GameObject gameObject = Object.Instantiate(original);
					UserDefinedAnimation userDefinedAnimation = gameObject.AddComponent<UserDefinedAnimation>();
					userDefinedAnimation.bindImage = gameObject.transform.Find("sprite").GetComponent<SpriteRenderer>();
					userDefinedAnimtionData.FillAnimation(userDefinedAnimation);
					return gameObject;
				}
			}
			return null;
		}

		public void AddSpriteFile(UserDefinedFrame file)
		{
			_spriteFiles.Add(file);
		}

		public void LoadSprites()
		{
			UserDefinedAnimtionData[] animations = data.animations;
			foreach (UserDefinedAnimtionData userDefinedAnimtionData in animations)
			{
				_spriteFiles.AddRange(userDefinedAnimtionData.attacks);
				_spriteFiles.AddRange(userDefinedAnimtionData.beattacks);
				_spriteFiles.AddRange(userDefinedAnimtionData.moves);
				_spriteFiles.AddRange(userDefinedAnimtionData.stands);
				_spriteFiles.AddRange(userDefinedAnimtionData.effects);
			}
			_loadIndex = 0;
			LoadNext();
		}

		public void LoadNext()
		{
			if (_loadIndex >= _spriteFiles.Count)
			{
				if (_callback != null)
				{
					_callback();
				}
				return;
			}
			UserDefinedFrame sp = _spriteFiles[_loadIndex];
			ResourceManager.progress = _loadIndex / _spriteFiles.Count;
			ResourceManager.detail = "正在载入MOD定义动画文件..." + sp.file;
			_parent.StartCoroutine(Tools.DownloadImage(ModManager.ModBaseUrl + "Animations/" + sp.file, delegate(object sprite)
			{
				sp.sprite = sprite as Sprite;
				_loadIndex++;
				LoadNext();
			}, delegate
			{
			}, new Vector2(sp.x, sp.y), 1f / sp.scale));
		}

		public IEnumerator LoadNextSprite()
		{
			UserDefinedFrame sp = _spriteFiles[_loadIndex];
			FileInfo file = new FileInfo("Mod/Animations/" + sp.file);
			ResourceManager.progress = 0f;
			ResourceManager.detail = "正在载入MOD定义动画文件..." + sp.file;
			WWW www = new WWW("file://" + file.FullName);
			yield return www;
			if (!string.IsNullOrEmpty(www.error))
			{
				Debug.LogError(www.error);
			}
			else
			{
				Sprite tmp = Sprite.Create(www.texture, new Rect(0f, 0f, www.texture.width, www.texture.height), new Vector2(sp.x, sp.y), 1f / sp.scale);
				sp.sprite = tmp;
				_loadIndex++;
				LoadNext();
			}
			www.Dispose();
		}
	}
}
