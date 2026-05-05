using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class SavePanelUI : MonoBehaviour
{
	public GameObject SelectMenuObj;

	public GameObject SaveItemObj;

	public GameObject ConfirmPanelObj;

	public GameObject DeleteSaveToggleObj;

	public SavePanelMode CurrentMode;

	public void OnCloseClicked()
	{
		base.gameObject.SetActive(false);
	}

	public void Show(SavePanelMode mode)
	{
		base.gameObject.SetActive(true);
		CurrentMode = mode;
		SelectMenu component = SelectMenuObj.GetComponent<SelectMenu>();
		component.Clear();
		if (mode == SavePanelMode.LOAD)
		{
			component.AddItem(GetSaveItem(-1));
			if (!CommonSettings.TOUCH_MODE)
			{
				component.AddItem(GetSaveItem(-2));
			}
		}
		for (int i = 0; i < 6; i++)
		{
			component.AddItem(GetSaveItem(i));
		}
	}

	public GameObject GetSaveItem(int index)
	{
		int currentIndex = index + 1;
		string saveName = "save" + index;
		string title = "存档" + currentIndex;
		if (index == -1)
		{
			saveName = "autosave";
			title = "自动存档";
		}
		else if (index == -2)
		{
			saveName = "fastsave";
			title = "快速存档";
		}
		GameObject gameObject = Object.Instantiate(SaveItemObj);
		gameObject.gameObject.SetActive(true);
		string saveContent = string.Empty;
		if (SaveManager.ExistSave(saveName))
		{
			saveContent = SaveManager.GetSave(saveName);
		}
		gameObject.GetComponent<SavePanelItemUI>().Bind(title, saveContent, delegate
		{
			if (DeleteSaveToggleObj.GetComponent<Toggle>().isOn)
			{
				ConfirmPanelObj.GetComponent<ConfirmPanel>().Show("提示，删除存档将永不可找回，确认吗？", delegate
				{
					AudioManager.Instance.PlayEffect("音效.装备");
					SaveManager.DeleteSave(saveName);
					Show(CurrentMode);
				});
			}
			else
			{
				AudioManager.Instance.PlayEffect("音效.装备");
				if (CurrentMode == SavePanelMode.SAVE)
				{
					if (index != -1 && !RuntimeData.Instance.AutoSaveOnly)
					{
						if (!string.IsNullOrEmpty(saveContent))
						{
							ConfirmPanelObj.GetComponent<ConfirmPanel>().Show("提示，将覆盖原存档。确认吗？", delegate
							{
								string content2 = RuntimeData.Instance.Save();
								SaveManager.SetSave(saveName, content2);
								ModData.ParamAdd("save", 1);
								ModData.SetParam("last_save_index", currentIndex);
								Show(CurrentMode);
							});
						}
						else
						{
							string content = RuntimeData.Instance.Save();
							SaveManager.SetSave(saveName, content);
							ModData.ParamAdd("save", 1);
							ModData.SetParam("last_save_index", currentIndex);
							Show(CurrentMode);
						}
					}
				}
				else if (CurrentMode == SavePanelMode.LOAD && SaveManager.ExistSave(saveName))
				{
					string save = SaveManager.GetSave(saveName);
					if (!string.IsNullOrEmpty(save))
					{
						RuntimeData.Instance.Load(save);
						RuntimeData.Instance.gameEngine.SwitchGameScene("map", RuntimeData.Instance.CurrentBigMap);
						base.gameObject.SetActive(false);
					}
				}
			}
		});
		return gameObject;
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
