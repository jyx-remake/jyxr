using System.Collections;
using System.Collections.Generic;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class DodgeUI : MonoBehaviour
{
	private class DodgePojo
	{
		private static Hashtable pojos = new Hashtable();

		private static float initLen = 5f;

		public Vector2 Vect { get; set; }

		public SpriteRenderer GameObject { get; set; }

		public static void Reset()
		{
			pojos.Clear();
		}

		public static DodgePojo Get(string name)
		{
			return pojos[name] as DodgePojo;
		}

		public static ICollection Values()
		{
			return pojos.Values;
		}

		public static void AddPojo(string name, SpriteRenderer gameObject)
		{
			DodgePojo dodgePojo = new DodgePojo();
			Vector2 vect = default(Vector2);
			vect.x = Random.Range(0.2f * initLen, initLen * 0.8f) * (float)(((double)Random.value > 0.5) ? 1 : (-1));
			vect.y = Mathf.Sqrt(initLen * initLen - vect.x * vect.x) * (float)(((double)Random.value > 0.5) ? 1 : (-1));
			dodgePojo.Vect = vect;
			dodgePojo.GameObject = gameObject;
			pojos.Add(name, dodgePojo);
		}
	}

	public SpriteRenderer dong;

	public SpriteRenderer xi;

	public SpriteRenderer nan;

	public SpriteRenderer bei;

	public SpriteRenderer zhu;

	public GameObject dialogPanel;

	public int type;

	private bool flag;

	private bool mouseFlag = true;

	private static float startTime;

	private float lastFastTime;

	private ArrayList uniqueItems = new ArrayList
	{
		"金丝道袍", "阔剑", "精钢拳套", "金刚杵", "柳叶刀", "罗汉拳谱", "天山掌法谱", "松风剑法秘籍", "华山剑法秘籍", "三分剑术",
		"雷震剑法秘籍", "南山刀法谱", "袖箭秘诀", "拂尘秘诀", "蛇鹤八打", "君子剑", "淑女剑", "乌蚕衣", "凌波微步图谱", "天下轻功总决"
	};

	private static CommonSettings.VoidCallBack _callback;

	private void Start()
	{
		if (!RuntimeData.Instance.IsInited)
		{
			RuntimeData.Instance.Init();
		}
		if (!flag && type == 0)
		{
			zhu.sprite = Resource.GetZhujueHead();
			DodgePojo.Reset();
			DodgePojo.AddPojo(dong.name, dong);
			DodgePojo.AddPojo(xi.name, xi);
			DodgePojo.AddPojo(nan.name, nan);
			DodgePojo.AddPojo(bei.name, bei);
		}
	}

	private void ShowDialog(string msg, CommonSettings.VoidCallBack callback = null)
	{
		string[] array = msg.Split('|');
		Show(array[0], array[1], callback);
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

	public void StartClick()
	{
		if (!flag)
		{
			lastFastTime = (startTime = Time.time);
			zhu.GetComponent<Rigidbody2D>().position = ScreenToWorld(Input.mousePosition);
			flag = true;
		}
	}

	public void MouseMove()
	{
		if (mouseFlag)
		{
			zhu.GetComponent<Rigidbody2D>().position = ScreenToWorld(Input.mousePosition);
		}
	}

	public void MouseLeave()
	{
		mouseFlag = false;
	}

	public void MouseEnter()
	{
		mouseFlag = true;
	}

	public void Show(string roleKey, string msg, CommonSettings.VoidCallBack callback = null)
	{
		StoryAction storyAction = new StoryAction();
		storyAction.type = "DIALOG";
		storyAction.value = roleKey + "#" + msg;
		Show(storyAction, callback);
	}

	public void Show(StoryAction action, CommonSettings.VoidCallBack callback = null)
	{
		_callback = callback;
		dialogPanel.gameObject.SetActive(true);
		string[] array = action.value.Split('#');
		string roleKey = array[0];
		string roleName = CommonSettings.getRoleName(roleKey);
		string text = array[1];
		dialogPanel.transform.Find("NameText").GetComponent<Text>().text = roleName;
		text = text.Replace("$MALE$", RuntimeData.Instance.maleName).Replace("$FEMALE$", RuntimeData.Instance.femaleName);
		text = text.Replace("[[red:", "<color='red'>").Replace("[[yellow:", "<color='yellow'>").Replace("]]", "</color>");
		dialogPanel.transform.Find("ContentText").GetComponent<Text>().text = text;
		dialogPanel.transform.Find("HeadImage").GetComponent<Image>().sprite = Resource.GetImage(CommonSettings.getRoleHead(roleKey));
	}

	public void OnClicked()
	{
		base.gameObject.SetActive(false);
		if (_callback != null)
		{
			_callback();
		}
	}

	public void OnJump()
	{
		base.gameObject.SetActive(false);
		if (_callback != null)
		{
			_callback();
		}
	}

	private void Update()
	{
		if (type != 0 || !this.flag)
		{
			return;
		}
		ICollection collection = DodgePojo.Values();
		bool flag = false;
		if ((double)(Time.time - lastFastTime) > 0.2)
		{
			lastFastTime = Time.time;
			flag = true;
		}
		foreach (DodgePojo item in collection)
		{
			if (flag)
			{
				item.Vect *= 1.01f;
			}
			item.GameObject.transform.Translate(item.Vect * Time.deltaTime * 60f);
		}
	}

	private Vector2 ScreenToWorld(Vector3 postion)
	{
		Vector3 vector = Camera.main.ScreenToWorldPoint(postion);
		return new Vector2(vector.x, vector.y);
	}

	private Item RandomItem(params string[] names)
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
		Item item = ResourceManager.Get<Item>(arrayList[Random.Range(0, arrayList.Count)] as string);
		if (uniqueItems.Contains(item.Name))
		{
			Dictionary<string, string> keyValues;
			Dictionary<string, string> dictionary = (keyValues = RuntimeData.Instance.KeyValues);
			string key2;
			string key = (key2 = "_tfkz");
			key2 = keyValues[key2];
			dictionary[key] = key2 + "#" + item.Name;
		}
		return item;
	}

	private void GameOver()
	{
		int num = Mathf.FloorToInt(Time.time - startTime);
		Queue queue = new Queue();
		queue.Enqueue("佟湘玉|你坚持了" + num + "秒！");
		if (num >= 5 && num < 10)
		{
			queue.Enqueue("佟湘玉|干得不错，奖你一点小礼物。");
			queue.Enqueue("主角|获得特制鸡腿 x 1");
			RuntimeData.Instance.addItem(ResourceManager.Get<Item>("特制鸡腿"));
		}
		else if (num >= 10 && num < 14)
		{
			queue.Enqueue("佟湘玉|太牛了，少侠我对你的敬意如滔滔江水不绝...一点小礼物，不成敬意。");
			Item item = RandomItem("冬虫夏草", "金丝道袍", "阔剑", "精钢拳套", "金刚杵", "柳叶刀");
			queue.Enqueue("主角|获得" + item.Name + " x 1");
			RuntimeData.Instance.addItem(item);
		}
		else if (num >= 14 && num < 17)
		{
			queue.Enqueue("佟湘玉|OMG...少侠我好崇拜你哦。");
			Item item2 = RandomItem("生生造化丹", "冬虫夏草", "罗汉拳谱", "天山掌法谱", "松风剑法秘籍", "华山剑法秘籍", "三分剑术", "雷震剑法秘籍", "南山刀法谱", "袖箭秘诀", "拂尘秘诀", "蛇鹤八打");
			queue.Enqueue("主角|获得" + item2.Name + " x 1");
			RuntimeData.Instance.addItem(item2);
		}
		else if (num >= 17 && num < 20)
		{
			queue.Enqueue("佟湘玉|OMG少侠，你真的是人类么？");
			Item item3 = RandomItem("生生造化丹", "黑玉断续膏", "君子剑", "淑女剑");
			queue.Enqueue("主角|获得" + item3.Name + " x 1");
			RuntimeData.Instance.addItem(item3);
		}
		else if (num >= 20 && num < 23)
		{
			queue.Enqueue("佟湘玉|...你已经是God Like了。");
			Item item4 = RandomItem("生生造化丹", "黑玉断续膏", "天王保命丹", "乌蚕衣");
			queue.Enqueue("主角|获得" + item4.Name + " x 1");
			RuntimeData.Instance.addItem(item4);
		}
		else if (num >= 23)
		{
			queue.Enqueue("佟湘玉|Oh, S**t！你已经超神了！");
			Item item5 = RandomItem("生生造化丹", "黑玉断续膏", "天王保命丹", "凌波微步图谱", "天下轻功总决");
			queue.Enqueue("主角|获得" + item5.Name + " x 1");
			RuntimeData.Instance.addItem(item5);
		}
		RuntimeData.Instance.DodgePoint = RuntimeData.Instance.DodgePoint + num * 2;
		if (RuntimeData.Instance.Team[0].shenfa >= CommonSettings.SMALLGAME_MAX_ATTRIBUTE)
		{
			queue.Enqueue("主角|貌似练这个已经没什么长进了...");
		}
		else if (RuntimeData.Instance.DodgePoint >= RuntimeData.Instance.GetTeamRole("主角").Attributes["shenfa"])
		{
			queue.Enqueue("主角|你的身法进步了！身法从【" + RuntimeData.Instance.Team[0].shenfa + "】提高至【" + (RuntimeData.Instance.Team[0].shenfa + 5) + "】！");
			RuntimeData.Instance.DodgePoint = 0;
			RuntimeData.Instance.Team[0].shenfa += 5;
		}
		else
		{
			queue.Enqueue("主角|你练习了一会儿，对轻身功夫似乎有了一些心得...");
		}
		ShowDialog(queue, delegate
		{
			RuntimeData.Instance.gameEngine.SwitchGameScene("map", "同福客栈");
		});
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (type == -1)
		{
			if (DodgePojo.Get(other.name) == null)
			{
				return;
			}
			ICollection collection = DodgePojo.Values();
			foreach (DodgePojo item in collection)
			{
				item.GameObject.gameObject.SetActive(false);
			}
			zhu.gameObject.SetActive(false);
			GameOver();
		}
		else if (!(other.name == "zhu"))
		{
			Vector2 vect = DodgePojo.Get(other.name).Vect;
			switch (type)
			{
			case 1:
			case 2:
				vect.y = 0f - vect.y;
				break;
			case 3:
			case 4:
				vect.x = 0f - vect.x;
				break;
			}
			DodgePojo.Get(other.name).Vect = vect;
		}
	}
}
