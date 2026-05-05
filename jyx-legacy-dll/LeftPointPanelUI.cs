using JyGame;
using UnityEngine;

public class LeftPointPanelUI : MonoBehaviour
{
	private Role _role;

	private RolePanelUI _ui;

	public void AddBili()
	{
		if (_role.bili <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0)
		{
			_role.bili++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			_ui.Refresh();
		}
	}

	public void AddWuxing()
	{
		if (_role.wuxing <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0)
		{
			_role.wuxing++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			_ui.Refresh();
		}
	}

	public void AddShenfa()
	{
		if (_role.shenfa <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0)
		{
			_role.shenfa++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			_ui.Refresh();
		}
	}

	public void AddFuyuan()
	{
		if (_role.fuyuan <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0)
		{
			_role.fuyuan++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			_ui.Refresh();
		}
	}

	public void AddGengu()
	{
		if (_role.gengu <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0)
		{
			_role.gengu++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			_ui.Refresh();
		}
	}

	public void AddDingli()
	{
		if (_role.dingli <= CommonSettings.MAX_ATTRIBUTE && _role.leftpoint > 0)
		{
			_role.dingli++;
			_role.leftpoint--;
			AudioManager.Instance.PlayEffect("音效.加点");
			_ui.Refresh();
		}
	}

	public void OnCancel()
	{
		base.gameObject.SetActive(false);
	}

	public void Show(Role r, RolePanelUI ui)
	{
		_role = r;
		_ui = ui;
		base.gameObject.SetActive(true);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
