# XMJH 指令迁移状态

本页是 `未适配指令清单.csv` 与 `GameEngine指令迁移清单.csv` 的代码落地快照。权威输入为 `XMJH/Scripts/storys.xml`、`storysCG.xml`、`storysPY.xml`；转换与隔离结果以 `jyx-legacy-data/reports/xmjh/story-isolation.json` 为准。游戏实际读取 `mods/xmjh/data`，必须通过带 `--runtime-data mods/xmjh/data` 的完整转换命令发布，不能把中间产物检查通过误认为运行时包已经更新。

## 已落地的未适配清单能力

| 旧能力 | Story v3 / Godot 落点 |
| --- | --- |
| `change_role_name`、`specified_role_name` | `input_name` / `set_character_name` |
| `cost_hour`、`to_chinesetime` | `advance_time_slots` / `advance_to_time_slot` |
| `fadeout` | `fade('out', duration)`，固定黑色淡出语义 |
| `female`、`key_is_female` | 文本性别值与 `set_gender` / `character_gender` |
| `head_v2` | `set_head(character, portrait)` |
| `randomitem` | `add_random_item` / `add_random_item_options` |
| `show_cloud` | 可存档的 `show_cloud(bool)` 冒险状态 |
| 日期、时辰、地图、门派、等级、生命、内力、技能条件 | Story v3 原生值和查询函数 |
| `should_finish_more_than` | `story_completion_count(story)` |
| `story_exceed_day` | `story_elapsed_days(story)` |
| `nick_more_than`、`jisha_more_than` | `achievement_count` / `kill_count` |
| `haogan_equals_than2` | 同类值比较；真实角色继续用 `favorability`，特殊计数器用 `story_number` |

`friendCount` 没有被擅自改成新队伍人数：按作者决定保留为 `friendcount(...)` 未适配查询，并隔离 `storysPY.xml` 的 `风月客店剧情`，具体位置见 `xmjh-deferred-command-locations.md`。

## 剧情数值变量

旧版借好感度表保存的采药、卷宗、声望、生活技能、业力、状态计数和门派威望已转换为可存档剧情数值：

- `change_story_number(name, delta)`：不存在时从 `0` 开始，最低保持 `0`。
- `story_number(name, defaultValue=0)`：条件查询。
- `list_story_numbers()`：把全部已建立的数值变量、总数和值写入江湖日志。
- 对白可使用 `$xmjh_caiyao$`、`$声望$` 等占位符；未知占位符保持原文。
- 采药和卷宗沿用稳定键 `xmjh_caiyao`、`xmjh_lsjz`；真实人物好感仍走关系系统。

## 已落地的 GameEngine 能力

| 旧动作 | 转换结果 | 运行时语义 |
| --- | --- | --- |
| `豪名指令` | `story_by_hero_name('豪名_')` | 读取当前主角名并进入 `豪名_<姓名>` 剧情。 |
| `获得物品` | `change_item(item, 1, false)` | 保留旧版不显示 toast 的语义。 |
| `减少物品` | `remove_item(item, 1, false)` | 保留旧版不显示 toast 的语义。普通 `cost_item` 现在默认显示失去物品 toast。 |
| 旧分支后缀物品名 | 运行时按已存在的基础物品解析数字后缀 | 兼容 `队友表决令3/4` 这类旧剧情写法，不新增虚假的物品定义。 |
| `公告` | `suggest(text, title)` | 复用提示框并自定义标题。 |
| `公告2` | `suggest2(text, title, button)` | 复用提示框并自定义标题和确认按钮。 |
| `is_zhujue_name` / `is_zhujue_head` | 主角姓名、头像查询 | 使用新引擎角色状态。 |
| `caiyao` / `lsjz` / `yeli` 等 | `story_number` 条件 | 不再依赖默认值为 50 的旧好感度表。 |

## 明确延期或删除

当前报告中的所有剩余 unsupported 都属于作者明确决定的范围：临时队伍与召回、赠礼与 `wpxz`、性格系统、命名系统后续重做、旧日志/平台/兑换/画质/加速指令、字体菜单、装备升级与定向洗练，以及已定位的 `IALOG`、`legacy_upgrade`、`URL`。完整剧情位置见 `xmjh-deferred-command-locations.md`。

## 2026-08-31 正式数据审计

- 总剧情段：`13034`
- 可加载剧情段：`12811`
- 因未适配查询、缺失依赖或其连锁引用而隔离：`223`
- 重名剧情段：`0`
- 剩余 unsupported 与两份清单明确延期集合对比：意外项 `0`，漏记项 `0`
- 实际运行目录复扫：`12811` 个可加载剧情段，意外 unsupported `0`；`减少物品`、`获得物品`、`公告`、`公告2` 等旧调用残留均为 `0`。

验证结果：

- Godot C# 主工程：构建成功，`0` 警告、`0` 错误。
- `Game.Tests`：`842 / 842` 通过。
- `jyx-legacy-data` Python：`124 / 124` 通过。
- `story-dsl`：`25 / 25` 通过。
- 三份权威 XML 已通过一键转换、Story v3 编译、资源/战斗/引用隔离及运行目录发布链。
