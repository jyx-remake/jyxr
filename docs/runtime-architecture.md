# 运行时架构

本文集中说明会话、内容装配、存档和地图执行的边界。战斗内部机制见 [战斗运行时架构](battle-runtime-architecture.md)，内容语法见 [DSL v3](game-dsl-v3.md)。

## 分层

| 层 | 职责与依赖 |
| --- | --- |
| Game.Expressions | 通用表达式语法、求值与调用注册，不依赖游戏领域 |
| Game.Core | 领域模型、规则、战斗与剧情运行时、存档记录；依赖 Expressions |
| Game.Content | JSON 输入、definition 引用解析、内容仓储与校验；依赖 Core、Expressions |
| Game.Application | GameSession、业务用例、持久化装配、会话事件与 MOD 模型；依赖 Core、Expressions |
| Game.Presentation | 展示流程、交互状态与宿主能力抽象；只依赖 Core |
| Game.Godot | Godot 节点、UI、资源、音频、存储适配和演出；独立工程位于 src/Game.Godot |

纯展示计算可归入 Presentation；领域规则、应用用例、序列化协议和资源路径解析各自保留在所属层。业务服务由 session 显式装配，UI 通过服务执行用例。

## 会话与持久化

[GameSession](../src/Game.Application/GameSession.cs) 是普通实例，持有当前状态、档案、配置、设置、仓储、应用服务和事件总线。`Game` 是 Godot 的全局访问入口，不是领域单例。

- `GameState` 保存本轮可存档状态，通过显式 setter 装配；`AdventureState` 承载周目、难度、门派、道德、好感和排名等上下文。
- `GameProfile` 保存跨存档统计、解锁和元宝，独立落盘为 `GameProfileRecord`，不嵌入单个 `SaveGame`。
- 运行态角色、技能和装备可直接持有已解析 definition；持久化记录只保存稳定 ID 与实例状态。
- [SaveGameService](../src/Game.Application/SaveGameService.cs) 恢复子状态后调用 `ReplaceState(...)`；档案加载使用 `ReplaceProfile(...)`。服务实例不重建，不长期缓存可被读档替换的状态。
- 异步操作应保留发起时的状态身份，完成时确认仍属于同一状态；取消宿主等待与拒绝旧结果写入是两个不同职责。

本地路径由 [ModStoragePaths](../src/Game.Application/Mods/ModStoragePaths.cs) 统一生成。无悔规则独立于战斗难度，只允许自动存档，且不受普通自动存档开关限制。

## 角色、队伍与物品

`Party` 是全局伙伴名册，`Members`、`Followers`、`Reserves` 三个池子中的实例互斥。入队和跟随优先移动既有实例；离队移入后备池，保留成长、装备与技能。存档角色记录覆盖整个名册。

`Inventory` 使用有序条目表示堆叠物品和独立装备；带实例差异的装备独立保存。`ChestState` 复用 Inventory，存取规则由 `ChestService` 执行。装备实例序列属于 GameState，由 `EquipmentInstanceFactory` 统一分配。

角色派生属性通过 `CharacterInstance.RebuildSnapshot()` 收集装备、天赋和满足条件的技能 affix，再由 resolver 展开。外功激活状态不决定其被动 affix 是否生效；内功 affix 是否要求装备由 `requiresEquippedInternalSkill` 明确表达。新增 affix 来源时要同时检查内容引用解析与角色快照收集。

新游戏与下一周目由 `SessionFlowService` 和 [NewGameStateFactory](../src/Game.Application/NewGameStateFactory.cs) 装配。下一周目保留银两和储物箱、重建初始队伍；全局元宝保留在 GameProfile。

## MOD 启动与内容装配

Godot 工程位于 `src/Game.Godot`，入口是 `res://scenes/ui/mod_launcher/mod_launcher_panel.tscn`。`res://` 只覆盖宿主目录，类库、测试和 MOD 数据位于其外。项目数据根由 `ProjectDataRootResolver` 统一确定：编辑器为仓库根（宿主目录的 `../..`），PC 导出为可执行文件目录，Android 为 `/storage/emulated/0/JYXR`。

[GameRuntimeBootstrap](../src/Game.Godot/Bootstrap/GameRuntimeBootstrap.cs) 接收 ModLoadout：

1. 按有效顺序加载 MOD 声明的 PCK，在运行时场景实例化之前完成资源覆盖。
2. 确保 `World`、`UIRoot`、`AudioManager` 存在于 `/root/__GameRuntime`。
3. 按主 MOD、依赖 addon、用户排序 addon 的顺序装配 loose data 与 patch。
4. 读取主 MOD 独立的设置、全局档案，构造 GameSession 并初始化宿主入口。
5. 绑定 UI、世界事件、限时剧情、自动存档和游玩计时。

`autoload` 是场景存放目录，不是 Godot autoload 注册。重复初始化会更换 session 和资源上下文，但不会完整销毁旧地图、面板、音乐与运行时节点；它不等于进程内 MOD 热切换。正式 reset/reload 仍需明确清理流程，并先 flush 宿主待保存档案。

[JsonContentLoader](../src/Game.Content/Loading/JsonContentLoader.cs) 负责定义索引、引用解析、affix 解析与仓储校验。正式内容不从 SampleData 读取；故事加载保持同步，避免 Godot 主线程 sync-over-async。当前定义依赖按有向无环图装配；若引入循环引用，需明确设计注册与 resolve 两阶段。

新增定义、修改已有定义、地图与剧情目录规则统一见 [MOD 数据补丁](mod-data-patching.md)。MOD 资源覆盖只走 PCK，[AssetResolver](../src/Game.Godot/Resources/AssetResolver.cs) 统一加载内置或已覆盖资源，不读取 MOD loose assets。

## 地图与全局事件

[MapService](../src/Game.Application/MapService.cs) 区分四种操作：

| 操作 | 语义 |
| --- | --- |
| EnterMap | 切图、恢复队伍战斗资源、记录位置、发布 MapChangedEvent、请求全局事件检查 |
| GetCurrentMap | 构建当前地图展示数据，不改变状态或触发事件 |
| InteractWithLocation | 根据距离和点位交互推进时间，生成带原状态身份的命令结果 |
| ExecuteInteractionAsync | 校验原状态、执行命令、确认点位完成、请求全局检查和自动存档 |

大地图分别记忆当前位置；首次进入使用 defaultLocation，显式入口可指定已有地点。移动耗时与坐标协议见 MOD 数据文档。命令在异步等待前后都校验所属 GameState，旧交互不继续派发或给新状态标记完成、请求存档。

`World.RefreshCurrentMap()` 原地更新展示，不重新进入地图，也不重新选播 BGM。读档使用 `World.RestoreCurrentMap()` 重建视图，避免继承旧交互的忙碌状态。

[WorldTriggerService](../src/Game.Application/WorldTriggerService.cs) 保存与发起状态绑定的待检查请求和执行中状态：

- 进入地图、成功完成点位交互、读档后请求检查；多个未执行请求合并。
- 每次检查按仓储顺序执行首个满足条件的事件。屏蔽期间保留请求，解除后继续检查。
- 执行中的事件内部切图不再次排队，避免递归触发；可重复事件仍需下一次外部请求。
- 一次性事件在命令成功完成且状态仍有效后才写入 CompletedTriggerIds，随后请求自动存档。
- 失败或取消不标记完成，下次请求可重试；这不意味着自动回滚命令已经产生的副作用。

[WorldTriggerCoordinator](../src/Game.Godot/Story/WorldTriggerCoordinator.cs) 挂在稳定的 World 下，等待地图交互、剧情和战斗空闲后执行；不把播放请求交给临时 MapScreen 的延迟回调。读档或解绑会取消旧执行。待检查和执行中状态不持久化，只有成功完成记录进入存档。

已有错误完成标记不会通过运行时猜测自动清除；修复具体存档是独立的数据操作。

## 剧情与会话事件

对话面板的“跳过”按钮会结束当前对白，并持续跳过本次剧情的后续对白；显示选项前恢复正常，选择后的对白需重新点击跳过。剧情结束或读档时清除跳过状态。跳过对白不省略剧情命令或其他演出。

对白正文按 Godot 富文本的实际换行与行高分页，关闭滚动条；整段文本保留同一份富文本排版，跨页格式连续。点击或确认键在逐字显示时补全当前页，当前页完整后翻到下一页，最后一页完成后才结束对白。逐字计数采用解析后的字符范围，不计 BBCode 标签。

跳过状态由 `StoryDialoguePanel` 管理；`UIRoot` 仅在显示选项与重置剧情展示时通知面板停止跳过。

剧情运行时位于 Core，应用入口是 StoryService。命令经 [StoryCommandDispatcher](../src/Game.Application/StoryCommandDispatcher.cs) 的调用注册表分派；应用层负责状态用例，GodotStoryRuntimeHost 负责对话、选项、场景与演出。

剧情当前使用 DSL/IR v3，地图和世界触发使用 `action` 与可选 `when`；不维护另一份命令清单，统一查 [DSL v3 参考](game-dsl-v3.md)。对白使用 `kind: dialogue`；选项宿主回传 ChoiceOptionView.Index，不能把过滤后的数组位置当作原选项标识。

`StoryTextInterpolator` 在进入宿主前处理主角与女主名字占位符，查找覆盖整个 Party 名册。限时剧情由 [StoryTimeKeyExpirationService](../src/Game.Application/StoryTimeKeyExpirationService.cs) 检查严格超过期限的 key；到期移除 key，仅对非空目标排队播放，由 TimedStoryCoordinator 等待当前剧情结束后执行。

[SessionEvents](../src/Game.Application/SessionEvents/SessionEvents.cs) 用于应用结果到 UI、持久化与协调器的通知。增加事件时明确发布方、订阅方，以及读档后的刷新或取消语义。事件类型与完整服务列表直接查看源码，避免在多份文档中复制。

## 验证与后续工作

普通 .NET 测试覆盖规则、内容装配、状态替换、失败重试和完成记录。Godot 实测覆盖赶路动画后的事件派发、地图节点生命周期、对白期间读档取消，以及刷新时的 BGM 行为；临时诊断使用独立数据目录。

局部取消和状态校验不代表所有宿主 UI 都已具备统一读档中断机制。未解决的确认框、选择面板等异步流程问题，以及其他后续设计集中在 [TODO](../TODO.md)。设计草案与现行实现分开阅读。
