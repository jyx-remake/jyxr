using System;
using System.Collections.Generic;
using System.Linq;

namespace JyGame
{
	public class SkillCoverTypeHelper
	{
		private SkillCoverType _type;

		private static string[] coverTypeInfoMap = new string[9] { "点攻击", "十字攻击", "米字攻击", "直线攻击", "面攻击", "扇形攻击", "环状攻击", "对角线攻击", "身前攻击" };

		private float[] dSizeMap = new float[9] { 0.12f, 0.15f, 0.1f, 0.2f, 0.3f, 0.23f, 0.2f, 0.3f, 0f };

		private int[] dBaseCaseSizeMap = new int[9] { 1, 0, 0, 1, 1, 1, 0, 0, 1 };

		private float[] dCastSizeMap = new float[9] { 0.25f, 0f, 0f, 0.2f, 0f, 0f, 0f, 0f, 0f };

		private int _typeCode
		{
			get
			{
				return (int)_type;
			}
		}

		public float dSize
		{
			get
			{
				return dSizeMap[_typeCode];
			}
		}

		public int baseCastSize
		{
			get
			{
				return dBaseCaseSizeMap[_typeCode];
			}
		}

		public float dCastSize
		{
			get
			{
				return dCastSizeMap[_typeCode];
			}
		}

		public string CoverTypeInfo
		{
			get
			{
				return GetSkillTypeChinese(_typeCode);
			}
		}

		public SkillCoverTypeHelper(SkillCoverType type)
		{
			_type = type;
		}

		public static string GetSkillTypeChinese(int type)
		{
			return coverTypeInfoMap[type];
		}

		public int CostMp(float power, int size)
		{
			int num = 0;
			switch (_type)
			{
			case SkillCoverType.NORMAL:
				return (int)(power * 2f * 8f);
			case SkillCoverType.CROSS:
				return (int)(power * (float)size * 8f);
			case SkillCoverType.STAR:
				return (int)((double)(power * (float)size) * 1.3 * 8.0);
			case SkillCoverType.LINE:
				return (int)((double)(power * (float)size) * 0.6 * 8.0);
			case SkillCoverType.FACE:
				return (int)(power * (float)size * 3f * 8f);
			case SkillCoverType.FAN:
				return (int)((double)(power * (float)size) * 2.5 * 8.0);
			case SkillCoverType.FRONT:
				return (int)(power * 2f * 8f);
			default:
				return (int)((double)(power * (float)size) * 1.5 * 8.0);
			}
		}

		public List<LocationBlock> GetSkillCoverBlocks(int x, int y, int spx, int spy, int coversize)
		{
			List<LocationBlock> list = new List<LocationBlock>();
			switch (_type)
			{
			case SkillCoverType.NORMAL:
				list.Add(new LocationBlock
				{
					X = x,
					Y = y
				});
				break;
			case SkillCoverType.CROSS:
			{
				list.Add(new LocationBlock
				{
					X = x,
					Y = y
				});
				for (int k = 1; k <= coversize; k++)
				{
					list.Add(new LocationBlock
					{
						X = x + k,
						Y = y
					});
					list.Add(new LocationBlock
					{
						X = x + k * -1,
						Y = y
					});
					list.Add(new LocationBlock
					{
						X = x,
						Y = y + k
					});
					list.Add(new LocationBlock
					{
						X = x,
						Y = y + k * -1
					});
				}
				break;
			}
			case SkillCoverType.LINE:
			{
				list.Add(new LocationBlock
				{
					X = x,
					Y = y
				});
				int num = 0;
				int num2 = 0;
				if (x < spx)
				{
					num = -1;
				}
				if (x > spx)
				{
					num = 1;
				}
				if (y < spy)
				{
					num2 = -1;
				}
				if (y > spy)
				{
					num2 = 1;
				}
				for (int i = 1; i <= coversize; i++)
				{
					list.Add(new LocationBlock
					{
						X = x + num * i,
						Y = y + num2 * i
					});
				}
				break;
			}
			case SkillCoverType.STAR:
			{
				list.Add(new LocationBlock
				{
					X = x,
					Y = y
				});
				for (int j = 1; j <= coversize; j++)
				{
					list.Add(new LocationBlock
					{
						X = x + j,
						Y = y
					});
					list.Add(new LocationBlock
					{
						X = x + j * -1,
						Y = y
					});
					list.Add(new LocationBlock
					{
						X = x,
						Y = y + j
					});
					list.Add(new LocationBlock
					{
						X = x,
						Y = y + j * -1
					});
					list.Add(new LocationBlock
					{
						X = x + j,
						Y = y + j
					});
					list.Add(new LocationBlock
					{
						X = x + j * -1,
						Y = y + j
					});
					list.Add(new LocationBlock
					{
						X = x + j,
						Y = y + j * -1
					});
					list.Add(new LocationBlock
					{
						X = x + j * -1,
						Y = y + j * -1
					});
				}
				break;
			}
			case SkillCoverType.FACE:
			{
				list.Add(new LocationBlock
				{
					X = x,
					Y = y
				});
				for (int num3 = x - coversize / 2; num3 < x + coversize / 2 + 1; num3++)
				{
					for (int num4 = y - coversize / 2; num4 < y + coversize / 2 + 1; num4++)
					{
						if (list.Count((LocationBlock p) => p.X == num3 && p.Y == num4) == 0)
						{
							list.Add(new LocationBlock
							{
								X = num3,
								Y = num4
							});
						}
					}
				}
				break;
			}
			case SkillCoverType.FAN:
			{
				list.Add(new LocationBlock
				{
					X = x,
					Y = y
				});
				int num = 0;
				int num2 = 0;
				if (x < spx)
				{
					num = -1;
				}
				if (x > spx)
				{
					num = 1;
				}
				if (y < spy)
				{
					num2 = -1;
				}
				if (y > spy)
				{
					num2 = 1;
				}
				if (num + num2 == 0)
				{
					break;
				}
				for (int num5 = 1; num5 <= coversize; num5++)
				{
					list.Add(new LocationBlock
					{
						X = x + num * num5,
						Y = y + num2 * num5
					});
					for (int num6 = 1; num6 <= num5; num6++)
					{
						if (num == 0)
						{
							list.Add(new LocationBlock
							{
								X = x + num * num5 + num6,
								Y = y + num2 * num5
							});
							list.Add(new LocationBlock
							{
								X = x + num * num5 - num6,
								Y = y + num2 * num5
							});
						}
						else
						{
							list.Add(new LocationBlock
							{
								X = x + num * num5,
								Y = y + num2 * num5 + num6
							});
							list.Add(new LocationBlock
							{
								X = x + num * num5,
								Y = y + num2 * num5 - num6
							});
						}
					}
				}
				break;
			}
			case SkillCoverType.RING:
			{
				for (int m = -coversize; m <= coversize; m++)
				{
					for (int n = -coversize; n <= coversize; n++)
					{
						if (Math.Abs(m) + Math.Abs(n) == coversize)
						{
							list.Add(new LocationBlock
							{
								X = x + m,
								Y = y + n
							});
						}
					}
				}
				break;
			}
			case SkillCoverType.X:
			{
				for (int l = 0; l < coversize; l++)
				{
					list.Add(new LocationBlock
					{
						X = x + l,
						Y = y + l
					});
					list.Add(new LocationBlock
					{
						X = x + l,
						Y = y - l
					});
					list.Add(new LocationBlock
					{
						X = x - l,
						Y = y + l
					});
					list.Add(new LocationBlock
					{
						X = x - l,
						Y = y - l
					});
				}
				break;
			}
			case SkillCoverType.FRONT:
				list.Add(new LocationBlock(x, y));
				if (x == spx)
				{
					list.Add(new LocationBlock(x - 1, y));
					list.Add(new LocationBlock(x + 1, y));
				}
				if (y == spy)
				{
					list.Add(new LocationBlock(x, y - 1));
					list.Add(new LocationBlock(x, y + 1));
				}
				break;
			}
			return list;
		}
	}
}
