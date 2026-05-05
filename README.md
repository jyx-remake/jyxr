# jyxr

## 声明

一切均免费，纯粹用爱发电。非常欢迎感兴趣的朋友加入。但仅供学习交流使用，请勿骑脸版权方。资源和数据文件为避免版权问题，不在本仓库中，因此不能直接运行。

尚未完工，可能会有较大、破坏性更新。

## 项目概述

基于 `.NET 10` 与 `Godot 4.6` 的 2D 半即时制战棋 RPG 内核原型。

仓库根目录就是 Godot 工程根，根目录下的 `engine-free-rpg.csproj` 是当前唯一的 Godot 宿主程序集项目。

当前目标不是补齐完整游戏，而是把角色规则、内容装配、剧情解释器、地图流程、存档模型、轻量战斗原型和 Godot 宿主边界先定稳。当前已接入第一版战斗内核与 Godot 战斗界面；正式战场系统仍按设计文档继续重建。

## 项目结构

- `data`
  - 正式 JSON 内容包。Godot 侧通过 `res://data` 加载。
  - 包含角色、技能、物品、装备、地图、商店、门派、资源、剧情等内容。
- `assets`
  - Godot 资源目录，当前主要包括 `art`、`audio`、战斗动画库与图集纹理。
- `autoload` / `scenes`
  - Godot 场景和 autoload 接线。
- `src/Game.Core`
  - 领域模型、定义模型、角色状态、技能实例、背包/装备实例、affix 投影、轻量战斗状态/引擎、剧情运行时、存档记录。
- `src/Game.Content`
  - JSON 内容读取、引用校验、runtime definition 装配、内存仓储。
  - 正式内容不放在这里；`SampleData` 只用于测试样例。
- `src/Game.Application`
  - 应用态 `GameSession`、全局档案、存读档服务、角色服务、背包服务、物品使用服务、商店服务、地图服务、剧情服务、剧情命令行、诊断日志抽象、会话事件。
- `src/Game.Godot`
  - Godot 宿主适配源码，由根目录 `engine-free-rpg.csproj` 编译。
  - 当前包括全局入口、preview bootstrap、Godot 内容包加载、本地存档/档案持久化、主菜单、失败界面、地图屏幕、HUD、英雄面板、角色面板、系统面板、储物箱、战斗 UI、剧情 UI、音频和资源解析。
- `test/Game.Tests`
  - 角色规则、内容加载、存读档、地图服务、剧情服务、战斗引擎、会话事件等测试。
- `legacy_scenes`
  - 原 Godot/GDScript 参考场景，作为迁移参考，不是当前主运行路径。
  - 已完成迁移的参考 UI 会逐步从这里移除，避免继续把旧实现误当作运行时依赖。
- `jyx-legacy-data` / `jyx-legacy-dll`
  - 原版参考资源，通过 git submodule 挂载。

## 当前分层

- `Game.Core`
  - 不依赖 Godot。
  - 保存稳定定义、运行时状态、存档记录和剧情运行时。
  - 运行时对象可以直接持有已解析 definition 引用，存档只保存稳定 ID 和实例状态。
- `Game.Content`
  - 把 JSON 内容装配成 `InMemoryContentRepository`。
  - 负责索引 definition、解析 definition 引用、集中处理 affix 引用、做仓储级校验。
- `Game.Application`
  - 持有当前应用态会话和用例服务。
  - 服务通过 `GameSession` 访问 `GameState`、内容仓储和其他应用服务。
- `Game.Godot`
  - 作为薄宿主层，负责 Godot 场景、UI、资源、音频和宿主表现。
  - 业务日志统一通过 `Game.Logger` 输出。

## 核心运行态

- `GameState`
  - 当前可存档的大状态。
  - 持有 `Adventure`、`Party`、`Inventory`、`Chest`、`EquipmentInstanceFactory`、`Currency`、`Clock`、`Location`、`MapEventProgress`、`Shop`、`Story`、`Journal`。
  - 不使用业务构造函数；读档或初始化后通过显式 setter 装配。
- `AdventureState`
  - 当前周目的运行态上下文。
  - 保存周目、难度、门派、道德、女主角好感和江湖排名。
  - 难度稳定模式 id 为 `normal`、`hard`、`crazy`。
- `Party`
  - 全局伙伴名册聚合，无队伍 id/name。
  - 分为 `Members`、`Followers`、`Reserves` 三个池子，同一 `CharacterInstance` 只能处于一个池子。
  - `Members` 是当前队伍；`Followers` 是随队但不在当前编队的角色；`Reserves` 保存入过队或跟随过但当前离队的角色。
- `GameProfile`
  - 全局档案状态，不属于单个 `SaveGame` 槽位。
  - 当前保存已解锁称号、累计死亡数、累计击杀数。
- `GameSession`
  - 普通应用态对象，不是单例类。
  - 持有当前 `GameState`、`GameProfile`、`GameConfig`、`IContentRepository`、应用服务和 `SessionEvents`。
  - 当前内置 `SaveGameService`、`ProfileService`、`SessionFlowService`、`PartyService`、`InventoryService`、`ChestService`、`ItemUseService`、`ShopService`、`CharacterService`、`MapService`、`StoryTimeKeyExpirationService`、`StoryService`。
  - 读档时通过 `ReplaceState(...)` 替换状态；读全局档案时通过 `ReplaceProfile(...)` 替换 `GameProfile`；现有服务实例不重建。
- `Game`
  - Godot 宿主层全局入口。
  - 转发 `Session`、`State`、`Profile`、`Config`、`ContentRepository`、`SaveGameService`、`ProfileService`、`SessionFlowService`、`PartyService`、`InventoryService`、`ChestService`、`ItemUseService`、`ShopService`、`CharacterService`、`MapService`、`StoryService`、`Audio`、`Logger`。
  - `Game.Initialize(...)` 只接收已构造的 `GameSession` 和 logger；`GameConfig` 从 `GameSession.Config` 读取。该方法是宿主启动装配，不是业务流程调用点。
- `GameConfig`
  - 当前从 `data/game-config.json` 读取，并挂在 `GameSession` 上。
  - 当前配置开局剧情、初始队伍、储物箱容量、角色/技能上限和随机战斗音乐池等预览运行参数。
- `SessionFlowService`
  - 负责新游戏与下一周目状态切换。
  - 新状态由 `NewGameStateFactory` 创建；下一周目保留银两和储物箱内容，周目数加 1。
- `SaveGame`
  - 单个存档槽数据。
  - 保存稳定 ID 和实例状态，不保存 runtime definition 引用。
  - `PartyRecord` 保存当前队伍、跟随者和后备池角色 ID；角色记录覆盖整个 `Party` 名册，保证离队角色成长、装备和技能不丢失。
  - 当前保存并恢复商店购买进度，用于限购商品跨存档延续。
- `GameProfileRecord`
  - 全局档案的持久化记录。
  - 当前由 Godot 宿主单独落盘，不并入 `GameState`。
- `StoryState`
  - 保存剧情变量、已完成剧本、最后一次执行剧本和剧情时间 key。
  - 时间 key 记录开始时间、限制天数、截止时间、目标剧本、是否已触发和触发时间，参与存读档。
- `JournalState`
  - 保存江湖日志条目。
  - 每条日志保存文本和 `ClockRecord` 时间快照，参与存读档。
- `ShopState`
  - 保存商店商品购买数量。
  - 购买 key 由 `shopId|productIndex|contentId` 组成，避免同名商品在不同商店或不同位置混淆。

## 内容加载

- 正式内容目录是仓库根 `data`。
- `src/Game.Content/Game.Content.csproj` 会把根目录 `data/**/*.json` 作为链接内容复制到输出目录。
- `JsonContentLoader` 支持：
  - `LoadFromDirectory(...)`
  - `LoadFromFile(...)`
  - `LoadFromPackage(...)`
- 目录加载会扫描根 JSON 文件和 `story/*.story.json`，并把 story segment id 建入内容仓储。
- `ContentPackage` 当前是公开类型，允许 Godot 宿主先用自身文件 API 组装内容包，再交给 `JsonContentLoader.LoadFromPackage(...)` 构建仓储。
- Godot 运行时通过 `GodotContentPackageLoader` 从 `res://data` 读取正式内容，避免导出后依赖 `ProjectSettings.GlobalizePath(...)` 和普通文件系统路径。
- 顶层 JSON 当前直接反序列化到 runtime definition，不维护完整 `XxxDto -> XxxDefinition` 平行层。
- 装配流程大致为：
  - 读取 JSON 到 `ContentPackage`
  - `BuildRepository(...)` 索引 definition、解析引用、装配 story segment
  - `ValidateRepository(...)` 做仓储级校验
- `characters.json` 中角色属性 `wuxue: -1` 表示 legacy 数据里的“武学常识无限”。当前加载阶段会在 `BuildRepository(...)` 边界把它规范化为 `9999`，避免运行时属性系统接收负值；这是临时大数方案，后续若正式建模无限语义，应替换该约定。
- 商店 JSON 当前支持普通银两价格 `price` 与元宝价格 `premiumPrice`。旧数据中的 `contentId == "元宝"` 与 `*残章` 商品会在内容校验和商店视图中被忽略，避免把货币或残章兼容条目当作正式商品。
- `FormSkillDefinition.Icon` 当前是可空字段；正式内容和 legacy 转换脚本不再要求为每个招式输出空 icon。
- 当前 definition 依赖按有向无环图处理，还不是真二阶段加载。若后续支持循环依赖，应改为“先注册 runtime definition 空壳，再统一 resolve 引用”。
- `IContentRepository` 当前同时提供 `GetXxx(...)` 与 `TryGetXxx(...)`；业务规则应按语义选择“缺失即失败”或“缺失回退”。
- `MapLocationDefinition.Name` 当前允许为空；Godot 地图按钮显示时优先用 `Name`，否则按 location id 通过资源/角色名解析回退。

## 角色、技能、Affix

- `CharacterDefinition` 是静态角色模板。
- `CharacterInstance` 是可持久化角色实例，持有自身 definition 引用。
- `CharacterInstance` 当前显式持有等级、经验、未分配属性点、外功/绝技激活状态和已装备内功。
- 技能运行时实例当前以这些类型为中心：
  - `ExternalSkillInstance`
  - `InternalSkillInstance`
  - `FormSkillInstance`
  - `SpecialSkillInstance`
  - `LegendSkillInstance`
- `CharacterService`
  - 负责属性加点、经验获取、升级成长、技能/天赋/绝技学习、外功激活、绝技激活、内功装备切换。
  - 升级曲线集中在 `CharacterLevelProgression`。
- 技能实例当前统一使用 `SkillInstance.DefaultMaxLevel = 20` 作为默认等级上限；legacy `maxlevel` 剧情命令只做技能存在性校验并发 toast，不再写入角色技能实例状态。
- 技能实例的 `Level` / `Exp` 当前已是运行时可变状态；初始化新游戏时 `NewGameStateFactory` 会把初始角色已有技能提升到各自 `MaxLevel`。
- `PartyService`
  - 负责当前队伍顺序调整、入队、跟随、离队入后备池和名册内角色改名/创建。
  - `join` / `follow` 会优先复用 `Followers` 或 `Reserves` 中已有实例；`leave` / `leave_follow` 会把角色移入 `Reserves`。
  - 队伍名册状态变化后统一发布 `PartyChangedEvent`；角色改名发布 `CharacterChangedEvent`。
- 当前保留的是轻量 `AffixDefinition` 角色投影系统，不是旧 attachment / modifier / battle hook 运行实现。
- 装备实例、天赋、Buff 通过 `IAffixProvider` 暴露普通 affix。
- 外功、内功通过 `SkillAffixDefinition` 暴露技能 affix。
- `CharacterInstance.RebuildSnapshot()` 收集角色当前生效 affix 后交给 resolver 展开。
- `GrantTalentAffix` 等需要仓储引用的 affix 当前由 `JsonContentLoader.ResolveAffixes(...)` 集中解析。
- 角色初始化和读档恢复后会立即 `RebuildSnapshot()`，保证装备、内功与派生天赋在 UI 中能直接看到。
- 技能说明、物品/装备说明与 affix 说明当前由应用层 formatter 输出，供 Godot 侧 tooltip 与描述展示使用。
- 物品/装备说明 formatter 已按新版语义把使用效果与 affix 词条拆开，避免 Godot 侧继续手拼 legacy 文案。

## 背包与装备

- `Inventory` 是全局无限背包。
- 背包用有序 `InventoryEntry` 保存普通堆叠条目和独立装备实例条目。
- 普通物品可堆叠；没有实例级差异的装备也可堆叠。
- 带 `ExtraAffixes` 的装备以独立 `EquipmentInstanceInventoryEntry` 保存。
- `EquipmentInstanceFactory` 统一生成装备实例。
- 装备实例 ID 格式为 `{equipmentDefinition.Id}_{globalSequence:D8}`。
- `EquipmentInstanceFactory` 的 next sequence 属于 `GameState` 级存档状态，不属于 `Inventory`。
- `InventoryService` 当前支持从堆叠装备或独立装备实例装备到角色，装备同槽位新物品时会把旧装备退回背包，并在穿脱后重建角色 snapshot。
- `ItemUseService` 当前作为背包使用入口，支持装备、武学书、绝技书、天赋书和基础强化道具。消耗品、功能道具和剧情物品仍按未接入主动使用处理。
- 背包 UI 使用 `InventoryPanel` 展示分类、物品 tooltip 和目标选择；物品使用通过应用层服务执行，不在 Godot 侧直接修改背包或角色。

## 储物箱与周目

- `ChestState` 是 `GameState` 的一部分，内部复用 `Inventory` 保存箱内堆叠物品和独立装备实例。
- `ChestService` 负责打开储物箱、存入、取出和容量计算。
- 储物箱容量由 `GameConfig.ChestBaseCapacity + Adventure.Round * GameConfig.ChestPerRoundCapacity` 得出。
- 剧情物品当前不能放入储物箱。
- 储物箱变更会同时发布 `InventoryChangedEvent` 与 `ChestChangedEvent`。
- 下一周目通过 `SessionFlowService.StartNextRound()` 创建新 `GameState`，保留当前银两和储物箱内容，重新创建初始队伍。
- Godot 侧已有 `ChestPanel`，地图事件类型 `xiangzi` 会打开储物箱面板。

## 商店

- `ShopService` 当前支持打开商店、银两/元宝购买、限购记录、出售背包物品与独立装备。
- 商品购买会扣除 `CurrencyState`、写入 `ShopState`、发布货币/背包相关事件，并把商品加入背包。
- 出售价格当前按物品基础价格的 50% 向下取整，最低为 1；不可丢弃或无价格物品不可出售。
- `ShopPanel` 支持买入/卖出、分类筛选、快速买入和银两/元宝余额展示。
- 地图 `shop` 事件和剧情宿主命令 `shop` 已接入 `UIRoot.ShowShopPanel(...)`。

## 战斗原型

- `Game.Core/Battle` 当前提供第一版轻量战斗内核：
  - `BattleState`、`BattleGrid`、`BattleUnit`、`BattleBuffInstance` 保存战斗局内状态。
  - `BattleEngine` 负责半即时行动条推进、行动开始/结束、移动、移动回滚、技能释放、物品使用、休息、Buff 回合流转和 hook 触发。
  - `BattleDamageCalculator` 负责当前技能伤害试算。
  - `BattleEvent` 输出结构化战斗事件，Godot 战斗 UI 根据事件播放飘字与日志。
- 战斗行动当前按固定 11x4 格子预览，玩家单位来自出战选择，敌方/临时单位从 `BattleDefinition` 组装。
- 移动当前支持可达格计算、敌方控制区额外移动消耗和 `IgnoreZoneOfControl` trait。
- 技能当前复用角色已有技能实例，按资源消耗、冷却、射程、影响范围、伤害和 Buff 处理。
- 战斗内物品当前支持消耗品效果，并通过 `CanUseItemOnAlly` trait 扩展队友目标。
- `src/Game.Godot/UI/Battle` 当前提供战斗界面、棋盘、单位视图、技能栏、物品面板、飘字和 UI 状态机。
- 地图 `battle` 事件和剧情 battle 命令当前会先打开出战选择，再进入 `BattleScreen`，不再使用胜负确认弹窗模拟。
- `assets/animation`、`assets/art/atlas`、`assets/art/atlas_texture` 保存由 legacy 资源导入的战斗单位/技能动画与图集资源；`AssetResolver` 统一加载角色模型动画和技能动画。
- `tools/LegacyAnimationImporter` 是当前 legacy 战斗动画资源导入工具。

## 地图与剧情

- `MapService.EnterMap(...)`
  - 切换当前地图，记录大地图当前位置，发布 `MapChangedEvent`。
- `MapService.InteractWithLocation(...)`
  - 处理地图点位事件。
  - 大地图移动会按距离推进时辰；点位交互额外消耗 1 个时辰。
  - 当前支持 `map`、`story`、`shop`、`xiangzi`、`battle` 和占位交互结果。
- `MapConditionEvaluator`
  - 处理地图事件条件，包括剧情完成状态、时间段等。
- `StoryService.RunAsync(...)`
  - 应用层剧情入口，按 story segment id 从内容仓储查找剧本并执行。
  - segment 完成后写入 `StoryState`。
- `StoryCommandLineService`
  - 支持把一行文本解析成剧情命令并直接执行。
  - 当前由系统面板内置控制台接入，方便在预览环境里触发内建命令或宿主命令。
- 正式 story JSON 中对白应使用 `kind == "dialogue"`；旧 `dialog` 命令不作为应用层内建命令承载普通对白。
- 开局问卷的难度选择会通过 `set_game_mode` 写入 `AdventureState.Difficulty`。
- `StoryCommandDispatcher`
  - 当前通过 `StoryCommandAttribute` + `StoryCommandBinder` 绑定内建命令。
  - 当前内建处理物品、货币、日志、剧情变量、冒险状态、人物成长、入队/跟随/离队、学习、移除技能、称号解锁等剧情命令。
  - `log` 会把日志条目追加到 `GameState.Journal`。
  - `set_time_key key limitDays targetStoryId` 会登记限时剧情 key；`clear_time_key key` 会取消登记。
  - `change_female_name` 默认操作角色 id `女主`；如果该角色不在 `Party` 名册中，会创建角色实例并放入 `Reserves` 后再改名。
- `StoryTextInterpolator`
  - 当前在应用层对白/选项进入宿主前处理 `$MALE$` 与 `$FEMALE$`。
  - 只解析主角和女主显示名，优先查 `Party` 全名册，其次查角色 definition；未知占位符保持原文本。
- `StoryTimeKeyExpirationService`
  - 根据当前 `ClockState` 检查未触发且已到截止时间的 story time key。
  - 到期后标记 `Triggered`、发布 `StoryStateChangedEvent`，并返回要执行的目标 story id。
- `StoryVariableResolver`
  - 当前把 `money` / `silver` / `gold` / `yuanbao` 直接投影到 `GameState.Currency`。
  - 当前把 `round` / `game_mode` 投影到 `GameState.Adventure`。
- `GodotStoryRuntimeHost`
  - 负责剧情等待式 UI 和宿主表现命令。
  - 当前支持对话、选项、音乐、音效、背景、提示、toast 开关、震屏、头像/模型、门派选择、命名、头像选择、roll 点、`map` 场景动作、`shop` 商店面板、出战选择、战斗界面、返回主菜单、重开、下一周目和 game over。

## Godot 宿主

- `PreviewGameBootstrap`
  - 通过 `GodotContentPackageLoader` 从 `res://data` 加载内容，并从 `game-config.json` 读取 `GameConfig`。
  - 当前预览初始队伍包含 `主角`、`阿青`、`华山郭襄`。
  - 用 `GodotStoryRuntimeHost` 创建 `GameSession`。
  - 初始化 `Game` 并把 `UIRoot` 绑定到 session events。
  - 同时把 `TimedStoryCoordinator` 绑定到当前 session events。
  - 启动后会尝试恢复本地全局档案。
- `GameFlow`
  - 负责主菜单、新游戏、下一周目和回主菜单的宿主级流程。
  - 新游戏/下一周目会运行 `Game.Config.InitialStorySegmentId` 指向的开局剧情。
- `MainMenu`
  - 当前作为 Godot 主场景入口，启动时初始化 preview session、隐藏 HUD、播放主菜单 BGM。
  - 支持新游戏、读档和音乐欣赏。
- `World`
  - 当前场景承载者。
  - 负责切换地图屏幕、设置背景、震屏和剧情动画占位。
  - 当前挂载 `TimedStoryCoordinator`，用于监听时间推进后触发限时剧情。
- `UIRoot`
  - 持有 HUD、面板层、模态层和 overlay 层。
  - 监听 `SessionEvents` 刷新 HUD。
  - 提供等待式 `ShowDialogueAsync(...)` 与 `ShowChoicesAsync(...)`。
  - 当前还负责 toast、hint、英雄面板、系统面板、失败界面和存档槽选择面板。
  - 当前还负责背包面板、商店面板、出战选择、战斗界面和装备选择模态面板的挂载。
  - 当前还负责储物箱面板的挂载。
  - 对话面板中的长文本当前支持鼠标滚轮滚动，文本区左键和空白区左键都可继续对白。
  - 当前已接入江湖日志面板入口。
  - 当前角色详情入口已切到 C# 版 `CharacterPanel`，并内嵌属性、装备、技能、天赋、传记五个 tab。
  - 角色面板当前支持在当前队伍内快速切换上一位/下一位角色。
  - 属性页当前已接入未分配属性点展示和手动加点。
  - 技能页当前已接入外功激活、绝技激活和内功装备切换；招式项仍是展示项。
  - 装备页当前按现有领域模型显示 `weapon`、`armor`、`accessory` 三个槽位，支持点击选择背包装备和右键卸装。
  - 系统面板当前包含本地设置、剧情命令行控制台、存档和读档入口。
  - 存档槽选择面板当前支持 4 个本地存档槽，并在写入存档时同步写入全局档案。
- `HudPanel`
  - 展示当前地图、世界时间、货币和主角头像。
  - 当前按钮可打开英雄面板、队伍面板、背包面板、日志面板和系统面板。
  - 队伍面板当前支持拖拽调整非主角成员顺序；主角固定在首位。
  - 世界时间统一走 `ClockFormatter.FormatDateTimeCn(...)`，当前格式为 `江湖一年一月一日 午时`。
- `MapScreen`
  - 当前支持剧情播放模式显示切换。
  - 剧情播放时会隐藏交互点、主角标记、大地图云层和小地图底部信息区。
  - 当前会把地图商店事件转发到商店面板，把 `xiangzi` 事件转发到储物箱面板，把 `battle` 事件转发到出战选择和战斗界面。
  - 当前“进入地图后的自动触发事件”消费编排暂时也在这里，后续应上移到 `World` 或专门的宿主 flow coordinator，收回 `MapScreen` 的职责边界。
- `TimedStoryCoordinator`
  - 监听 `ClockChangedEvent` 与 `SaveLoadedEvent`。
  - 检查到期 story time key 后排队运行目标剧情，等待当前剧情表现结束后再执行，并在地图场景中刷新当前地图。
- `AssetResolver`
  - 底层资源加载收敛为两类：
    - `LoadTextureResource(...)`
    - `LoadAudioResource(...)`
  - 角色头像、说话人展示等是语义包装，最终仍落到纹理加载。
  - 说话人展示会优先查整个 `Party` 名册，因此后备池角色的改名和头像也能用于剧情对白。
  - 支持内容 resource id、`res://...`、`assets/...`、`art/...`、`audio/...` 和常见扩展名回退。
  - 当前还支持从 `assets/animation/combatant` 与 `assets/animation/skill` 加载角色战斗动画库和技能动画库。
- `AudioManager`
  - 管理 BGM 和 SFX。
  - 单首 BGM 播放完会循环当前曲目。
  - 多 BGM 播放池会随机播放，单曲结束后继续从池子随机挑下一首，并避免连续重复同一首。

## 会话事件

`GameSession.Events` 当前发布这些应用事件：

- `MapChangedEvent`
- `ClockChangedEvent`
- `CurrencyChangedEvent`
- `AdventureStateChangedEvent`
- `InventoryChangedEvent`
- `ChestChangedEvent`
- `ItemAcquiredEvent`
- `ToastRequestedEvent`
- `PartyChangedEvent`
- `CharacterChangedEvent`
- `CharacterLeveledUpEvent`
- `JournalChangedEvent`
- `StoryStateChangedEvent`
- `SaveLoadedEvent`
- `ProfileChangedEvent`
- `ProfileLoadedEvent`
- `AchievementUnlockedEvent`

Godot 侧当前主要由 `UIRoot`、HUD、角色面板、英雄面板、储物箱面板和 `TimedStoryCoordinator` 订阅，用于刷新 HUD、弹 toast、更新全局档案展示和触发限时剧情。后续复杂 UI 应优先通过 Presenter 收敛显示逻辑；状态联动足够复杂时再考虑 MVVM。

## 当前缺口

- 战斗系统仍是第一版原型；AI 行动、正式战场结算、胜负回写、奖励/失败投影和完整演出控制器还未正式建模。
- 内容加载还不是真二阶段。
- affix 引用解析入口仍需继续收敛。
- `Party` 和 `Inventory` 当前都是全局模型；多队伍、多编队、多背包归属还未建模。
- 地图对象、交互物、事件系统、演出系统和地图会话层仍待正式建模。
- 进入地图自动触发事件当前由 `MapScreen` 参与消费，职责边界还不够干净；后续应把这部分宿主编排从 screen 上移。
- 背包当前只接入装备、武学书、绝技书、天赋书和基础强化道具；普通消耗品、功能道具、剧情物品的主动使用仍未完整建模。
- 商店当前已接入基础买卖流程；更完整的商店规则、商品刷新、回购和战斗入口仍未建模。
- 剧情时间 key 当前只按天数与世界时辰做简单到期触发；更完整的限时任务、失败回滚、优先级和触发上下文仍未建模。
- 技能切换已落地属性页加点、外功/绝技激活、内功装备切换，但完整正式方案仍未完全覆盖所有技能管理需求；设计草案见 `docs/character-skill-switching-design.md`。

更多细项见 `TODO.md`。

## 常用命令

```powershell
dotnet test
dotnet build engine-free-rpg.csproj
```

## 参考文档

- `TODO.md`
- `docs/map-migration-design.md`
- `docs/battlefield-system-design.md`
- `docs/battle-float-text-and-speech-style.md`
- `docs/attachment-design.md`
- `docs/battle-effect-phase-hook-design.md`
- `docs/effect-trigger-model-comparison.md`
- `docs/character-skill-switching-design.md`
