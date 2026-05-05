using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleUI : MonoBehaviour
{
	public GameObject input;

	public GameObject systemPanel;

	public void OnExecute()
	{
		string text = input.GetComponent<InputField>().text;
		systemPanel.gameObject.SetActive(false);
		if (text.Split(' ').Length > 1)
		{
			RuntimeData.Instance.gameEngine.SwitchGameScene(text.Split(' ')[0], text.Split(' ')[1]);
		}
		else
		{
			RuntimeData.Instance.gameEngine.SwitchGameScene(text, string.Empty);
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
