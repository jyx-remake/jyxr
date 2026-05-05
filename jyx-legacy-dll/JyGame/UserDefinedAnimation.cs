using UnityEngine;

namespace JyGame
{
	public class UserDefinedAnimation : MonoBehaviour
	{
		public Sprite[] stands;

		public Sprite[] attacks;

		public Sprite[] moves;

		public Sprite[] beattacks;

		public Sprite[] effects;

		public SpriteRenderer bindImage;

		public string currentState;

		private int currentIndex;

		private CommonSettings.VoidCallBack _callback;

		public void Play(string state, CommonSettings.VoidCallBack callback = null)
		{
			currentState = state;
			currentIndex = 0;
			_callback = callback;
		}

		private void Start()
		{
			InvokeRepeating("PlayNextFrame", 1f / 6f, 1f / 6f);
		}

		public void PlayNextFrame()
		{
			if (string.IsNullOrEmpty(currentState))
			{
				return;
			}
			Sprite[] array = null;
			switch (currentState)
			{
			case "stand":
				array = stands;
				break;
			case "attack":
				array = attacks;
				break;
			case "move":
				array = moves;
				break;
			case "be":
				array = beattacks;
				break;
			case "effect":
				array = effects;
				break;
			}
			if (array == null)
			{
				return;
			}
			if (currentIndex >= array.Length)
			{
				currentIndex = 0;
				if (currentState == "attack" || currentState == "be")
				{
					currentState = "stand";
					PlayNextFrame();
					return;
				}
				if (currentState == "effect")
				{
					if (_callback != null)
					{
						_callback();
					}
					Object.Destroy(base.gameObject);
				}
			}
			Sprite sprite = array[currentIndex];
			bindImage.sprite = sprite;
			currentIndex++;
		}
	}
}
