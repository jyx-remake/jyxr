using System;
using System.Collections.Generic;
using UnityEngine;

namespace JyGame
{
	public class BattleBlock : MonoBehaviour
	{
		private BattleBlockStatus _status;

		public static float BASIC_SCALE
		{
			get
			{
				return Convert.ToSingle(LuaManager.GetConfigDouble("BATTLE_MOVEBLOCK_SCALE"));
			}
		}

		public BattleBlockStatus Status
		{
			get
			{
				return _status;
			}
			set
			{
				_status = value;
				switch (_status)
				{
				case BattleBlockStatus.Hide:
					base.gameObject.SetActive(false);
					break;
				case BattleBlockStatus.Normal:
					GetComponent<SpriteRenderer>().color = Color.white;
					base.gameObject.SetActive(true);
					break;
				case BattleBlockStatus.HighLightGreen:
					GetComponent<SpriteRenderer>().color = Color.green;
					base.gameObject.SetActive(true);
					break;
				case BattleBlockStatus.HighLightRed:
					GetComponent<SpriteRenderer>().color = Color.red;
					base.gameObject.SetActive(true);
					break;
				case BattleBlockStatus.HighLightBlue:
					GetComponent<SpriteRenderer>().color = Color.blue;
					base.gameObject.SetActive(true);
					break;
				}
			}
		}

		public bool IsActive { get; set; }

		public List<LocationBlock> RelatedBlocks { get; set; }

		public void Reset()
		{
			IsActive = false;
			SetFocus(false);
			Status = BattleBlockStatus.Hide;
		}

		public void SetFocus(bool isFocus)
		{
			if (isFocus)
			{
				base.transform.localScale = new Vector3(BASIC_SCALE * 2f, BASIC_SCALE * 2f, 0f);
			}
			else
			{
				base.transform.localScale = new Vector3(BASIC_SCALE * 1f, BASIC_SCALE * 1f, 0f);
			}
		}

		private void Start()
		{
			Reset();
		}

		private void Update()
		{
		}
	}
}
