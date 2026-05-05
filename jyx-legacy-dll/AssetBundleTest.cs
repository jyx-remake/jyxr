using System.Collections;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class AssetBundleTest : MonoBehaviour
{
	public GameObject img;

	public GameObject img2;

	public void OnClicked()
	{
		StartCoroutine(DownloadImage());
	}

	private IEnumerator DownloadImage()
	{
		WWW www = WWW.LoadFromCacheOrDownload("http://127.0.0.1/assetbundle/maps", 0);
		WWW www2 = WWW.LoadFromCacheOrDownload("http://127.0.0.1/assetbundle/battlebg", 0);
		WWW www3 = WWW.LoadFromCacheOrDownload("http://127.0.0.1/assetbundle/audios", 0);
		yield return www;
		yield return www2;
		yield return www3;
		AssetBundle mab = null;
		AssetBundle mab2 = null;
		AssetBundle mab3 = null;
		if (!string.IsNullOrEmpty(www.error))
		{
			Debug.Log(www.error);
		}
		else
		{
			mab = www.assetBundle;
			Sprite p = mab.LoadAsset<Sprite>("dalunsizhengdian");
			img.GetComponent<Image>().sprite = p;
		}
		if (!string.IsNullOrEmpty(www2.error))
		{
			Debug.Log(www2.error);
		}
		else
		{
			mab2 = www2.assetBundle;
			Sprite p2 = mab2.LoadAsset<Sprite>("city");
			img2.GetComponent<Image>().sprite = p2;
		}
		if (!string.IsNullOrEmpty(www3.error))
		{
			Debug.Log(www3.error);
		}
		else
		{
			mab3 = www3.assetBundle;
			AudioClip c = mab3.LoadAsset<AudioClip>("city1");
			AudioManager.Instance.audioMgr.clip = c;
			AudioManager.Instance.audioMgr.Play();
		}
		yield return 0;
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
