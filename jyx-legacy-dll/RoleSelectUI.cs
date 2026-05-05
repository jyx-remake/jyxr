using System.Collections.Generic;
using JyGame;
using UnityEngine;

public class RoleSelectUI : MonoBehaviour
{
	public GameObject RoleMenuObj;

	public RoleSelectMenu selectMenu
	{
		get
		{
			return RoleMenuObj.GetComponent<RoleSelectMenu>();
		}
	}

	public void Load(string battleKey, List<string> forbiddenKeys, CommonSettings.VoidCallBack cancelCallback)
	{
		Battle battle = ResourceManager.Get<Battle>(battleKey);
		selectMenu.Show(battle, forbiddenKeys, cancelCallback);
	}

	private void Start()
	{
		Init();
		GameEngine gameEngine = RuntimeData.Instance.gameEngine;
		Battle battle = ResourceManager.Get<Battle>(gameEngine.CurrentSceneValue);
		Load(battle.Key, gameEngine.BattleSelectRole_CurrentForbbidenKeys, gameEngine.BattleSelectRole_CurrentCancelCallback);
	}

	private void Init()
	{
	}

	private void Update()
	{
	}
}
