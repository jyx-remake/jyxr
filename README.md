# jyxr

基于 .NET 10 与 Godot 4.7.2 C# 的 2D 半即时制战棋 RPG 内核原型。

项目已接通角色与技能、背包装备、商店、地图与剧情、MOD 启动器、存读档，以及战斗 AI、结算和演出链路。主分支基本完工，但仍可能有较大、破坏性更新；已知缺口见 [TODO](TODO.md)。

## 声明

一切均免费，纯粹用爱发电。非常欢迎感兴趣的朋友加入。但仅供学习交流使用，请勿骑脸版权方。部分原版资源与数据通过 `jyx-legacy-data` / `jyx-legacy-dll` 子模块提供；基础 MOD 内容位于 `mods/jyxr-base`。克隆后需要初始化子模块，并自行确认相关素材的使用权限。

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

使用 [ExportGame.ps1](tools/ExportGame.ps1) 导出本体或共享资源包：

```powershell
./tools/ExportGame.ps1 -Target Windows
./tools/ExportGame.ps1 -Target Android -TestSigning
./tools/ExportGame.ps1 -Target Shared
```

本体使用 `ExportRelease`，保留 csproj 中的 trimming 配置和 `project.godot` 中的版本号。Android 构建目录固定在 `export/android-build`，APK 输出到 `export/android/JYXR.apk`。测试签名密钥保存在 `export/android/jyxr-test.keystore` 并复用，仅用于测试；无法覆盖不同签名的安装版本。正式签名不传 `-TestSigning`，在当前进程环境配置 `GODOT_ANDROID_KEYSTORE_RELEASE_PATH`、`GODOT_ANDROID_KEYSTORE_RELEASE_USER`、`GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD`。

首次使用时，将 `tools/ExportGame.example.psd1` 复制为 `tools/ExportGame.local.psd1`，填写本机 Windows 和 Android 模板的绝对路径。本地配置已被 Git 忽略；不要将本机路径写入示例文件或导出预设。`Godot` 可填写 PATH 中的命令名或可执行文件绝对路径，默认命令为 `Godot_v4.7.2-stable_mono_win64.exe`。命令行的 `-Godot`、`-WindowsTemplate`、`-AndroidTemplate` 优先于本地配置；未指定目标平台模板时脚本会明确报错。

脚本按仓库位置计算 `export/android-build` 的绝对路径，临时调整导出预设、按需生成 Godot 要求的 `.sln`，结束时恢复预设并移除本次生成的 `.sln`；日志保存在 `export/<target>-export.log`。脚本不复制 MOD 或玩家存档，共享 PCK 仍需与对应 MOD 数据一起分发。

[导出预设](src/Game.Godot/export_presets.cfg) 分为三项，输出路径相对于 Godot 工程根：

| 预设 | 默认输出（相对于仓库根） | 内容 |
| --- | --- | --- |
| `Windows Base` | `export/windows/金庸群侠传XR.exe` | Windows 宿主、场景、脚本与启动器等基础资源 |
| `Android Base` | `export/android/JYXR.apk` | Android 宿主及相同的本体资源 |
| `Shared Resources PCK` | `export/shared/base.pck` | `assets/animation`、`assets/art`、`assets/audio` |

本体使用“导出所有资源”并以 `assets/animation/*,assets/art/*,assets/audio/*` 排除共用资源；PCK 使用“导出所选资源”（不逐项勾选），以相同通配符加入资源。Godot 的 `*` 匹配完整路径，包含子目录，不需要 `**`。PCK 排除 `.uid` 辅助文件，导入资源仍由 Godot 正常处理。新增这三个目录内的资源会自动纳入 PCK。字体、启动器图片、shader、theme 和 video 仍随本体导出。

PCK 预设借用 Windows 导出平台，保留现有纹理配置与 `extra_pck` 特性；编辑器中使用 **Export PCK/ZIP**，不要使用 **Export Project**。命令行使用上面的 `-Target Shared`，脚本会自动创建输出目录并调用 `--export-pack`。

发布时将同一份 `base.pck` 放入对应 MOD 目录，并在该 MOD 的 `mod.json` 中通过 `packs` 声明相对路径（例如 `"packs": ["base.pck"]`）。仅把 PCK 放在本体旁不会自动加载；`mods` 数据也需单独分发。Windows 的数据根为可执行文件目录，Android 为 `/storage/emulated/0/JYXR`。Windows 本体导出的配套 PCK 与 .NET 数据目录也需一同分发。

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
| `legacy_scenes`、`jyx-legacy-data`、`jyx-legacy-dll` | 原版与迁移参考，不是当前主运行路径 |

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
| MOD 内容编辑与迁移 | [数据补丁](docs/mod-data-patching.md)、[原版 MOD 迁移](docs/legacy-mod-migration-guide.md) |
| 战斗实现与状态效果 | [战斗运行时架构](docs/battle-runtime-architecture.md)、[Buff 指南](docs/buff-effects-guide.md) |
| 战斗扩展设计 | [Effect 与 Phase Hook](docs/battle-effect-phase-hook-design.md)、[伤害 Timing 设计](docs/battle-damage-timing-design.md) |
| 待办与设计草案 | [TODO](TODO.md)、[战斗后续议题](docs/battle-runtime-refactoring-backlog.md)、[技能切换草案](docs/character-skill-switching-design.md) |
| MOD 扩展调研 | [STS2 风格 MOD 分析](docs/sts2-style-modding-analysis.md) |

专题文档描述职责、协议和设计取舍；完整类型、字段和命令实现以对应源码及测试为准。
