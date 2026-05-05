using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("sale")]
	public class ShopSale
	{
		[XmlAttribute]
		public string name;

		private int _price = -1;

		[XmlAttribute]
		public int limit = -1;

		[XmlAttribute]
		public int yuanbao = -1;

		[XmlAttribute]
		public int price
		{
			get
			{
				if (_price == -1 && yuanbao == -1)
				{
					Item item = ResourceManager.Get<Item>(name);
					return item.price;
				}
				if (_price > 0)
				{
					return _price;
				}
				return -1;
			}
			set
			{
				_price = value;
			}
		}

		public string GetPriceInfo()
		{
			string text = name + ",价格:";
			if (price > 0)
			{
				text = text + " 银两 " + price;
			}
			else if (yuanbao > 0)
			{
				string text2 = text;
				text = text2 + " <color='yellow'>元宝" + yuanbao + "</color>";
			}
			return text;
		}
	}
}
