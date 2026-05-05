using System;
using System.Collections.Generic;
using UnityEngine;

namespace JyGame
{
	public class AttackLogic
	{
		public static BattleField field;

		public static void Log(string msg)
		{
			if (field != null)
			{
				field.Log(msg);
			}
		}

		public static AttackResult Attack(SkillBox skill, BattleSprite source, BattleSprite target, BattleField bf)
		{
			AttackResult attackResult = new AttackResult();
			BattleAI aI = bf.AI;
			if (target.Team != source.Team && source.HasBuff("溜须拍马"))
			{
				string msg = source.Role.Name + "的溜须拍马状态取消！";
				attackResult.AddCastInfo(target, "竟然敢打我？！");
				source.DeleteBuff("溜须拍马");
				source.Refresh();
				Log(msg);
			}
			if (source.Role.HasTalent("飞向天际") && target.Team != source.Team && Tools.ProbabilityTest(0.3))
			{
				attackResult.AddCastInfo(source, "给我飞吧！");
				Log(source.Role.Name + "天赋【飞向天际】发动");
				Log(target.Role.Name + "被击飞");
				int num = 0;
				num = ((source.X - target.X != 0) ? ((source.X - target.X) / Math.Abs(source.X - target.X)) : 0);
				int num2 = 0;
				num2 = ((source.Y - target.Y != 0) ? ((source.Y - target.Y) / Math.Abs(source.Y - target.Y)) : 0);
				int num3 = target.X;
				int num4 = target.Y;
				int num5 = 1;
				while (true)
				{
					int num6 = ((num != 0) ? (-num * num5) : 0);
					int num7 = ((num2 != 0) ? (-num2 * num5) : 0);
					int num8 = target.X + num6;
					int num9 = target.Y + num7;
					if (!aI.IsEmptyBlock(num8, num9))
					{
						break;
					}
					num5++;
					num3 = num8;
					num4 = num9;
				}
				target.Pos = new Vector2(num3, num4);
			}
			if (skill.Name == "天地同寿")
			{
				attackResult.AddCastInfo(source, "狗贼，我跟你拼了！");
				double random = Tools.GetRandom(0.5, 1.0);
				attackResult.Hp = (int)((double)source.Hp * random);
				source.Hp = 0;
				source.MaxHp -= 10;
				Log(source.Role.Name + "使用天地同寿，自杀");
			}
			else if (skill.Name == "同归剑法")
			{
				attackResult.AddCastInfo(source, "来吧！！看我同归剑法");
				attackResult.Hp = (int)(1000.0 + Tools.GetRandom(0.0, 0.2) * (double)target.MaxHp);
				source.Hp -= 1000;
				attackResult.AddAttackInfo(source, "自伤1000", Color.white);
				Log(source.Role.Name + "使用同归剑法，自伤HP1000");
			}
			else if (skill.Name == "六合劲")
			{
				attackResult.AddCastInfo(source, "看我内功的劲力！");
				target.Sp = 0.0;
				target.Balls = 0;
				attackResult.AddAttackInfo(target, "集气清零", Color.yellow);
				attackResult.AddAttackInfo(target, "怒气清零", Color.yellow);
				Log(source.Role.Name + "使用六合劲，对方集气、怒气清零");
			}
			else if (skill.Name == "腐尸毒")
			{
				BuffInstance buff = target.GetBuff("中毒");
				if (buff != null)
				{
					attackResult.AddCastInfo(source, "腐尸毒!");
					int num10 = (attackResult.Hp = (int)((double)((buff.Level + 1) * buff.LeftRound * 50) * Tools.GetRandom(1.0, 2.0)));
					attackResult.AddAttackInfo(target, "清算中毒", Color.red);
					Log(source.Role.Name + "使用腐尸毒，清算中毒效果。造成" + num10 + "点真实伤害");
				}
			}
			else if (skill.Name == "笑傲江湖曲")
			{
				attackResult.AddCastInfo(source, "铮铮一曲玉石碎，为君且作沧海歌！");
				foreach (BattleSprite sprite in bf.Sprites)
				{
					if (sprite.Team == source.Team)
					{
						BuffInstance buff2 = sprite.GetBuff("攻击强化");
						Buff buff3 = new Buff();
						buff3.Name = "攻击强化";
						buff3.Level = 5;
						buff3.Round = 3;
						BuffInstance buffInstance = new BuffInstance();
						buffInstance.buff = buff3;
						buffInstance.Owner = source;
						buffInstance.LeftRound = buff3.Round;
						BuffInstance buffInstance2 = buffInstance;
						if (buff2 == null)
						{
							sprite.AddBuff(buffInstance2);
						}
						else if (buffInstance2.LeftRound >= buff2.LeftRound)
						{
							buff2 = buffInstance2;
						}
						attackResult.AddAttackInfo(sprite, "攻击力上升！", Color.red);
						bf.ShowSkillAnimation(skill, sprite.X, sprite.Y);
						sprite.Refresh();
						Log(sprite.Role.Name + "由于笑傲江湖曲，攻击力上升");
					}
				}
			}
			else if (skill.Name == "清心普善咒")
			{
				attackResult.AddCastInfo(source, "高山流水，韵远流长。");
				foreach (BattleSprite sprite2 in bf.Sprites)
				{
					if (sprite2.Team != source.Team)
					{
						continue;
					}
					Log(sprite2.Role.Name + "异常状态解除！");
					attackResult.AddAttackInfo(sprite2, "异常状态解除！", Color.white);
					bf.ShowSkillAnimation(skill, sprite2.X, sprite2.Y);
					if (sprite2.Buffs == null || sprite2.Buffs.Count == 0)
					{
						continue;
					}
					List<BuffInstance> list = new List<BuffInstance>();
					foreach (BuffInstance buff21 in sprite2.Buffs)
					{
						if (!buff21.IsDebuff)
						{
							list.Add(buff21);
						}
					}
					sprite2.Buffs.Clear();
					sprite2.Buffs = list;
					sprite2.Refresh();
				}
			}
			else if (skill.Name == "阿碧的歌声")
			{
				attackResult.AddCastInfo(source, "吴歌一曲醉芙蓉~");
				foreach (BattleSprite sprite3 in bf.Sprites)
				{
					if (sprite3.Team != source.Team)
					{
						continue;
					}
					Log(sprite3.Role.Name + "异常状态解除！");
					attackResult.AddAttackInfo(sprite3, "异常状态解除！", Color.white);
					bf.ShowSkillAnimation(skill, sprite3.X, sprite3.Y);
					if (sprite3.Buffs == null || sprite3.Buffs.Count == 0)
					{
						continue;
					}
					List<BuffInstance> list2 = new List<BuffInstance>();
					foreach (BuffInstance buff22 in sprite3.Buffs)
					{
						if (!buff22.IsDebuff)
						{
							list2.Add(buff22);
						}
					}
					sprite3.Buffs.Clear();
					sprite3.Buffs = list2;
					sprite3.Refresh();
				}
			}
			else if (skill.Name == "飞星术")
			{
				attackResult.AddCastInfo(source, "给我中吧！");
				foreach (BattleSprite sprite4 in bf.Sprites)
				{
					attackResult.AddAttackInfo(sprite4, "中毒", Color.green);
					BuffInstance buff4 = sprite4.GetBuff("中毒");
					int randomInt = Tools.GetRandomInt(1, 10);
					Buff buff5 = new Buff();
					buff5.Name = "中毒";
					buff5.Level = randomInt;
					buff5.Round = Tools.GetRandomInt(3, 6);
					BuffInstance buffInstance = new BuffInstance();
					buffInstance.buff = buff5;
					buffInstance.Owner = sprite4;
					buffInstance.LeftRound = buff5.Round;
					BuffInstance buffInstance3 = buffInstance;
					if (buff4 == null)
					{
						sprite4.AddBuff(buffInstance3);
					}
					else if (buffInstance3.LeftRound >= buff4.LeftRound)
					{
						buff4 = buffInstance3;
					}
					sprite4.Refresh();
				}
			}
			else if (skill.Name == "吴侬软语")
			{
				attackResult.AddCastInfo(source, "风轻轻，柳青青，侬的怒气快平息~");
				foreach (BattleSprite sprite5 in bf.Sprites)
				{
					if (sprite5.Team == source.Team)
					{
						continue;
					}
					Log(sprite5.Role.Name + "增益状态解除！");
					attackResult.AddAttackInfo(sprite5, "增益状态解除！", Color.white);
					bf.ShowSkillAnimation(skill, sprite5.X, sprite5.Y);
					if (source.Role.HasTalent("吴侬软语"))
					{
						BuffInstance buffInstance = new BuffInstance();
						buffInstance.buff = new Buff
						{
							Name = "晕眩",
							Level = 0
						};
						buffInstance.Owner = sprite5;
						buffInstance.LeftRound = 2;
						BuffInstance buffInstance4 = buffInstance;
						BuffInstance buff6 = sprite5.GetBuff("晕眩");
						if (buff6 == null)
						{
							sprite5.Buffs.Add(buffInstance4);
						}
						else if (buffInstance4.LeftRound >= buff6.LeftRound)
						{
							buff6 = buffInstance4;
						}
						Log(sprite5.Role.Name + "晕眩");
						attackResult.AddAttackInfo(sprite5, "晕眩", Color.red);
						sprite5.Refresh();
					}
					if (sprite5.Buffs == null || sprite5.Buffs.Count == 0)
					{
						continue;
					}
					List<BuffInstance> list3 = new List<BuffInstance>();
					foreach (BuffInstance buff23 in sprite5.Buffs)
					{
						if (buff23.IsDebuff)
						{
							list3.Add(buff23);
						}
					}
					sprite5.Buffs.Clear();
					sprite5.Buffs = list3;
					sprite5.Refresh();
				}
			}
			else if (skill.Name == "易容术")
			{
				attackResult.AddCastInfo(source, "嘻嘻，待我易容改面，藏于暗处~");
				attackResult.Hp = 0;
				BuffInstance buff7 = source.GetBuff("易容");
				Buff buff8 = new Buff();
				buff8.Name = "易容";
				buff8.Level = 0;
				buff8.Round = 3;
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = buff8;
				buffInstance.Owner = source;
				buffInstance.LeftRound = buff8.Round;
				BuffInstance buffInstance5 = buffInstance;
				if (buff7 == null)
				{
					source.AddBuff(buffInstance5);
				}
				else if (buffInstance5.LeftRound >= buff7.LeftRound)
				{
					buff7 = buffInstance5;
				}
				source.Refresh();
			}
			else if (skill.Name == "过肩摔")
			{
				int num11 = source.X - target.X;
				int num12 = source.Y - target.Y;
				int x = source.X + num11;
				int y = source.Y + num12;
				if (aI.IsEmptyBlock(x, y))
				{
					target.SetPos(x, y);
				}
				int num13 = Math.Abs(source.Role.AttributesFinal["dingli"] - target.Role.AttributesFinal["dingli"]);
				attackResult.Hp += Tools.GetRandomInt(num13 * 5, num13 * 20);
			}
			else if (skill.Name == "神龙摆尾")
			{
				int num14 = source.X - target.X;
				int num15 = source.Y - target.Y;
				int x2 = target.X;
				int y2 = target.Y;
				int num16 = 1;
				while (true)
				{
					int num17 = ((num14 != 0) ? (-num14 * num16) : 0);
					int num18 = ((num15 != 0) ? (-num15 * num16) : 0);
					int num19 = target.X + num17;
					int num20 = target.Y + num18;
					if (!aI.IsEmptyBlock(num19, num20))
					{
						break;
					}
					num16++;
					x2 = num19;
					y2 = num20;
				}
				target.SetPos(x2, y2);
				attackResult.Hp += Tools.GetRandomInt(num16 * 100, num16 * 500);
			}
			else if (skill.Name == "武穆兵法")
			{
				foreach (BattleSprite sprite6 in bf.Sprites)
				{
					if (sprite6.Team == source.Team)
					{
						BuffInstance buff9 = sprite6.GetBuff("攻击强化");
						Buff buff10 = new Buff();
						buff10.Name = "攻击强化";
						buff10.Level = 5;
						buff10.Round = 3;
						BuffInstance buffInstance = new BuffInstance();
						buffInstance.buff = buff10;
						buffInstance.Owner = source;
						buffInstance.LeftRound = buff10.Round;
						BuffInstance buffInstance6 = buffInstance;
						if (buff9 == null)
						{
							sprite6.Buffs.Add(buffInstance6);
						}
						else if (buffInstance6.LeftRound >= buff9.LeftRound)
						{
							buff9 = buffInstance6;
						}
						buff9 = sprite6.GetBuff("防御强化");
						buff10 = new Buff();
						buff10.Name = "防御强化";
						buff10.Level = 5;
						buff10.Round = 3;
						buffInstance = new BuffInstance();
						buffInstance.buff = buff10;
						buffInstance.Owner = source;
						buffInstance.LeftRound = buff10.Round;
						buffInstance6 = buffInstance;
						if (buff9 == null)
						{
							sprite6.Buffs.Add(buffInstance6);
						}
						else if (buffInstance6.LeftRound >= buff9.LeftRound)
						{
							buff9 = buffInstance6;
						}
						buff9 = sprite6.GetBuff("神速攻击");
						buff10 = new Buff();
						buff10.Name = "神速攻击";
						buff10.Level = 5;
						buff10.Round = 3;
						buffInstance = new BuffInstance();
						buffInstance.buff = buff10;
						buffInstance.Owner = source;
						buffInstance.LeftRound = buff10.Round;
						buffInstance6 = buffInstance;
						if (buff9 == null)
						{
							sprite6.Buffs.Add(buffInstance6);
						}
						else if (buffInstance6.LeftRound >= buff9.LeftRound)
						{
							buff9 = buffInstance6;
						}
						Log(sprite6.Role.Name + "被武穆遗书强化");
						attackResult.AddAttackInfo(sprite6, "强化！", Color.red);
						bf.ShowSkillAnimation(skill, sprite6.X, sprite6.Y);
						sprite6.Refresh();
					}
				}
			}
			else
			{
				attackResult = GetAttackResult(skill, source, target, bf);
				if (source.Role.HasTalent("真武七截阵"))
				{
					foreach (BattleSprite sprite7 in bf.Sprites)
					{
						if (sprite7.Role.HasTalent("真武七截阵") && sprite7.Role.Key != source.Role.Key && sprite7.Team == source.Team)
						{
							double random2 = Tools.GetRandom(0.0, 1.0);
							if (attackResult.Hp > 0)
							{
								attackResult.Hp = (int)((double)attackResult.Hp * (1.0 + random2 / 10.0));
							}
							attackResult.AddAttackInfo(sprite7, "真武七截阵", Color.red);
							Log(sprite7.Role.Name + "真武七截阵发动，叠加攻击效果");
						}
					}
				}
				if (source.Role.HasTalent("金刚伏魔圈"))
				{
					int num21 = 0;
					foreach (BattleSprite sprite8 in bf.Sprites)
					{
						if (sprite8.Role.HasTalent("金刚伏魔圈") && sprite8.Team == source.Team)
						{
							num21++;
						}
					}
					if (num21 >= 3)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * 1.5);
						foreach (BattleSprite sprite9 in bf.Sprites)
						{
							if (sprite9.Role.HasTalent("金刚伏魔圈") && sprite9.Team == source.Team)
							{
								attackResult.AddCastInfo(sprite9, "金刚伏魔圈！");
								Log(sprite9.Role.Name + "金刚伏魔圈发动，叠加攻击效果");
							}
						}
					}
					else
					{
						foreach (BattleSprite sprite10 in bf.Sprites)
						{
							if (sprite10.Role.HasTalent("金刚伏魔圈") && sprite10.Team == source.Team)
							{
								attackResult.AddAttackInfo(sprite10, "金刚伏魔圈解除!", Color.yellow);
								sprite10.Role.RemoveTalent("金刚伏魔圈");
								Log(sprite10.Role.Name + "金刚伏魔圈被破阵了！");
							}
						}
					}
				}
				if (source.Role.HasTalent("峨眉宗师") && (skill.Name.Contains("飘雪穿云掌") || skill.Name.Contains("四象掌") || skill.Name.Contains("佛光普照") || skill.Name.Contains("截手九式") || skill.Name.Contains("峨眉剑法") || skill.Name.Contains("回风拂柳剑") || skill.Name.Contains("灭剑绝剑") || skill.Name.Contains("九阴白骨爪") || skill.Name.Contains("霹雳雷火弹") || skill.Name.Contains("落英神剑掌") || skill.Name.Contains("玉箫剑法") || skill.Name.Contains("弹指神通")))
				{
					string[] array = new string[3] { "庄生晓梦迷蝴蝶，望帝春心托杜鹃...", "佛光普照空悲切,西子捧心谁能绝!", "貂蝉拜月望不断,昭君出塞意缠绵!" };
					attackResult.AddCastInfo(source, array[Tools.GetRandomInt(0, array.Length) % array.Length], 0.5f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.5, 3.0));
					}
					Log(source.Role.Name + "天赋【峨眉宗师】发动，增加攻击力");
				}
				if (source.Role.HasTalent("刚柔并济") && (skill.Name.Contains("太极拳") || skill.Name.Contains("太极剑") || skill.Name.Contains("绵掌") || skill.Name.Contains("玄虚刀法") || skill.Name.Contains("倚天屠龙笔法") || skill.Name.Contains("柔云剑法") || skill.Name.Contains("绕指柔剑") || skill.Name.Contains("神门十三剑") || skill.Name.Contains("绝户虎爪手")))
				{
					string[] array2 = new string[3] { "以静制动，以柔克刚", "辩位于尺寸毫厘，制动于擒扑封闭", "太极生圆" };
					attackResult.AddCastInfo(source, array2[Tools.GetRandomInt(0, array2.Length) % array2.Length], 0.3f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.1, 1.2));
					}
					Log(source.Role.Name + "天赋【刚柔并济】发动，增加攻击力");
				}
				if (source.Role.HasTalent("易经伐髓") && (skill.Name.Contains("罗汉拳") || skill.Name.Contains("韦陀棍") || skill.Name.Contains("般若掌") || skill.Name.Contains("拈花指") || skill.Name.Contains("伏魔棍") || skill.Name.Contains("达摩剑法") || skill.Name.Contains("大金刚掌") || skill.Name.Contains("如来千叶手") || skill.Name.Contains("天竺佛指") || skill.Name.Contains("须弥山掌") || skill.Name.Contains("燃木刀法") || skill.Name.Contains("龙爪手")))
				{
					string[] array3 = new string[3] { "无色无相，无嗔无狂。", "一花一世界，一叶一菩提。", "扫地扫地扫心地，心地不扫空扫地。" };
					attackResult.AddCastInfo(source, array3[Tools.GetRandomInt(0, array3.Length) % array3.Length], 0.5f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.2, 2.0));
					}
					Log(source.Role.Name + "天赋【易经伐髓】发动，增加攻击力");
				}
				if (source.Role.HasTalent("天龙.盖世英雄") && (skill.Name.Contains("降龙十八掌") || skill.Name.Contains("擒龙功") || skill.Name.Contains("易筋经") || skill.Name.Contains("打狗棒法")))
				{
					string[] array4 = new string[3] { "龙战于野！", "或跃在渊!", "吃我这一招!" };
					attackResult.AddCastInfo(source, array4[Tools.GetRandomInt(0, array4.Length) % array4.Length], 0.5f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.2, 1.8));
					}
					Log(source.Role.Name + "天赋【天龙.盖世英雄】发动，增加攻击力");
				}
				if (source.Role.HasTalent("铁剑掌门") && (skill.Name.Contains("铁剑剑法") || skill.Name.Contains("漫天花雨") || skill.Name.Contains("碧落苍穹") || skill.Name.Contains("铁血大旗功")))
				{
					string[] array5 = new string[2] { "看我铁剑门的厉害！", "吃我这一招!" };
					attackResult.AddCastInfo(source, array5[Tools.GetRandomInt(0, array5.Length) % array5.Length], 0.5f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * 1.4);
					}
					Log(source.Role.Name + "天赋【铁剑掌门】发动，增加攻击力");
				}
				if (source.Role.HasTalent("射雕英雄") && (skill.Name.Contains("降龙十八掌") || skill.Name.Contains("打狗棒法") || skill.Name.Contains("九阴真经")))
				{
					string[] array6 = new string[3] { "蓉儿，看我的!", "侠之大者，为国为民", "呵！！" };
					attackResult.AddCastInfo(source, array6[Tools.GetRandomInt(0, array6.Length) % array6.Length], 0.5f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.2, 1.5));
					}
					Log(source.Role.Name + "天赋【射雕英雄】发动，增加攻击力");
				}
				if (source.Role.HasTalent("玲珑璇玑") && (skill.Name.Contains("打狗棒法") || skill.Name.Contains("九阴真经")))
				{
					string[] array7 = new string[1] { "靖哥哥，看我的!" };
					attackResult.AddCastInfo(source, array7[Tools.GetRandomInt(0, array7.Length) % array7.Length], 0.35f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.2, 1.5));
					}
					Log(source.Role.Name + "天赋【玲珑璇玑】发动，增加攻击力");
				}
				if (source.Role.HasTalent("大理世家") && (skill.Name.Contains("六脉神剑") || skill.Name.Contains("一阳指")))
				{
					string[] array8 = new string[3] { "无形剑气！", "大理段氏的威名!", "我戳死你!" };
					attackResult.AddCastInfo(source, array8[Tools.GetRandomInt(0, array8.Length) % array8.Length], 0.5f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.2, 1.5));
					}
					Log(source.Role.Name + "天赋【大理世家】发动，增加攻击力");
				}
				bool flag = bf.IsRoleInField("木婉清", source.Team);
				if (source.Role.Name == "段誉" && source.Role.HasTalent("木婉清的眷恋") && flag)
				{
					string[] array9 = new string[3] { "婉妹，我绝不辜负你的期望！", "婉妹，看我段誉大显神通!", "婉妹，有我在！" };
					attackResult.AddCastInfo(source, array9[Tools.GetRandomInt(0, array9.Length) % array9.Length], 0.5f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.2, 1.8));
					}
					Log(source.Role.Name + "天赋【木婉清的眷恋】发动，增加攻击力");
				}
				if (source.Role.Name == "袁承志" && source.Role.HasTalent("长平公主的眷恋"))
				{
					string[] array10 = new string[3] { "阿九，我绝不辜负你的期望！", "阿九妹子...你如今真的出家了么？！", "阿九..." };
					attackResult.AddCastInfo(source, array10[Tools.GetRandomInt(0, array10.Length) % array10.Length], 0.5f);
					if (attackResult.Hp > 0)
					{
						attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.2, 1.5));
					}
					Log(source.Role.Name + "天赋【长平公主的眷恋】发动，增加攻击力");
				}
				if (source.Role.HasTalent("段王爷的电眼") && (target.Role.Female || target.Role.HasTalent("阉人")) && Tools.ProbabilityTest(0.5))
				{
					string[] array11 = new string[3] { "我段某人帅么~", "哎呀，看我段某人给你秀秀~", "我就是镇南王爷段正淳~" };
					attackResult.AddCastInfo(source, array11[Tools.GetRandomInt(0, array11.Length) % array11.Length]);
					Buff buff11 = new Buff();
					buff11.Name = "晕眩";
					buff11.Level = 0;
					buff11.Round = 2;
					attackResult.Debuff.Add(buff11);
					Log(source.Role.Name + "天赋【段王爷的电眼】发动，" + target.Role.Name + "被秀晕了");
				}
				if (target.Role.HasTalent("至空至明") && Tools.ProbabilityTest(0.15))
				{
					target.SkillCdRecover();
					attackResult.AddCastInfo(target, new string[2] { "浅斟低吟浮名尽 (天赋*至空至明发动)", "流觞曲水入梦来 (天赋*至空至明发动)" }, 0.5f);
					Log(target.Role.Name + "天赋【至空至明】发动，所有技能冷却");
				}
				if (source.Role.HasTalent("无形剑气") && skill.Name == "六脉神剑")
				{
					int num22 = Math.Abs(source.Mp - target.Mp);
					if (num22 > 10000)
					{
						num22 = 10000;
					}
					int num23 = (int)((double)num22 * Tools.GetRandom(0.1, 0.25));
					attackResult.Hp += num23;
					Log(source.Role.Name + "天赋【无形剑气】发动，造成额外伤害" + num23);
				}
			}
			string text = string.Empty;
			bool flag2 = false;
			BuffInstance buff12 = target.GetBuff("飘渺");
			bool flag3 = false;
			if (target.Team != source.Team)
			{
				flag3 = true;
			}
			if (target.Team == source.Team && (attackResult.Hp > 0 || attackResult.Mp > 0))
			{
				flag3 = true;
			}
			if (flag3 && buff12 != null && Tools.ProbabilityTest((double)buff12.Level * 0.07))
			{
				flag2 = true;
			}
			if (flag3 && target.Role.HasTalent("飘然") && Tools.ProbabilityTest(0.08))
			{
				attackResult.AddCastInfo(target, "我闪！ (天赋*飘然发动)", 0.5f);
				Log(target.Role.Name + "天赋【飘然】发动，躲避攻击");
				flag2 = true;
			}
			if (target.Role.HasTalent("赵敏的眷念") && Tools.ProbabilityTest(0.1))
			{
				attackResult.AddCastInfo(target, "敏敏，我没事的！ (天赋*赵敏的眷念发动)", 0.5f);
				Log(target.Role.Name + "天赋【赵敏的眷念】发动，躲避攻击");
				flag2 = true;
			}
			if (target.Team != source.Team && target.Role.HasTalent("鹿鼎.一品鹿鼎公") && Tools.ProbabilityTest(0.25))
			{
				attackResult.AddCastInfo(target, "好汉，饶命饶命饶命饶命（念晕你！）");
				Log(target.Role.Name + "天赋【鹿鼎.一品鹿鼎公】发动，躲避攻击。" + source.Role.Name + "被念晕了");
				flag2 = true;
				Buff buff13 = new Buff();
				buff13.Name = "晕眩";
				buff13.Level = 0;
				buff13.Round = 3;
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = buff13;
				buffInstance.Owner = source;
				buffInstance.Level = buff13.Level;
				buffInstance.LeftRound = buff13.Round;
				BuffInstance item = buffInstance;
				source.Buffs.Add(item);
				attackResult.AddAttackInfo(source, "晕眩", Color.white);
				source.Refresh();
			}
			if (target.Team != source.Team && target.Role.HasTalent("沾衣十八跌") && Tools.ProbabilityTest(0.1))
			{
				attackResult.AddCastInfo(target, "沾衣十八跌！");
				flag2 = true;
				Log(target.Role.Name + "天赋【沾衣十八跌】发动，躲避攻击。" + source.Role.Name + "被震晕了");
				Buff buff14 = new Buff();
				buff14.Name = "晕眩";
				buff14.Level = 0;
				buff14.Round = 2;
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = buff14;
				buffInstance.Owner = source;
				buffInstance.Level = buff14.Level;
				buffInstance.LeftRound = buff14.Round;
				BuffInstance buff15 = buffInstance;
				source.AddBuff(buff15);
				source.Refresh();
			}
			if (target.Team != source.Team && target.Role.HasTalent("段王爷的电眼") && (source.Role.Female || source.Role.HasTalent("阉人")) && Tools.ProbabilityTest(0.5))
			{
				attackResult.AddCastInfo(source, "他好帅，真舍不得伤害他...(天赋*段王爷的电眼发动)", 0.5f);
				Log(target.Role.Name + "天赋【段王爷的电眼】发动，躲避攻击");
				flag2 = true;
			}
			if (flag3 && target.Role.HasTalent("孤独求败") && Tools.ProbabilityTest(0.05))
			{
				attackResult.AddCastInfo(target, "我立于天下武学的巅峰！ (天赋*孤独求败发动)", 0.2f);
				Log(target.Role.Name + "天赋【孤独求败】发动，躲避攻击");
				flag2 = true;
			}
			if (flag3 && target.Role.HasTalent("雪山飞狐") && Tools.ProbabilityTest(0.1))
			{
				attackResult.AddCastInfo(target, "雪山飞狐！", 0.5f);
				Log(target.Role.Name + "天赋【雪山飞狐】发动，躲避攻击");
				flag2 = true;
			}
			if (flag3 && target.Role.HasTalent("神行百变"))
			{
				if ((double)((float)target.Hp / (float)target.MaxHp) >= 0.8)
				{
					if (Tools.ProbabilityTest(0.15))
					{
						attackResult.AddCastInfo(target, "看我神行百变！", 0.5f);
						Log(target.Role.Name + "天赋【神行百变】发动，躲避攻击");
						flag2 = true;
					}
				}
				else if (Tools.ProbabilityTest(0.07))
				{
					attackResult.AddCastInfo(target, "看我神行百变！", 0.5f);
					Log(target.Role.Name + "天赋【神行百变】发动，躲避攻击");
					flag2 = true;
				}
			}
			if (flag3 && target.Role.HasTalent("奇门遁甲") && Tools.ProbabilityTest(0.09))
			{
				attackResult.AddCastInfo(target, "看我五行之术！", 0.5f);
				Log(target.Role.Name + "天赋【奇门遁甲】发动，躲避攻击");
				flag2 = true;
			}
			if (flag2 && flag3 && source.Role.HasTalent("奇门遁甲") && Tools.ProbabilityTest(0.8))
			{
				attackResult.AddCastInfo(source, "看我五行之术！", 0.5f);
				Log(source.Role.Name + "天赋【奇门遁甲】发动，必定命中");
				flag2 = false;
			}
			if (flag2 && flag3 && source.Role.HasTalent("铁口直断") && Tools.ProbabilityTest(0.1))
			{
				attackResult.AddCastInfo(source, "我掐指一算，哎呀，你有血光之灾！", 0.5f);
				Log(source.Role.Name + "天赋【铁口直断】发动，命中");
				flag2 = false;
			}
			if (flag2 && flag3 && source.Role.HasTalent("锐眼") && Tools.ProbabilityTest(0.05))
			{
				attackResult.AddCastInfo(source, "哼！我看到了真相！", 0.5f);
				Log(source.Role.Name + "天赋【锐眼】发动，命中");
				flag2 = false;
			}
			if (flag3 && source.Role.HasTalent("白内障") && Tools.ProbabilityTest(0.1))
			{
				attackResult.AddCastInfo(source, "让我穿针眼？算了吧！", 0.5f);
				Log(source.Role.Name + "天赋【白内障】发动，MISS");
				flag2 = true;
			}
			if (flag2)
			{
				double num24 = 0.0;
				foreach (Trigger trigger in source.Role.GetTriggers("mingzhong"))
				{
					num24 += (double)((float)int.Parse(trigger.Argvs[0]) / 100f);
				}
				if (Tools.ProbabilityTest(num24))
				{
					flag2 = false;
				}
			}
			double num25 = 0.1;
			if (target.Role.HasTalent("慕容世家"))
			{
				num25 *= 2.0;
			}
			if (target.Role.HasTalent("斗转星移") && Tools.ProbabilityTest(num25) && target.Team != source.Team)
			{
				attackResult.AddCastInfo(target, "以彼之道，还施彼身！（天赋*斗转星移发动！）");
				attackResult.AddAttackInfo(target, "斗转星移", Color.magenta);
				flag2 = true;
				int num26 = (int)((float)attackResult.Hp * 0.25f);
				int num27 = (int)((float)attackResult.Mp * 0.25f);
				if (num26 > 0)
				{
					text = string.Format("-{0}", num26);
				}
				else if (num26 < 0)
				{
					text = string.Format("+{0}", -num26);
				}
				if (num26 != 0)
				{
					attackResult.AddAttackInfo(source, text, Color.white);
					source.Hp -= num26;
				}
				if (num27 > 0)
				{
					text = string.Format("-内力{0}", num27);
				}
				else if (num27 < 0)
				{
					text = string.Format("+内力{0}", -num27);
				}
				if (num27 != 0)
				{
					attackResult.AddAttackInfo(source, text, Color.blue);
					source.Mp -= num27;
				}
				Log(target.Role.Name + "天赋【斗转星移】发动，躲避攻击，反弹伤害" + num26 + ",反弹内力" + num27);
				bf.ShowSkillAnimation(skill, source.X, source.Y);
			}
			if (flag2)
			{
				attackResult.Hp = 0;
				attackResult.Mp = 0;
				attackResult.costBall = 0;
				attackResult.Critical = false;
				attackResult.Buff.Clear();
				attackResult.Debuff.Clear();
				attackResult.AddAttackInfo(target, "MISS", Color.white);
			}
			float num28 = 0f;
			if (target.Role.HasTalent("万年长春"))
			{
				num28 = 0.1f;
			}
			if (attackResult.Hp > 0 && target.Role.HasTalent("不老长春") && Tools.ProbabilityTest(0.1 + (double)num28))
			{
				int num29 = (int)((float)attackResult.Hp * 0.35f);
				Log(target.Role.Name + "天赋【不老长春】发动，回血" + num29);
				if (target.GetBuff("重伤") != null)
				{
					Log(target.Role.Name + "由于重伤，回血效果减半");
					num29 /= 2;
				}
				attackResult.AddAttackInfo(target, string.Format("不老长春回血{0}", num29), Color.green);
				target.Hp += num29;
				attackResult.Hp = 0;
				attackResult.Mp = 0;
				attackResult.costBall = 0;
				attackResult.Critical = false;
				attackResult.Buff.Clear();
				attackResult.Debuff.Clear();
			}
			if (skill.Name == "解毒" && source.Team == target.Team && target.GetBuff("中毒") != null)
			{
				target.DeleteBuff("中毒");
				attackResult.AddAttackInfo(target, "解毒", Color.white);
				Log(target.Role.Name + "中毒解除");
			}
			if (source.Role.HasTalent("死生茫茫") && attackResult.Hp > 0 && Tools.ProbabilityTest(0.1))
			{
				Log(source.Role.Name + "天赋【生死茫茫】发动");
				text = string.Format("-{0}", (int)((double)attackResult.Hp * 0.05));
				attackResult.AddAttackInfo(target, text, Color.yellow);
				int num30 = Math.Min((int)((double)attackResult.Hp * 0.05), (int)((double)target.MaxHp * 0.1));
				if (target.MaxHp > num30)
				{
					target.MaxHp -= num30;
				}
				else
				{
					target.MaxHp = 1;
				}
				if (target.Hp > target.MaxHp)
				{
					target.Hp = target.MaxHp;
				}
				Log(target.Role.Name + "生命值上限" + text);
				int num31 = (int)((double)attackResult.Hp * 0.5);
				if (source.GetBuff("重伤") != null)
				{
					Log(target.Role.Name + "由于重伤，回血效果减半");
					num31 /= 2;
				}
				source.Hp += num31;
				Log(source.Role.Name + "天赋【生死茫茫】发动，吸血" + num31);
				text = string.Format("吸血{0}", num31);
				attackResult.AddAttackInfo(source, text, Color.red);
				string info = "啊！好疼！" + target.Role.Name + "气血上限减少" + (int)((double)attackResult.Hp * 0.15) + "！！";
				attackResult.AddCastInfo(target, info);
				string info2 = "嘿嘿...（" + source.Role.Name + "天赋【死生茫茫】发动！）";
				attackResult.AddCastInfo(source, info2);
			}
			if (target.Role.HasTalent("真武七截阵"))
			{
				foreach (BattleSprite sprite11 in bf.Sprites)
				{
					if (sprite11.Role.HasTalent("真武七截阵") && sprite11.Team == target.Team && sprite11.Role.Key != target.Role.Key && Tools.GetRandom(0.0, 1.0) <= 0.5)
					{
						Log(sprite11.Role.Name + "阵法【真武七截阵】发动，增加防御力");
						attackResult.AddCastInfo(sprite11, "真武七截阵");
						attackResult.Hp = (int)((float)attackResult.Hp * 0.3f);
						target = sprite11;
						attackResult.targetX = target.X;
						attackResult.targetY = target.Y;
						break;
					}
				}
			}
			if (attackResult.Hp > 0)
			{
				List<BattleSprite> list4 = new List<BattleSprite>();
				list4.Clear();
				foreach (BattleSprite sprite12 in bf.Sprites)
				{
					if (sprite12.Role.HasTalent("五行阵") && sprite12.Team == target.Team && sprite12.Role.Key != target.Role.Key && Math.Abs(sprite12.X - target.X) + Math.Abs(sprite12.Y - target.Y) <= 5 && Tools.ProbabilityTest(0.5))
					{
						list4.Add(sprite12);
					}
				}
				int num32 = (attackResult.Hp = (int)((double)attackResult.Hp / (double)(list4.Count + 1)));
				foreach (BattleSprite item2 in list4)
				{
					Log(item2.Role.Name + "阵法【五行阵】发动，增加防御力");
					item2.Hp -= num32;
					attackResult.AddCastInfo(item2, "五行秘术！");
				}
			}
			if (attackResult.Hp > 0 && target.Role.HasTalent("八卦阵") && Tools.ProbabilityTest(0.5))
			{
				List<BattleSprite> list5 = new List<BattleSprite>();
				list5.Clear();
				foreach (BattleSprite sprite13 in bf.Sprites)
				{
					if (sprite13.Team == target.Team && sprite13.Role.Key != target.Role.Key && Math.Abs(sprite13.X - target.X) + Math.Abs(sprite13.Y - target.Y) <= 5)
					{
						list5.Add(sprite13);
					}
				}
				if (list5.Count > 0)
				{
					int num33 = (int)((double)attackResult.Hp * 0.8);
					attackResult.Hp = (int)((double)attackResult.Hp * 0.2);
					BattleSprite battleSprite = list5[Tools.GetRandomInt(0, list5.Count) % list5.Count];
					attackResult.AddCastInfo(battleSprite, "八卦阵发动，替我挡着！（八卦阵发动！）");
					Log(battleSprite.Role.Name + "阵法【八卦阵】发动，增加防御力");
					battleSprite.Hp -= num33;
					attackResult.AddAttackInfo(battleSprite, string.Format("-{0}", num33), Color.yellow);
				}
			}
			if (target.Team != source.Team && source.GetBuff("易容") != null)
			{
				attackResult.AddCastInfo(source, "看招！（" + source.Role.Name + "易容后，发动奇袭！）");
				Log(source.Role.Name + "发动奇袭，攻击力增加，易容取消");
				if (attackResult.Hp > 0)
				{
					attackResult.Hp = (int)((double)attackResult.Hp * Tools.GetRandom(1.1, 1.3));
				}
				source.DeleteBuff("易容");
				source.Refresh();
			}
			if (attackResult.Hp > 0)
			{
				text = string.Format("-{0}", attackResult.Hp);
			}
			else if (attackResult.Hp < 0)
			{
				text = string.Format("+{0}", -attackResult.Hp);
			}
			if (attackResult.Hp != 0)
			{
				if (attackResult.Critical)
				{
					attackResult.AddAttackInfo(target, "暴击 " + text, Color.yellow);
					Log("暴击！！" + target.Role.Name + "受到伤害" + attackResult.Hp);
				}
				else
				{
					attackResult.AddAttackInfo(target, text, Color.white);
					Log(target.Role.Name + "受到伤害【" + attackResult.Hp + "】");
				}
				target.Hp -= attackResult.Hp;
			}
			float num34 = 0f;
			if ((skill.Name == "血刀大法" || skill.Name == "血刀大法.吸") && attackResult.Hp > 0)
			{
				if (skill.Name == "血刀大法")
				{
					num34 = 0.1f;
				}
				else if (skill.Name == "血刀大法.吸")
				{
					num34 = 0.2f;
				}
				if (source.Role.HasTalent("血海魔功"))
				{
					attackResult.AddCastInfo(source, "嘿嘿嘿，吸光你们！", 0.3f);
					num34 += 0.2f;
				}
				if (source.Role.HasTalent("血魔刀法"))
				{
					attackResult.AddCastInfo(source, "血...", 0.3f);
					num34 += 0.15f;
				}
			}
			if (source.Role.HasTalent("嗜血狂魔"))
			{
				attackResult.AddCastInfo(source, "看我血刀门的厉害！", 0.3f);
				num34 += 0.05f + (float)((double)(0.05f * (float)source.Role.level) / 30.0);
			}
			foreach (Trigger trigger2 in source.Role.GetTriggers("xi"))
			{
				num34 += (float)((double)int.Parse(trigger2.Argvs[0]) / 100.0);
			}
			if (num34 > 0f && attackResult.Hp > 0)
			{
				int num35 = (int)((float)attackResult.Hp * num34);
				if (source.GetBuff("重伤") != null)
				{
					num35 /= 2;
					Log(source.Role.Name + "由于重伤效果，回复减半");
				}
				text = string.Format("吸血{0}", num35);
				attackResult.AddAttackInfo(source, text, Color.red);
				source.Hp += num35;
				Log(source.Role.Name + "吸血" + num35);
			}
			if (source.Role.HasTalent("北冥神功") && target.Mp > 0 && source != target)
			{
				int num36 = (int)((double)(source.Role.gengu * 2 * source.Role.GetEquippedInternalSkill().Level / 10 * (2 - target.Role.dingli / 100)) * Tools.GetRandom(1.0, 1.5));
				if (num36 > target.Mp)
				{
					num36 = target.Mp;
				}
				text = string.Format("吸内{0}", num36);
				attackResult.AddAttackInfo(source, text, Color.blue);
				source.Mp += num36;
				target.Mp -= num36;
				Log(source.Role.Name + "天赋【北冥神功】发动，吸取内力" + num36);
			}
			if (source.Role.HasTalent("鲲跃北溟") && target.Mp > 0 && source != target && Tools.ProbabilityTest(0.5))
			{
				int num37 = source.Role.gengu * source.Role.GetEquippedInternalSkill().Level / 10 * (2 - target.Role.dingli / 100);
				if (num37 > target.Mp)
				{
					num37 = target.Mp;
				}
				text = string.Format("吸内{0}", num37);
				attackResult.AddAttackInfo(source, text, Color.blue);
				source.Mp += num37;
				target.Mp -= num37;
				Log(source.Role.Name + "天赋【鲲跃北溟】发动，吸取内力" + num37);
			}
			if (source.Role.HasTalent("化功大法") && target.Mp > 0 && source != target && Tools.ProbabilityTest(0.5))
			{
				int num38 = source.Role.gengu * source.Role.GetEquippedInternalSkill().Level / 10 * 3 * (2 - target.Role.dingli / 100);
				if (num38 > target.Mp)
				{
					num38 = target.Mp;
				}
				text = string.Format("-内力{0}", num38);
				attackResult.AddAttackInfo(target, text, Color.blue);
				target.Mp -= num38;
				Log(source.Role.Name + "天赋【化功大法】发动，化去内力" + num38);
			}
			if (source.Role.HasTalent("吸星大法") && target.Mp > 0 && source != target)
			{
				int num39 = (int)((double)(source.Role.gengu * 2 * source.Role.GetEquippedInternalSkill().Level / 10 * (2 - target.Role.dingli / 100)) * Tools.GetRandom(1.0, 2.0));
				if (num39 > target.Mp)
				{
					num39 = target.Mp;
				}
				text = string.Format("吸内{0}", num39);
				attackResult.AddAttackInfo(source, text, Color.blue);
				source.Mp += num39;
				target.Mp -= num39;
				Log(source.Role.Name + "天赋【吸星大法】发动，吸取内力" + num39);
				BuffInstance buff16 = target.GetBuff("封穴");
				Buff buff17 = new Buff();
				buff17.Name = "封穴";
				buff17.Level = 0;
				buff17.Round = 3;
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = buff17;
				buffInstance.Owner = source;
				buffInstance.LeftRound = buff17.Round;
				BuffInstance buffInstance7 = buffInstance;
				if (buff16 == null)
				{
					target.Buffs.Add(buffInstance7);
				}
				else if (buffInstance7.LeftRound >= buff16.LeftRound)
				{
					buff16 = buffInstance7;
				}
				attackResult.AddAttackInfo(target, "穴位被封！", Color.red);
				Log(target.Role.Name + "被吸星大法封穴");
				bf.ShowSkillAnimation(skill, target.X, target.Y);
				target.Refresh();
			}
			if (source.Role.HasTalent("玄门罡气") && source.Role.GetEquippedInternalSkill().Name == "九阴神功")
			{
				int randomInt2 = Tools.GetRandomInt(10, 40);
				Log(source.Role.Name + "天赋【玄门罡气】发动。" + target.Role.Name + "减少集气" + randomInt2);
				target.Sp -= randomInt2;
				attackResult.AddAttackInfo(target, "-集气 " + randomInt2, Color.yellow);
			}
			LuaManager.Call("AttackLogic_extendTalents2", source, target, skill, bf, attackResult);
			if (attackResult.Mp != 0)
			{
				string info3 = string.Empty;
				if (attackResult.Mp > 0)
				{
					info3 = string.Format("-{0}内力", attackResult.Mp);
				}
				else if (attackResult.Hp < 0)
				{
					info3 = string.Format("+{0}内力", -attackResult.Mp);
				}
				if (attackResult.Critical)
				{
					attackResult.AddAttackInfo(target, info3, Color.blue);
				}
				else
				{
					attackResult.AddAttackInfo(target, info3, Color.blue);
				}
				target.Mp -= attackResult.Mp;
			}
			if (attackResult.costBall != 0)
			{
				string info4 = string.Empty;
				if (attackResult.costBall > 0)
				{
					info4 = string.Format("-{0}怒气", attackResult.costBall);
				}
				else if (attackResult.Hp < 0)
				{
					info4 = string.Format("+{0}怒气", -attackResult.costBall);
				}
				if (attackResult.Critical)
				{
					attackResult.AddAttackInfo(target, info4, Color.cyan);
				}
				else
				{
					attackResult.AddAttackInfo(target, info4, Color.cyan);
				}
				target.Balls -= attackResult.costBall;
			}
			if (source.Role.HasTalent("墨家传人") && (skill.Name.Contains("墨拳") || skill.Name.Contains("墨剑") || skill.Name.Contains("缚星锁")))
			{
				int randomInt3 = Tools.GetRandomInt(0, Buff.DebuffNames.Length - 1);
				Buff buff18 = new Buff();
				buff18.Name = Buff.DebuffNames[randomInt3];
				buff18.Level = 3;
				buff18.Round = 3;
				buff18.Property = 100;
				attackResult.Debuff.Add(buff18);
			}
			foreach (Buff item3 in attackResult.Buff)
			{
				BuffInstance buff19 = source.GetBuff(item3.Name);
				BuffInstance buffInstance = new BuffInstance();
				buffInstance.buff = item3;
				buffInstance.Owner = source;
				buffInstance.Level = item3.Level;
				buffInstance.LeftRound = item3.Round;
				BuffInstance buffInstance8 = buffInstance;
				if (buff19 == null)
				{
					source.Buffs.Add(buffInstance8);
				}
				else if (buffInstance8.Level >= buff19.Level)
				{
					buff19.buff = buffInstance8.buff;
					buff19.Owner = buffInstance8.Owner;
					buff19.Level = buffInstance8.Level;
					buff19.LeftRound = buffInstance8.LeftRound;
				}
				if (buffInstance8.buff.Name == "魔神降临")
				{
				}
				source.AttackInfo(item3.Name + "(" + item3.Level + ")", Color.yellow);
				Log(source.Role.Name + "获得增益状态【" + item3.Name + "】，等级" + item3.Level);
			}
			bool flag4 = false;
			foreach (BuffInstance buff24 in target.Buffs)
			{
				if (buff24.buff.Name == "圣战")
				{
					flag4 = true;
					Log(target.Role.Name + "圣战状态，不受一切减益效果影响");
					break;
				}
			}
			if (!flag4)
			{
				double num40 = 0.0;
				foreach (Trigger trigger3 in target.Role.GetTriggers("anti_debuff"))
				{
					num40 += (double)int.Parse(trigger3.Argvs[0]) / 100.0;
				}
				foreach (Buff item4 in attackResult.Debuff)
				{
					if (!Tools.ProbabilityTest(num40))
					{
						BuffInstance buff20 = target.GetBuff(item4.Name);
						BuffInstance buffInstance = new BuffInstance();
						buffInstance.buff = item4;
						buffInstance.Owner = target;
						buffInstance.Level = item4.Level;
						buffInstance.LeftRound = item4.Round;
						BuffInstance buffInstance9 = buffInstance;
						if (source.Role.HasTalent("毒系精通") && buffInstance9.buff.Name.Equals("中毒"))
						{
							Log(source.Role.Name + "天赋【毒系精通】发动，增强用毒效果");
							attackResult.AddCastInfo(source, "我毒！（天赋*毒系精通发动)");
							buffInstance9.Level += 3;
							buffInstance9.LeftRound += 2;
						}
						if (source.Role.HasTalent("毒圣") && buffInstance9.buff.Name.Equals("中毒"))
						{
							Log(source.Role.Name + "天赋【毒圣】发动，增强用毒效果");
							attackResult.AddCastInfo(source, "无人能解！（天赋*毒圣发动)");
							buffInstance9.Level += 5;
							buffInstance9.LeftRound += 4;
						}
						if (source.Role.HasTalent("我是疯子") && Tools.ProbabilityTest(0.5))
						{
							attackResult.AddCastInfo(source, "我是疯子！");
							buffInstance9.Level += Tools.GetRandomInt(0, 2);
							buffInstance9.LeftRound += Tools.GetRandomInt(0, 2);
						}
						if (buff20 == null)
						{
							target.Buffs.Add(buffInstance9);
						}
						else if (buffInstance9.Level >= buff20.Level)
						{
							buff20.buff = buffInstance9.buff;
							buff20.Owner = buffInstance9.Owner;
							buff20.Level = buffInstance9.Level;
							buff20.LeftRound = buffInstance9.LeftRound;
						}
						target.AttackInfo(item4.Name + "(" + item4.Level + ")", Color.red);
						Log(target.Role.Name + "获得减益状态【" + buffInstance9.buff.Name + "】，等级" + buffInstance9.Level);
					}
				}
			}
			source.Refresh();
			target.Refresh();
			double num41 = 0.5 + (double)(float)target.Role.fuyuan / 200.0 * 0.2;
			if (target.Role.HasTalent("暴躁"))
			{
				num41 += 0.15;
			}
			if (Tools.ProbabilityTest(num41) && source.Team != target.Team)
			{
				target.Balls++;
				if (target.Role.HasTalent("斗魂"))
				{
					target.Balls++;
					Log(target.Role.Name + "天赋【斗魂】发动，怒气增益翻倍");
				}
			}
			int num42 = 0;
			double num43 = 0.0;
			if (source.Role.HasTalent("幽居"))
			{
				num42 = 1;
				num43 += (double)(0.005f * (float)source.Role.level);
			}
			if (source.Role.HasTalent("素心神剑"))
			{
				num42 = 1;
				num43 += Tools.GetRandom(0.0, 0.2);
			}
			if (source.Role.HasTalent("左右互搏"))
			{
				num42 = 1;
				num43 += Tools.GetRandom(0.0, 0.6);
			}
			foreach (BuffInstance buff25 in source.Buffs)
			{
				if (buff25.buff.Name == "左右互博" || buff25.buff.Name == "醉酒")
				{
					num42 = 1;
					num43 += Tools.GetRandom(0.0, (float)buff25.Level * 0.1f);
				}
				else if (buff25.buff.Name == "神速攻击")
				{
					num42 = 2;
					num43 += Tools.GetRandom(0.0, (float)buff25.Level * 0.1f);
					break;
				}
			}
			for (int i = 0; i < num42; i++)
			{
				int num44 = (int)((double)attackResult.Hp * (0.6 - 0.3 * (double)i));
				if (Tools.ProbabilityTest(num43))
				{
					if (num44 > 0)
					{
						text = string.Format("多重攻击-{0}", num44);
					}
					else if (num44 < 0)
					{
						text = string.Format("多重攻击+{0}", -num44);
					}
					if (attackResult.Critical)
					{
						attackResult.AddAttackInfo(target, text, Color.yellow);
					}
					else
					{
						attackResult.AddAttackInfo(target, text, Color.white);
					}
					Log("多重攻击！！" + target.Role.Name + "受到伤害" + num44);
					target.Hp -= num44;
				}
			}
			return attackResult;
		}

		public static AttackResult GetAttackResult(SkillBox skill, BattleSprite sourceSprite, BattleSprite targetSprite, BattleField bf)
		{
			Role role = sourceSprite.Role;
			Role role2 = targetSprite.Role;
			AttackResult attackResult = new AttackResult();
			AttackFormula attackFormula = new AttackFormula();
			if (skill.IsSpecial)
			{
				attackResult.Critical = true;
				foreach (Buff buff15 in skill.Buffs)
				{
					if (buff15.IsDebuff)
					{
						double num = 0.0;
						num = ((buff15.Property != -1) ? ((double)(buff15.Property / 100)) : (2.0 - (double)role2.AttributesFinal["dingli"] / 100.0 * 0.5));
						if (Tools.ProbabilityTest(num))
						{
							attackResult.Debuff.Add(buff15);
						}
					}
					else
					{
						double num2 = 0.0;
						num2 = ((buff15.Property != -1) ? ((double)(buff15.Property / 100)) : (1.8 + (double)role2.AttributesFinal["fuyuan"] / 100.0 * 0.5));
						if (Tools.ProbabilityTest(num2))
						{
							attackResult.Buff.Add(buff15);
						}
					}
				}
				if (skill.Name == "华佗再世")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "救死扶伤，医者本分也。" });
					int num3 = (int)((double)(role.AttributesFinal["gengu"] + role.AttributesFinal["fuyuan"]) * Tools.GetRandom(5.0, 15.0)) + (int)((double)role2.maxhp * Tools.GetRandom(0.1, 0.3));
					attackResult.Hp = -num3;
					return attackResult;
				}
				if (skill.Name == "解毒")
				{
					attackResult.AddCastInfo(sourceSprite, new string[2] { "百毒不侵！", "这都是小case" });
					attackResult.Hp = 0;
					return attackResult;
				}
				if (skill.Name == "闪电貂")
				{
					attackResult.AddCastInfo(sourceSprite, new string[2] { "貂儿，上！", "大坏人呀！" });
					attackResult.Hp = 0;
					return attackResult;
				}
				if (skill.Name == "一刀两断")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "啊！！！！斩！" });
					if (Tools.ProbabilityTest(0.5))
					{
						attackResult.Hp = (int)((float)targetSprite.Role.Attributes["hp"] / 2f);
						return attackResult;
					}
					attackResult.Hp = 0;
					return attackResult;
				}
				if (skill.Name == "沉鱼落雁")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "我...美么？" });
					attackResult.Hp = 0;
					attackResult.costBall = targetSprite.Balls;
					return attackResult;
				}
				if (skill.Name == "溜须拍马")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "各位好汉英明神武，鸟生鱼汤~" });
					attackResult.Hp = 0;
					return attackResult;
				}
				if (skill.Name == "打鸡血")
				{
					attackResult.AddCastInfo(sourceSprite, new string[3] { "啊~~~我的左手正在熊熊燃烧！", "爆发吧，小宇宙!", "来一管新鲜的鸡血！" });
					attackResult.Hp = 0;
					attackResult.costBall = -2;
					return attackResult;
				}
				if (skill.Name == "诗酒飘零")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "美酒过后诗百篇，醉卧长安梦不觉" });
					attackResult.Hp = 0;
					attackResult.costBall = 0;
					return attackResult;
				}
				if (skill.Name == "凌波微步")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "凌波微步，罗袜生尘..." });
					attackResult.Hp = 0;
					return attackResult;
				}
				if (skill.Name == "襄儿的心愿")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "神雕大侠！襄儿在呼唤你！" });
					attackResult.Hp = Tools.GetRandomInt(1000, 2000 + 100 * role.Level);
					return attackResult;
				}
				if (skill.Name == "火枪")
				{
					int num4 = Math.Abs(role.fuyuan - role2.fuyuan);
					attackResult.AddCastInfo(sourceSprite, new string[2] { "BIU BIU BIU!", "让你瞧瞧红毛鬼子的火器!" });
					attackResult.Hp = 200 + Tools.GetRandomInt(5 * num4, 20 * num4);
					return attackResult;
				}
				if (skill.Name == "撒石灰")
				{
					attackResult.AddCastInfo(sourceSprite, new string[2] { "看我的石灰粉！", "弄瞎你!" });
					return attackResult;
				}
				if (skill.Name == "雪遁步行")
				{
					attackResult.AddCastInfo(sourceSprite, new string[2] { "看我雪遁步行！", "血刀门也有轻功，不知道吧？哈哈！" });
					return attackResult;
				}
				if (skill.Name == "武穆兵法")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "将在谋，不在勇，吾万人敌" });
				}
				return LuaManager.Call<AttackResult>("AttackLogic_extendSpecialSkill", new object[5] { attackResult, skill, sourceSprite, targetSprite, bf });
			}
			int num5 = 0;
			switch (skill.Type)
			{
			case 0:
				num5 = role.AttributesFinal["quanzhang"];
				break;
			case 1:
				num5 = role.AttributesFinal["jianfa"];
				break;
			case 2:
				num5 = role.AttributesFinal["daofa"];
				break;
			case 3:
				num5 = role.AttributesFinal["qimen"];
				break;
			case 4:
				num5 = role.AttributesFinal["gengu"];
				break;
			default:
				Debug.LogError("error, skillType = " + skill.Type);
				return null;
			}
			if (role.HasTalent("浪子剑客") && skill.Type == 1)
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "无招胜有招!", "剑随心动" }, 0.1f);
			}
			if (role.HasTalent("神拳无敌") && skill.Type == 0)
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "一双铁拳打天下!", "看谁的拳头更硬！" }, 0.1f);
			}
			InternalSkillInstance equippedInternalSkill = role.GetEquippedInternalSkill();
			InternalSkillInstance equippedInternalSkill2 = role2.GetEquippedInternalSkill();
			double num6 = (skill.Tiaohe ? ((float)Math.Max(equippedInternalSkill.Yin, equippedInternalSkill.Yang) / 100f) : ((skill.Suit > 0f) ? (skill.Suit * (float)equippedInternalSkill.Yang / 100f) : ((!(0f + skill.Suit < 0f)) ? 0f : ((0f - skill.Suit) * (float)equippedInternalSkill.Yin / 100f))));
			double num7 = 1.0;
			double num8 = 1.0;
			if (RuntimeData.Instance.gameEngine.battleType != BattleType.Zhenlongqiju)
			{
				num7 += CommonSettings.ZHOUMU_ATTACK_ADD * (double)(RuntimeData.Instance.Round - 1);
				num8 += CommonSettings.ZHOUMU_DEFENCE_ADD * (double)(RuntimeData.Instance.Round - 1);
			}
			double attackLow = (double)skill.Power * (2.0 + (double)num5 / 200.0) * 2.5 * (4.0 + (double)role.AttributesFinal["bili"] / 120.0) * (1.0 + num6);
			double attackHigh = (double)skill.Power * (2.0 + (double)num5 / 200.0) * 2.5 * (4.0 + (double)role.AttributesFinal["bili"] / 120.0) * (1.0 + num6) * (double)(1f + equippedInternalSkill.Attack);
			double critical_chacne = (double)role.AttributesFinal["fuyuan"] / 50.0 / 20.0 * (double)(1f + equippedInternalSkill.Critical) * (1.0 + num6);
			critical_chacne += (double)role.CriticalProbabilityAdd();
			double defence = 150.0 + (10.0 + (double)role2.AttributesFinal["dingli"] / 40.0 + (double)role2.AttributesFinal["gengu"] / 70.0) * 8.0 * (double)(1f + equippedInternalSkill2.Defence);
			if (sourceSprite.Team == 2)
			{
				attackLow *= num7;
				attackHigh *= num7;
			}
			if (targetSprite.Team == 2)
			{
				defence *= num8;
			}
			if (role.HasTalent("异世人"))
			{
				double num13 = 0.2;
				if (role.HasTalent("草头百姓"))
				{
					num13 = 0.5;
				}
				if ((double)((float)role.Attributes["hp"] / (float)role.Attributes["maxhp"]) <= num13)
				{
					attackResult.AddCastInfo(sourceSprite, new string[2] { "来自异世的威力！", "天外飞仙！" }, 0.2f);
					attackLow *= 2.0;
					attackHigh *= 2.0;
					critical_chacne *= 2.0;
				}
			}
			if (role2.HasTalent("异世人"))
			{
				double num14 = 0.2;
				if (role.HasTalent("草头百姓"))
				{
					num14 = 0.5;
				}
				if ((double)((float)role2.Attributes["hp"] / (float)role2.Attributes["maxhp"]) <= num14)
				{
					attackResult.AddCastInfo(targetSprite, new string[2] { "绝不会倒下！ ", "固若金汤！" }, 0.2f);
					defence *= 1.5;
				}
			}
			if (role.HasTalent("夫妻同心"))
			{
				foreach (BattleSprite sprite in field.Sprites)
				{
					if (sprite.Role != role && sprite.Role.Female != role.Female && sprite.Role.HasTalent("夫妻同心") && sprite.Team == role.Sprite.Team)
					{
						if (Tools.ProbabilityTest(0.3))
						{
							attackResult.AddCastInfo(sourceSprite, "夫妻同心！");
							attackResult.AddCastInfo(sprite, "夫妻同心！");
						}
						attackLow *= Tools.GetRandom(1.2, 2.0);
						attackHigh *= Tools.GetRandom(1.2, 2.0);
						break;
					}
				}
			}
			if (role2.HasTalent("夫妻同心"))
			{
				foreach (BattleSprite sprite2 in field.Sprites)
				{
					if (sprite2.Role != role2 && sprite2.Role.Female != role2.Female && sprite2.Role.HasTalent("夫妻同心") && sprite2.Team == role2.Sprite.Team)
					{
						if (Tools.ProbabilityTest(0.3))
						{
							attackResult.AddCastInfo(sourceSprite, "夫妻同心！");
							attackResult.AddCastInfo(sprite2, "夫妻同心！");
						}
						defence *= Tools.GetRandom(1.2, 2.0);
						break;
					}
				}
			}
			if (role2.HasTalent("金刚"))
			{
				attackResult.AddCastInfo(targetSprite, new string[2] { "金刚不坏很耐打！", "壮哉我！抗住啊！" }, 0.1f);
				defence *= 1.2;
				defence += (double)(10 * role2.Level);
			}
			if (role.HasTalent("混元一气") && skill.Name.Contains("混元掌"))
			{
				critical_chacne += 0.25;
				attackResult.AddCastInfo(sourceSprite, new string[3] { "混元一气！", "引气归田", "抱元归一" }, 0.1f);
			}
			if (role2.HasTalent("混元一气") && role2.GetEquippedInternalSkill().Name.Equals("混元功"))
			{
				defence *= 1.5;
				attackResult.AddCastInfo(targetSprite, new string[3] { "混元一气！", "引气归田", "抱元归一" }, 0.1f);
			}
			if (role.HasTalent("奋战") && !role.HasTalent("异世人") && (double)((float)role.Attributes["hp"] / (float)role.Attributes["maxhp"]) <= 0.3)
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "杀杀杀！", "跟我来！" }, 0.2f);
				attackLow *= 1.5;
				attackHigh *= 1.5;
				critical_chacne *= 1.5;
			}
			if (role.HasTalent("不稳定的六脉神剑") && skill.Name.Contains("六脉神剑"))
			{
				attackResult.AddCastInfo(sourceSprite, new string[3] { "还是不能随心所欲施展…… ", "六脉神剑，给我挣点气呀", "啊呀，对不起！" }, 0.1f);
				attackLow *= 0.5;
				if (attackLow < 0.0)
				{
					attackLow = 0.0;
				}
				attackHigh *= 1.5;
			}
			if (role.HasTalent("好色") && role2.Female)
			{
				attackResult.AddCastInfo(sourceSprite, new string[3] { "花姑娘，大大的！", "哟西，花姑娘 ", "美女，我所欲也" }, 0.15f);
				attackLow *= 1.2;
				attackHigh *= 1.2;
			}
			if (role.Female && role2.HasTalent("好色"))
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "色狼，受死吧！", "讨厌！" }, 0.15f);
				defence *= 0.8;
			}
			if (role.HasTalent("神雕大侠") && (skill.Name == "玄铁剑法" || skill.Name == "黯然销魂掌"))
			{
				if (skill.Name == "黯然销魂掌")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "黯然销魂，唯别而已。" });
				}
				if (skill.Name == "玄铁剑法")
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "重剑无锋，大巧不工。" });
				}
				critical_chacne += 0.25;
				attackLow *= 1.3;
				attackHigh *= 1.3;
			}
			if (role.HasTalent("雪山飞狐") && skill.Name.Contains("胡家刀法"))
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "雪山飞狐！", "飞天狐狸！" });
				critical_chacne += 0.5;
			}
			if (role.HasTalent("阴谋家"))
			{
				double num15 = (double)role2.Attributes["hp"] / (double)role2.Attributes["maxhp"];
				attackLow *= 1.0 + 0.5 * (1.0 - num15);
				attackHigh *= 1.0 + 0.5 * (1.0 - num15);
			}
			if (role.HasTalent("孤独求败") && Tools.ProbabilityTest(0.3))
			{
				attackResult.AddCastInfo(sourceSprite, new string[3] { "洞悉一切弱点", "你不是我的对手", "我，站在天下武学之巅" }, 0.1f);
				defence *= 0.3;
				critical_chacne += 0.25;
			}
			if (role.HasTalent("太极高手") && skill.Name.Contains("太极"))
			{
				attackResult.AddCastInfo(sourceSprite, new string[4] { "以柔克刚！", "左右野马分鬃", "白鹤晾翅", "左揽雀尾" }, 0.1f);
				critical_chacne += 0.25;
			}
			if (role.HasTalent("太极宗师") && skill.Name.Contains("太极"))
			{
				attackResult.AddCastInfo(sourceSprite, new string[6] { "意体相随！", "四两拨千斤！", "以柔克刚！", "左右野马分鬃", "白鹤晾翅", "左揽雀尾" }, 0.1f);
				attackLow *= 1.2;
				attackHigh *= 1.2;
				critical_chacne += 0.15;
			}
			if (role2.HasTalent("太极宗师") && role2.GetEquippedInternalSkill().Name.Contains("太极"))
			{
				attackResult.AddCastInfo(targetSprite, new string[2] { "意体相随！", "四两拨千斤！" }, 0.15f);
				defence *= 1.2;
			}
			if (role2.HasTalent("太极宗师") && role2.GetEquippedInternalSkill().Name.Contains("纯阳无极功"))
			{
				attackResult.AddCastInfo(targetSprite, new string[2] { "我几十年的童子身不是白守的！", "纯阳无极功" }, 0.2f);
				defence *= 1.2;
			}
			if (role2.HasTalent("臭蛤蟆") && role2.GetEquippedInternalSkill().Name.Contains("蛤蟆功"))
			{
				attackResult.AddCastInfo(targetSprite, new string[1] { "呱！！！尝尝我蛤蟆功的厉害。" }, 0.1f);
				defence *= 1.3;
			}
			if (role.HasTalent("臭蛤蟆"))
			{
				if (skill.Name.Contains("蛤蟆功"))
				{
					attackResult.AddCastInfo(sourceSprite, new string[2] { "让你们见识见识蛤蟆功的威力！", "呱！！！尝尝我蛤蟆功的厉害。" }, 0.1f);
					attackLow += 400.0;
					attackHigh += 400.0;
				}
				else if (role.GetEquippedInternalSkill().Name.Contains("蛤蟆功"))
				{
					attackResult.AddCastInfo(sourceSprite, new string[1] { "呱！！" }, 0.1f);
					attackLow += 250.0;
					attackHigh += 250.0;
				}
			}
			if (role.HasTalent("猎人") && role2.Animal)
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "颤抖吧，猎物们！", "我，是打猎的能手！" }, 0.1f);
				attackLow *= 1.5;
				attackHigh *= 1.5;
			}
			if (role2.HasTalent("金钟罩") && Tools.ProbabilityTest(0.25))
			{
				attackResult.AddCastInfo(targetSprite, new string[2] { "我扛！", "切换防御姿态！" }, 0.1f);
				defence *= 2.0;
			}
			if (role.HasTalent("阉人") && (skill.Name.Contains("葵花宝典") || skill.Name.Contains("辟邪剑法")))
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "你以为我JJ是白切的？", "嘿嘿嘿嘿……" }, 0.1f);
				critical_chacne = 1.0;
			}
			if (role.HasTalent("怒不可遏"))
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "老子要发飙啦！", "怒火，将会焚烧一切！" }, 0.1f);
				int balls = sourceSprite.Balls;
				attackLow *= 1.0 + (double)balls * 0.1;
				attackHigh *= 1.0 + (double)balls * 0.1;
			}
			if (role2.HasTalent("暴躁"))
			{
				critical_chacne += 0.1;
			}
			if (role.HasTalent("精打细算") && Tools.ProbabilityTest(0.25))
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "你漫天要价,我落地还钱", "九出十三归!" }, 0.1f);
				critical_chacne *= Tools.GetRandom(1.0, 2.0);
				attackLow *= Tools.GetRandom(1.0, 1.5);
				attackHigh *= Tools.GetRandom(1.0, 1.5);
			}
			if (role.HasTalent("精明"))
			{
				if (Tools.ProbabilityTest(0.25))
				{
					attackResult.AddCastInfo(sourceSprite, new string[2] { "想要骗我不容易", "说好的倍伤呢?" }, 0.1f);
				}
				critical_chacne *= Tools.GetRandom(1.0, 1.5);
				attackLow *= Tools.GetRandom(1.0, 1.3);
				attackHigh *= Tools.GetRandom(1.0, 1.3);
			}
			double num16 = role.AttributesFinal["fuyuan"] / 100 - role2.AttributesFinal["dingli"] / 100;
			if (num16 < 0.0)
			{
				num16 = 0.0;
			}
			double num17 = 0.25;
			if (role.HasTalent("神拳无敌"))
			{
				num17 += 0.12;
			}
			if (role.HasTalent("铁拳无双") && Tools.ProbabilityTest(num17) && skill.Type == 0)
			{
				Buff buff = new Buff();
				buff.Name = "晕眩";
				buff.Level = 0;
				buff.Round = 2;
				attackResult.Debuff.Add(buff);
				attackResult.AddCastInfo(sourceSprite, new string[2] { "尝尝我的拳头的滋味！", "拳头硬才是硬道理！" }, 0.1f);
			}
			if (role.HasTalent("追魂") && Tools.ProbabilityTest(num16))
			{
				attackResult.AddCastInfo(sourceSprite, new string[1] { "夺命追魂！" }, 0.1f);
				BuffInstance buffInstance = null;
				foreach (BuffInstance buff16 in targetSprite.Buffs)
				{
					if (buff16.buff.Name == "伤害加深")
					{
						buffInstance = buff16;
						break;
					}
				}
				if (buffInstance == null)
				{
					Buff buff2 = new Buff();
					buff2.Name = "伤害加深";
					buff2.Level = 1;
					buff2.Round = 4;
					attackResult.Debuff.Add(buff2);
				}
				else
				{
					Buff buff3 = new Buff();
					buff3.Name = "伤害加深";
					buff3.Level = ((buffInstance.Level + 1 > 10) ? 10 : (buffInstance.Level + 1));
					buff3.Round = 4;
					attackResult.Debuff.Add(buff3);
				}
			}
			if (role.HasTalent("诸般封印") && Tools.GetRandom(0.0, 1.0) <= num16)
			{
				Buff buff4 = new Buff();
				buff4.Name = "诸般封印";
				buff4.Level = 0;
				buff4.Round = 2;
				attackResult.Debuff.Add(buff4);
			}
			if (role.HasTalent("剑封印") && Tools.ProbabilityTest(num16))
			{
				Buff buff5 = new Buff();
				buff5.Name = "剑封印";
				buff5.Level = 0;
				buff5.Round = 2;
				attackResult.Debuff.Add(buff5);
			}
			if (role.HasTalent("刀封印") && Tools.ProbabilityTest(num16))
			{
				Buff buff6 = new Buff();
				buff6.Name = "刀封印";
				buff6.Level = 0;
				buff6.Round = 2;
				attackResult.Debuff.Add(buff6);
			}
			if (role.HasTalent("拳掌封印") && Tools.ProbabilityTest(num16))
			{
				Buff buff7 = new Buff();
				buff7.Name = "拳掌封印";
				buff7.Level = 0;
				buff7.Round = 2;
				attackResult.Debuff.Add(buff7);
			}
			if (role.HasTalent("奇门封印") && Tools.ProbabilityTest(num16))
			{
				Buff buff8 = new Buff();
				buff8.Name = "奇门封印";
				buff8.Level = 0;
				buff8.Round = 2;
				attackResult.Debuff.Add(buff8);
			}
			if (role.HasTalent("阴阳") && Tools.ProbabilityTest((double)role.Level * 0.01))
			{
				Buff buff9 = new Buff();
				buff9.Name = "麻痹";
				buff9.Level = 0;
				buff9.Round = 2;
				attackResult.Debuff.Add(buff9);
			}
			if (role.HasTalent("寒冰真气") && Tools.ProbabilityTest((double)role.Level * 0.5))
			{
				Buff buff10 = new Buff();
				buff10.Name = "麻痹";
				buff10.Level = 3;
				buff10.Round = 2;
				attackResult.Debuff.Add(buff10);
			}
			if (role.HasTalent("大小姐") || role.HasTalent("自我主义"))
			{
				float num18 = 0.1f;
				if (role.HasTalent("自我主义"))
				{
					num18 = 0.18f;
				}
				int num19 = 0;
				foreach (BattleSprite sprite3 in bf.Sprites)
				{
					if (sprite3.Team == sourceSprite.Team)
					{
						num19++;
					}
				}
				if (num19 > 10)
				{
					num19 = 10;
				}
				attackLow *= (double)(1f + num18 * (float)num19);
				attackHigh *= (double)(1f + num18 * (float)num19);
				if (role.HasTalent("大小姐"))
				{
					attackResult.AddCastInfo(sourceSprite, new string[3] { "哼！", "你们，不准欺负我！", "谁让你们欺负我的！" }, 0.1f);
				}
				else if (role.HasTalent("自我主义"))
				{
					attackResult.AddCastInfo(sourceSprite, new string[2] { "老子才不管你们的死活！", "哼！唯我独尊。" }, 0.1f);
				}
			}
			foreach (BattleSprite sprite4 in bf.Sprites)
			{
				if ((sprite4.Role.HasTalent("大小姐") || sprite4.Role.HasTalent("自我主义")) && sprite4 != sourceSprite && sprite4.Team == sourceSprite.Team)
				{
					attackHigh *= 0.9;
					attackLow *= 0.9;
				}
			}
			if (role.HasTalent("破甲") && Tools.ProbabilityTest(0.3))
			{
				defence *= 0.9;
				defence -= 70.0;
				if (defence < 0.0)
				{
					defence = 0.0;
				}
				attackResult.AddCastInfo(sourceSprite, new string[2] { "看我致命一击！", "无视护甲！" }, 0.3f);
			}
			if (role.HasTalent("芷若的眷念"))
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "芷若，看我的！", "芷若，我永不会忘记汉江之遇。" }, 0.2f);
				attackLow *= 1.1;
				attackHigh *= 1.1;
			}
			if (role.HasTalent("臭叫花") && skill.Name.Contains("打狗棒法"))
			{
				attackHigh *= 1.2;
				attackLow *= 1.2;
				critical_chacne += 0.2;
				attackResult.AddCastInfo(sourceSprite, new string[2] { "叫花子人穷志不穷！", "这年头叫花子也不好当啊。" }, 0.2f);
			}
			if (role.HasTalent("金蛇郎君") && skill.Name.Contains("金蛇剑法"))
			{
				critical_chacne += 0.5;
				attackResult.AddCastInfo(sourceSprite, new string[2] { "金蛇郎君的意志!", "看我的金蛇剑法" }, 0.1f);
			}
			if (role.HasTalent("金蛇狂舞") && skill.Name.Contains("金蛇剑法"))
			{
				attackHigh *= 1.4;
				attackLow *= 1.4;
				attackResult.AddCastInfo(sourceSprite, new string[1] { "金蛇狂舞!" }, 0.1f);
			}
			if (role.HasTalent("铁骨墨萼") && skill.Name.Contains("连城剑法"))
			{
				attackHigh *= 1.4;
				attackLow *= 1.4;
				attackResult.AddCastInfo(sourceSprite, new string[4] { "天花落不尽，处处鸟衔飞", "孤鸿海上来，池潢不敢顾", "俯听闻惊风，连山若波涛", "落日照大旗，马鸣风萧萧" }, 0.2f);
				critical_chacne += 0.2;
			}
			if (role2.HasTalent("御风"))
			{
				critical_chacne -= 0.3 * (double)(role2.Level / 30);
				attackResult.AddCastInfo(targetSprite, new string[2] { "髣髴兮若轻云之蔽月", "飘飖兮若流风之回雪" }, 0.1f);
			}
			if (role.HasTalent("俗家弟子") && Tools.ProbabilityTest(0.3))
			{
				double num20 = 0.8 - 0.005 * (double)role.Level;
				defence *= num20;
				attackResult.AddCastInfo(sourceSprite, new string[3] { "少林美名天下传", "内练一口气，外练筋骨皮", "看我少林俗家弟子的厉害" }, 0.1f);
			}
			if (role.HasTalent("北冥真气") && role.GetEquippedInternalSkill().Name == "北冥神功")
			{
				attackLow *= 1.8;
				attackHigh *= 1.8;
			}
			if (role2.HasTalent("北冥真气") && role2.GetEquippedInternalSkill().Name == "北冥神功")
			{
				defence *= 2.0;
			}
			if (role.HasTalent("老江湖"))
			{
				attackHigh *= 0.9;
				attackLow *= 1.2;
				if (attackLow > attackHigh)
				{
					double num21 = attackLow;
					attackLow = attackHigh;
					attackHigh = num21;
				}
			}
			if (role.HasTalent("左手剑"))
			{
				attackHigh *= 1.05;
				attackLow *= 1.05;
				critical_chacne -= 0.02;
			}
			if (role.HasTalent("右臂有伤"))
			{
				attackHigh *= 0.95;
				attackLow *= 0.95;
			}
			if (role.HasTalent("神兵"))
			{
				attackHigh *= 1.05;
				attackLow *= 1.05;
			}
			if (role.HasTalent("神经病"))
			{
				attackHigh *= 1.1;
			}
			if (role.HasTalent("鲁莽"))
			{
				attackHigh *= 1.06;
				attackLow *= 1.06;
			}
			if (role2.HasTalent("苦命儿"))
			{
				defence += 30.0;
			}
			if (role.HasTalent("阴毒") && role2.Sprite.HasBuff("中毒"))
			{
				attackLow *= 1.0 + 0.2 * ((double)role.level / 30.0);
				attackHigh *= 1.0 + 0.2 * ((double)role.level / 30.0);
			}
			if (role.HasTalent("狗杂种") && Tools.ProbabilityTest(0.3) && (role.GetEquippedInternalSkill().Name == "太玄神功" || role.GetEquippedInternalSkill().Name == "罗汉伏魔功"))
			{
				critical_chacne += 0.3;
				attackResult.AddCastInfo(sourceSprite, new string[3] { "阿黄叫你回家吃饭啦！", "不许叫我狗杂种！", "狗杂种也能逆袭啊❤" }, 0.2f);
			}
			if (role2.HasTalent("狗杂种") && (role2.GetEquippedInternalSkill().Name == "太玄神功" || role2.GetEquippedInternalSkill().Name == "罗汉伏魔功"))
			{
				attackResult.AddCastInfo(targetSprite, new string[2] { "不许叫我狗杂种！", "狗杂种也能逆袭啊❤" }, 0.2f);
				defence += 250.0;
			}
			attackFormula.attackLow = attackLow;
			attackFormula.attackUp = attackHigh;
			attackFormula.criticalHit = critical_chacne;
			attackFormula.defence = defence;
			LuaManager.Call("AttackLogic_extendTalents3", sourceSprite, targetSprite, skill, bf, attackResult, attackFormula);
			attackLow = attackFormula.attackLow;
			attackHigh = attackFormula.attackUp;
			critical_chacne = attackFormula.criticalHit;
			defence = attackFormula.defence;
			foreach (Buff buff17 in skill.Buffs)
			{
				if (buff17.IsDebuff)
				{
					double num22 = 0.0;
					if (buff17.Property == -1)
					{
						num22 = (double)role.AttributesFinal["fuyuan"] / 100.0 - 0.5 * ((double)role2.AttributesFinal["dingli"] / 100.0);
						if (num22 < 0.1)
						{
							num22 = 0.1;
						}
					}
					else
					{
						num22 = buff17.Property / 100;
					}
					if (Tools.ProbabilityTest(num22))
					{
						attackResult.Debuff.Add(buff17);
					}
				}
				else
				{
					double num23 = 0.0;
					num23 = ((buff17.Property != -1) ? ((double)(buff17.Property / 100)) : ((double)(role.AttributesFinal["fuyuan"] / 300)));
					if (Tools.ProbabilityTest(num23))
					{
						attackResult.Buff.Add(buff17);
					}
				}
			}
			double num24 = 1.5;
			foreach (Trigger allTrigger in role.GetAllTriggers())
			{
				switch (allTrigger.Name)
				{
				case "attack":
					attackLow += double.Parse(allTrigger.Argvs[0]) / 2.0;
					attackHigh += double.Parse(allTrigger.Argvs[0]);
					break;
				case "powerup_quanzhang":
					if (skill.Type == 0)
					{
						double num27 = 1.0 + (double)int.Parse(allTrigger.Argvs[0]) / 100.0;
						attackLow *= num27;
						attackHigh *= num27;
					}
					break;
				case "powerup_jianfa":
					if (skill.Type == 1)
					{
						double num28 = 1.0 + (double)int.Parse(allTrigger.Argvs[0]) / 100.0;
						attackLow *= num28;
						attackHigh *= num28;
					}
					break;
				case "powerup_daofa":
					if (skill.Type == 2)
					{
						double num26 = 1.0 + (double)int.Parse(allTrigger.Argvs[0]) / 100.0;
						attackLow *= num26;
						attackHigh *= num26;
					}
					break;
				case "powerup_qimen":
					if (skill.Type == 3)
					{
						double num25 = 1.0 + (double)int.Parse(allTrigger.Argvs[0]) / 100.0;
						attackLow *= num25;
						attackHigh *= num25;
					}
					break;
				case "critical":
					num24 += (double)int.Parse(allTrigger.Argvs[0]) / 100.0;
					break;
				}
			}
			foreach (ItemInstance item in role.Equipment)
			{
				bool flag = false;
				if (item.type == 1)
				{
					if (role.HasTalent("拳系装备") && skill.Type != 0)
					{
						flag = true;
					}
					if (role.HasTalent("剑系装备") && skill.Type != 1)
					{
						flag = true;
					}
					if (role.HasTalent("刀系装备") && skill.Type != 2)
					{
						flag = true;
					}
					if (role.HasTalent("奇门装备") && skill.Type != 3)
					{
						flag = true;
					}
					if (skill.Type == 4)
					{
						flag = false;
					}
					if (flag)
					{
						attackHigh *= 0.9;
						attackLow *= 0.9;
					}
					break;
				}
			}
			foreach (Trigger allTrigger2 in role2.GetAllTriggers())
			{
				switch (allTrigger2.Name)
				{
				case "defence":
					defence += double.Parse(allTrigger2.Argvs[0]);
					critical_chacne -= double.Parse(allTrigger2.Argvs[1]) / 100.0;
					if (critical_chacne < 0.0)
					{
						critical_chacne = 0.0;
					}
					break;
				}
			}
			BuffInstance buff11 = sourceSprite.GetBuff("攻击强化");
			BuffInstance buff12 = sourceSprite.GetBuff("攻击弱化");
			if (buff11 != null)
			{
				attackLow *= 1.0 + (double)buff11.Level / 10.0;
				attackHigh *= 1.0 + (double)buff11.Level / 10.0;
			}
			if (buff12 != null)
			{
				attackLow *= 1.0 - (double)buff12.Level / 10.0;
				attackHigh *= 1.0 - (double)buff12.Level / 10.0;
			}
			BuffInstance buff13 = targetSprite.GetBuff("伤害加深");
			if (buff13 != null)
			{
				attackLow *= Math.Pow(1.15, buff13.Level);
				attackHigh *= Math.Pow(1.15, buff13.Level);
			}
			if (RuntimeData.Instance.GameMode == "normal" && sourceSprite.Team == 1)
			{
				attackLow *= 2.0;
				attackHigh *= 2.0;
			}
			if (RuntimeData.Instance.GameMode == "normal" && sourceSprite.Team != 1)
			{
				attackLow *= 0.5;
				attackHigh *= 0.5;
			}
			BuffInstance buff14 = targetSprite.GetBuff("防御强化");
			if (buff14 != null)
			{
				defence += (double)((buff14.Level + 1) * 20);
			}
			double num29 = defenceDescAttack(defence);
			bool flag2 = Tools.ProbabilityTest(critical_chacne);
			if (sourceSprite.GetBuff("魔神降临") != null)
			{
				flag2 = true;
				num24 += 0.25;
				attackResult.AddCastInfo(sourceSprite, new string[2] { "尝尝魔神的威力！", "嘿嘿嘿嘿嘿！" }, 0.2f);
			}
			if (skill.Name == "一刀两断")
			{
				flag2 = false;
			}
			attackResult.Hp = (int)(Tools.GetRandom(attackLow, attackHigh) * ((!flag2) ? 1.0 : num24) * (1.0 - num29));
			if (role2.HasTalent("乾坤大挪移奥义"))
			{
				attackResult.Hp = (int)((double)attackResult.Hp * 0.5);
				attackResult.AddCastInfo(targetSprite, new string[3] { "铜墙铁壁！（乾坤大挪移奥义发动）", "乾坤大挪移奥义式！", "打不疼我（乾坤大挪移奥义发动）" }, 0.15f);
			}
			else if (role2.HasTalent("乾坤大挪移") && Tools.ProbabilityTest(0.5))
			{
				attackResult.Hp = (int)((double)attackResult.Hp * 0.5);
				attackResult.AddCastInfo(targetSprite, new string[3] { "我挪！", "乾坤大挪移！", "打不疼我" }, 0.15f);
			}
			if (role2.HasTalent("精打细算") && Tools.ProbabilityTest(0.2))
			{
				if (attackResult.Hp > 200)
				{
					attackResult.Hp = 200;
				}
				attackResult.AddCastInfo(targetSprite, new string[1] { "嘿嘿，你可伤不到我。" }, 0.3f);
			}
			else if (role2.HasTalent("精明") && Tools.ProbabilityTest(0.25))
			{
				if (attackResult.Hp > 500)
				{
					attackResult.Mp += attackResult.Hp - 500;
					attackResult.Hp = 500;
				}
				attackResult.AddCastInfo(targetSprite, new string[1] { "这一招太狠了，我得躲开一点。" }, 0.3f);
			}
			if (targetSprite.Team != sourceSprite.Team && targetSprite.GetBuff("溜须拍马") != null)
			{
				attackResult.Hp = 0;
				attackResult.Mp = 0;
				attackResult.Buff.Clear();
				attackResult.AddCastInfo(targetSprite, new string[1] { "好汉饶命啊！ (溜须拍马生效)" });
			}
			if (targetSprite.Team != sourceSprite.Team && targetSprite.GetBuff("易容") != null)
			{
				attackResult.Hp = 0;
				attackResult.Mp = 0;
				attackResult.Buff.Clear();
				attackResult.AddCastInfo(targetSprite, new string[1] { "改面易容，伺机而动！ (易容生效)" });
			}
			if (role.HasTalent("黑天死炎") && Tools.ProbabilityTest(0.25))
			{
				attackResult.Hp = Math.Max(attackResult.Hp, (int)((float)targetSprite.Hp * 0.25f));
				attackResult.AddCastInfo(sourceSprite, new string[1] { "黑暗！（天赋*黑天死炎发动）" });
			}
			if (role2.HasTalent("宝甲"))
			{
				attackResult.Hp = (int)((double)attackResult.Hp * 0.95);
			}
			if (role2.HasTalent("神经病"))
			{
				attackResult.Hp = (int)((double)attackResult.Hp * 1.1);
			}
			if (role2.HasTalent("鲁莽"))
			{
				attackResult.Hp = (int)((double)attackResult.Hp * 1.1);
			}
			if (role.HasTalent("攀云乘龙") && Tools.ProbabilityTest(0.5))
			{
				attackResult.AddCastInfo(sourceSprite, new string[2] { "我从月下来，偷走你最爱……", "攀云乘龙，神行百变，千变万劫！" }, 0.1f);
				int num30 = sourceSprite.Role.AttributesFinal["shenfa"];
				attackResult.Hp += Tools.GetRandomInt(num30, num30 * 2);
			}
			attackResult.Hp = ((attackResult.Hp > 0) ? attackResult.Hp : 0);
			attackResult.Critical = flag2;
			if (sourceSprite.Team == targetSprite.Team)
			{
				bool flag3 = false;
				if (skill.IsAoyi)
				{
					flag3 = true;
				}
				if (sourceSprite.Role.HasTalent("灵心慧质"))
				{
					flag3 = true;
				}
				if (flag3)
				{
					attackResult.Hp = 0;
					attackResult.Debuff.Clear();
					attackResult.Mp = 0;
				}
				else if (attackResult.Hp > 0)
				{
					attackResult.Hp /= 4;
				}
			}
			LuaManager.Call("AttackLogic_extendTalents", sourceSprite, targetSprite, skill, bf, attackResult);
			if (attackResult.Hp > 0)
			{
				double num31 = (double)attackResult.Hp / (double)role2.Attributes["maxhp"];
				if (num31 > 1.0)
				{
					num31 = 1.0;
				}
				attackResult.Hp = (int)((double)attackResult.Hp * (1.0 - 0.4 * num31));
			}
			return attackResult;
		}

		public static double defenceDescAttack(double defence)
		{
			return 0.9 - Math.Pow(0.9, 0.02 * (defence + 50.0));
		}
	}
}
