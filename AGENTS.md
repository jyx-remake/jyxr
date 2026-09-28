# 项目协作约定

这是基于 .NET 10 与 Godot 4.7.2 C# 的 2D 半即时制战棋 RPG。仓库根是 .NET 解决方案根；Godot 工程与宿主程序集位于 `src/Game.Godot`，其他类库与它平级。

## 工作方式

- 遵循第一性原理，从职责与状态模型解决问题，不做补丁性或兼容性修改。
- 用户要求实现、修复或重构时，在已授权范围内完成必要修改与验证；仅讨论方案或排查原因时，不顺手修改正式代码。
- 除非用户明确要求，不访问当前工作区以外的文件。复现存读档问题使用隔离的临时数据目录，不覆盖玩家存档。
- 保留用户和其他流程的未提交修改，不使用破坏性 Git 命令。手动编辑使用 `apply_patch`。
- 提交信息采用带 scope 的 Conventional Commits，例如 `fix(world-events): preserve triggers across map transitions`。
- 只阅读当前任务需要的代码和文档。文档与实现冲突时核实代码与测试，并在本次涉及的范围内修正文档。

## 工程边界

- `Game.Expressions` 提供通用表达式；`Game.Core` 承载领域规则和状态；`Game.Content` 装配、解析和校验内容；`Game.Application` 负责会话与用例。
- `Game.Presentation` 只依赖 `Game.Core`，表达交互状态和展示流程。`Game.Godot` 承载节点、资源、音频和演出，不把业务规则挪进 UI。
- `GameSession` 是普通实例，不改成静态单例。服务通过当前 session 取状态；读档替换 `State`，不重建服务。跨异步等待的操作应确认仍属于原状态。
- 运行态可持有已解析 definition；存档只保存稳定 ID 和实例状态。`GameProfile` 独立于单个存档。
- 正式内容在 `mods/<modId>/data`，新增定义与修改既有定义的 MOD patch 分工见下方文档。资源覆盖走 PCK，不增加 loose assets 覆盖路径。
- `World`、`UIRoot`、`AudioManager` 由 bootstrap 挂在 `/root/__GameRuntime`，不是 Godot autoload；先完成初始化，再使用各自 `Instance`。
- Godot 运行期日志使用 `Game.Logger`。资源加载统一经 `AssetResolver`，不要在面板重复拼接资源路径或实现扩展名回退。
- 场景脚本取节点优先使用 `unique_name_in_owner = true` 与 `%Name`；仅在层级本身具有语义时使用固定 NodePath。UI 默认使用轻量面板脚本，复杂显示可提取 Presenter，不为每个面板创建 ViewModel。
- `tests/Game.Tests` 不引用 Godot 宿主或 GodotSharp。节点生命周期、延迟回调和实际演出需在 Godot 中验证。
- `legacy_scenes`、`jyx-legacy-data`、`jyx-legacy-dll` 用于参考，不恢复已移除的旧运行路径。

## 按任务查阅

| 任务 | 入口 |
| --- | --- |
| 启动项目、目录位置、常用命令 | [README](README.md) |
| 会话、存档、MOD 启动、地图与全局事件 | [运行时架构](docs/runtime-architecture.md) |
| 剧情、条件表达式、地图 action、控制台 | [游戏内容 DSL v3](docs/game-dsl-v3.md) |
| MOD 数据目录、补丁、原版迁移 | [MOD 数据补丁](docs/mod-data-patching.md)、[迁移指南](docs/legacy-mod-migration-guide.md) |
| 战斗规则、结算、AI、表现边界 | [战斗运行时架构](docs/battle-runtime-architecture.md)；修改 Hook 时读 [Effect 与 Phase Hook](docs/battle-effect-phase-hook-design.md) |
| 后续设计与未解决事项 | [TODO](TODO.md)、[战斗后续议题](docs/battle-runtime-refactoring-backlog.md)；设计草案不视为已实现行为 |

## 验证入口

按改动影响选择验证，已有通过结果且代码未变化时不重复运行。文档修改检查内容与链接即可；涉及宿主时补宿主构建，涉及执行时序时补实际 Godot 场景验证。

```powershell
# 普通 .NET 测试，可加 --filter 限定受影响用例
dotnet test engine-free-rpg.slnx

# Godot 宿主编译
dotnet build src/Game.Godot/engine-free-rpg.csproj

# 新增或删除 Godot C# 文件后检查配对的 .uid
./tools/ValidateGodotScriptUids.ps1
```

在任务范围内可自行执行本地构建、隔离测试并修复本次改动引入的失败。结束时说明变更、验证结果和仍未验证的部分。

## 文档维护

本文件只保留跨任务需要的约束和阅读入口。架构事实写入对应专题文档，待办集中在 `TODO.md` 或专题 backlog；不要追加会话摘要、完整成员列表或已完成迁移清单。
