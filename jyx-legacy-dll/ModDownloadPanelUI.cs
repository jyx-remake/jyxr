using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class ModDownloadPanelUI : MonoBehaviour
{
	public Slider slider;

	public Text text;

	public ModItemUI modUI;

	public Button abortButton;

	public Button startButton;

	private ModItemUI _modItem;

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void Bind(ModItemUI modItem)
	{
		_modItem = modItem;
		modUI.image.sprite = modItem.image.sprite;
		modUI.text.text = modItem.text.text;
		modUI.desc.text = modItem.desc.text;
		startButton.gameObject.SetActive(false);
	}

	public void Show(float progress, string info)
	{
		base.gameObject.SetActive(true);
		slider.value = progress;
		text.text = info;
	}

	public void Hide()
	{
		base.gameObject.SetActive(false);
	}

	public void DownloadFinished()
	{
		text.text = "下载完成!";
		startButton.gameObject.SetActive(true);
	}

	public void OnCancel()
	{
		Hide();
		_modItem.AbortDownload();
		if (_modItem._parent != null)
		{
			_modItem._parent.OnLocalMod();
		}
	}

	public void OnStartGame()
	{
		ModManager.SetCurrentMod(_modItem._mod);
		LoadingUI.Load("MainMenu");
	}
}
