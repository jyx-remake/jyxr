using UnityEngine;
using UnityEngine.UI;

namespace JyGame
{
	public class HeadSelectMenu : MonoBehaviour
	{
		public GameObject ItemPrefab;

		public GameObject SelectMenuObj;

		public string currentSelection = string.Empty;

		public CommonSettings.StringCallBack _Callback;

		private SelectMenu selectMenu
		{
			get
			{
				return SelectMenuObj.GetComponent<SelectMenu>();
			}
		}

		public void Show(string[] heads, CommonSettings.StringCallBack Callback)
		{
			_Callback = Callback;
			base.gameObject.SetActive(true);
			selectMenu.Clear();
			currentSelection = heads[0];
			foreach (string text in heads)
			{
				string head = text;
				GameObject item = Object.Instantiate(ItemPrefab);
				item.gameObject.SetActive(true);
				item.transform.Find("IconImage").GetComponent<Image>().sprite = Resource.GetImage(head);
				item.GetComponent<Button>().onClick.AddListener(delegate
				{
					foreach (Transform item2 in selectMenu.selectContent)
					{
						Transform transform2 = item2;
						item2.Find("StatusSelected").gameObject.SetActive(false);
					}
					item.transform.Find("StatusSelected").gameObject.SetActive(true);
					currentSelection = head;
				});
				if (text.Equals(heads[0]))
				{
					item.transform.Find("StatusSelected").gameObject.SetActive(true);
				}
				selectMenu.AddItem(item);
			}
			selectMenu.Show();
		}

		public void OnConfirm()
		{
			_Callback(currentSelection);
		}

		public void Hide()
		{
			base.gameObject.SetActive(false);
		}

		private void Start()
		{
		}

		private void Update()
		{
		}
	}
}
