# 会话续接摘要

除非用户明确要求，不要访问当前工作区以外的文件。

遵循第一性原理，且不做补丁性、兼容性修改。

## 当前项目定位

- 这是一个基于 `.NET 10` 与 `Godot 4.6` 的 2D 半即时制战棋 RPG 内核原型。
- 仓库根目录就是 Godot 工程根。
- 根目录 `engine-free-rpg.csproj` 是当前唯一 Godot 宿主程序集项目，只编译 `src/Game.Godot/**/*.cs`，并引用 `Game.Content` 与 `Game.Application`。
- 当前重点是：
  - 角色与技能规则内核
  - 轻量 affix 投影
  - 轻量战斗原型
  - JSON 内容装配
  - 剧本解释器与剧情状态
  - 地图进入与点位交互
  - 持久化模型
  - 应用层会话与会话事件
  - Godot 宿主层、HUD、地图、背包、商店、战斗 UI、剧情 UI、音频接线
- 当前已接入第一版轻量战斗内核与 Godot 战斗界面；正式战场系统后续仍按 `docs/battlefield-system-design.md` 继续重建。

## 当前目录与数据位置

- `data`
  - 正式 JSON 内容包。Godot 侧通过 `res://data` 加载。
  - 不要再把正式内容放回 `src/Game.Content/Data`。
- `assets`
  - Godot 资源目录，当前主要是 `art`、`audio`、战斗动画库与图集纹理。
- `autoload`
  - Godot autoload 场景，包括 `World`、`UIRoot`、`AudioManager`。
- `scenes`
  - 当前 Godot 主场景、地图场景、HUD、剧情 UI、队伍 UI 等。
- `src/Game.Core`
  - 领域模型、定义模型、角色状态、技能实例、背包/装备实例、affix 投影、轻量战斗状态/引擎、剧情运行时、存档记录。
- `src/Game.Content`
  - JSON 内容输入、引用校验、runtime definition 装配、内存仓储。
  - `SampleData` 只保留测试样例内容。
- `src/Game.Application`
  - 应用态会话、全局档案、存读档、角色服务、背包服务、物品使用服务、商店服务、地图服务、剧情服务、剧情命令行、诊断日志抽象、会话事件。
- `src/Game.Godot`
  - Godot 宿主适配层源码，由根目录 `engine-free-rpg.csproj` 编译。
  - 当前还包含 Godot 内容包加载、本地存档/档案持久化适配 `src/Game.Godot/Persistence`、主菜单、失败界面、储物箱 UI 和战斗 UI。
- `legacy_scenes`
  - 原 Godot/GDScript 参考场景，不是当前主运行路径。
- `jyx-legacy-data` / `jyx-legacy-dll`
  - 原版参考资源，通过 git submodule 挂载。

## 当前分层边界

- `Game.Core`
  - 不依赖 Godot。
  - 可以持有内容仓储抽象 `IContentRepository`，但不直接依赖具体 loader。
  - 存放定义、运行时状态、存档记录、剧情 runtime。
- `Game.Content`
  - 负责把 JSON 装配成 `InMemoryContentRepository`。
  - 负责 definition 引用解析、affix 引用集中解析和仓储级校验。
- `Game.Application`
  - 负责用例级服务和应用态会话。
  - 服务显式接收 `GameSession`，业务代码通过私有转发属性访问 `State`、`ContentRepository` 或其他服务。
- `Game.Godot`
  - 薄宿主层，负责 Godot 节点、场景、资源、音频、UI 与宿主表现。
  - Godot 侧业务日志统一经 `Game.Logger` 输出；运行期代码不要直接调用 `GD.Print` / `GD.PushError`。

## 当前运行时与存档约束

- 运行时对象可以直接持有已解析 definition 引用：
  - `CharacterInstance.Definition`
  - 技能运行时实例的 `Definition`
  - `EquipmentInstance.Definition`
  - `InventoryEntry` 中的 `ItemDefinition` / `EquipmentDefinition`
- 存档层只保存稳定 ID 和实例状态，不保存 runtime definition 引用。
- runtime definition 应尽量在内容装配阶段拿到已解析引用，不在高频路径反复按字符串查仓储。
- `GameState` 是当前可存档的大状态，持有：
  - `Adventure`
  - `Party`
  - `Inventory`
  - `Chest`
  - `EquipmentInstanceFactory`
  - `Currency`
  - `Clock`
  - `Location`
  - `MapEventProgress`
  - `Shop`
  - `Story`
  - `Journal`
- `GameState` 不使用业务构造函数，创建后通过显式 setter 装配状态。
- `AdventureState` 是当前周目的运行态上下文，属于 `GameState`，当前持有：
  - `Round`
  - `Difficulty`
  - `SectId`
  - `Morality`
  - `Favorability`
  - `Rank`
- `AdventureState.Difficulty` 当前使用稳定模式 id：
  - `normal`
  - `hard`
  - `crazy`
- `GameProfile` 是当前全局档案状态，不属于单个 `SaveGame` 槽位。
- `GameProfile` 当前持有：
  - 已解锁称号
  - 累计死亡数
  - 累计击杀数
- `SaveGame` 是单个存档槽数据。
- `SaveGame` 不保存 `GameProfile`；全局档案单独持久化为 `GameProfileRecord`。
- 读档由 `SaveGameService.LoadSave(...)` 恢复各子状态，创建新的 `GameState`，然后调用 `GameSession.ReplaceState(...)`。
- 读全局档案由 `ProfileService.LoadProfile(...)` 恢复 `GameProfile`，然后调用 `GameSession.ReplaceProfile(...)`。
- 当前阶段服务私有字段视为非存档运行态；读档只替换 `GameSession.State`，读档案只替换 `GameSession.Profile`，不额外重建或刷新服务。

## 当前会话与宿主入口

- `GameSession` 是普通类，不是单例。
- 不要把 `GameSession` 改成静态单例类，除非用户明确要求。
- `GameSession` 当前持有：
  - `State`
  - `Profile`
  - `Config`
  - `ContentRepository`
  - `SaveGameService`
  - `ProfileService`
  - `SessionFlowService`
  - `PartyService`
  - `CharacterService`
  - `InventoryService`
  - `ChestService`
  - `ItemUseService`
  - `ShopService`
  - `MapService`
  - `StoryTimeKeyExpirationService`
  - `StoryService`
  - `SessionEvents`
- `Game` 是 Godot 宿主层全局入口，当前转发：
  - `Session`
  - `State`
  - `Profile`
  - `Config`
  - `ContentRepository`
  - `SaveGameService`
  - `ProfileService`
  - `SessionFlowService`
  - `PartyService`
  - `CharacterService`
  - `InventoryService`
  - `ChestService`
  - `ItemUseService`
  - `ShopService`
  - `MapService`
  - `StoryService`
  - `Audio`
  - `Logger`
- `Game.Initialize(...)` 只接收已构造的 `GameSession` 和 logger；`GameConfig` 从 `GameSession.Config` 读取。它属于宿主启动装配，不属于业务流程调用点。
- `GameConfig` 当前从 `data/game-config.json` 读取，并挂在 `GameSession` 上，承载开局剧情、初始队伍、储物箱容量、角色/技能上限和随机战斗音乐池等预览运行参数。
- `SessionFlowService` 当前负责新游戏与下一周目状态切换；下一周目保留银两和储物箱内容，周目数加 1，再按 `GameConfig.InitialPartyCharacterIds` 重建初始队伍。
- `NewGameStateFactory` 当前集中创建新 `GameState`；初始化角色会调用 `LevelUpAllSkillsMaxLevel()` 把已有技能提到各自 `MaxLevel`。
- `PreviewGameBootstrap` 当前通过 `GodotContentPackageLoader` 从 `res://data` 加载内容，读取 `game-config.json`，创建初始 `GameSession`，把 `UIRoot` 与 `TimedStoryCoordinator` 绑定到 session events，并尝试加载本地全局档案。

## 当前内容加载设计

- 正式内容默认按目录中的多个 JSON 文件加载，不再依赖单个大内容包文件。
- 正式内容目录位于仓库根 `data`，Godot 侧通过 `res://data` 加载。
- `src/Game.Content/SampleData` 只保留测试样例内容。
- `Game.Content.csproj` 通过链接根目录 `data/**/*.json` 把正式内容拷贝到输出目录。
- `JsonContentLoader` 入口包括：
  - `LoadFromDirectory(...)`
  - `LoadFromFile(...)`
  - `LoadFromPackage(...)`
- 当前会加载 `story/*.story.json`，并把 segment id 建入内容仓储。
- `ContentPackage` 当前是公开类型，允许 Godot 宿主先用自身文件 API 组装内容包，再交给 `JsonContentLoader.LoadFromPackage(...)` 构建仓储。
- Godot 运行时通过 `GodotContentPackageLoader` 从 `res://data` 读取正式内容，避免导出后依赖 `ProjectSettings.GlobalizePath(...)` 和普通文件系统路径。
- 顶层 JSON 使用 `System.Text.Json` 直接反序列化到 runtime definition；当前不维护整套 `XxxDto -> XxxDefinition`。
- `FormSkillDefinition.Icon` 当前允许为空；legacy 转换脚本不再为招式输出空 icon 字段。
- 商店 JSON 当前支持 `price` 与 `premiumPrice`；旧 `contentId == "元宝"` 与 `*残章` 商品仍会被校验和运行时商店视图忽略。
- `BuildRepository(...)` 负责索引 definition、解析引用、调用 definition `Resolve(...)`、建立 story segment 索引。
- `ValidateRepository(...)` 负责仓储级校验。
- 当前 story JSON 解析是同步的，避免 Godot 主线程启动时出现 sync-over-async 卡死。
- 当前 definition 依赖按有向无环图处理，还不是真二阶段加载。
- 如果后续需要支持定义间循环依赖，应改成：
  - 先注册 runtime definition 空壳
  - 再统一 resolve 引用
- 当前 affix 引用解析集中在 `JsonContentLoader.ResolveAffixes(...)`，新增 affix 来源时必须确认不会漏掉 `GrantTalentAffix.Resolve(...)` 等需要仓储引用的条目。
- `IContentRepository` 当前同时提供 `GetXxx(...)` 与 `TryGetXxx(...)`；业务规则按语义选择“缺失即失败”或“缺失回退”。
- `MapLocationDefinition.Name` 当前允许为空；Godot 地图按钮显示时优先用 `Name`，否则按 location id 通过资源/角色名解析回退。

## 当前角色、技能与 Affix 状态

- `CharacterDefinition`
  - 静态角色模板定义。
- `CharacterInstance`
  - 持久化角色实例，持有自身 definition 引用。
- `CharacterInstance` 当前还持有：
  - `Level`
  - `Experience`
  - `UnspentStatPoints`
  - `EquippedInternalSkillId`
- 当前技能运行时中心：
  - `ExternalSkillInstance`
  - `InternalSkillInstance`
  - `FormSkillInstance`
  - `SpecialSkillInstance`
- `ExternalSkillInstance` / `SpecialSkillInstance` 当前持有显式激活状态。
- `InternalSkillInstance` 当前通过 `CharacterInstance.EquippedInternalSkillId` 表达装备态。
- 技能实例当前默认上限是 `SkillInstance.DefaultMaxLevel = 20`。
- 技能实例的 `Level` / `Exp` 当前是运行时可变状态；默认 `MaxLevel` 为 `SkillInstance.DefaultMaxLevel`。
- `CharacterService` 当前负责：
  - 属性加点
  - 经验获取与升级
  - 学习外功/内功/天赋/绝技
  - 外功激活切换
  - 绝技激活切换
  - 内功装备切换
- `PartyService` 当前负责：
  - 当前队伍顺序调整
  - 入队与跟随切换
  - 当前队伍或跟随角色离队时移入后备池
  - 从后备池重新入队或跟随时复用已有角色实例
  - 名册内角色改名，或创建角色实例到后备池后改名
  - 队伍名册变更后的 `PartyChangedEvent` 发布
- 升级经验与显示进度当前集中在 `CharacterLevelProgression`。
- 当前代码保留的是轻量 `AffixDefinition` 系统，不是旧 attachment / modifier / battle hook 运行实现。
- 装备实例、天赋、Buff 通过 `IAffixProvider` 暴露普通 affix。
- `EquipmentInstance.Affixes` 由装备 definition affix 与实例级 `ExtraAffixes` 合并得到。
- 外功、内功通过 `SkillAffixDefinition` 暴露技能 affix：
  - `minimumLevel`
  - `requiresEquippedInternalSkill`
  - `effect`
- `CharacterInstance.RebuildSnapshot()` 从角色自身收集当前生效 affix：
  - 装备实例
  - 已解锁天赋
  - 满足等级条件的外功 affix
  - 满足等级与装备条件的内功 affix
- `AffixResolver` 负责把来源 affix 转成 active affix，并调用 `TalentResolver` 展开 `GrantTalentAffix`。
- 外功启用状态当前不影响外功被动 affix。
- 内功 affix 默认不要求装备；只有 `requiresEquippedInternalSkill` 为 `true` 时才要求当前装备。
- `WeaponType` 当前统一定义在 `Game.Core.Model.Enums`，技能与 affix 共用同一枚举。

## 当前背包、装备与队伍状态

- `Party` 当前是全局伙伴名册，无队伍 id/name。
- `Party` 当前分为三个角色池：
  - `Members`：当前队伍成员。
  - `Followers`：随队但不在当前编队的角色。
  - `Reserves`：入过队或跟随过、当前离队的角色。
- 同一个 `CharacterInstance` 只能处于 `Members`、`Followers`、`Reserves` 其中一个池子。
- `join` / `follow` 会优先从其他池子移动已有实例，不会重新创建角色。
- `leave` / `leave_follow` / `leave_all` 会把角色移入 `Reserves`，不丢弃角色成长、装备和技能状态。
- `PartyRecord` 保存三组角色 ID；`SaveGame.Characters` 覆盖整个 `Party` 名册。
- `Inventory` 当前是全局无限背包。
- 背包使用有序 `InventoryEntry` 保存普通堆叠条目和独立装备实例条目。
- 普通物品可堆叠；没有实例级差异的装备也可堆叠。
- 带实例级 `ExtraAffixes` 的装备以独立条目保存。
- `EquipmentInstanceFactory` 统一生成装备实例，实例 ID 格式为 `{equipmentDefinition.Id}_{globalSequence:D8}`。
- `EquipmentInstanceFactory` 的 next sequence 属于 `GameState` 级存档状态，不属于 `Inventory`。
- `InventoryService` 当前装备同槽位物品时会把旧装备退回背包，并在穿脱后重建角色 snapshot。
- `ItemUseService` 当前负责背包主动使用入口：
  - 装备
  - 武学书
  - 绝技书
  - 天赋书
  - 基础强化道具
- 普通消耗品、功能道具、剧情物品仍不作为主动使用效果处理。
- `ShopState` 当前记录商店购买数量，参与 `SaveGame` 存读档。
- `ShopService` 当前负责打开商店、银两/元宝购买、限购记录、出售背包物品。
- `ChestState` 当前是 `GameState` 的一部分，内部复用 `Inventory` 保存箱内堆叠物品和独立装备实例。
- `ChestService` 当前负责打开储物箱、存入、取出和容量计算。
- 储物箱容量由 `GameConfig.ChestBaseCapacity + Adventure.Round * GameConfig.ChestPerRoundCapacity` 得出。
- 剧情物品当前不能放入储物箱。
- 储物箱变更会同时发布 `InventoryChangedEvent` 与 `ChestChangedEvent`。
- 下一周目会保留当前银两和储物箱内容。
- 如果后续支持多队伍/多编队，需要重新建模队伍集合、队伍标识、背包归属与存档结构。

## 当前战斗原型

- `Game.Core/Battle` 当前提供第一版轻量战斗内核：
  - `BattleState`
  - `BattleGrid`
  - `BattleUnit`
  - `BattleBuffInstance`
  - `BattleEngine`
  - `BattleDamageCalculator`
  - `BattleEvent`
- `BattleEngine` 当前负责：
  - 半即时行动条推进
  - 行动开始/结束
  - 移动与移动回滚
  - 技能释放
  - 战斗内物品使用
  - 休息
  - Buff 回合流转
  - hook 触发
- 战斗行动当前按固定 11x4 格子预览。
- 玩家单位来自出战选择，敌方/临时单位从 `BattleDefinition` 组装。
- 移动当前支持可达格计算、敌方控制区额外移动消耗和 `IgnoreZoneOfControl` trait。
- 技能当前复用角色已有技能实例，按资源消耗、冷却、射程、影响范围、伤害和 Buff 处理。
- 战斗内物品当前支持消耗品效果，并通过 `CanUseItemOnAlly` trait 扩展队友目标。
- `BattleEvent` 输出结构化战斗事件，Godot 战斗 UI 根据事件播放飘字与日志。
- `src/Game.Godot/UI/Battle` 当前提供战斗界面、棋盘、单位视图、技能栏、物品面板、飘字和 UI 状态机。
- 地图 `battle` 事件和剧情 battle 命令当前会先打开出战选择，再进入 `BattleScreen`，不再使用胜负确认弹窗模拟。
- `assets/animation`、`assets/art/atlas`、`assets/art/atlas_texture` 保存由 legacy 资源导入的战斗单位/技能动画与图集资源。
- `AssetResolver` 当前支持从 `assets/animation/combatant` 与 `assets/animation/skill` 加载角色战斗动画库和技能动画库。
- `tools/LegacyAnimationImporter` 是当前 legacy 战斗动画资源导入工具。
- 战斗系统仍是原型；AI 行动、正式战场结算、胜负回写、奖励/失败投影和完整演出控制器还未正式建模。

## 当前地图与剧情接线

- `MapService.EnterMap(...)`
  - 切换当前地图。
  - 大地图会记忆当前位置。
  - 发布 `MapChangedEvent`。
- `MapService.InteractWithLocation(...)`
  - 处理地图点位事件。
  - 大地图移动按距离消耗时辰。
  - 点位交互额外消耗 1 个时辰。
  - 当前根据事件类型返回 `map`、`story`、`shop`、`xiangzi`、`battle` 或占位结果。
- `MapConditionEvaluator`
  - 当前支持剧情完成状态、时间段、概率等地图条件。
- 地图事件中 `type == "story"` 当前已在 `MapScreen` 接到 `Game.StoryService.RunAsync(...)`。
- 地图事件中 `type == "shop"` 当前已在 `MapScreen` 接到 `UIRoot.ShowShopPanel(...)`。
- 地图事件中 `type == "xiangzi"` 当前已在 `MapScreen` 接到 `UIRoot.ShowChestPanel()`。
- 地图事件中 `type == "battle"` 当前已在 `MapScreen` 接到出战选择与 `UIRoot.ShowBattleScreenAsync(...)`。
- `StoryService`
  - 应用层剧情执行入口，固定注入 `IRuntimeHost`。
  - segment 完成后写入 `GameState.Story`。
- `StoryService.CommandLine`
  - 当前暴露剧情命令行执行入口。
- 正式 story JSON 中对白应使用 `kind == "dialogue"`；旧 `dialog` 命令不作为应用层内建命令承载普通对白。
- 开局问卷的难度选择会通过 `set_game_mode` 写入 `AdventureState.Difficulty`。
- `StoryCommandDispatcher`
  - 当前通过 `StoryCommandAttribute` + `StoryCommandBinder` 绑定内建命令。
- 剧情命令当前除 `log` 外，还已接入：
  - `item`
  - `cost_item`
  - `get_money`
  - `cost_money`
  - `yuanbao`
  - `cost_day`
  - `set_flag`
  - `clear_flag`
  - `set_time_key`
  - `clear_time_key`
  - `daode`
  - `haogan`
  - `rank`
  - `menpai`
  - `upgrade`
  - `grant_point` / `get_point`
  - `grant_exp` / `get_exp`
  - `levelup`
  - `change_female_name`
  - `join`
  - `follow`
  - `leave`
  - `leave_follow`
  - `leave_all`
  - `learn`
  - `remove`
  - `maxlevel`
  - `nick`
  - `set_round`
  - `set_game_mode`
  - `minus_maxpoints`
  - `growtemplate`
- 剧情命令 `log`
  - 当前由 `StoryCommandDispatcher` 内建处理。
  - 追加到 `GameState.Journal`，并保存当前 `ClockRecord` 时间快照。
- 剧情命令 `set_time_key`
  - 参数为 `key`、限制天数、目标 story segment id。
  - 会校验目标 segment 存在，并把限时剧情 key 写入 `GameState.Story.TimeKeys`。
- 剧情命令 `clear_time_key`
  - 按 key 移除尚未触发或已登记的限时剧情 key。
- `nick`
  - 当前按资源组 `nick` 校验称号资源，并通过 `ProfileService` 解锁全局称号。
- `change_female_name`
  - 默认角色 id 为 `女主`。
  - 如果 `女主` 不在 `Party.Members`、`Party.Followers` 或 `Party.Reserves` 中，会创建角色实例并放入 `Party.Reserves` 后再改名。
  - 已存在时只改名，不改变其当前池子。
- `Game.Core/Story`
  - 当前已有轻量剧本解释器、表达式求值、JSON parser 与 runtime session。
- `Game.Application`
  - 当前已有：
    - `StoryService`
    - `StoryConditionEvaluator`
    - `StoryCommandDispatcher`
    - `StoryCommandLineService`
    - `StoryVariableResolver`
    - `ApplicationStoryRuntimeHost`
    - `NullRuntimeHost`
- `StoryVariableResolver`
  - 当前把 `money` / `silver` / `gold` / `yuanbao` 投影到 `GameState.Currency`。
  - 当前还把 `round` / `game_mode` 投影到 `GameState.Adventure`。
- `StoryTextInterpolator`
  - 当前在应用层对白/选项进入宿主前处理 `$MALE$` 与 `$FEMALE$`。
  - 只解析主角和女主显示名，优先查 `Party` 全名册，其次查角色 definition；未知占位符保持原文本。
- `StoryTimeKeyExpirationService`
  - 根据当前 `ClockState` 检查未触发且已到截止时间的 story time key。
  - 到期后标记 `Triggered`、发布 `StoryStateChangedEvent`，并返回要执行的目标 story id。
- `Game.Godot/Story/GodotStoryRuntimeHost`
  - 当前负责：
    - UI 等待：`DialogueAsync`、`ChooseOptionAsync`
    - 宿主表现：`music`、`effect`、`background`、`suggest`、`toast`、`shake`、`head`、`animation`
    - 开局宿主 UI：`select_menpai` / `select_sect`、`input_name`、`select_head`、`roll_stats`
    - 场景动作：`map`、`shop`
    - 宿主流程：出战选择、战斗界面、`mainmenu`、`restart`、`nextzhoumu`、`gameover`
- 剧情命令 `map` 当前不由应用层内建处理，而是由 `GodotStoryRuntimeHost` 调 `Game.MapService.EnterMap(...)` 后驱动 `World`。
- 剧情命令 `shop` 当前不由应用层内建处理，而是由 `GodotStoryRuntimeHost` 打开商店面板并等待面板关闭。

## 当前 Godot UI、资源与音频约束

- Godot UI 当前优先使用轻量 MVC / 面板脚本式写法。
- 创建 Godot 场景时，如果脚本需要获取节点，目标节点优先设置 `unique_name_in_owner = true`，并优先通过唯一节点名访问。
- 只有节点层级本身是语义边界时，才使用固定 `NodePath`。
- Godot UI 复杂后，优先把显示逻辑抽到 Presenter；当状态很多、联动很多、派生很多、同步很烦时，再考虑 MVVM。
- 只在必要时才引入 `XxxVm` / `XxxViewModel` 类，不默认为每个面板建立 VM。
- `UIRoot`
  - 持有 HUD、面板层、模态层和 overlay 层。
  - 提供 `ShowDialogueAsync(...)`、`ShowChoicesAsync(...)`、`ShowSuggestion(...)`。
  - 当前还提供 `ShowToast(...)`、`ShowConfirmAsync(...)`、`ShowHeroPanel()`、`ShowInventoryPanel()`、`ShowShopPanel(...)`、`ShowSystemPanel()`、`ShowSaveSlotSelectionPanel(...)`。
  - 当前还提供 `ShowChestPanel()` 与 `ShowGameOverScreen()`。
  - 当前还提供 `ShowCombatantSelectPanelAsync(...)` 与 `ShowBattleScreenAsync(...)`。
  - 订阅 `SessionEvents` 后刷新 HUD。
  - 对话面板的长文本当前由 `RichTextLabel` 自行处理滚轮滚动；文本区左键与空白区左键都可继续对白。
- `UIRoot` 当前会订阅物品获得、角色升级、称号解锁、档案变化等事件，用于 toast 与全局档案持久化。
- 江湖日志面板
  - 当前已迁移到 `scenes/ui/journal` + `src/Game.Godot/UI/Journal`。
  - HUD 的日志按钮已接到 `UIRoot.ShowGameLogPanel()`。
- 英雄面板
  - 当前已迁移到 `scenes/ui/hero_panel` + `src/Game.Godot/UI/Hero`。
  - 当前展示江湖进度、道德/好感、称号列表与完成度。
- 系统面板
  - 当前已迁移到 `scenes/ui/system_panel` + `src/Game.Godot/UI/System`。
  - 当前提供设置项、本地存档/读档/删档入口与剧情命令行控制台。
- 存档槽选择面板
  - 当前支持 4 个本地存档槽。
  - 写存档时会同步写入全局档案；读档时会优先从独立 profile 文件恢复全局档案。
  - 当前支持 `Save` / `Load` / `Delete` 三种模式。
  - 当前支持“删除、覆盖存档无需确认”复选框。
- 储物箱面板
  - 当前已迁移到 `scenes/ui/chest_panel` + `src/Game.Godot/UI/Chest`。
  - 当前支持背包与储物箱之间存取普通物品和独立装备实例。
  - 当前通过 `ChestService` 执行规则，不在 Godot 侧直接改 `Inventory`。
- 主菜单与失败界面
  - 主菜单当前是 Godot 主场景入口，启动时初始化 preview session、隐藏 HUD、播放主菜单 BGM。
  - 主菜单支持新游戏、读档和音乐欣赏。
  - 失败界面会显示全局档案累计死亡数，支持重开、读档和退出。
- 提示与 toast
  - 当前分别由 `HintBox` 与 `ToastPanel` 提供。
  - toast 背景资源当前位于 `assets/art/ui/toast.png`。
  - 确认弹窗当前由 `ConfirmDialog` 提供。
- 剧情播放模式
  - 当前会隐藏 HUD。
  - 地图场景会隐藏交互点、主角标记、大地图云层和小地图底部信息区。
- HUD 世界时间统一通过 `ClockFormatter.FormatDateTimeCn(...)` 输出，当前格式为 `江湖一年一月一日 午时`。
- `AssetResolver` 的底层职责应收敛为纹理、音频与动画库资源加载：
  - 纹理走 `LoadTextureResource(...)`
  - 音频走 `LoadAudioResource(...)`
  - 角色头像、说话人展示等可以保留为语义包装，但不要重新实现资源 id 解析、路径拼接、扩展名回退和 `ResourceLoader` 检查。
  - 说话人展示优先查整个 `Party` 名册，因此 `Reserves` 中的角色改名和头像也会用于剧情对白。
  - 战斗动画库当前走 `LoadCombatantAnimation(...)` 与 `LoadSkillAnimation(...)`，底层从 `assets/animation` 解析 Godot `AnimationLibrary`。
- `AudioManager.PlayBgm(IReadOnlyList<string>)` 是多 BGM 播放池入口；播放完一首后会从池子中随机挑下一首，并避免连续重复同一首。
- 单首 BGM 播放完后循环当前曲目。
- `TimedStoryCoordinator`
  - 当前挂在 `World` 下，并由 `PreviewGameBootstrap` 绑定当前 session。
  - 监听 `ClockChangedEvent` 与 `SaveLoadedEvent`，检查到期 story time key，等待当前剧情表现结束后排队运行目标剧情。
  - 限时剧情结束后，如果当前场景是地图，会刷新当前地图。

## 当前会话事件

- `GameSession.Events` 当前发布：
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
- `UIRoot` 当前主要监听：
  - `MapChangedEvent`
  - `ClockChangedEvent`
  - `CurrencyChangedEvent`
  - `AdventureStateChangedEvent`
  - `ItemAcquiredEvent`
  - `ToastRequestedEvent`
  - `CharacterLeveledUpEvent`
  - `SaveLoadedEvent`
  - `AchievementUnlockedEvent`
  - `ProfileChangedEvent`
  - `ProfileLoadedEvent`
- `ChestPanel` 当前监听：
  - `InventoryChangedEvent`
  - `ChestChangedEvent`
  - `SaveLoadedEvent`
- `TimedStoryCoordinator` 当前监听：
  - `ClockChangedEvent`
  - `SaveLoadedEvent`
- 新增会话事件时，应明确谁发布、谁订阅、是否需要读档后补刷新。

## 当前诊断日志

- 当前诊断日志抽象暂放在 `Game.Application`：
  - `IDiagnosticLogger`
  - `DiagnosticLoggerExtensions`
  - `ConsoleDiagnosticLogger`
  - `GodotDiagnosticLogger`
  - `NullDiagnosticLogger`
- 后续如果补 `Infrastructure` / `Hosting` 层，应迁移诊断日志抽象。

## 当前已知缺口

请先看：

- `TODO.md`

重点未解决项包括：

- `CharacterDefinition` 初始技能/装备表达待重审。
- 技能动态上限/精通突破还没有正式建模；当前只有默认上限与 `maxlevel` toast。
- 内容加载尚未升级为真二阶段。
- affix 引用解析入口需要继续收敛。
- 当前 `Party` 是全局伙伴名册，无队伍 id/name。
- 当前 `Inventory` 是全局背包。
- 地图、剧情与商店已初步接通；battle 已有第一版原型，但 AI 行动、正式战场结算、胜负回写、奖励/失败投影和完整演出控制器仍未完整接线。
- 剧情时间 key 当前只按天数与世界时辰做简单到期触发；更完整的限时任务、失败回滚、优先级和触发上下文仍未建模。
- 世界/地图层、地图交互对象、事件/剧本系统、演出系统、地图会话层仍待建模。
- 持久化层后续可能要补 `MapStateRecord`、`InteractableRecord`、`StoryFlagRecord`。
- 当前 DTO 与 runtime definition 存在较多重复字段，后续应收缩 DTO 噪音。
- 全局档案目前只覆盖称号和简单累计统计，更完整的 meta progression 还未建模。
- 背包主动使用只覆盖装备、武学书、绝技书、天赋书和基础强化道具；普通消耗品、功能道具、剧情物品仍未完整接线。

## 当前工作区习惯

- 手动代码编辑使用 `apply_patch`。
- 不要用 destructive git 命令。
- 不要回滚用户或其他流程造成的未提交改动。
- git 提交信息遵循 `Conventional Commits`。
- 若只做方案讨论，不要直接改代码。
- 用户明确要求实现或修改时再改代码。
- 如果涉及最新外部信息，先查再答。
- 常用验证：
  - `dotnet test`
  - `dotnet build engine-free-rpg.csproj`

## 当前 UI 迁移进度

- 角色面板当前已切到新的 `scenes/ui/character_panel/character_panel.tscn`。
- 角色面板已接入：
  - 属性 tab
  - 装备 tab
  - 技能 tab
  - 天赋 tab
  - 传记 tab
  - 当前队伍内上一位 / 下一位角色快速切换
- 装备 tab 已迁移到 `scenes/ui/character_panel/equipment_tab.tscn` + `src/Game.Godot/UI/Character/CharacterEquipmentTab.cs`。
- 属性页当前已接入未分配属性点展示与手动加点。
- 装备页当前按现有领域模型展示 3 个槽位，并支持点击选择背包装备、右键卸装：
  - weapon
  - armor
  - accessory
- 技能页当前已接入外功激活、绝技激活、内功装备切换；招式项仍是只读展示。
- 技能说明、物品/装备说明与 affix 说明当前走应用层 formatter 输出，Godot 侧不再手拼 legacy 文案。
- 物品/装备说明 formatter 当前已复用 legacy-dll 的核心表述方向，并按新版语义把使用效果与 affix 词条拆开输出。
- 天赋页当前使用现有 `UnlockedTalents` + `EffectiveTalents` 语义，不再迁回旧的武学常识点系统。
- 角色初始化与读档恢复后已重建 snapshot，内功/装备派生天赋应能直接在 UI 中显示。
- HUD 当前已接通：
  - 英雄按钮
  - 队伍按钮
  - 日志按钮
  - 系统按钮
  - `RunInfo` tooltip，会显示当前难度与周目
- 队伍面板当前支持拖拽排序：
  - 非主角成员可拖拽调整顺序
  - 主角固定在队伍首位
- 背包按钮当前已接入 `InventoryPanel`。
- 背包面板当前支持分类、tooltip、目标选择和应用层物品使用。
- 商店面板当前支持买入/卖出、分类、快速买入、银两/元宝价格、限购和商品 tooltip。
- 储物箱面板当前支持背包与箱内物品互相转移，并显示容量。
- 战斗 UI 当前已接入：
  - 出战选择
  - 战斗棋盘
  - 单位视图
  - 技能栏
  - 战斗物品面板
  - 战斗飘字
  - 地图与剧情 battle 入口
- 英雄面板、系统面板、存档槽选择面板、toast、hint 当前都已迁到 C# 运行路径。
- `PreviewGameBootstrap` 当前默认初始队伍为：
  - `主角`
  - `阿青`
  - `华山郭襄`
- 技能切换完整正式方案仍未完全落地，但底层状态和基础交互已经接入：
  - `docs/character-skill-switching-design.md`

## 当前 legacy 参考约束

- `legacy_scenes` 只保留仍有迁移价值的参考场景。
- 已完成迁移的基础控件、角色面板、英雄面板、提示框、背包、商店等旧 GDScript 参考允许移除，不要再把这些目录恢复成运行依赖。

## 参考文档

- `TODO.md`
- `docs/map-migration-design.md`
- `docs/battlefield-system-design.md`
- `docs/battle-float-text-and-speech-style.md`
- `docs/attachment-design.md`
- `docs/battle-effect-phase-hook-design.md`
- `docs/effect-trigger-model-comparison.md`
- `docs/character-skill-switching-design.md`
