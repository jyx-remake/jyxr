using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("shop")]
	public class Shop : BasePojo
	{
		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute]
		public string pic;

		[XmlAttribute]
		public string music;

		private string _key = string.Empty;

		[XmlElement("sale")]
		public List<ShopSale> Sales;

		public override string PK
		{
			get
			{
				return Name;
			}
		}

		[XmlAttribute]
		public string key
		{
			get
			{
				if (string.IsNullOrEmpty(_key))
				{
					return Name;
				}
				return _key;
			}
			set
			{
				_key = value;
			}
		}

		public Dictionary<ItemInstance, int> GetAvaliableSales()
		{
			Dictionary<ItemInstance, int> dictionary = new Dictionary<ItemInstance, int>();
			foreach (ShopSale sale in Sales)
			{
				string text = "shopBuyKey_" + key + sale.name;
				if (sale.limit != -1)
				{
					int num = 0;
					if (RuntimeData.Instance.KeyValues.ContainsKey(text))
					{
						num = int.Parse(RuntimeData.Instance.KeyValues[text]);
					}
					if (num < sale.limit)
					{
						dictionary.Add(new ItemInstance
						{
							Name = sale.name
						}, sale.limit - num);
					}
				}
				else
				{
					dictionary.Add(new ItemInstance
					{
						Name = sale.name
					}, -1);
				}
			}
			return dictionary;
		}

		public ShopSale GetSale(ItemInstance item)
		{
			foreach (ShopSale sale in Sales)
			{
				if (item.Name == sale.name)
				{
					return sale;
				}
			}
			return null;
		}

		public void BuyItem(string itemName, int count = 1)
		{
			string text = "shopBuyKey_" + key + itemName;
			if (RuntimeData.Instance.KeyValues.ContainsKey(text))
			{
				int num = int.Parse(RuntimeData.Instance.KeyValues[text]);
				num += count;
				RuntimeData.Instance.KeyValues[text] = num.ToString();
			}
			else
			{
				RuntimeData.Instance.KeyValues[text] = count.ToString();
			}
		}
	}
}
