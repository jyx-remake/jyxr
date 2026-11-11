# 旧 MOD 迁移指南

本文面向从 legacy 版本迁移到当前 MOD 内容格式的作者，记录会改变剧情语义的数据差异和迁移要求。

迁移时应修改源内容，使其直接符合当前数据模型。不要依赖按显示名回退、旧字段别名或运行期兼容逻辑。

## 1. 分离角色实例 ID 与人物 Definition ID

人物实例的 `characterId` 是剧情指令、队伍状态和存档引用角色的稳定身份；`CharacterDefinition.id` 只标识首次创建实例时使用的静态模板；`name` 只用于显示，可以重复，也可能在运行时被改名。

`join`、`follow` 的当前格式为：

```text
join <characterId> [definitionId]
follow <characterId> [definitionId]
```

省略 `definitionId` 时默认与 `characterId` 相同。实例已存在于当前队伍、跟随池或后备池时，指令只移动并复用原实例，不更换其 Definition，也不丢失成长状态。

迁移前，应汇总 `game-config.json` 初始队伍、`join`、`follow`、`join_random` 及 MOD 自定义入队入口。如果多个 Definition 表达同一个可持续成长的剧情人物，只是初始阶段或强度不同，应为它们选择同一个稳定 `characterId`，并仅在首次加入命令中区分模板。

例如：

```json
[
  {"id": "程英.初级", "name": "程英"},
  {"id": "程英.高级", "name": "程英"}
]
```

应迁移为：

```text
join 程英 程英.初级
join 程英 程英.高级
```

地图条件、离队指令、人物成长、武学学习、属性判断和霹雳堂等按人物实例运行的剧情统一引用 `程英`：

```json
{"type": "not_in_team", "value": "程英"}
```

队伍条件只保留 `in_team(characterId)` 和 `not_in_team(characterId)`。legacy 的 `key_in_team`、`key_not_in_team` 应分别迁移为 `in_team`、`not_in_team`，当前运行时不再注册 `key_*` 条件。

如果两个同名 Definition 确实表示需要同时存在、分别成长或分别参与条件判断的角色实例，则继续使用不同的 `characterId`，例如 `袁承志` 与 `儿时袁承志`。不要仅因为显示名相同就合并实例身份。

`definitionId` 只在首次创建时生效。若业务需要把已有角色从一个模板升级或转换为另一个模板，应建立独立的“角色转化/重建”用例，不能让 `join` 隐式完成。低级程英升级为高级模板只是这一通用规则的一个例子。

对话和选项的说话人属于展示解析；同一人物的不同模板共用显示身份时，也应优先写稳定 `characterId`。

## 2. 地图剧情入口与一次性去重

地图事件的入口身份 `(mapId, locationId, eventId)`、`story(...)` 的 target 和 `repeatMode == "once"` 的去重身份是三个不同概念。迁移器不能机械地为每条地图事件记录分配彼此独立的 once 去重 ID，也不能仅因多个入口指向同一个 story target，就把所有入口合并成同一种重复模式。

### 同一 story 同时存在 once 与 infinite 入口

同一个 story target 可能同时拥有 once 入口和未声明 `repeatMode` 的 infinite 入口，而且 infinite 入口可能先于 once 入口执行。infinite 入口的执行不能消耗 once 入口的去重状态；入口顺序也不能作为迁移前提。

已知案例：

- 太湖的 `tlbb.dy_阿朱阿碧` 有两个 50% 概率入口。once 入口与 infinite 入口位于同一地点，非 once 入口可能先命中，之后 once 入口仍可能命中；这是确定存在的反序风险。
- 雁门关穆人清的 `bixuejianShyou_闯王剧情2` 同时存在 once 与 infinite 入口，两个入口由不同剧情阶段控制。但这种可能是没问题的。
- 这种组合还可能跨地图出现。`original_新手地图.送镖` 的大地图入口是 once，龙门镖局 NPC 入口是 infinite。正常路线通常先经过大地图 once，因此风险较低；但传送、MOD 修改入口或剧情跳转后仍可能反序触发。

### 多个 once 入口共享 target

多个 once 入口也可能有意共享同一个 story target，并依靠 target 级去重表达“同一剧情可从多个位置触发，但全局只播放一次”。如果迁移后每条入口记录都使用自己的 once 去重 ID，这些剧情会重复播放。

已知案例：

- `碧血剑_穆人清`：2 个 NPC 入口。
- `笑傲江湖_昆仑山雪莲`：4 个山洞入口。
- `original_华山论剑`：华山同一地点的两组条件入口。
- `original_剑魔荒冢独孤求败`：大地图入口和荒冢内 NPC 入口。
- `original_同福客栈`：4 个 NPC 入口。
- `tlbb.dy_西夏驸马.酒楼传闻`：3 家酒楼入口。

因此，迁移后的模型必须显式保留以下语义：地图事件 ID 在所属地点内唯一，并与地图、地点 ID 共同标识入口记录；story target 标识要执行的剧情；once 去重键允许多个 once 入口共享，但 infinite 入口不参与也不消耗该去重键。若未来允许作者显式配置 once 去重键，未配置时应从旧数据的 target 语义迁移，而不是默认使用入口事件 ID。

## 3. 地图移动耗时不要按 `mapunit@time` 原样迁移

legacy 的 `mapunit@time` 把移动规则写在目的地点上，但“前往某地点需要多久”通常取决于所在地图的时间尺度、起点、终点和路径，不是地点自身的固有属性。因此，当前迁移器应忽略该属性，不要在 `MapLocationDefinition` 中建立同名字段或倍率字段。

后续需要恢复这部分语义时，应优先设计地图级时间消耗倍率：同一张地图上的普通移动统一使用该倍率，以表达世界地图、城市地图等不同尺度。大地图仍由坐标距离与 `travelSpeed` 决定基础耗时，地图级倍率只调整该地图整体的时间消耗，不替代距离计算。

如果某个地点确实存在不随起点变化的额外耗时，例如需要固定时间穿过特殊区域，应把它建模为该地点或其进入事件的固定额外耗时，而不是伪装成通用倍率。若耗时实际取决于路线，则应进一步建立路径级规则。

在这些协议正式建立前，不要根据 `mapunit@time` 推导、取整或写入当前 JSON，以免把 legacy 数据布局固化为新的错误模型。

## 4. 手动指定大地图默认进入点位

legacy 转换器不会推断大地图入口。完成地图目录拆分和坐标迁移后，必须为每张大地图手动增加 `defaultLocation`，其值应引用该地图已有且具有坐标的 `location.id`。例如基础大地图使用 `"defaultLocation": "南贤居"`。

不带地点参数的 `map(mapId)` 会优先恢复该大地图的存档位置；尚无记录时才使用 `defaultLocation`。需要从确定地点进入时使用 `map(mapId, locationId)`，该地点坐标会成为新的记忆位置。本版不支持任意坐标入口。

## 5. 将剧情 XML 转成 `.story`

仓库提供独立的 .NET 10 命令行项目 `tools/StoryXmlConverter`。在仓库根目录运行以下命令，将包含 `<root><story ...>...</story></root>` 的剧情 XML 转成当前源码格式。

```powershell
# 自动选择输出文件名
dotnet run --project tools/StoryXmlConverter -- "path/to/main.xml"

# 指定输出文件
dotnet run --project tools/StoryXmlConverter -- "path/to/main.xml" "path/to/main.story"
```

默认输出位于输入文件旁，依次选用 `main.converted.story`、`main.converted-2.story` 等可用文件名。指定的输出路径已存在时转换失败。工具先完成转换并调用 `Game.StoryDsl` 校验语法，再写入 UTF-8 无 BOM 文件。成功返回退出码 `0`，失败返回 `1`，XML 和 DSL 错误会标明对应文件及位置。

转换规则包含以下内容。

- `story@name` 转成段头，对白、选项和战斗结果转成对应的缩进结构。
- 旧命令转成当前函数调用，变量操作转成赋值或 `del`，条件转成共享表达式。队友属性查询会增加 `in_team(...)` 判断。
- `[[red:文本]]` 等旧颜色标记转成 BBCode，正文换行转成 `[br]`。`newbie`、`touch` 按已移除指令处理。
- 同组选项或战斗结果中的条件分支全部保留。存在条件分支时采用最后一个无条件结果作为默认分支。全部为无条件结果时，优先采用首个数字 `ret` 结果，其次采用首个结果。其余冲突结果保留为 `//` 注释，供作者复核。

生成后应复核注释中的冲突分支、角色实例 ID、命令参数和剧情目标，再将文件放入目标 MOD 的 `data/stories` 并加载 MOD 验证内容引用。源码编写方式见[剧情源码编写指南](story-authoring.md)。
