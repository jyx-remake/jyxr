using System.Collections;
using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class WhacAMole : MonoBehaviour
{
	private class MolePojo
	{
		private float activeTime;

		private static float timeout = 0.7f;

		private static string[] items = new string[10] { "大还丹", "大蟠桃", "冬虫夏草", "九转熊蛇丸", "生生造化丹", "柳叶刀", "金丝道袍", "黄金项链", "血刀", "乌蚕衣" };

		private int type;

		public int Point { get; private set; }

		public string Audio { get; private set; }

		public string Item { get; private set; }

		public bool IsTimeout
		{
			get
			{
				if (Time.time - activeTime > timeout)
				{
					if (type == 0)
					{
						activeTime = Time.time + Random.Range(0.7f, 5f);
					}
					else
					{
						activeTime = Time.time + (float)Random.Range(4, 10);
					}
					return true;
				}
				return false;
			}
		}

		public bool IsAwake
		{
			get
			{
				return Time.time > activeTime;
			}
		}

		public MolePojo(int point)
			: this(point, "音效.男惨叫")
		{
		}

		public MolePojo()
		{
			type = 1;
			activeTime = Time.time + (float)Random.Range(4, 10);
		}

		public MolePojo(int point, string audio)
		{
			Point = point;
			Audio = audio;
			activeTime = Time.time + Random.Range(0.7f, 3f);
		}

		private string RandomItem(params string[] names)
		{
			if (!RuntimeData.Instance.KeyValues.ContainsKey("_tfkz"))
			{
				RuntimeData.Instance.KeyValues.Add("_tfkz", string.Empty);
			}
			ArrayList arrayList = new ArrayList();
			for (int i = 0; i < names.Length; i++)
			{
				if (!uniqueItems.Contains(names[i]) || !RuntimeData.Instance.KeyValues["_tfkz"].Contains(names[i]))
				{
					arrayList.Add(names[i]);
				}
			}
			return arrayList[Random.Range(0, arrayList.Count)] as string;
		}

		public void Active()
		{
			if (type == 1)
			{
				Item = RandomItem(items);
			}
			activeTime = Time.time;
		}
	}

	public GameObject dong;

	public GameObject xi;

	public GameObject nan;

	public GameObject bei;

	public GameObject wu;

	public GameObject bomb;

	public GameObject dialogPanel;

	private DodgeUI dialog;

	private Dictionary<GameObject, MolePojo> pojos = new Dictionary<GameObject, MolePojo>();

	private ArrayList hidePojo = new ArrayList();

	private ArrayList showPojo = new ArrayList();

	private Dictionary<string, int> items = new Dictionary<string, int>();

	private static ArrayList uniqueItems = new ArrayList { "柳叶刀", "金丝道袍", "黄金项链", "乌蚕衣" };

	private int point;

	private float bombTime;

	private float startTime;

	private bool flag = true;

	private static Vector3 target = new Vector3(720f, -450f);

	private Vector3 moveDirect;

	private void Start()
	{
		pojos.Add(dong, new MolePojo(-2, "音效.敢点老娘"));
		pojos.Add(xi, new MolePojo(4));
		pojos.Add(nan, new MolePojo(2));
		pojos.Add(bei, new MolePojo(3));
		pojos.Add(wu, new MolePojo());
		hidePojo.AddRange(pojos.Keys);
		dialog = dialogPanel.GetComponent<DodgeUI>();
		startTime = Time.time;
	}

	private void GameOver()
	{
		Queue queue = new Queue();
		foreach (string key in items.Keys)
		{
			RuntimeData.Instance.addItem(ResourceManager.Get<Item>(key), items[key]);
			queue.Enqueue("白展堂|获得" + key + " x " + items[key]);
		}
		RuntimeData.Instance.biliPoint = RuntimeData.Instance.biliPoint + point;
		if (RuntimeData.Instance.Team[0].bili >= CommonSettings.SMALLGAME_MAX_ATTRIBUTE)
		{
			RuntimeData.Instance.biliPoint = 0;
			queue.Enqueue("主角|貌似现在练这个已经没法提高臂力了。。");
		}
		else if (RuntimeData.Instance.biliPoint >= RuntimeData.Instance.Team[0].bili)
		{
			RuntimeData.Instance.biliPoint = 0;
			queue.Enqueue("主角|你的臂力进步了！臂力从【" + RuntimeData.Instance.Team[0].bili + "】提高至【" + (RuntimeData.Instance.Team[0].bili + 5) + "】！");
			RuntimeData.Instance.Team[0].bili += 5;
		}
		else
		{
			queue.Enqueue("主角|你练习了一会儿，对臂力功夫似乎有了一些心得...");
		}
		ShowDialog(queue, delegate
		{
			RuntimeData.Instance.gameEngine.SwitchGameScene("map", "同福客栈");
		});
	}

	private void ShowDialog(string msg, CommonSettings.VoidCallBack callback = null)
	{
		string[] array = msg.Split('|');
		dialog.Show(array[0], array[1], callback);
	}

	private void ShowDialog(Queue msgs, CommonSettings.VoidCallBack callback = null)
	{
		ShowDialog(msgs.Dequeue() as string, delegate
		{
			if (msgs.Count > 0)
			{
				ShowDialog(msgs, callback);
			}
			else
			{
				callback();
			}
		});
	}

	private void Update()
	{
		if (!flag)
		{
			return;
		}
		if (Time.time - startTime > 30f)
		{
			flag = false;
			foreach (GameObject item in showPojo)
			{
				item.SetActive(false);
			}
			GameOver();
			return;
		}
		for (int num = hidePojo.Count - 1; num >= 0; num--)
		{
			GameObject gameObject2 = hidePojo[num] as GameObject;
			if (pojos[gameObject2].IsAwake)
			{
				pojos[gameObject2].Active();
				if (pojos[gameObject2].Item != null)
				{
					gameObject2.GetComponent<Image>().sprite = Resource.GetImage(ResourceManager.Get<Item>(pojos[gameObject2].Item).pic);
				}
				gameObject2.SetActive(true);
				gameObject2.transform.position = new Vector3(Random.Range(-487, 494), Random.Range(-249, 239));
				hidePojo.RemoveAt(num);
				showPojo.Add(gameObject2);
			}
		}
		for (int num2 = showPojo.Count - 1; num2 >= 0; num2--)
		{
			GameObject gameObject3 = showPojo[num2] as GameObject;
			if ((!(gameObject3 == wu) || !(moveDirect != Vector3.zero)) && pojos[gameObject3].IsTimeout)
			{
				gameObject3.SetActive(false);
				showPojo.RemoveAt(num2);
				hidePojo.Add(gameObject3);
			}
		}
		if ((double)(Time.time - bombTime) > 0.5)
		{
			bomb.SetActive(false);
		}
		if (moveDirect != Vector3.zero)
		{
			wu.transform.Translate(moveDirect);
			if (Vector3.Distance(wu.transform.position, target) < 1f)
			{
				moveDirect = Vector3.zero;
			}
		}
	}

	public void Hit(GameObject sender)
	{
		MolePojo molePojo = pojos[sender];
		point += molePojo.Point;
		if (molePojo.Audio != null)
		{
			AudioManager.Instance.PlayEffect(molePojo.Audio);
		}
		if (molePojo.Item != null)
		{
			moveDirect = (target - sender.transform.position) / 20f;
			if (items.ContainsKey(molePojo.Item))
			{
				Dictionary<string, int> dictionary2;
				Dictionary<string, int> dictionary = (dictionary2 = items);
				string item;
				string key = (item = molePojo.Item);
				int num = dictionary2[item];
				dictionary[key] = num + 1;
			}
			else
			{
				items.Add(molePojo.Item, 1);
			}
			if (uniqueItems.Contains(molePojo.Item))
			{
				Dictionary<string, string> keyValues;
				Dictionary<string, string> dictionary3 = (keyValues = RuntimeData.Instance.KeyValues);
				string item;
				string key2 = (item = "_tfkz");
				item = keyValues[item];
				dictionary3[key2] = item + "#" + molePojo.Item;
			}
		}
		else
		{
			bombTime = Time.time;
			bomb.transform.position = sender.transform.position;
			bomb.SetActive(true);
		}
	}
}
