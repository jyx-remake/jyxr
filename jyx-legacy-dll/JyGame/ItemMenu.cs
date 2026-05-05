using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JyGame
{
	public class ItemMenu : MonoBehaviour
	{
		public GameObject ItemPrefab;

		public GameObject SelectMenuObj;

		public GameObject ItemPreviewPanelObj;

		private ItemFilter _filter;

		private string _title;

		private Dictionary<ItemInstance, int> _items;

		private CommonSettings.ObjectCallBack _callback;

		private CommonSettings.VoidCallBack _cancelCallback;

		private CommonSettings.JudgeCallback _isItemActiveCallback;

		private SelectMenu selectMenu
		{
			get
			{
				return SelectMenuObj.GetComponent<SelectMenu>();
			}
		}

		public void OnButtonAll()
		{
			SetFilter(ItemFilter.All);
		}

		public void OnButtonZhuangbei()
		{
			SetFilter(ItemFilter.Zhuangbei);
		}

		public void OnButtonMiji()
		{
			SetFilter(ItemFilter.Miji);
		}

		public void OnButtonCosta()
		{
			SetFilter(ItemFilter.Costa);
		}

		public void OnButtonSpecial()
		{
			SetFilter(ItemFilter.Special);
		}

		public void SetFilter(ItemFilter filter)
		{
			_filter = filter;
			Refresh();
		}

		private void Refresh()
		{
			ItemPreviewPanelObj.SetActive(false);
			base.transform.Find("TitleText").GetComponent<Text>().text = _title;
			selectMenu.Clear();
			List<KeyValuePair<ItemInstance, int>> list = new List<KeyValuePair<ItemInstance, int>>();
			foreach (KeyValuePair<ItemInstance, int> item2 in _items)
			{
				ItemType type = item2.Key.Type;
				if ((_filter != ItemFilter.Costa || type == ItemType.Costa) && (_filter != ItemFilter.Miji || type == ItemType.Book || type == ItemType.SpeicalSkillBook || type == ItemType.TalentBook) && (_filter != ItemFilter.Zhuangbei || type == ItemType.Weapon || type == ItemType.Armor || type == ItemType.Accessories) && (_filter != ItemFilter.Special || type == ItemType.Special || type == ItemType.Upgrade || type == ItemType.Canzhang))
				{
					list.Add(item2);
				}
			}
			list.Sort((KeyValuePair<ItemInstance, int> x, KeyValuePair<ItemInstance, int> y) => x.Key.PK.CompareTo(y.Key.PK));
			foreach (KeyValuePair<ItemInstance, int> item3 in list)
			{
				ItemInstance item = item3.Key;
				GameObject gameObject = Object.Instantiate(ItemPrefab);
				gameObject.GetComponent<ItemUI>().Bind(item, item3.Value, delegate
				{
					_callback(item);
				}, _isItemActiveCallback, ItemPreviewPanelObj);
				selectMenu.AddItem(gameObject);
			}
			if (_cancelCallback != null)
			{
				selectMenu.Show(delegate
				{
					Hide();
					CancelButtonClicked();
				});
			}
			else
			{
				selectMenu.Show();
			}
		}

		public void Show(string title, List<ItemInstance> items, CommonSettings.ObjectCallBack callback, CommonSettings.VoidCallBack cancelCallback = null, CommonSettings.JudgeCallback isItemActiveCallback = null)
		{
			Dictionary<ItemInstance, int> dictionary = new Dictionary<ItemInstance, int>();
			foreach (ItemInstance item in items)
			{
				if (dictionary.ContainsKey(item))
				{
					Dictionary<ItemInstance, int> dictionary3;
					Dictionary<ItemInstance, int> dictionary2 = (dictionary3 = dictionary);
					ItemInstance key2;
					ItemInstance key = (key2 = item);
					int num = dictionary3[key2];
					dictionary2[key] = num + 1;
				}
				else
				{
					dictionary.Add(item, 1);
				}
			}
			Show(title, dictionary, callback, cancelCallback, isItemActiveCallback);
		}

		public void Show(string title, Dictionary<ItemInstance, int> items, CommonSettings.ObjectCallBack callback, CommonSettings.VoidCallBack cancelCallback = null, CommonSettings.JudgeCallback isItemActiveCallback = null)
		{
			_title = title;
			_cancelCallback = cancelCallback;
			_items = items;
			_callback = callback;
			_isItemActiveCallback = isItemActiveCallback;
			base.gameObject.SetActive(true);
			Refresh();
		}

		public void Hide()
		{
			base.gameObject.SetActive(false);
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
