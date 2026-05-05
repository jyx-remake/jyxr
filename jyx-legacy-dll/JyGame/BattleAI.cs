using System;
using System.Collections.Generic;

namespace JyGame
{
	public class BattleAI
	{
		private BattleField Field;

		private int[] directionX = new int[4] { 1, 0, -1, 0 };

		private int[] directionY = new int[4] { 0, 1, 0, -1 };

		public BattleAI(BattleField field)
		{
			Field = field;
		}

		public List<LocationBlock> GetMoveRange(int x, int y)
		{
			List<LocationBlock> list = new List<LocationBlock>();
			Queue<MoveSearchHelper> queue = new Queue<MoveSearchHelper>();
			queue.Enqueue(new MoveSearchHelper
			{
				X = x,
				Y = y,
				Cost = 0
			});
			while (queue.Count > 0)
			{
				MoveSearchHelper moveSearchHelper = queue.Dequeue();
				int x2 = moveSearchHelper.X;
				int y2 = moveSearchHelper.Y;
				int cost = moveSearchHelper.Cost;
				bool flag = false;
				foreach (LocationBlock item in list)
				{
					if (item.X == x2 && item.Y == y2)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
				list.Add(new LocationBlock
				{
					X = x2,
					Y = y2
				});
				for (int i = 0; i < 4; i++)
				{
					int num = x2 + directionX[i];
					int num2 = y2 + directionY[i];
					int num3 = 1;
					if (!Field.currentSprite.Role.HasTalent("轻功大师"))
					{
						for (int j = 0; j < 4; j++)
						{
							int x3 = num + directionX[j];
							int y3 = num2 + directionY[j];
							BattleSprite sprite = Field.GetSprite(x3, y3);
							if (sprite != null && sprite.Team != Field.currentSprite.Team)
							{
								num3 = 2;
								break;
							}
						}
					}
					if (IsEmptyBlock(num, num2) && cost + num3 <= Field.currentSprite.MoveAbility)
					{
						queue.Enqueue(new MoveSearchHelper
						{
							X = num,
							Y = num2,
							Cost = cost + num3
						});
					}
				}
			}
			return list;
		}

		public bool IsEmptyBlock(int x, int y)
		{
			return x >= 0 && x < BattleField.MOVEBLOCK_MAX_X && y >= 0 && y < BattleField.MOVEBLOCK_MAX_Y && Field.GetSprite(x, y) == null;
		}

		public List<MoveSearchHelper> GetWay(int x, int y, int ex, int ey, bool ignoreSpirits = true)
		{
			if (x == ex && y == ey)
			{
				return new List<MoveSearchHelper>();
			}
			bool[,] array = new bool[BattleField.MOVEBLOCK_MAX_X, BattleField.MOVEBLOCK_MAX_Y];
			for (int i = 0; i < BattleField.MOVEBLOCK_MAX_X; i++)
			{
				for (int j = 0; j < BattleField.MOVEBLOCK_MAX_Y; j++)
				{
					array[i, j] = false;
				}
			}
			Queue<MoveSearchHelper> queue = new Queue<MoveSearchHelper>();
			List<MoveSearchHelper> list = new List<MoveSearchHelper>();
			queue.Enqueue(new MoveSearchHelper
			{
				X = x,
				Y = y
			});
			array[x, y] = true;
			while (queue.Count > 0)
			{
				MoveSearchHelper moveSearchHelper = queue.Dequeue();
				if (moveSearchHelper.X == ex && moveSearchHelper.Y == ey)
				{
					do
					{
						list.Add(moveSearchHelper);
						moveSearchHelper = moveSearchHelper.front;
					}
					while (moveSearchHelper != null);
					list.Reverse();
					return list;
				}
				for (int k = 0; k < 4; k++)
				{
					int num = moveSearchHelper.X + directionX[k];
					int num2 = moveSearchHelper.Y + directionY[k];
					if ((!ignoreSpirits || !(Field.GetSprite(num, num2) != null)) && num >= 0 && num < BattleField.MOVEBLOCK_MAX_X && num2 >= 0 && num2 < BattleField.MOVEBLOCK_MAX_Y && !array[num, num2])
					{
						queue.Enqueue(new MoveSearchHelper
						{
							X = num,
							Y = num2,
							front = moveSearchHelper
						});
						array[num, num2] = true;
					}
				}
			}
			return new List<MoveSearchHelper>();
		}

		private int GetAbsoluteDistance(BattleSprite a, BattleSprite b)
		{
			return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
		}

		private int GetDistance(BattleSprite a, BattleSprite b)
		{
			List<MoveSearchHelper> way = GetWay(a.X, a.Y, b.X, b.Y, false);
			if (way == null)
			{
				return 0;
			}
			return way.Count;
		}

		private int GetAbsoluteDistance(int x1, int y1, int x2, int y2)
		{
			return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
		}

		private int GetDistance(int x1, int y1, int x2, int y2)
		{
			List<MoveSearchHelper> way = GetWay(x1, y1, x2, y2, false);
			if (way == null)
			{
				return 0;
			}
			return way.Count;
		}

		public AIResult GetAIResult()
		{
			AIResult aIResult = new AIResult();
			AIResult aIResult2 = LuaManager.Call<AIResult>("AI_GetAIResult", new object[2] { this, aIResult });
			if (aIResult2 != null)
			{
				return aIResult2;
			}
			List<LocationBlock> moveRange = GetMoveRange(Field.currentSprite.X, Field.currentSprite.Y);
			if ((double)Field.currentSprite.Hp / (double)Field.currentSprite.Role.maxhp < 0.3 && Tools.ProbabilityTest(0.5))
			{
				int num = 0;
				{
					foreach (LocationBlock item2 in moveRange)
					{
						int num2 = int.MaxValue;
						foreach (BattleSprite sprite2 in Field.Sprites)
						{
							int distance = GetDistance(sprite2.X, sprite2.Y, item2.X, item2.Y);
							if (sprite2.Team != Field.currentSprite.Team && distance < num2)
							{
								num2 = distance;
							}
						}
						if (num2 > num)
						{
							num = num2;
							aIResult.MoveX = item2.X;
							aIResult.MoveY = item2.Y;
							aIResult.IsRest = true;
						}
					}
					return aIResult;
				}
			}
			double num3 = 0.0;
			List<SkillBox> list = new List<SkillBox>();
			foreach (SkillBox avaliableSkill in Field.currentSprite.Role.GetAvaliableSkills())
			{
				if (avaliableSkill.Status != SkillStatus.Ok)
				{
					continue;
				}
				if (list.Count < 5)
				{
					list.Add(avaliableSkill);
					continue;
				}
				double num4 = double.MaxValue;
				SkillBox skillBox = null;
				foreach (SkillBox item3 in list)
				{
					if ((double)item3.Power < num4)
					{
						num4 = item3.Power;
						skillBox = item3;
					}
				}
				if (skillBox != null && avaliableSkill.Power > skillBox.Power)
				{
					list.Remove(skillBox);
					list.Add(avaliableSkill);
				}
			}
			List<LocationBlock> list2 = new List<LocationBlock>();
			list2.Add(new LocationBlock
			{
				X = Field.currentSprite.X,
				Y = Field.currentSprite.Y
			});
			if (moveRange.Count > 20)
			{
				while (list2.Count < 20)
				{
					LocationBlock item = moveRange[Tools.GetRandomInt(0, moveRange.Count - 1)];
					if (!list2.Contains(item))
					{
						list2.Add(item);
					}
				}
			}
			else
			{
				list2 = moveRange;
			}
			AttackResultCache attackResultCache = new AttackResultCache(Field.currentSprite, Field);
			foreach (SkillBox item4 in list)
			{
				if (item4.Status != SkillStatus.Ok)
				{
					continue;
				}
				foreach (LocationBlock item5 in list2)
				{
					int x = item5.X;
					int y = item5.Y;
					List<LocationBlock> skillCastBlocks = item4.GetSkillCastBlocks(x, y);
					foreach (LocationBlock item6 in skillCastBlocks)
					{
						double num5 = 0.0;
						List<LocationBlock> skillCoverBlocks = item4.GetSkillCoverBlocks(item6.X, item6.Y, x, y);
						foreach (LocationBlock item7 in skillCoverBlocks)
						{
							BattleSprite sprite = Field.GetSprite(item7.X, item7.Y);
							if (!(sprite == null) && !(sprite == Field.currentSprite))
							{
								AttackResult attackResult = attackResultCache.GetAttackResult(item4, sprite);
								aIResult.totalAttackComputeNum++;
								if (sprite.Team != Field.currentSprite.Team)
								{
									num5 += (double)attackResult.Hp;
								}
								else if (sprite.Team == Field.currentSprite.Team && RuntimeData.Instance.FriendlyFire)
								{
									num5 -= (double)(attackResult.Hp / 2);
								}
							}
						}
						if (num5 > num3)
						{
							aIResult.MoveX = x;
							aIResult.MoveY = y;
							aIResult.skill = item4;
							aIResult.IsRest = false;
							aIResult.AttackX = item6.X;
							aIResult.AttackY = item6.Y;
							num3 = num5;
						}
					}
				}
			}
			if (aIResult.skill != null)
			{
				return aIResult;
			}
			int num6 = int.MaxValue;
			BattleSprite battleSprite = null;
			foreach (BattleSprite sprite3 in Field.Sprites)
			{
				if (sprite3.Team != Field.currentSprite.Team)
				{
					int distance2 = GetDistance(sprite3, Field.currentSprite);
					if (distance2 < num6)
					{
						num6 = distance2;
						battleSprite = sprite3;
					}
				}
			}
			if (battleSprite != null)
			{
				int num7 = int.MaxValue;
				int x2 = Field.currentSprite.X;
				int y2 = Field.currentSprite.Y;
				foreach (LocationBlock item8 in moveRange)
				{
					int distance3 = GetDistance(item8.X, item8.Y, battleSprite.X, battleSprite.Y);
					if (distance3 <= num7)
					{
						num7 = distance3;
						x2 = item8.X;
						y2 = item8.Y;
					}
				}
				aIResult.skill = null;
				aIResult.MoveX = x2;
				aIResult.MoveY = y2;
				aIResult.IsRest = true;
				if (Tools.ProbabilityTest(0.5))
				{
					foreach (SpecialSkillInstance specialSkill in Field.currentSprite.Role.SpecialSkills)
					{
						if (specialSkill.Status == SkillStatus.Ok && specialSkill.HitSelf && specialSkill.IsUsed)
						{
							aIResult.skill = specialSkill;
							aIResult.AttackX = x2;
							aIResult.AttackY = y2;
							aIResult.IsRest = false;
						}
					}
				}
				return aIResult;
			}
			aIResult.MoveX = Field.currentSprite.X;
			aIResult.MoveY = Field.currentSprite.Y;
			aIResult.IsRest = true;
			return aIResult;
		}
	}
}
