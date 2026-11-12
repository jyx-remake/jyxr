# 运行时架构

本文集中说明会话、内容装配、存档和地图执行的边界。战斗内部机制见 [战斗运行时架构](battle-runtime-architecture.md)，内容语法见 [DSL v3](game-dsl-v3.md)。

## 分层

| 层 | 职责与依赖 |
| --- | --- |
| Game.Expressions | 通用表达式语法、求值与调用注册，不依赖游戏领域 |
| Game.StoryDsl | `.story` 源码解析与编译，直接构建 Core 剧情模型；依赖 Core、Expressions |
| Game.Core | 领域模型、规则、战斗与剧情运行时、存档记录；依赖 Expressions |
| Game.Content | JSON 与剧情源码输入、definition 引用解析、内容仓储与校验；依赖 Core、Expressions、StoryDsl |
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

[SessionExecutionService](../src/Game.Application/SessionExecutionService.cs) 管理执行范围、终局结果与可恢复检查点。运行状态由当前外层执行范围及 `StoryOutcome` 共同决定；没有执行中流程且没有终局结果时允许手动、快速保存。进入地图交互前先捕获检查点，再移动、推进时间与执行命令。独立剧情、全局事件、限时剧情和物品剧情也建立外层执行范围，嵌套剧情共享入口检查点。存在当前地图时，入口检查点请求自动落盘；自动存档开关和无悔规则由宿主统一执行。执行中、战败、通关和主菜单阶段禁止创建当前状态存档。

`StoryService.ExecuteAsync` 返回 `StoryOutcome`，事件流以 `StoryEndedEvent` 给出结果。战败、通关与返回主菜单先由应用层设置终止阶段，再调用宿主展示界面；结果终止调用链。正常完成的外层交互提交一次性事件标记、恢复队伍资源，再保存结束检查点。战败或通关保留入口自动档，读档可重新挑战整段流程。剧情内部的段完成标记仍即时参与条件求值。

异常、取消或提前释放外层执行范围时，恢复入口快照，通过 `SaveLoadedEvent` 使旧状态任务失效，再以 `ExecutionAbortedEvent` 重建宿主展示。读档、重开与下一周目会取消原执行，清除原检查点身份；新状态独立执行开场。自动存档请求携带状态身份与已捕获的 `SaveGame`，宿主仅写入仍有效的检查点。存档先写同目录临时文件、flush 后原子替换，自动档替换时将上一份完整文件保留为 `autosave.json.bak`。

`profile.json` 与 `settings.json` 在启动时读取，运行中使用内存状态。World 下的 `ProfilePersistenceCoordinator` 每 60 秒真实时间、结束游玩和退出时检查点保存 profile；场景树暂停不停止保存周期，战斗加速不改变周期。失焦或收到移动端 `NotificationApplicationPaused` 时，先暂停并结算游玩计时，再同步 flush profile 和 settings，不等待下一帧。恢复通知与焦点通知独立维护，只有应用未挂起、有焦点且场景树未暂停时才恢复游玩计时。移动端后台进程可能停止执行，不能依赖周期或退出回调，强杀与断电仍只能恢复到最后成功保存的数据。

档案业务变更、自动/手动/快速存档均不额外触发 profile 写入。写入以最后成功保存的快照去重，因此连续失焦与挂起通知不会重复写入相同数据；失败保留内存状态，等下一个保存时机重试。单个存档与 profile 不保证同步落盘。

`UserSettingsService` 立即应用变化的设置并更新内存，持久化由同一宿主协调器驱动；大地图缩放不在临时面板内维护待保存计时器。profile、用户设置和启动器设置均先写同目录临时文件，再替换目标文件。配置内容 `game-config.json` 只参与启动内容装配。

## 角色、队伍与物品

`Party` 是全局伙伴名册，`Members`、`Followers`、`Reserves` 三个池子中的实例互斥。入队和跟随优先移动既有实例；离队移入后备池，保留成长、装备与技能。存档角色记录覆盖整个名册。

`Inventory` 使用有序条目表示堆叠物品和独立装备；带实例差异的装备独立保存。`ChestState` 复用 Inventory，存取规则由 `ChestService` 执行。装备实例序列属于 GameState，由 `EquipmentInstanceFactory` 统一分配。

背包、储物箱与洗练选装共用 `ItemTagBar`，标签来自当前候选物品的 definition。`ItemCatalogFilter` 在 Presentation 中管理分类、标签与匹配规则；数据刷新保留仍有效的标签，切换分类或标签消失时回到全部。洗练选装只展示装备标签，包含白装堆叠与独立装备。

`EquipmentRefinementService` 编排选装、操作选择、付费候选与结果应用，显式区分新增、替换和退出。每次候选消耗 1 元宝、8 选 1；附加词条按组计数，最多增加至 4 条，满额仍可替换。白装确认新增后才从堆叠拆出一件实例；异步选择完成后统一校验原状态、档案、背包条目和词条快照，拒绝失效结果。

背包选人界面支持选择使用数量。`ItemUseService` 对可消耗的场外堆叠物品逐件校验、应用效果和扣除库存，返回实际使用数量；条件不再满足或出现未经确认的失效效果时停止，已完成的使用保留。装备、不消耗的物品及剧情效果仅允许单次使用，战斗内物品仍遵循战斗行动规则。

角色派生属性通过 `CharacterInstance.RebuildSnapshot()` 收集装备、天赋和满足条件的技能 affix，再由 resolver 展开。外功激活状态不决定其被动 affix 是否生效；内功 affix 是否要求装备由 `requiresEquippedInternalSkill` 明确表达。新增 affix 来源时要同时检查内容引用解析与角色快照收集。

新游戏与下一周目由 `SessionFlowService` 和 [NewGameStateFactory](../src/Game.Application/NewGameStateFactory.cs) 装配。下一周目保留银两和储物箱、重建初始队伍；全局元宝保留在 GameProfile。

## UI 宿主与展示协调

`UIRoot` 装配场景节点与宿主对象，提供面向调用方的展示入口。`PanelHost` 管理主面板、替换式弹窗和临时面板的所有权，关闭时立即隐藏并排队释放，退出场景树时移除引用。装备选择、物品选人等模态面板通过 `ShowModalPanel` 加入宿主；战斗界面使用自己的 `PanelHost` 管理道具、人物状态和结算弹窗。

宿主根据直接子节点的显示顺序提供返回目标。详情宿主和战斗界面通过 `IPanelBackTargetProvider` 提供内部宿主的目标，人物名册与战斗状态通过 `IPanelBackHandler` 显式转发给内嵌人物面板。`PanelBackInputRouter` 在右键按下时记录最上层交互目标，松开时确认目标仍有效且仍处于最上层，再调用返回操作。目标按提示层、模态层、战斗层和普通面板层的显示顺序查找。右键手势的松开事件由已捕获的界面消费，避免关闭弹窗后继续触发棋盘返回。

`JyPanel` 通过已有关闭信号返回，关闭按钮隐藏或禁用时保留当前界面。系统面板复用返回操作，确认框返回取消，提示框完成当前提示等待。人物属性加点浮层优先收起。装备槽及装备卡片在 GUI 输入阶段通过 `ConsumeRightClick()` 接管当前右键手势，继续执行卸装。

`StoryChoicePanel` 按调用场景设置 `AllowCancel`，默认要求选中有效选项。`UIRoot.ShowChoicesAsync` 返回有效索引；`ShowCancelableChoicesAsync` 允许右键返回，并用 `null` 表示用户取消，也支持空选项列表。选择等待和收起由选择面板管理，每次展示持有独立的完成源，旧展示的回调与清理只作用于对应展示；外部 `CancellationToken` 和主动隐藏会中断等待。

`StoryPresentationCoordinator` 协调对白、选择、字幕、视频和视觉效果的展示切换与重置，具体面板负责自己的异步等待和清理。对白的延迟隐藏与展示版本校验由 `StoryDialoguePanel` 持有。`SessionPresentationCoordinator` 管理会话事件订阅、提示文案、地图返回时的场景与音乐恢复，以及读档后的表现恢复。

读档或执行中断后的表现恢复会关闭宿主管理的普通、模态、详情和战斗面板，重置剧情展示及右键手势。读档事件同时收起临时面板、确认框与提示框，待处理确认返回 `false`，提示等待正常完成。确认框与提示框按独立完成源清理展示，旧等待的清理不会收起后续展示。

## 虚拟化数据网格

固定尺寸的数据卡片共用 [VirtualGrid](../src/Game.Godot/UI/Base/VirtualGrid.cs)，直接放在 ScrollContainer 下。场景声明列数、卡片尺寸和间距，页面提供数据快照、节点工厂与绑定回调。控件维护完整内容高度，只实例化视口及上下各一行的卡片，滚动时复用移出视口的节点。混合奖励按卡片类型分别复用。

[VirtualGridLayout](../src/Game.Presentation/Collections/VirtualGridLayout.cs) 在 Presentation 中计算可见范围、条目位置与排序插入槽。背包、商店、储物箱、武学、选装、选人、出战选择、战斗奖励、存档和头像列表使用同一套布局。选中项和操作目标由页面的数据标识维护，节点信号在工厂中连接一次，绑定回调更新当前条目与显示状态。

队伍页保留桌面拖拽、移动端长按与边缘自动滚动。拖动期间固定源卡片以维持 Godot 拖放生命周期，落点及插入指示由完整成员快照和网格坐标计算。成员变化或读档结束当前拖动并替换快照，排序提交仍通过 PartyService.MoveMember 执行。键盘方向导航按数据索引跨越虚拟化边界并滚动到目标，持有焦点的卡片在滚出视口时继续保留。

## MOD 启动与内容装配

Godot 工程位于 `src/Game.Godot`，入口是 `res://scenes/ui/mod_launcher/mod_launcher_panel.tscn`。`res://` 只覆盖宿主目录，类库、测试和 MOD 数据位于其外。项目数据根由 `ProjectDataRootResolver` 统一确定：编辑器为仓库根（宿主目录的 `../..`），PC 导出为可执行文件目录，Android 为 `/storage/emulated/0/JYXR`。

Android 启动器在扫描 MOD 前检查“所有文件访问权限”；未授权时通过 Godot 权限接口直接打开系统授权页面。返回游戏并恢复焦点后重新检查，授权成功才扫描 MOD；未授权时保留“前往授权”入口，不自动反复跳转。此流程不包含 Android 10 及以下的传统存储权限请求。

[GameRuntimeBootstrap](../src/Game.Godot/Bootstrap/GameRuntimeBootstrap.cs) 接收 ModLoadout：

1. 按有效顺序加载 MOD 声明的 PCK，建立外部静态图片与音频索引并重建媒体资源上下文，在运行时场景实例化之前完成资源准备。
2. 确保 `World`、`UIRoot`、`AudioManager` 存在于 `/root/__GameRuntime`。
3. 按主 MOD、依赖 addon、用户排序 addon 的顺序装配 loose data 与 patch。
4. 读取主 MOD 独立的设置、全局档案，构造 GameSession 并初始化宿主入口。
5. 绑定 UI、世界事件、限时剧情、自动存档和游玩计时。

`autoload` 是场景存放目录，不是 Godot autoload 注册。重复初始化会更换 session 和资源上下文，但不会完整销毁旧地图、面板、音乐与运行时节点；它不等于进程内 MOD 热切换。正式 reset/reload 仍需明确清理流程，并先 flush 宿主待保存档案。

[JsonContentLoader](../src/Game.Content/Loading/JsonContentLoader.cs) 负责定义索引、引用解析、affix 解析与仓储校验。正式内容不从 SampleData 读取；故事加载保持同步，避免 Godot 主线程 sync-over-async。当前定义依赖按有向无环图装配；若引入循环引用，需明确设计注册与 resolve 两阶段。

剧情输入统一为 `data/stories/**/*.story`。[StorySourceReader](../src/Game.Content/Loading/StorySourceReader.cs) 在每次内容加载时调用独立的 `Game.StoryDsl` 编译器，直接构建包含已解析表达式的 `StoryScript` 和各类 Step。[StoryContentCatalog](../src/Game.Content/Loading/StoryContentCatalog.cs) 按加载顺序装配剧情段，同一 MOD 内重复段名报错，跨 MOD 同名段由后加载者整体替换，并向加载报告写入双方来源。最终运行目录只包含生效段，随后统一校验跨文件引用和内容引用。正式发布携带 `.story` 源码，语法、诊断和编译归一化规则见 [剧情源码编写指南](story-authoring.md)。

`ContentPackage` 承载 JSON 定义，已编译剧情通过独立参数传入仓储构建入口。`StoryScript` 保存类型化剧情段，调用与表达式直接持有解析后的语法树。

新增定义、修改已有定义、地图与剧情目录规则统一见 [MOD 数据补丁](mod-data-patching.md)。[AssetResolver](../src/Game.Godot/Resources/AssetResolver.cs) 通过 [ExternalMediaCatalog](../src/Game.Content/Loading/ExternalMediaCatalog.cs) 查询有效 MOD 的 `assets/art` 图片与 `assets/audio` 音频，再查询 Godot 已挂载资源。外部资源按 MOD 顺序后者优先，同一 MOD 内按扩展名候选顺序匹配。视频、动画库以及场景直接引用的资源通过 Godot 资源系统加载。

外部音频通过 Godot 原生 OGG Vorbis、MP3、WAV 加载器创建音频流，并按实际文件路径注册到 Godot 原生资源缓存。重复请求复用仍由播放器或调用方持有的资源，释放时机跟随引擎引用计数与 C# GC。`AudioManager` 控制音乐循环、播放列表和音效多音轨。基础音频随 `mods/jyxr-base/assets/audio` 分发。

基础内容的 `battle_bg`、`head`、`icon`、`item`、`map` 图片随 `mods/jyxr-base/assets/art` 分发。这些内容图片的场景预览统一引用 `assets/art/editor_samples` 下的头像、图标、背景三张样板图。节点就绪时清空样板纹理或绑定真实资源，头像与图标缺失时清空对应显示，复用界面时也会覆盖旧纹理。系统面板使用 `地图.竹林`，宝箱面板使用 `场景.商店`，两个训练小游戏使用 `地图.客栈`。

图片解析索引保存引用到实际来源的映射。[TextureCache](../src/Game.Godot/Resources/TextureCache.cs) 按底层纹理实例维护字节预算和 LRU，桌面默认 `256 MiB`，移动端默认 `128 MiB`。命中任何资源别名或图集帧都会刷新其底图的最近访问顺序。容量按基础层尺寸与格式估算，常见三分量格式按 GPU 四分量对齐保守计费，同一底图仅计一次。估算过程只查询元数据。超出预算时移除最久未访问底图及其关联视图的缓存强引用。单张底图超过预算时保留弱引用，交由实际使用者持有。

Godot `WeakRef` 按实际来源复用仍由节点或其他资源持有的纹理。每加载 64 个来源清理失效弱引用。淘汰由缓存释放强引用，纹理由 Godot 引用计数和 C# GC 回收，实际显存释放可能延迟。剧情对话和选择面板隐藏时清空头像纹理引用。预算控制缓存强引用的估算量，全局纹理显存还包含界面、动画、场景和其他持有者的资源。

Godot 调试器自定义监控 `assets/texture_cache/*` 提供预算、保留字节数、底图数、来源数、强缓存命中、弱引用复用、来源加载和淘汰次数。来源加载次数统计成功调用加载入口的次数，其中包含 Godot 内部资源缓存命中。媒体解析失败和加载失败按媒体类型隔离，共保留最近 1024 条记录，超出后允许重新尝试。界面关闭、切图和读档继续复用当前资源上下文，重建时清空索引和图片缓存并重新扫描外部文件。外部图片解码后修复透明边缘，再创建纹理。运行中编辑文件后需要重新启动游戏才能稳定使用新资源。完整的进程内 MOD 热切换仍受上面的运行时清理边界约束。

## 地图与全局事件

[MapService](../src/Game.Application/MapService.cs) 区分以下操作：

| 操作 | 语义 |
| --- | --- |
| EnterMap | 切图、恢复队伍战斗资源、记录位置、发布 MapChangedEvent、请求全局事件检查 |
| GetCurrentMap | 构建当前地图展示数据，不改变状态或触发事件 |
| ReturnToMap | 校验原状态与可保存阶段，恢复队伍战斗资源，请求全局检查，发布 MapReturnedEvent，然后请求自动存档 |
| InteractWithLocation | 建立入口检查点，根据距离和点位交互推进时间，生成持有执行范围与原状态身份的命令结果 |
| ExecuteInteractionAsync | 校验原状态、执行整段命令、确认点位完成，再调用 ReturnToMap |

大地图分别记忆当前位置；首次进入使用 defaultLocation，显式入口可指定已有地点。移动耗时与坐标协议见 MOD 数据文档。命令在异步等待前后都校验所属 GameState，旧交互不继续派发或给新状态标记完成、请求存档。

地图交互结果持有执行范围，调用方使用 `using` 覆盖赶路演出与命令执行。正常完成会结束该范围；赶路失败、取消或未执行命令即释放时恢复入口检查点。

`World.RefreshCurrentMap()` 原地更新展示，不重新进入地图，也不重新选播 BGM。成功结束整段地图交互、全局事件、限时剧情或独立调试剧情时，通过 `ReturnToMap` 恢复血量、内力、怒气和技能冷却；连续战斗中途不执行此操作。宿主响应 `MapReturnedEvent` 刷新地图和 HUD，恢复地图音乐（没有地图音乐则停止 BGM，相同播放列表保持播放）。自动存档发生在资源恢复之后。读档使用 `World.RestoreCurrentMap()` 重建视图，保留存档中的战斗资源，避免继承旧交互的忙碌状态。

[WorldTriggerService](../src/Game.Application/WorldTriggerService.cs) 保存与发起状态绑定的待检查请求和执行中状态：

- 进入地图、成功完成点位交互、读档后请求检查；多个未执行请求合并。
- 每次调度按仓储顺序执行首个满足条件且本轮尚未执行的事件；成功后继续检查，直到没有符合条件的事件。后执行事件改变条件后，先前未符合条件的事件仍可在本轮执行。屏蔽期间保留请求，解除后继续检查。
- 执行中的事件内部切图不另开一轮检查，避免递归触发。每个事件一轮最多成功执行一次；可重复事件再次执行需要下一轮外部请求。
- 一次性事件在命令成功完成且状态仍有效后才写入 CompletedTriggerIds，随后请求自动存档。
- 失败或取消时恢复该事件入口的 GameState 检查点，下次请求可重试。GameProfile 中的跨存档统计与解锁独立保留。

[WorldEventCoordinator](../src/Game.Godot/Story/WorldEventCoordinator.cs) 挂在稳定的 World 下，统一等待地图交互、剧情和战斗空闲，先执行到期限时剧情，再处理全局事件。读档或解绑会取消旧执行。待检查、执行中状态和本轮已执行集合不持久化，只有一次性事件的成功完成记录进入存档。

已有错误完成标记不会通过运行时猜测自动清除；修复具体存档是独立的数据操作。

## 剧情与会话事件

对话面板的“跳过”按钮会结束当前对白，并持续跳过本次剧情的后续对白；显示选项前恢复正常，选择后的对白需重新点击跳过。剧情结束或读档时清除跳过状态。跳过对白不省略剧情命令或其他演出。

对白正文按 Godot 富文本的实际换行与行高分页，关闭滚动条；整段文本保留同一份富文本排版，跨页格式连续。点击或确认键在逐字显示时补全当前页，当前页完整后翻到下一页，最后一页完成后才结束对白。逐字计数采用解析后的字符范围，不计 BBCode 标签。

跳过状态由 `StoryDialoguePanel` 管理；`UIRoot` 仅在显示选项与重置剧情展示时通知面板停止跳过。

剧情运行时位于 Core，应用入口是 StoryService。命令经 [StoryCommandDispatcher](../src/Game.Application/StoryCommandDispatcher.cs) 的调用注册表分派；应用层负责状态用例，GodotStoryRuntimeHost 负责对话、选项、场景与演出。

剧情当前使用 DSL v3，地图和世界触发使用 `action` 与可选 `when`；命令清单统一查 [DSL v3 参考](game-dsl-v3.md)。对白对应 `DialogueStep`；选项宿主回传 ChoiceOptionView.Index，不能把过滤后的数组位置当作原选项标识。

`StoryTextInterpolator` 在进入宿主前处理主角与女主名字占位符，查找覆盖整个 Party 名册。限时剧情由 [StoryTimeKeyExpirationService](../src/Game.Application/StoryTimeKeyExpirationService.cs) 检查严格超过期限的 key。非空目标的到期 key 继续保存在 StoryState 中，作为持久化待办；空目标直接移除。WorldEventCoordinator 按到期顺序执行待办。执行范围在消费 key 前建立，正常完成后提交消费结果，异常或取消恢复 key；保存、退出与读档后可重新发现待办，旧状态的任务不能进入新状态执行。

[SessionEvents](../src/Game.Application/SessionEvents/SessionEvents.cs) 用于应用结果到 UI、持久化与协调器的通知。增加事件时明确发布方、订阅方，以及读档后的刷新或取消语义。事件类型与完整服务列表直接查看源码，避免在多份文档中复制。

## 验证与后续工作

普通 .NET 测试覆盖规则、内容装配、状态替换、失败重试和完成记录。Godot 实测覆盖赶路动画后的事件派发、地图节点生命周期、对白期间读档取消，以及刷新时的 BGM 行为；临时诊断使用独立数据目录。

局部取消和状态校验不代表所有宿主 UI 都已具备统一读档中断机制。未解决的确认框、选择面板等异步流程问题，以及其他后续设计集中在 [TODO](../TODO.md)。设计草案与现行实现分开阅读。
