# MOD 内容扩展与数据补丁

## 当前扩展方式

项目通过 JSON 定义、`.story` 剧情源码、结构化数据补丁和 PCK 资源包扩展游戏。启动器选择一个 `type: "game"` 主 MOD，并启用多个 `type: "addon"` 扩展。主 MOD 不限定为 `jyxr-base`；addon 可通过 `dependencies` 声明所需主 MOD 或其他 addon 的 ID。依赖先于使用者加载，用户排序必须满足依赖关系；缺失依赖、循环依赖或依赖另一个主 MOD 会阻止组合解析。

| 需求 | 当前做法 |
| --- | --- |
| 新增角色、物品、武学、地图等定义 | 在 MOD 的 `data` 中提供完整定义，使用新的稳定 ID |
| 新增剧情 | 在 `data/stories/**/*.story` 中编写源码，语法见 [剧情源码编写指南](story-authoring.md)，指令见 [游戏内容 DSL v3](game-dsl-v3.md) |
| 修改已有剧情 | 在 `.story` 中提供同名段，按 MOD 加载顺序整体替换 |
| 修改已有 JSON 定义 | 使用 `patches/**/*.patch.json`，操作规则见下文 |
| 新增或覆盖内容字段引用的静态图片 | 放在 MOD 的 `assets/art` 中，支持 PNG、JPG、JPEG、WebP |
| 新增或覆盖动画、场景、主题、音视频等 Godot 资源 | 导出 PCK，在 `mod.json` 的 `packs` 中声明相对路径 |
| 迁移旧版内容 | 按 [旧 MOD 迁移指南](legacy-mod-migration-guide.md) 更新源数据 |

### MOD 清单与目录

每个 MOD 位于数据根的 `mods/<modId>`，目录内提供 `mod.json`。清单必须包含 `id`、`name`、`version`、`type`、`dependencies`、`saveImpact`。例如，一个修改基础内容的 addon：

```json
{
  "id": "example-addon",
  "name": "示例扩展",
  "version": "1.0.0",
  "type": "addon",
  "dependencies": ["jyxr-base"],
  "saveImpact": "gameplay",
  "packs": [],
  "assemblies": []
}
```

`saveImpact` 支持 `none`、`gameplay`、`structural`，主 MOD 必须声明 `structural`；存档记录加载组合，用于读档时的变更风险判断。玩家数据按主 MOD 隔离。数据根位置、启动流程与存档规则见 [运行时架构](runtime-architecture.md)。

主 MOD 必须提供 `data` 目录。PCK 路径相对于所在 MOD 目录，必须指向存在的 `.pck` 文件。PCK 按有效 MOD 加载顺序和各清单中的 `packs` 顺序加载，后加载的包可以覆盖相同资源路径。启动时先加载 PCK、建立外部图片索引，再创建运行时节点、装配 JSON 内容并建立会话。

发布时应同步清单、`data`、`patches`、外部图片 `assets/art` 和清单引用的资源包。本体升级不会自动迁移 MOD。缺少必填字段等无效清单目前会被发现流程跳过，可能表现为启动器没有可用 MOD。

Android 启动器提供“导入 ZIP”，通过系统文件选择器选择压缩包，导入成功后自动重新扫描。ZIP 根目录可以直接包含单个 MOD 的 `mod.json` 和内容，也可以包含多个完整 MOD 文件夹；导入目标统一为 `mods/<清单 id>`。导入前先在独立临时目录解压并校验清单、数据目录和 PCK 引用，拒绝越界路径、符号链接和重复 ID。相同 ID 的 MOD 整个目录会被替换，旧版本中已删除的文件不会残留；玩家存档及设置不受影响。替换失败时恢复原 MOD，临时目录在正常完成或回滚后清理。

### 外部静态图片

角色头像、物品图片、技能图标、地图与战斗背景等内容字段统一通过 `AssetResolver.LoadTexture` 加载。例如，将文件放在 `mods/my-addon/assets/art/head/my_hero.png`，角色的 `portrait` 填写 `head/my_hero.png`，也可填写 `head/my_hero`。资源 ID 继续通过资源定义的 `value` 引用相同的相对路径。路径使用 `/`，大小写与文件名一致。

解析顺序如下。

1. 在有效 MOD 的外部图片目录中，从后加载的 MOD 向前查找。
2. 未写扩展名时，同一 MOD 内依次尝试 `.png`、`.jpg`、`.jpeg`、`.webp`。写明扩展名时只匹配该文件。
3. 外部图片没有匹配结果时，通过 Godot 资源系统查询原有图片和 `.tres`、`.res` 纹理资源。所有有效 MOD 的外部图片优先于已挂载 PCK 中的同引用图片。

外部图片目录可以包含自定义子目录。图片来源在启动时确定，纹理按实际来源复用。ZIP 导入会连同 `assets/art` 一起安装，无需增加 `packs` 声明。

强引用缓存采用字节预算 LRU，桌面默认 `128 MiB`，移动端默认 `64 MiB`，超出预算后按最近访问顺序淘汰。正在使用的纹理由界面继续持有，并可通过弱引用复用。超大单图正常加载，仅保留弱引用。缓存容量依据基础层尺寸和格式估算，完整的游戏纹理显存还包含其他资源持有者的占用。

解析与加载失败共保留最近 1024 条记录。覆盖文件损坏时记录加载失败，修复后重新启动游戏生效。

该入口作用于内容字段引用的独立静态图片。角色与技能动画的切帧、时序，以及场景直接引用的 UI 图片、主题和其他 Godot 资源，继续通过 PCK 提供。

### 代码扩展边界

`assemblies` 字段目前仅解析和规范化路径，启动流程不加载其中的 DLL。项目没有 MOD initializer、独立代码插件 API 或 Harmony 加载流程；宿主启用 `EnableDynamicLoading` 不代表已经支持代码 MOD。内容作者应使用当前内置的剧情命令、条件与战斗效果类型；新增执行行为需要修改对应引擎实现。清单也不接受 `harmony` 字段。

实现入口为 `Game.Application/Mods` 下的清单、发现和加载组合解析，`Game.Content/Loading/JsonContentLoader.cs` 的内容装配，以及 `Game.Godot/Bootstrap/GameRuntimeBootstrap.cs` 的启动装配。

## 数据装配规则

运行时按主 MOD、依赖 addon、用户排序 addon 的顺序装配内容。每个 MOD 先注册 `data` 中的完整新定义，编译并装配 `stories/**/*.story`，再执行针对 JSON 定义的 `patches/**/*.patch.json`。补丁文件按规范化相对路径排序，文件内操作按声明顺序执行。

已有角色、物品、地图等 JSON 定义出现同 ID 完整定义时会中止加载。新增内容写入对应普通数据文件，修改已有 JSON 定义使用补丁。剧情使用下文的按段替换规则。

## Map 数据目录

Map 定义使用 `data/maps/**/*.json` 目录协议，不读取旧的 `data/maps.json`。主 MOD 必须提供 `data/maps` 目录，目录可以为空；addon 可以省略该目录。

目录会递归扫描，文件按规范化相对路径排序。每个 JSON 文件可以保存单个 Map 对象，也可以保存 Map 数组：

```text
data/maps/large.json
data/maps/small.json
data/maps/sects/wudang.json
```

文件路径只用于内容组织和错误诊断，不参与 Map ID。所有文件中的 `MapDefinition.id` 仍然全局唯一；addon 若要修改已有 Map，必须使用 `map` 补丁目标。

大地图使用固定的 `1920×1080` 逻辑坐标空间，不接受旧的 `800×600` 坐标，也暂不支持自定义逻辑尺寸。地点的 `position` 表示图标下方中点所落在地图上的地理锚点，不是图片左上角；坐标必须落在逻辑范围内。大地图还必须声明正数 `travelSpeed` 和指向自身已有地点的 `defaultLocation`；小地图必须省略这些字段或将 `travelSpeed` 写为 `0`。移动耗时按 `floor(distance / travelSpeed)` 计算。

```json
{
  "id": "world",
  "name": "World",
  "kind": "large",
  "travelSpeed": 22,
  "defaultLocation": "town",
  "locations": [
    {"id": "town", "position": {"x": 1098, "y": 717}, "events": []}
  ]
}
```

首次进入大地图时使用 `defaultLocation` 的坐标，之后每张大地图分别记忆自己的当前位置。`map('world', 'town')` 可显式从已有地点进入并更新该地图的记忆位置；当前不支持裸坐标或独立入口定义。

## 剧情源码目录

剧情使用 `data/stories/**/*.story`，文件按规范化相对路径排序。MOD 装配后的脚本 ID 为 MOD ID 加上相对于 `stories` 的路径，去掉 `.story` 后缀，例如 MOD `inn-addon` 的 `stories/支线/客栈.story` 对应 `inn-addon/支线/客栈`。不同 MOD 可以使用相同文件名。单独调用目录加载接口时，脚本 ID 使用去掉后缀的相对路径。剧情执行与替换均使用文件内的段名。

加载器递归读取 `.story` 源码，每次加载时自动编译为类型化剧情模型。发布时携带源码文件。

- 每个 MOD 内的段名唯一，跨文件重复也会中止加载。
- addon 使用新段名新增剧情，使用已有段名整体替换该段。
- 多个 MOD 提供同名段时，后加载者生效，加载报告记录段名、双方 MOD 和源文件路径。
- 段替换只影响该段，原文件中的其他段继续参与装配。
- 最终生效段组成全局剧情目录，再统一校验跳转、调用和内容引用。

例如，addon 要修改基础 MOD 的 `客栈_问候`，可以在自己的任意 `.story` 文件中提供完整的新段。

```text
# 客栈_问候
掌柜：客官，今日茶水免费。
```

多个 MOD 修改同一段时需要协调加载顺序和完整内容。剧情变更统一通过源码段替换表达。

## 文件格式

```json
{
  "format": 2,
  "operations": [
    {
      "op": "merge",
      "target": {"kind": "character", "id": "主角"},
      "value": {
        "name": "新的名字",
        "stats": {"bili": 50}
      }
    }
  ]
}
```

支持以下目标：`battle`、`scopedBattleEffect`、`character`、`externalSkill`、`gameTip`、`growTemplate`、`internalSkill`、`legendSkill`、`map`、`worldTrigger`、`resource`、`sect`、`shop`、`specialSkill`、`item`、`itemTag`、`equipmentRandomAffixTable`、`buff`、`talent`、`tower` 和 `gameConfig`。

普通目标必须提供稳定 `id`，`gameConfig` 不提供 ID。

## 结构化路径

`path` 是从目标根节点开始的数组：

- 字符串表示对象字段。
- `{"id": "..."}` 表示从当前数组中选择具有该 `id` 的元素。
- 省略 `path` 表示操作整个目标。

```json
{
  "op": "merge",
  "target": {"kind": "externalSkill", "id": "玄冥神掌"},
  "path": ["formSkills", {"id": "玄冥神掌.九幽归天"}],
  "value": {"cooldown": 2}
}
```

ID 选择必须恰好找到一个元素；找不到或出现重复 ID 都会中止加载。它适用于任意由带 `id` 对象组成的数组，不需要为具体字段单独登记。无稳定 ID 的数组只能整体设置或使用 `append`、`prepend`。

## 对象和值操作

### merge

递归合并对象。没有声明的字段保持不变；标量、`null` 和数组替换原值。

```json
{
  "op": "merge",
  "target": {"kind": "character", "id": "主角"},
  "value": {
    "name": "新的名字",
    "stats": {"bili": 50}
  }
}
```

### set

设置或整体替换目标节点。最后一个路径段是字段名时允许创建该字段；中间路径必须存在。整体替换实体或带 ID 的数组元素时必须保留原 ID。

```json
{
  "op": "set",
  "target": {"kind": "externalSkill", "id": "玄冥神掌"},
  "path": ["formSkills", {"id": "玄冥神掌.九幽归天"}, "buffs"],
  "value": []
}
```

### remove

删除完整定义、story segment、对象字段或按 ID 选中的数组元素。不能删除 `gameConfig`，也不能删除身份字段。

```json
{
  "op": "remove",
  "target": {"kind": "map", "id": "大地图"},
  "path": ["locations", {"id": "昆仑山"}]
}
```

### test

要求当前节点与 `value` JSON 深度相等，否则中止加载。它适合在依赖基础内容具体原值时显式保护兼容性。

```json
{
  "op": "test",
  "target": {"kind": "character", "id": "主角"},
  "path": ["name"],
  "value": "小虾米"
}
```

`null` 始终表示 JSON null，不表示删除；删除必须使用 `remove`。

## 数组操作

### append 和 prepend

```json
{
  "op": "prepend",
  "target": {"kind": "gameConfig"},
  "path": ["initialPartyCharacterIds"],
  "values": ["郭襄"]
}
```

如果数组元素包含 `id`，操作完成后会检查 ID 不重复。

### insertBefore 和 insertAfter

在带 ID 的锚点前后插入新元素。新元素必须包含未被占用的 `id`。

```json
{
  "op": "insertAfter",
  "target": {"kind": "tower", "id": "炼狱"},
  "path": ["stages"],
  "anchor": {"id": "已有阶段"},
  "value": {"id": "新增阶段", "name": "新增阶段"}
}
```

### moveBefore 和 moveAfter

移动已有的带 ID 元素：

```json
{
  "op": "moveBefore",
  "target": {"kind": "tower", "id": "炼狱"},
  "path": ["stages"],
  "item": {"id": "要移动的阶段"},
  "anchor": {"id": "目标阶段"}
}
```

数组字面量仍表示整体替换，`[]` 表示清空。

## 冲突与身份约束

- 定义的 `id`、story segment 的 `name` 和按 ID 选中的列表元素 `id` 不能被修改。
- `item.category` 是反序列化判别字段，不能通过 `merge` 修改；需要整体 `set` 该 item。
- 不同 addon 修改同一字段时后加载者生效，同时产生包含双方 MOD 和字段路径的警告。
- 所有操作结束后，合并结果仍会经过完整的强类型反序列化、引用解析和仓储校验。
