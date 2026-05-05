using JyGame;
using UnityEngine;
using UnityEngine.UI;

public class BattleConfigUI : MonoBehaviour
{
	public GameObject battleFieldObj;

	public BattleField battleField
	{
		get
		{
			return battleFieldObj.GetComponent<BattleField>();
		}
	}

	public void OnAutoBattle()
	{
		bool isOn = base.transform.Find("AutoBattleToggle").GetComponent<Toggle>().isOn;
		battleField.OnAuttoBattleSet(isOn);
	}

	public void OnSpeedUp()
	{
		if (Configer.IsSpeedUp = base.transform.Find("SpeedUpToggle").GetComponent<Toggle>().isOn)
		{
			Time.timeScale = (float)(1.7999999523162842 * LuaManager.GetConfigDouble("BATTLE_SPEEDUP_RATE"));
		}
		else
		{
			Time.timeScale = 1.8f;
		}
	}

	private void Start()
	{
		base.transform.Find("SpeedUpToggle").GetComponent<Toggle>().isOn = Configer.IsSpeedUp;
		base.transform.Find("AutoBattleToggle").GetComponent<Toggle>().isOn = Configer.IsAutoBattle;
		if (Configer.IsSpeedUp)
		{
			Time.timeScale = (float)(1.7999999523162842 * LuaManager.GetConfigDouble("BATTLE_SPEEDUP_RATE"));
		}
		else
		{
			Time.timeScale = 1.8f;
		}
	}

	private void Update()
	{
	}

	private void OnDestroy()
	{
		Time.timeScale = 1f;
	}
}
