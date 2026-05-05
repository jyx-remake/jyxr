using UnityEngine;

public class ChuzhaoSprite : MonoBehaviour
{
	public void Kill()
	{
		Object.Destroy(base.gameObject);
	}

	public void Attach(GameObject parent)
	{
		base.transform.SetParent(parent.transform, false);
		base.transform.localPosition = new Vector3(-9f, 47.8f);
		GetComponent<Animator>().Play("attack");
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
