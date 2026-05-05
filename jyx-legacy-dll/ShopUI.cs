using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
	public static Shop CurrentShop;

	public static ShopType Type;

	public GameObject BackgroundObj;

	public GameObject BuyButtonTextObj;

	public GameObject SellButtonTextObj;

	public GameObject ItemMenuObj;

	public GameObject InfoTextObj;

	public GameObject MoneyTextObj;

	public GameObject YuanbaoTextObj;

	public GameObject ItemDetailPanelObj;

	public GameObject MessageBoxObj;

	public GameObject OneKeyBuySellToggleObj;

	private ShopStatus CurrentStatus;

	private ItemMenu itemMenu
	{
		get
		{
			return ItemMenuObj.GetComponent<ItemMenu>();
		}
	}

	private Text infoText
	{
		get
		{
			return InfoTextObj.GetComponent<Text>();
		}
	}

	private ItemSkillDetailPanelUI itemDetailPanel
	{
		get
		{
			return ItemDetailPanelObj.GetComponent<ItemSkillDetailPanelUI>();
		}
	}

	private MessageBoxUI messageBox
	{
		get
		{
			return MessageBoxObj.GetComponent<MessageBoxUI>();
		}
	}

	private bool IsOneKeyBuySell
	{
		get
		{
			return OneKeyBuySellToggleObj.GetComponent<Toggle>().isOn;
		}
	}

	private void ShowMoney()
	{
		MoneyTextObj.GetComponent<Text>().text = RuntimeData.Instance.Money.ToString();
		YuanbaoTextObj.GetComponent<Text>().text = RuntimeData.Instance.Yuanbao.ToString();
	}

	private void BuySale(ItemInstance item)
	{
		ShopSale sale = CurrentShop.GetSale(item);
		if (sale.yuanbao == -1)
		{
			if (RuntimeData.Instance.Money < sale.price)
			{
				messageBox.Show("购买失败", "你身上的钱不够啊", Color.white);
				return;
			}
			RuntimeData.Instance.Money -= sale.price;
			RuntimeData.Instance.addItem(item);
			AudioManager.Instance.PlayEffect("音效.装备");
			CurrentShop.BuyItem(item.Name);
			Show(ShopStatus.BUY);
		}
		else if (RuntimeData.Instance.Yuanbao < sale.yuanbao)
		{
			messageBox.Show("购买失败", "你身上的元宝不够啊", Color.white);
		}
		else
		{
			RuntimeData.Instance.Yuanbao -= sale.yuanbao;
			RuntimeData.Instance.addItem(item);
			AudioManager.Instance.PlayEffect("音效.装备");
			CurrentShop.BuyItem(item.Name);
			Show(ShopStatus.BUY);
		}
	}

	private void Show(ShopStatus status)
	{
		ShowMoney();
		CurrentStatus = status;
		switch (status)
		{
		case ShopStatus.BUY:
		{
			OneKeyBuySellToggleObj.SetActive(true);
			infoText.text = "客官需要买点什么？";
			Dictionary<ItemInstance, int> avaliableSales = CurrentShop.GetAvaliableSales();
			itemMenu.Show(string.Empty, avaliableSales, delegate(object obj)
			{
				ItemInstance item = obj as ItemInstance;
				ShopSale sale = CurrentShop.GetSale(item);
				infoText.text = sale.GetPriceInfo();
				if (IsOneKeyBuySell)
				{
					BuySale(item);
				}
				else
				{
					itemDetailPanel.Show(item, ItemDetailMode.Selectable, delegate
					{
						BuySale(item);
					});
				}
			});
			break;
		}
		case ShopStatus.SELL:
			OneKeyBuySellToggleObj.SetActive(true);
			infoText.text = "请问您要卖什么？";
			itemMenu.Show(string.Empty, RuntimeData.Instance.Items, delegate(object obj)
			{
				ItemInstance item = obj as ItemInstance;
				int price = item.price / 2;
				if (IsOneKeyBuySell)
				{
					RuntimeData.Instance.Money += price;
					RuntimeData.Instance.addItem(item, -1);
					AudioManager.Instance.PlayEffect("音效.装备");
					Show(ShopStatus.SELL);
				}
				else
				{
					infoText.text = string.Format("我出{0}两银子,你卖吗？", price);
					itemDetailPanel.Show(item, ItemDetailMode.Sellable, delegate
					{
						RuntimeData.Instance.Money += price;
						RuntimeData.Instance.addItem(item, -1);
						AudioManager.Instance.PlayEffect("音效.装备");
						Show(ShopStatus.SELL);
					}, null, delegate
					{
						int num = RuntimeData.Instance.Items[item];
						RuntimeData.Instance.Money += price * num;
						RuntimeData.Instance.addItem(item, -num);
						AudioManager.Instance.PlayEffect("音效.装备");
						Show(ShopStatus.SELL);
					});
				}
			}, null, delegate(object obj)
			{
				ItemInstance itemInstance = obj as ItemInstance;
				return itemInstance.price > 0;
			});
			break;
		case ShopStatus.XIANGZI_GET:
			OneKeyBuySellToggleObj.SetActive(false);
			infoText.text = "请问您取出什么物品？当前总数" + RuntimeData.Instance.XiangziCount + "/" + RuntimeData.Instance.MaxXiangziItemCount;
			itemMenu.Show(string.Empty, RuntimeData.Instance.Xiangzi, delegate(object obj)
			{
				ItemInstance item = obj as ItemInstance;
				itemDetailPanel.Show(item, ItemDetailMode.Selectable, delegate
				{
					RuntimeData.Instance.xiangziAddItem(item, -1);
					RuntimeData.Instance.addItem(item);
					AudioManager.Instance.PlayEffect("音效.装备");
					Show(ShopStatus.XIANGZI_GET);
				});
			});
			break;
		case ShopStatus.XIANGZI_PUT:
			OneKeyBuySellToggleObj.SetActive(false);
			infoText.text = "请问您要存入什么物品？当前总数" + RuntimeData.Instance.XiangziCount + "/" + RuntimeData.Instance.MaxXiangziItemCount;
			if (RuntimeData.Instance.XiangziCount >= RuntimeData.Instance.MaxXiangziItemCount)
			{
				infoText.text += "【已满，无法再存更多了】";
			}
			itemMenu.Show(string.Empty, RuntimeData.Instance.Items, delegate(object obj)
			{
				if (RuntimeData.Instance.XiangziCount < RuntimeData.Instance.MaxXiangziItemCount)
				{
					ItemInstance item = obj as ItemInstance;
					if (item.Type != ItemType.Mission && item.Type != ItemType.Canzhang)
					{
						itemDetailPanel.Show(item, ItemDetailMode.Selectable, delegate
						{
							RuntimeData.Instance.xiangziAddItem(item);
							RuntimeData.Instance.addItem(item, -1);
							AudioManager.Instance.PlayEffect("音效.装备");
							Show(ShopStatus.XIANGZI_PUT);
						});
					}
				}
			}, null, delegate(object obj)
			{
				ItemInstance itemInstance = obj as ItemInstance;
				return (itemInstance.Type != ItemType.Mission && itemInstance.Type != ItemType.Canzhang) ? true : false;
			});
			break;
		}
	}

	public void OnBuy()
	{
		if (Type == ShopType.SHOP)
		{
			Show(ShopStatus.BUY);
		}
		else
		{
			Show(ShopStatus.XIANGZI_PUT);
		}
	}

	public void OnSell()
	{
		if (Type == ShopType.SHOP)
		{
			Show(ShopStatus.SELL);
		}
		else
		{
			Show(ShopStatus.XIANGZI_GET);
		}
	}

	public void OnExit()
	{
		RuntimeData.Instance.gameEngine.SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
	}

	private void Start()
	{
		if (Type == ShopType.SHOP)
		{
			if (CurrentShop == null)
			{
				Debug.LogError("未定义SHOP！");
			}
			else
			{
				Show(ShopStatus.BUY);
			}
		}
		else if (Type == ShopType.XIANGZI)
		{
			BuyButtonTextObj.GetComponent<Text>().text = "存入";
			SellButtonTextObj.GetComponent<Text>().text = "取出";
			BackgroundObj.GetComponent<Image>().sprite = Resource.GetImage("地图.客栈");
			Show(ShopStatus.XIANGZI_GET);
		}
	}

	private void Update()
	{
	}
}
