# 剧情指令参考

本文面向内容作者和调试者，说明剧情 JSON 的 `command` step 与游戏内系统面板控制台可执行的当前注册指令。

## 控制台语法

控制台输入格式：

```text
command arg1 arg2 ...
```

- 参数用空白分隔；包含空白的文本用双引号包住，例如 `log "踏入江湖"`。
- 参数会按顺序自动解析为 `bool`、数字或字符串；整数参数不接受小数。
- 字符串参数可以直接写中文内容或内容 id，例如 `item 道口烧鸡 1`。
- 当前控制台不支持 `jump`。
- 剧情 JSON 与控制台共用同一套指令绑定；JSON 中参数来自 `args` 数组。

剧情 JSON 示例：

```json
{
  "kind": "command",
  "name": "item",
  "args": ["道口烧鸡", 1]
}
```

## 执行模型

- 指令先由 `StoryCommandDispatcher` 处理应用层逻辑。
- 应用层未注册的指令会转发到 Godot 宿主层 `GodotStoryRuntimeHost`。
- 应用层指令主要修改可存档状态，例如背包、货币、时间、剧情状态、队伍和角色。
- Godot 宿主层指令主要驱动地图、商店、音频、画面表现、开局 UI 和流程跳转。
- 指令失败会抛出错误；系统面板控制台会显示错误信息。

## 参数枚举

常用固定参数：

- `set_game_mode mode`: `normal`、`hard`、`crazy`。
- `toast mode`: `on` 显示 toast，`off` 抑制 toast。
- `learn type`: `skill`、`external`、`internal`、`special`、`talent`；`skill` 会按外功、内功、特技顺序尝试匹配。
- `upgrade target`: 属性名，或 `skill`、`external`、`internal`；`skill` 会按外功、内功顺序尝试匹配。
- `remove type`: `skill`、`external`、`internal`、`special`、`talent`；`skill` 会按外功、内功、特技顺序尝试匹配。
- 属性名支持中文显示名和 JSON code，包括：
  `拳掌/quanzhang`、`剑法/jianfa`、`刀法/daofa`、`奇门/qimen`、`臂力/bili`、`身法/shenfa`、`悟性/wuxing`、`福缘/fuyuan`、`根骨/gengu`、`定力/dingli`、`武学点/wuxue`、`气血上限/max_hp/maxhp`、`内力上限/max_mp/maxmp`、`攻击力/attack`、`防御力/defence`、`闪避率/evasion`、`命中率/accuracy`、`暴击率/crit_chance`、`暴击伤害/crit_mult`、`抗暴率/anti_crit_chance`、`吸血/lifesteal`、`抗异常/anti_debuff`、`集气速度/speed`、`移动力/movement`。

## 指令参考

### 物品与货币

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `item` | `itemId [quantity=1]` | 向背包加入物品。 | `item 道口烧鸡 2` |
| `cost_item` | `itemId [quantity=1]` | 从背包扣除物品；数量不足会失败。 | `cost_item 道口烧鸡 1` |
| `get_money` | `amount` | 增加银两。 | `get_money 1000` |
| `cost_money` | `amount` | 扣除银两；余额不足会失败。 | `cost_money 500` |
| `yuanbao` | `amount` | 调整元宝；负数表示扣除，余额不足会失败。 | `yuanbao 10` |

### 装备洗练

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `xilian` | `...` | 打开洗练流程：选择背包中带附加词条的装备，选择旧词条，消耗 1 元宝后从 8 个新候选中选择替换；无可洗装备会跳转到 `洗练_没有装备`。 | `xilian 0` |

### 时间与周目状态

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `cost_day` | `days` | 推进世界日期。 | `cost_day 3` |
| `set_round` | `round` | 设置当前周目数。 | `set_round 2` |
| `set_game_mode` | `mode` | 设置难度模式。 | `set_game_mode hard` |

### 剧情状态与日志

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `log` | `text` | 追加江湖日志，并记录当前时间快照。 | `log "踏入江湖"` |
| `set_flag` | `variableName` | 设置剧情布尔变量为 `true`。 | `set_flag 初遇女主` |
| `clear_flag` | `variableName` | 移除剧情变量。 | `clear_flag 初遇女主` |
| `set_time_key` | `key limitDays targetStoryId` | 创建限时剧情 key，到期后触发目标剧情。 | `set_time_key 夜探 3 original_夜探失败` |
| `clear_time_key` | `key` | 移除限时剧情 key。 | `clear_time_key 夜探` |
| `world_trigger` | `on/off` | 开启或阻塞世界触发入口。 | `world_trigger off` |

### 江湖属性

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `daode` | `delta` | 调整道德。 | `daode 5` |
| `haogan` | `delta` | 调整好感。 | `haogan -3` |
| `menpai` | `sectId` | 设置门派 id。 | `menpai 星宿派` |
| `rank` | `...` | 当前注册为空操作，用于兼容已有剧情。 | `rank 1` |
| `touch` | `...` | 当前注册为空操作，用于兼容已有剧情。 | `touch foo` |

### 角色成长

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `upgrade` | `stat characterId value` | 增加角色基础属性。 | `upgrade 拳掌 主角 10` |
| `upgrade` | `skill characterId skillId levels` | 自动匹配并提升外功或内功；外功优先。 | `upgrade skill 主角 野球拳 1` |
| `upgrade` | `external characterId skillId levels` | 提升外功等级。 | `upgrade external 主角 野球拳 1` |
| `upgrade` | `internal characterId skillId levels` | 提升内功等级。 | `upgrade internal 主角 基本内功 1` |
| `minus_maxpoints` | `characterId tenths` | 按十分比缩放气血、内力和十维属性；`tenths` 范围为 `0..10`。 | `minus_maxpoints 主角 5` |
| `growtemplate` | `characterId growTemplateId` | 设置角色成长模板 id。 | `growtemplate 主角 default` |
| `grant_point` / `get_point` | `characterId value` | 增加自由属性点。 | `grant_point 主角 3` |
| `grant_exp` / `get_exp` | `characterId value` | 增加经验，并按经验结算升级。 | `grant_exp 主角 500` |
| `levelup` | `characterId [levels=1]` | 直接提升角色等级。 | `levelup 主角 2` |

### 队伍与角色外观

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `change_female_name` | `name [characterId=女主]` | 改名；目标不存在时创建到后备池。 | `change_female_name "阿九"` |
| `join` | `characterId` | 角色加入当前队伍；已有实例会从其他池移动。 | `join 阿青` |
| `follow` | `characterId` | 角色加入随队池。 | `follow 女主` |
| `leave` | `characterIdOrName` | 当前队伍角色离队并进入后备池。 | `leave 阿青` |
| `leave_follow` | `characterIdOrName` | 随队角色离队并进入后备池。 | `leave_follow 女主` |
| `leave_all` | 无 | 当前队伍成员全部离队并进入后备池。 | `leave_all` |
| `head` | `portraitId` | 设置主角头像。 | `head hero_01` |
| `animation` | `characterId modelId` | 设置角色战斗模型 id。 | `animation 主角 male_sword` |

### 技能、天赋、称号

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `learn` | `skill characterId skillId [level=1]` | 自动匹配并学习外功、内功或特技；按外功、内功、特技顺序尝试。 | `learn skill 主角 野球拳 1` |
| `learn` | `external characterId skillId [level=1]` | 学习外功。 | `learn external 主角 野球拳 1` |
| `learn` | `internal characterId skillId [level=1]` | 学习内功。 | `learn internal 主角 基本内功 1` |
| `learn` | `special characterId skillId` | 学习特技。 | `learn special 主角 妙手回春` |
| `learn` | `talent characterId talentId` | 学习天赋。 | `learn talent 主角 妙手空空` |
| `remove` | `skill characterId skillId` | 自动匹配并移除外功、内功或特技；按外功、内功、特技顺序尝试。 | `remove skill 主角 野球拳` |
| `remove` | `external characterId skillId` | 移除外功。 | `remove external 主角 野球拳` |
| `remove` | `internal characterId skillId` | 移除内功。 | `remove internal 主角 基本内功` |
| `remove` | `special characterId skillId` | 移除特技。 | `remove special 主角 妙手回春` |
| `remove` | `talent characterId talentId` | 移除天赋。 | `remove talent 主角 妙手空空` |
| `maxlevel` | `skillId level` | 当前只发布“武学精通”toast，不改变技能上限状态。 | `maxlevel 野球拳 5` |
| `nick` | `achievementId` | 解锁全局称号；会校验 `nick.{achievementId}` 资源存在。 | `nick 初入江湖` |

### 地图、商店、音画表现

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `map` / `set_map` / `tutorial` | `mapId` | 进入地图。 | `map 大地图` |
| `shop` | `shopId` | 打开商店面板，并等待关闭。 | `shop 新手村商店` |
| `music` | `trackId...` | 播放单首 BGM 或 BGM 池；至少需要一个参数。 | `music bgm_001 bgm_002` |
| `effect` | `effectId` | 播放音效。 | `effect hit_01` |
| `background` | `backgroundId` | 设置世界背景。 | `background changan_night` |
| `suggest` | `text` | 显示剧情提示，并等待提示流程结束。 | `suggest "前方似有异动"` |
| `shake` | `[amplitude=12] [duration=0.22]` | 播放屏幕震动。 | `shake 16 0.3` |

### 开局与流程 UI

| 指令 | 参数 | 效果 | 示例 |
| --- | --- | --- | --- |
| `toast` | `on/off` | 开启或抑制 toast 显示。 | `toast off` |
| `select_menpai` / `select_sect` | 无 | 打开门派选择 UI，并运行所选门派入口剧情。 | `select_menpai` |
| `input_name` | `characterId [defaultName=""]` | 打开改名 UI，并写回角色名。 | `input_name 主角 "小虾米"` |
| `select_head` | `characterId` | 打开头像选择 UI，并写回角色头像。 | `select_head 主角` |
| `roll_stats` | 无 | 打开主角随机属性 UI。 | `roll_stats` |
| `mainmenu` | 无 | 返回主菜单。 | `mainmenu` |
| `restart` | `[mode=restart]` | 重新开始新游戏；当前只支持 `restart`。 | `restart` |
| `nextzhoumu` | 无 | 进入下一周目流程。 | `nextzhoumu` |
| `gameover` | 无 | 增加全局死亡数，并显示失败界面。 | `gameover` |
| `gamefin` | 无 | 显示通关界面。 | `gamefin` |

### 当前占位指令

以下指令已注册，但当前未完整实现。执行时只发布“`指令名`指令暂未实现”的 toast：

| 指令 | 当前状态 |
| --- | --- |
| `game` | 小游戏流程占位。 |
| `newbie` | 新手引导流程占位。 |
| `tower` | 爬塔玩法占位。 |
| `huashan` | 华山论剑玩法占位。 |
| `trial` | 试炼玩法占位。 |
| `zhenlongqiju` | 珍珑棋局玩法占位。 |
| `arena` | 擂台玩法占位。 |
