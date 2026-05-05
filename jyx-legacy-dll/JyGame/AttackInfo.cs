using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace JyGame
{
	public class AttackInfo : MonoBehaviour
	{
		public void Display(int x, int y, string text, Color c, Transform parent, CommonSettings.VoidCallBack callback)
		{
			base.gameObject.GetComponent<Text>().text = text;
			base.gameObject.GetComponent<Text>().color = c;
			base.transform.SetParent(parent);
			base.transform.position = new Vector3(BattleField.ToScreenX(x), BattleField.ToScreenY(y) + 90, 0f);
			base.transform.DOMove(new Vector3(BattleField.ToScreenX(x) + Tools.GetRandomInt(-50, 50), BattleField.ToScreenY(y) + 150 + Tools.GetRandomInt(0, 50)), 1.5f).SetEase(Ease.OutElastic).OnComplete(delegate
			{
				Object.Destroy(base.gameObject);
				if (callback != null)
				{
					callback();
				}
			});
			base.transform.DOScale(new Vector3(1.3f, 1.3f), 1.5f).SetEase(Ease.OutExpo);
		}

		public void DisplayPopinfo(string text, Color c, Transform parent)
		{
			base.gameObject.GetComponent<Text>().text = text;
			base.gameObject.GetComponent<Text>().color = c;
			base.gameObject.GetComponent<Text>().fontSize = 30;
			base.transform.SetParent(parent);
			base.transform.position = new Vector3(0f, 0f);
			base.transform.DOMoveY(100f, 2f).OnComplete(delegate
			{
				Object.Destroy(base.gameObject);
			});
		}

		private void Start()
		{
		}

		private void Update()
		{
		}
	}
}
