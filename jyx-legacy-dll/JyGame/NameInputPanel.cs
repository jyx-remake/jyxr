using UnityEngine;
using UnityEngine.UI;

namespace JyGame
{
	public class NameInputPanel : MonoBehaviour
	{
		public GameObject messageBoxObj;

		private CommonSettings.StringCallBack _confirmCallBack;

		private Text InputName
		{
			get
			{
				return base.transform.Find("NameInputField").Find("Text").GetComponent<Text>();
			}
		}

		private InputField Input
		{
			get
			{
				return base.transform.Find("NameInputField").GetComponent<InputField>();
			}
		}

		public void Clear()
		{
			InputName.text = string.Empty;
		}

		public void Show(string inputNameText, CommonSettings.StringCallBack confirmCallBack = null)
		{
			_confirmCallBack = confirmCallBack;
			Input.text = inputNameText;
			base.gameObject.SetActive(true);
		}

		public void Hide()
		{
			base.gameObject.SetActive(false);
		}

		public void ConfirmButtonClicked()
		{
			if (string.IsNullOrEmpty(InputName.text))
			{
				return;
			}
			InputName.text = InputName.text.Replace(" ", string.Empty);
			if (InputName.text != "小虾米" && InputName.text != "铃兰")
			{
				foreach (Role item in ResourceManager.GetAll<Role>())
				{
					if (item.Name == InputName.text)
					{
						if (messageBoxObj != null)
						{
							messageBoxObj.GetComponent<MessageBoxUI>().Show("错误", "名字不能与已有NPC重名!", Color.red);
						}
						return;
					}
				}
			}
			if (_confirmCallBack != null)
			{
				Hide();
				_confirmCallBack(InputName.text);
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
