using UnityEngine;
using UnityEngine.UI;

namespace JyGame
{
	public class LogMenu : MonoBehaviour
	{
		private CommonSettings.VoidCallBack _cancelCallback;

		private Text logText
		{
			get
			{
				return base.transform.Find("SelectPanel").Find("LogText").GetComponent<Text>();
			}
		}

		public void Clear()
		{
			logText.text = string.Empty;
		}

		public void Show(CommonSettings.VoidCallBack cancelCallback = null)
		{
			setLog(RuntimeData.Instance.Log);
			_cancelCallback = cancelCallback;
			base.gameObject.SetActive(true);
		}

		public void Hide()
		{
			base.gameObject.SetActive(false);
		}

		public void setLog(string text)
		{
			logText.text = text;
		}

		public void CancelButtonClicked()
		{
			if (_cancelCallback != null)
			{
				_cancelCallback();
			}
		}

		private void Start()
		{
		}

		private void Update()
		{
		}
	}
}
