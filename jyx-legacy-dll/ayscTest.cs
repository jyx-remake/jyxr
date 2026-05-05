using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ayscTest : MonoBehaviour
{
	public Image asycImage;

	public void OnTest()
	{
		Texture2D texture2D = new Texture2D(1, 1);
		Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
		asycImage.sprite = sprite;
		StartCoroutine(startDownloadImage(texture2D));
	}

	private IEnumerator startDownloadImage(Texture2D t)
	{
		WWW www = new WWW("file:///" + Application.dataPath + "/icon.png");
		yield return www;
		www.LoadImageIntoTexture(t);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
