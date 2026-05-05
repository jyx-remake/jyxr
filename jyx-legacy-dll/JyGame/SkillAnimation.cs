using UnityEngine;

namespace JyGame
{
	public class SkillAnimation : MonoBehaviour
	{
		private CommonSettings.VoidCallBack _callback;

		private void Start()
		{
		}

		private void Update()
		{
		}

		public void Display(int x, int y, CommonSettings.VoidCallBack callback = null)
		{
			base.transform.position = new Vector3(BattleField.ToScreenX(x), BattleField.ToScreenY(y), -200 + y);
			_callback = callback;
		}

		public void DisplayEffectNotFollowSprite()
		{
			base.transform.position = new Vector3(0f, 0f, -1f);
		}

		public void DisplayEffect(int x, int y)
		{
			base.transform.position = new Vector3(BattleField.ToScreenX(x), BattleField.ToScreenY(y) + 50, -1f);
		}

		public void SetCallback(CommonSettings.VoidCallBack callback)
		{
			_callback = callback;
		}

		private void Clear()
		{
			Object.Destroy(base.gameObject);
			if (_callback != null)
			{
				_callback();
			}
		}
	}
}
