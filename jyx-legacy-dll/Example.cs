using Umeng;
using UnityEngine;

public class Example : MonoBehaviour
{
	private void Start()
	{
		Analytics.StartWithAppKeyAndChannelId("app key", "App Store");
		Analytics.SetLogEnabled(true);
	}

	private void OnGUI()
	{
		if (GUI.Button(new Rect(150f, 100f, 500f, 100f), "StartLevel"))
		{
			GA.StartLevel("your level name");
		}
		if (GUI.Button(new Rect(150f, 300f, 500f, 100f), "FinishLevel"))
		{
			GA.FinishLevel("your level name");
		}
	}
}
