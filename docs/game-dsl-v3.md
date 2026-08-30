# 游戏内容 DSL v3 参考

本文是剧情、地图事件、世界触发、天关条件和调试控制台的统一参考。内容作者通常只需阅读“快速开始”“条件表达式”“查询参考”和“指令参考”；引擎扩展方式放在文末。

## 快速开始

剧情 command、地图 action 和控制台完整模式都写成函数调用：

```text
change_item('小还丹', 5)
change_silver(-500)
journal('得知了一个江湖传闻。')
story('星宿派_毒物林2')
```

分支、选项、地图事件、世界触发和天关的 `when` 写成返回 Boolean 的表达式：

```text
silver >= 500
in_team('女主') and story_completed('女主入队')
current_time_slot not in ['子', '丑']
not story_completed('星宿派_毒物林3胜利') and chance(0.1)
```

DSL 字符串优先使用单引号，提高 json 可读性。

地图与世界触发使用 `action` 和可选 `when`，天关使用可选 `when`。省略 `when` 等价于 `true`。

### 剧情选项条件

剧情对白后的选项支持单项尾缀条件，以及互斥的 `if / elif / else` 分支块：

```text
主角：请选择
- 离开
- 购买 if silver >= 100
if morality >= 50
  - 正道选项
  - 特殊正道选项 if rank <= 5
elif morality < 0
  - 邪道选项
else
  - 中立选项

if has_var('hidden_route')
  - 隐藏路线
```

每个 `if` 开启一条独立条件链，只有紧随的 `elif / else` 与它互斥。普通选项、单项条件和多个条件链可以按源码顺序混用。分支条件与访问到的单项条件各求值一次；未选中的 case 及其中的单项条件不会求值。所有条件过滤后没有可见选项时剧情执行失败。

## 条件表达式

### 值与字面量

仅支持 Boolean、Number、String 和同构 List：

```text
true
false
12
-3.5
6.02e23
'江湖'
['辰', '巳', '午']
```

Number 是 invariant-culture `double`。绑定到 `int` 参数时，数值必须没有小数部分且位于 `Int32` 范围内。不同基础类型之间没有隐式转换。

不支持 `null`、赋值表达式、成员访问、索引、三元表达式、语句序列或字符串拼接。列表元素异构、除零、非有限结果和异类型比较都会报错。

### 运算符与优先级

从高到低：

1. 括号、列表和函数调用
2. 一元 `not` / `!`、`+`、`-`
3. `*`、`/`、`%`
4. `+`、`-`
5. `<`、`<=`、`>`、`>=`、`in`、`not in`、`!in`
6. `==`、`!=`
7. `and`、`&&`
8. `or`、`||`

`and`/`&&` 与 `or`/`||` 都短路。内容中优先使用单词形式，复杂条件用括号明确意图：

```text
in_team('郭襄') and character_level('郭襄') >= 20
not (difficulty == 'crazy' or round >= 3)
'辰' in ['子', '丑', '辰']
current_time_slot !in ['子', '丑']
```

`value in list` 等价于 `contains(list, value)`；`not in` 和 `!in` 是反向形式。右侧必须是列表，非空列表的元素类型必须与左侧一致。

标识符区分大小写。内建值、函数与指令统一使用小写 snake_case；剧本变量还可以使用中文等无大小写 Unicode 字母。标识符可以字母或下划线开头，后续可以包含数字，例如 `是否拜师`、`门派声望_2`。

## 查询参考

### 内建值

| 名称 | 类型 | 含义 |
| --- | --- | --- |
| `silver` | Number | 当前银两。 |
| `yuanbao` | Number | 全局档案元宝。 |
| `round` | Number | 当前周目。 |
| `difficulty` | String | `normal`、`hard` 或 `crazy`。 |
| `sect` | String | 当前门派 id。 |
| `morality`、daode | Number | 当前道德. |
| `rank` | Number | 当前江湖排名值。 |
| `elapsed_days` | Number | 已经过的世界天数。 |
| `current_map` | String | 当前地图 id。 |
| `current_time_slot` | String | 当前时辰，以十二地支表示：`子`、`丑`、`寅`、`卯`、`辰`、`巳`、`午`、`未`、`申`、`酉`、`戌`、`亥`。 |
| `friend_count` | Number | 正式队员数量，只计算可参战队友（members），不含跟随队友。 |

### 查询函数

| 调用 | 返回 | 说明 |
| --- | --- | --- |
| `item_count(item_id)` | Number | 背包中的物品数量；未知物品 id 记录 Warning 并返回 `0`。 |
| `favorability(character_id='女主')` | Number | 指定关系目标的好感；别名 `haogan`。 |
| `character_level(character_id)` | Number | 活动队伍角色等级。 |
| `character_stat(character_id, stat)` | Number | 活动队伍角色基础属性。 |
| `skill_level(character_id, skill_id)` | Number | 外功或内功等级；角色未学会时为 `0`。 |
| `map_event_completed(map_id, location_id, event_id)` | Boolean | 指定地图地点下的事件是否已完成；事件 ID 只需在所属地点内唯一。 |
| `story_completed(story_id)` | Boolean | 剧情是否完成；别名 `should_finish`。 |
| `last_story_is(story_id)` | Boolean | 最近完成的剧情是否匹配；别名 `follow_story`。 |
| `has_time_key(key)` | Boolean | 是否存在剧情限时 key。 |
| `in_team(character_id)` | Boolean | 是否在活动队伍；别名 `active_party_contains`。 |
| `has_var(name)` | Boolean | 动态剧情变量是否存在。 |
| `has_flag(name)` | Boolean | 不存在时为 `false`；存在但不是 Boolean 时报错。 |
| `contains(list, value)` | Boolean | 严格类型的列表成员判断。 |
| `chance(probability)` | Boolean | 按 `0..1` 概率随机判断。 |

活动队伍包含正式队员（Members）和跟随队友（Followers）。角色等级、属性和技能查询要求目标位于其中之一；主角可以直接查询，查询其他角色时考虑先用短路保护：

```text
skill_level('主角', '野球拳') >= 10
in_team('郭襄') and character_level('郭襄') >= 20
in_team('郭襄') and skill_level('郭襄', '峨眉剑法') >= 5
```

### 动态变量与 flag

变量首次写入时确定类型，之后不能改型。直接读取不存在的变量会报错；内建值和当前 StoryExecutionContext 变量是保留名。

```text
quest_stage = 1
quest_stage += 1
quest_stage -= 1
set_flag('met_heroine')
has_flag('met_heroine') and quest_stage >= 2
clear_flag('met_heroine')
del quest_stage
```

赋值和 `del` 是剧情 step，不能写在条件、函数参数、地图 action 或调试控制台中。`=` 创建变量或写入同类型值；`+=` / `-=` 只适用于已存在的 Number。`del` 清除不存在的名称时记录 Warning 并继续剧情。`set_flag(name)` 等价于写入 Boolean `true`；`clear_flag(name)` 等价于删除变量，并与裸语句共享保留名、类型和事件规则。

## 指令参考

下表使用 canonical 名称。默认值写在签名中，方括号不属于 DSL。

### 物品与货币

| 调用 | 说明 | 别名 |
| --- | --- | --- |
| `change_item(item_id, delta=1)` | 按正负数量调整物品。 | `item` |
| `remove_item(item_id, quantity=1)` | 移除正数数量的物品，数量不足时报错。 | `cost_item` |
| `add_random_item(item_ids, quantity=1)` | 从非空字符串列表随机选择一种并添加。 | `item_random` |
| `change_silver(delta)` | 按正负值调整银两。 | `get_money` |
| `change_yuanbao(delta)` | 按正负值调整元宝。 | `yuanbao` |

```text
change_item('小还丹', 5)
change_item('小还丹', -2)
remove_item('剧情信物')
add_random_item(['小还丹', '大还丹'], 1)
change_silver(-500)
```

### 时间与冒险状态

| 调用 | 说明 | 别名 |
| --- | --- | --- |
| `advance_days(days)` | 推进正数天数。 | `cost_day` |
| `set_round(round)` | 设置正数周目。 | — |
| `set_difficulty(id)` | 设置 `normal`、`hard` 或 `crazy`。 | `set_game_mode` |
| `set_no_regret(enabled)` | 设置无悔模式。 | — |
| `set_sect(id)` | 设置门派。 | `menpai` |
| `change_morality(delta)` | 调整道德。 | `daode` |
| `change_favorability(character_id, delta)` | 调整指定关系目标好感。 | `haogan` |
| `set_rank(rank)` | 设置江湖排名值。 | — |

```text
advance_days(3)
set_difficulty('hard')
set_no_regret(true)
set_sect('星宿派')
change_favorability('女主', 5)
```

### 剧情状态

| 调用 | 说明 | 别名 |
| --- | --- | --- |
| `journal(text)` | 写入带当前时间快照的江湖日志。 | `log` |
| `set_flag(name)` | 写入 Boolean flag。 | — |
| `clear_flag(name)` | 删除 flag；不存在时记录 Warning 并正常返回。 | — |
| `set_time_key(key, days, story_id='')` | 创建限时剧情 key；省略 story id 时到期仅移除 key。 | — |
| `clear_time_key(key)` | 删除限时剧情 key；不存在时记录 Warning 并正常返回。 | — |
| `world_triggers(enabled)` | 开启或阻塞世界触发。 | — |

```text
journal('踏入江湖。')
set_time_key('夜探', 3, '夜探失败')
world_triggers(false)
```

### 角色成长

| 调用 | 说明 | 别名 |
| --- | --- | --- |
| `change_stat(character_id, stat, delta)` | 调整角色基础属性。 | — |
| `set_growth(character_id, growth_id)` | 设置成长模板。 | `growtemplate` |
| `scale_stats(character_id, ratio)` | 按 `0..1` 比例缩放基础最大生命、最大内力、十维属性和未分配属性点。 | — |
| `grant_points(character_id, points)` | 增加正数自由属性点。 | `grant_point`、`get_point` |
| `grant_exp(character_id, experience)` | 增加正数经验并结算升级。 | `get_exp` |
| `level_up(character_id, levels=1)` | 直接提升正数等级。 | `levelup` |
| `upgrade_external(character_id, skill_id, levels=1)` | 明确升级外功。 | — |
| `upgrade_internal(character_id, skill_id, levels=1)` | 明确升级内功。 | — |
| `upgrade_skill(character_id, skill_id, levels=1)` | 按外功、内功顺序自动分类升级。 | — |
| `maxlevel(skill_id, levels=1, once_key='')` | 永久增加指定数值的武学等级上限；同一非空 once key 只生效一次。周目加成只影响当前有效上限，不改变本指令的永久增量。once key如果为空，DSL编译时候会自动生成。 | `max_skill_level` |

属性名支持中文显示名或 code：`拳掌/quanzhang`、`剑法/jianfa`、`刀法/daofa`、`奇门/qimen`、`臂力/bili`、`身法/shenfa`、`悟性/wuxing`、`福缘/fuyuan`、`根骨/gengu`、`定力/dingli`、`武学点/wuxue`、`气血上限/max_hp/maxhp`、`内力上限/max_mp/maxmp`、`攻击力/attack`、`防御力/defence`、`闪避率/evasion`、`命中率/accuracy`、`暴击率/crit_chance`、`暴击伤害/crit_mult`、`抗暴率/anti_crit_chance`、`吸血/lifesteal`、`抗异常/anti_debuff`、`集气速度/speed`、`移动力/movement`。

```text
change_stat('主角', '拳掌', 10)
grant_exp('主角', 500)
upgrade_external('主角', '野球拳', 2)
maxlevel('野球拳', 5, 'reward.野球拳.mastery')
```

### 队伍、学习与称号

| 调用 | 说明 | 别名 |
| --- | --- | --- |
| `join(character_id, definition_id?)` | 加入正式队伍；definition id 省略时使用 character id。 | — |
| `join_random(character_ids)` | 从非空候选列表随机加入一名角色。 | — |
| `follow(character_id, definition_id?)` | 加入随队池；definition id 省略时使用 character id。 | — |
| `leave(character_id)` | 正式队员离队并进入后备池。 | — |
| `leave_follower(character_id)` | 随队角色离队并进入后备池。 | `leave_follow` |
| `leave_all()` | 所有正式队员离队并进入后备池。 | — |
| `learn(character_id, target_id, level=1)` | 自动分类学习。 | — |
| `remove(character_id, target_id)` | 自动分类移除。 | — |
| `learn_external/internal(character_id, skill_id, level=1)` | 明确学习外功或内功。 | — |
| `learn_special/talent(character_id, target_id)` | 明确学习特技或天赋。 | — |
| `remove_external/internal/special/talent(character_id, target_id)` | 明确分类移除。 | — |
| `unlock_achievement(id)` | 解锁 `nick` 资源组中的全局称号。 | `nick` |

`learn/remove` 按外功、内功、特技、天赋顺序命中第一个同 ID Definition。外功、内功使用 level；特技、天赋忽略 level 的具体数值。需要无歧义时使用显式分类指令。

```text
join('程英', '程英.初级')
join_random(['程英', '郭襄'])
learn('主角', '野球拳', 10)
learn_talent('主角', '妙手空空')
remove_external('主角', '野球拳')
```

### 场景、音频与媒体

| 调用 | 说明 | 别名 |
| --- | --- | --- |
| `story(id)` | 执行另一段剧情。 | — |
| `map(id, location_id?)` | 进入地图；大地图可指定已有地点作为落点。 | `set_map`、`tutorial` |
| `shop(id)` | 打开商店并等待关闭。 | — |
| `chest()` | 打开储物箱并等待关闭。 | `xiangzi` |
| `battle(id)` | 选择出战角色并进入战斗；地图事件也可传入旧版 `id#次数#强化等级`，参数会被兼容解析。 | — |

从旧版 XML 转换的战斗段可能带有 `#次数#强化等级` 后缀。转换器会将
它们写入 Story v3 battle step 的 `totalBattles`、`battleLevel` 字段，
并使用去掉后缀的 `battleId` 查找战斗定义；因此不会再把
`战斗名#1#3` 误判为缺失战斗。
| `music(...track_ids)` | 播放单曲或非空 BGM 池。 | — |
| `sound(id)` | 播放音效。 | `effect` |
| `background(id)` | 设置世界背景。 | — |
| `video(id)` | 播放 `.ogv` 剧情视频并等待结束。 | `movie` |
| `suggest(text)` | 显示并等待剧情提示。 | — |
| `toast(enabled)` | 开启或抑制 toast。 | — |

每个地图事件必须声明在所属地点内唯一且稳定的 `id`。`once` 事件在 command 成功后按 `mapId + locationId + eventId` 记录完成状态，因此调整事件数组顺序不会改变存档语义。地图 action 可调用当前会话注册的任意 StoryCommand，不限于场景指令；世界触发会在派发一次性 command 前记录完成状态，防止换图递归触发。

```json
{
  "id": "大地图-黑木崖-岳父",
  "action": "story('笑傲江湖_黑木崖岳父')",
  "repeatMode": "once"
}
```

```text
story('星宿派_毒物林2')
map('大地图')
map('大地图', '南贤居')
change_silver(25)
music('音乐.逍遥1', '音乐.逍遥2')
video('视频.开场')
toast(false)
```

大地图的单参数调用优先恢复该地图已保存的位置；尚无记录时使用地图定义的
`defaultLocation`。双参数调用会跳转到目标地图已有地点并覆盖该地图的位置记忆。
地点是否隐藏或是否有可用事件不影响其作为落点。小地图不得使用第二参数，DSL
也不接受任意坐标。

### 视觉表现

| 调用 | 说明 |
| --- | --- |
| `shake(amplitude=10, duration=0.5)` | 衰减震屏。 |
| `fade(mode, duration=0.5)` | `in` 淡入或 `out` 淡出黑场。 |
| `flash(preset='white', duration=0.25, strength=1)` | 闪屏；preset 为 `white/red/gold/blue`。 |
| `filter(preset, strength=1, duration=0.3)` | 保持滤镜；preset 为 `grayscale/sepia/cold/warm/poison/night`。 |
| `clear_filter(duration=0.3)` | 清除滤镜。 |
| `distort(preset, strength=1, duration=0.3)` | 保持形变；preset 为 `ripple/wave/heat/fisheye`。 |
| `clear_distort(duration=0.3)` | 清除形变。 |
| `tint(color, strength=0.25, duration=0.3)` | 使用 `#RRGGBB` 或 `#RRGGBBAA` 染色。 |
| `clear_tint(duration=0.3)` | 清除染色。 |
| `wait(duration)` | 按真实时间等待。 |
| `intertitle(text, position='center', mode='typewriter', speed=36)` | 显示文字过场；position 为 `upper/center/lower`。 |

duration、震屏 amplitude 不得为负数，strength 必须在 `0..1`。滤镜、形变和染色分别保持到被替换、显式清除或剧情表现流程结束。

```text
fade('out', 0.6)
background('地图.夜晚客栈')
filter('night', 0.7, 0)
intertitle('[color=light_blue]数年之后……[/color][br]江湖风波又起')
fade('in', 0.6)
```

### 角色创建、外观与流程

| 调用 | 说明 | 别名 |
| --- | --- | --- |
| `select_sect()` | 打开门派选择并执行所选入口剧情。 | `select_menpai` |
| `input_name(character_id, default_name='')` | 打开改名 UI；目标不存在时创建到后备池。 | — |
| `select_portrait(character_id)` | 打开头像选择。 | `select_head` |
| `roll_stats()` | 打开主角随机属性 UI。 | — |
| `set_portrait(character_id, portrait_id)` | 设置指定角色头像。 | `head` |
| `set_model(character_id, model_id)` | 设置角色战斗模型。 | `animation` |
| `main_menu()` | 返回主菜单。 | `mainmenu` |
| `restart()` | 记录当前周目通关并重新开始游戏。 | — |
| `next_round()` | 记录当前周目通关并进入下一周目。 | `nextzhoumu` |
| `game_over()` | 记录死亡并显示失败界面。 | `gameover` |
| `game_complete()` | 记录当前周目通关并显示通关界面。 | `gamefin` |

```text
select_portrait('主角')
set_portrait('主角', 'hero_01')
input_name('主角', '小虾米')
set_model('主角', 'male_sword')
```

### 特殊流程

| 调用 | 说明 | 别名 |
| --- | --- | --- |
| `minigame(id)` | 运行小游戏，如 `qinggong`、`dianxue`。 | `game` |
| `refine()` | 打开装备洗练流程。 | `xilian` |
| `tower()` | 运行天关流程。 | — |
| `huashan()` | 运行华山论剑流程。 | — |
| `trial()` | 运行试炼流程。 | — |
| `zhenlong()` | 运行珍珑棋局流程。 | `zhenlongqiju` |
| `arena(callback_story_id='')` | 运行擂台，结束后可跳转剧情。 | — |

```text
minigame('qinggong')
refine()
arena('擂台结束')
```

### 调试控制台

控制台优先接受与内容完全相同的 DSL：

```text
change_item('小还丹', 5)
run_story('debug_visual_effects')
run_battle('测试战斗')
```

也接受一般控制台风格写法（未加引号的 token 会按 Boolean、Number、String 解析，但列表等复杂参数必须使用完整 DSL。）：

```
change_item 小还丹 5
run_story debug_visual_effects
run_battle 测试战斗
```

`run_story` 和 `run_battle` 仅存在于调试注册表，剧情及地图 action 不能调用。

## 别名与不兼容边界

别名与 canonical 方法共享签名和校验，不做参数重排、数值换算或旧语义适配。

- 查询：`should_finish → story_completed`、`follow_story → last_story_is`、`active_party_contains → in_team`、`haogan → favorability`。
- 业务：`item → change_item`、`cost_item → remove_item`、`item_random → add_random_item`、`get_money → change_silver`、`yuanbao → change_yuanbao`、`cost_day → advance_days`、`set_game_mode → set_difficulty`、`log → journal`、`daode → change_morality`、`menpai → set_sect`、`growtemplate → set_growth`、`grant_point/get_point → grant_points`、`get_exp → grant_exp`、`levelup → level_up`、`max_skill_level → maxlevel`、`leave_follow → leave_follower`、`nick → unlock_achievement`、`game → minigame`、`xilian → refine`、`zhenlongqiju → zhenlong`。
- 宿主：`set_map/tutorial → map`、`xiangzi → chest`、`effect → sound`、`movie → video`、`select_menpai → select_sect`、`select_head → select_portrait`、`head → set_portrait`、`animation → set_model`、`mainmenu → main_menu`、`nextzhoumu → next_round`、`gameover → game_over`、`gamefin → game_complete`。

不支持以下旧语义：

- `cost_money(amount)`：改写为 `change_silver(-amount)`。
- 反向或阈值谓词：改用普通运算符。
- `probability` 字段：改用 `chance(0..1)`。
- `learn(type, character_id, target_id)` 和 `remove(type, ...)`：改用自动分类或显式分类指令。
- 隐式主角 `head(portrait_id)`：必须显式写角色 id。
- 字符串 `toast('on'/'off')`：必须传 Boolean。
- `set_var/change_var/remove_var`：剧情中改用裸赋值、复合赋值和 `del`。

XML 转换器会把通用 `SET_FLAG/CLEAR_FLAG/SET_VAR/CHANGE_VAR/REMOVE_VAR` 转为上述状态语句；`NO_GLOBAL_EVENT` 是世界触发器开关特例。转换结果必须写入独立草稿或临时目录审查，不直接覆盖 `mods/jyxr-base/data` 中的正式 JSON。

常见迁移写法：

```text
not story_completed('剧情ID')
silver >= 500
current_time_slot in ['辰', '巳']
in_team('郭襄') and character_level('郭襄') >= 10
rank != -1 and rank <= 10
change_silver(-100)
join_random(['程英', '郭襄'])
set_portrait('主角', 'hero_01')
```

## 引擎扩展附录

`Game.Expressions` 是独立 `net10.0` 项目，仅依赖 Parlot，不引用 RPG 领域、内容加载、应用层或 Godot。语法由预构建的 Parlot Fluent parser 声明，内容装配时解析为不可变 AST，运行期不重复解析字符串。

公共入口：

- `ExpressionParser.ParseExpression`：解析任意表达式。
- `ExpressionParser.ParseCall`：要求根节点为函数调用。
- `ExpressionEvaluator`：在显式环境中同步求值。
- `ExpressionAnalyzer`：检查符号、权限、参数数量、静态类型和期望返回类型。
- `ExpressionFunctionRegistryBuilder.AddLibrary`：扫描纯函数 library。
- `AsyncExpressionCallRegistryBuilder<T>.AddLibrary<TAttribute>`：扫描异步 command library。
- `ExpressionCallRegistryBuilder<T>.AddLibrary<TAttribute>`：扫描同步调用 library。

业务扩展声明普通强类型 .NET 方法：

```csharp
[ExpressionFunction("item_count")]
public int ItemCount(string itemId) => ...;

[StoryCommand("change_item", "item")]
public void ChangeItem(string itemId, int delta = 1) => ...;
```

支持 `string`、`bool`、`int`、`double` 及对应数组/只读列表。最后一个 `params T[]` 是可变参数。异步 command 可以在末尾声明不计入 DSL arity 的 `CancellationToken`，返回 `Task`、`ValueTask` 或带 command result 的泛型版本；纯函数必须同步。

`ExpressionValue` 只用于剧情变量赋值、`contains` 等真正动态的边界。Library 通过构造函数获得会话或服务。函数、StoryCommand 和 DebugCommand 分属不同注册表，名称冲突、非法签名和跨权限调用在注册或分析阶段失败。
