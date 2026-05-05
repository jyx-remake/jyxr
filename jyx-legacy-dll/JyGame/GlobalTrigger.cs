using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("trigger")]
	public class GlobalTrigger : BasePojo
	{
		private string _pk;

		[XmlAttribute]
		public string story;

		[XmlElement("condition")]
		public List<Condition> Conditions;

		public override string PK
		{
			get
			{
				return _pk;
			}
		}

		public GlobalTrigger()
		{
			_pk = Guid.NewGuid().ToString();
		}

		public static GlobalTrigger GetCurrentTrigger()
		{
			if (RuntimeData.Instance.HasFlag(CommonSettings.flagNoGlobalEvent))
			{
				return null;
			}
			foreach (GlobalTrigger item in ResourceManager.GetAll<GlobalTrigger>())
			{
				if (RuntimeData.Instance.KeyValues.ContainsKey(item.story))
				{
					continue;
				}
				bool flag = true;
				foreach (Condition condition in item.Conditions)
				{
					if (!TriggerLogic.judge(condition))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					return item;
				}
			}
			return null;
		}
	}
}
