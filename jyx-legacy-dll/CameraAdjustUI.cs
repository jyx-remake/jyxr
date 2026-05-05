using UnityEngine;

public class CameraAdjustUI : MonoBehaviour
{
	private float devHeight = 640f;

	private float devWidth = 1140f;

	private void Start()
	{
		float num = Screen.height;
		float orthographicSize = GetComponent<Camera>().orthographicSize;
		float num2 = (float)Screen.width * 1f / (float)Screen.height;
		float num3 = orthographicSize * 2f * num2;
		if (num3 < devWidth)
		{
			orthographicSize = devWidth / (2f * num2);
			GetComponent<Camera>().orthographicSize = orthographicSize;
		}
	}

	private void Update()
	{
	}
}
