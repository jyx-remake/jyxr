# 导出与发布

目前项目主要面向 Windows 开发与验证，其他平台尚未完成同等程度的验证，不代表不支持。仓库已提供 Android 导出预设；APK 构建和签名验证通过不等于完整游戏流程验证。以下脚本目前在 Windows 环境使用。

使用 [ExportGame.ps1](../tools/ExportGame.ps1) 导出本体或共享资源包：

```powershell
./tools/ExportGame.ps1 -Target Windows
./tools/ExportGame.ps1 -Target Android -TestSigning
./tools/ExportGame.ps1 -Target Shared
```

本体使用 `ExportRelease`，保留 csproj 中的 trimming 配置和 `project.godot` 中的版本号。Android 构建目录固定在 `export/android-build`，APK 输出到 `export/android/金庸群侠传XR.apk`。测试签名密钥保存在 `export/android/jyxr-test.keystore` 并复用，仅用于测试；无法覆盖不同签名的安装版本。正式签名不传 `-TestSigning`，在当前进程环境配置 `GODOT_ANDROID_KEYSTORE_RELEASE_PATH`、`GODOT_ANDROID_KEYSTORE_RELEASE_USER`、`GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD`。

首次使用时，将 `tools/ExportGame.example.psd1` 复制为 `tools/ExportGame.local.psd1`，填写本机 Windows 和 Android 模板的绝对路径。本地配置已被 Git 忽略；不要将本机路径写入示例文件或导出预设。`Godot` 可填写 PATH 中的命令名或可执行文件绝对路径，默认命令为 `Godot_v4.7.2-stable_mono_win64.exe`。命令行的 `-Godot`、`-WindowsTemplate`、`-AndroidTemplate` 优先于本地配置；未指定目标平台模板时脚本会明确报错。

脚本按仓库位置计算 `export/android-build` 的绝对路径，临时调整导出预设、按需生成 Godot 要求的 `.sln`，结束时恢复预设并移除本次生成的 `.sln`；日志保存在 `export/<target>-export.log`。脚本不复制 MOD 或玩家存档，共享 PCK 仍需与对应 MOD 数据一起分发。

[导出预设](../src/Game.Godot/export_presets.cfg) 分为三项，输出路径相对于 Godot 工程根：

| 预设 | 默认输出（相对于仓库根） | 内容 |
| --- | --- | --- |
| `Windows Base` | `export/windows/金庸群侠传XR.exe` | Windows 宿主、场景、脚本与启动器等基础资源 |
| `Android Base` | `export/android/金庸群侠传XR.apk` | Android 宿主及相同的本体资源 |
| `Shared Resources PCK` | `export/shared/base.pck` | `assets/animation`、`assets/art`、`assets/audio` |

本体使用“导出所有资源”并以 `assets/animation/*,assets/art/*,assets/audio/*` 排除共用资源；PCK 使用“导出所选资源”（不逐项勾选），以相同通配符加入资源。Godot 的 `*` 匹配完整路径，包含子目录，不需要 `**`。PCK 排除 `.uid` 辅助文件，导入资源仍由 Godot 正常处理。新增这三个目录内的资源会自动纳入 PCK。字体、启动器图片、shader、theme 和 video 仍随本体导出。

PCK 预设借用 Windows 导出平台，保留现有纹理配置与 `extra_pck` 特性；编辑器中使用 **Export PCK/ZIP**，不要使用 **Export Project**。命令行使用上面的 `-Target Shared`，脚本会自动创建输出目录并调用 `--export-pack`。

发布时将同一份 `base.pck` 放入对应 MOD 目录，并在该 MOD 的 `mod.json` 中通过 `packs` 声明相对路径（例如 `"packs": ["base.pck"]`）。仅把 PCK 放在本体旁不会自动加载；`mods` 数据也需单独分发。Windows 的数据根为可执行文件目录，Android 为 `/storage/emulated/0/JYXR`。Windows 本体导出的配套 PCK 与 .NET 数据目录也需一同分发。
