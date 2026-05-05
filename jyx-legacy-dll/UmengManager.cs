using UnityEngine;

public class UmengManager : MonoBehaviour
{
	private bool isPause = true;

	private void Start()
	{
		Object.DontDestroyOnLoad(base.transform.gameObject);
	}
}
