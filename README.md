# jyxr

基于 .NET 10 与 Godot 4.7.2 C# 的 2D 半即时制战棋 RPG 内核原型。

项目已接通角色与技能、背包装备、商店、地图与剧情、MOD 启动器、存读档，以及战斗 AI、结算和演出链路。主要功能已完成，后续以功能完善、体验优化和问题修复为主；已知问题与后续计划见 [TODO](TODO.md)。

## 开源协议与资源范围

项目本体采用 [GNU GPLv3](LICENSE) 开源，可按协议使用、修改和分发。分发时请遵守 GPL 要求，保留作者 **虹乡俗人** 及其他贡献者的署名、版权和许可声明。

纹理、音频、字体、游戏数据等外部资产不在本仓库的授权范围内，需自行获取并确认使用与分发权限。第三方内容及子模块遵循各自许可。

## 开发与运行

准备 .NET 10 SDK 和 Godot 4.7.2 的 C#/.NET 版本，在仓库根目录执行：

```powershell
git submodule update --init --recursive
dotnet build src/Game.Godot/engine-free-rpg.csproj
```

使用 Godot 打开 [src/Game.Godot/project.godot](src/Game.Godot/project.godot)，运行项目后进入 MOD 启动器，选择基础 MOD 或 addon 加载组合，再进入游戏主菜单。基础 MOD 需包含有效的 `mod.json` 和 `data` 目录。

普通 .NET 测试：

```powershell
dotnet test engine-free-rpg.slnx
```

Godot 节点、输入、动画和异步演出需在引擎内另行验证。发布已导出 Windows 版本的 C# 热更程序集使用 [PublishGodotCSharpHotfix.ps1](tools/PublishGodotCSharpHotfix.ps1)。

## 导出

本体、共享资源包和签名配置见 [导出与发布](docs/exporting.md)。当前主要在 Windows 平台开发与验证；其他平台尚未充分验证，不代表不支持。

## 目录

| 位置 | 用途 |
| --- | --- |
| `engine-free-rpg.slnx`、`Directory.Build.props` | .NET 解决方案与共享编译设置 |
| `src/Game.Expressions` | 通用表达式解析与调用绑定 |
| `src/Game.Core` | 领域定义、运行态、战斗规则、剧情解释器和存档记录 |
| `src/Game.Content` | JSON 装配、引用解析与仓储校验；`SampleData` 为测试样例 |
| `src/Game.Application` | 会话、业务用例、MOD 模型、存读档与会话事件 |
| `src/Game.Presentation` | 不依赖 Godot 的展示流程与交互状态 |
| `src/Game.Godot` | 独立 Godot 工程，包含宿主 csproj、代码、场景、资源与导出配置 |
| `tests` | 普通 .NET 测试 |
| `mods` | game/addon MOD；基础内容在 `mods/jyxr-base/data` |
| `launcher`、`userdata` | 启动器设置与按主 MOD 隔离的玩家数据 |
| `src/Game.Godot/{assets,scenes,autoload,addons}` | Godot 资源、场景与插件；运行时节点由 bootstrap 创建 |
| `jyx-legacy-data`、`jyx-legacy-dll` | 原版与迁移参考，不是当前主运行路径 |

## MOD 与玩家数据

内容从各 MOD 的 loose `data` 与 `patches` 装配，资源覆盖通过 PCK。加载顺序、补丁协议和存档影响声明见 [MOD 数据补丁](docs/mod-data-patching.md)。

项目数据根在编辑器下为仓库根（Godot 工程根的 `../..`），PC 导出后为可执行文件目录，Android 为 `/storage/emulated/0/JYXR`。启动器配置位于 `launcher/settings.json`；存档、全局档案和设置位于 `userdata/<主MOD id>/`。`mods` 与玩家数据独立于 Godot 的 `res://`，迁移宿主目录不会移动玩家存档。

`autoload` 中的运行时节点并非项目设置中的 autoload。启动与存储边界见 [运行时架构](docs/runtime-architecture.md)。

## 文档

| 主题 | 文档 |
| --- | --- |
| 代理协作约定 | [AGENTS.md](AGENTS.md) |
| 分层、会话、持久化、地图与全局事件 | [运行时架构](docs/runtime-architecture.md) |
| 剧情、地图 action、条件与控制台 | [游戏内容 DSL v3](docs/game-dsl-v3.md) |
| MOD 内容扩展、编辑与迁移 | [内容扩展与数据补丁](docs/mod-data-patching.md)、[原版 MOD 迁移](docs/legacy-mod-migration-guide.md) |
| 战斗实现与状态效果 | [战斗运行时架构](docs/battle-runtime-architecture.md)、[Buff 指南](docs/buff-effects-guide.md) |
| 战斗扩展设计 | [Effect 与 Phase Hook](docs/battle-effect-phase-hook-design.md)、[伤害 Timing 设计](docs/battle-damage-timing-design.md) |
| 角色技能切换 | [技能切换](docs/character-skill-switching.md) |
| 待办与设计草案 | [TODO](TODO.md)、[战斗后续议题](docs/battle-runtime-refactoring-backlog.md) |

专题文档描述职责、协议和设计取舍；完整类型、字段和命令实现以对应源码及测试为准。
