using UnityEngine;
using UnityEngine.UI;

public class CoverTest : MonoBehaviour
{
	public GameObject testTextObj;

	private void Start()
	{
		Text component = testTextObj.GetComponent<Text>();
		for (int i = 0; i < 100; i++)
		{
			component.text += string.Format("{0}\n", i);
		}
	}

	private void Update()
	{
	}

	public void Test()
	{
		Debug.Log("test");
	}
}
