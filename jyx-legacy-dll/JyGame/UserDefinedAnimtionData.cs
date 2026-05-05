using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace JyGame
{
	[XmlType]
	public class UserDefinedAnimtionData
	{
		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public string type;

		[XmlElement("frame")]
		public UserDefinedFrame[] frames;

		[XmlIgnore]
		public IEnumerable<UserDefinedFrame> stands
		{
			get
			{
				UserDefinedFrame[] array = frames;
				foreach (UserDefinedFrame f in array)
				{
					if (f.tag == "stand")
					{
						yield return f;
					}
				}
			}
		}

		[XmlIgnore]
		public IEnumerable<UserDefinedFrame> attacks
		{
			get
			{
				UserDefinedFrame[] array = frames;
				foreach (UserDefinedFrame f in array)
				{
					if (f.tag == "attack")
					{
						yield return f;
					}
				}
			}
		}

		[XmlIgnore]
		public IEnumerable<UserDefinedFrame> moves
		{
			get
			{
				UserDefinedFrame[] array = frames;
				foreach (UserDefinedFrame f in array)
				{
					if (f.tag == "move")
					{
						yield return f;
					}
				}
			}
		}

		[XmlIgnore]
		public IEnumerable<UserDefinedFrame> beattacks
		{
			get
			{
				UserDefinedFrame[] array = frames;
				foreach (UserDefinedFrame f in array)
				{
					if (f.tag == "beattack")
					{
						yield return f;
					}
				}
			}
		}

		[XmlIgnore]
		public IEnumerable<UserDefinedFrame> effects
		{
			get
			{
				UserDefinedFrame[] array = frames;
				foreach (UserDefinedFrame f in array)
				{
					if (f.tag == "effect")
					{
						yield return f;
					}
				}
			}
		}

		public void FillAnimation(UserDefinedAnimation ani)
		{
			if (type == "role")
			{
				ani.stands = GetSprites(stands);
				ani.attacks = GetSprites(attacks);
				ani.moves = GetSprites(moves);
				ani.beattacks = GetSprites(beattacks);
				ani.bindImage.sprite = ani.stands[0];
				ani.currentState = "stand";
			}
			else if (type == "effect")
			{
				ani.effects = GetSprites(effects);
				ani.bindImage.sprite = ani.effects[0];
			}
		}

		private Sprite[] GetSprites(IEnumerable<UserDefinedFrame> list)
		{
			List<Sprite> list2 = new List<Sprite>();
			foreach (UserDefinedFrame item in list)
			{
				list2.Add(item.sprite);
			}
			return list2.ToArray();
		}
	}
}
