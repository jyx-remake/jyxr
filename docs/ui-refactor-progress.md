# UI 自适应重构进度

## 当前阶段

第一轮最小闭环：建立设计画布基础，并优先修复 HUD 右下按钮组的贴边错位问题。

## 本轮目标

- 新增可复用的 `DesignCanvas` 控件。
- 将 HUD 右下菜单按钮组迁入设计画布内的右下锚点。
- 保持 HUD 现有按钮行为不变。
- 通过构建验证本轮代码没有破坏编译。

## 本轮不做

- 不迁地图点位坐标。
- 不迁战斗棋盘和行动栏。
- 不重做 `JyPanel`。
- 不调整应用层服务、存档、剧情命令和战斗内核。

## 验证矩阵

后续每轮 UI 迁移优先验证：

- `1920x1080`
- `1366x768`
- `1920x1200`
- `3440x1440`

## 风险清单

| 等级 | 区域 | 风险 | 状态 |
| --- | --- | --- | --- |
| P0 | HUD 右下按钮组 | 固定 `offset_left = 1730`、`offset_top = 885`，非 16:9 下不是真正贴边 | 本轮处理 |
| P0 | 地图点位 | 背景 stretch 与点位坐标换算不是同一模型 | 待处理 |
| P0 | 战斗棋盘 | 棋盘、单位、飘字、技能动画必须共享坐标转换 | 待处理 |
| P1 | Toast | 背景与文字固定全屏坐标，宽高比变化后可能不居中 | 待处理 |
| P1 | JyPanel | 背景和关闭按钮使用固定坐标，影响所有继承面板 | 待处理 |

## 本轮改动记录

- 新增 `src/Game.Godot/UI/Layout/DesignCanvas.cs`。
  - 固定设计尺寸默认为 `1920x1080`。
  - 自动按当前控件尺寸等比缩放内部 `DesignRoot`。
  - 自动居中 `DesignRoot`。
- 修改 `scenes/ui/hud/hud.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将 `MenueButtonGroup` 移入 `DesignRoot`。
  - 将 `MenueButtonGroup` 改为设计画布内右下锚点。
  - 保留 `HeroButton`、`TeamButton`、`BackpackButton`、`LogButton`、`SystemButton` 唯一节点名，避免影响 `HudPanel.cs` 的节点获取。

## 本轮验证记录

- `dotnet --version` 成功，当前 SDK 为 `10.0.300`。
- `dotnet build engine-free-rpg.csproj` 未能完成有效验证。
  - 命令在约 5 分钟后返回 `Build FAILED`。
  - 输出为 `0 Warning(s)`、`0 Error(s)`，没有具体编译错误。
- `dotnet build engine-free-rpg.csproj --no-restore` 同样在约 5 分钟后返回 `Build FAILED`。
  - 输出为 `0 Warning(s)`、`0 Error(s)`，没有具体编译错误。
- `/Users/zheng/Downloads/Godot.app/Contents/MacOS/Godot --headless --path . --quit` 可启动 Godot 并退出。
  - 当前 headless 运行未加载 C# script loader，输出为全项目既有 C# 脚本资源无法加载。
  - 该输出不是本轮新增 `DesignCanvas.cs` 独有问题。

本轮仍需要在 Godot 编辑器或可用的 C# 构建环境中做一次视觉验证：

- `1920x1080`：右下按钮组应与改前视觉位置一致。
- `1920x1200`：右下按钮组应保持在设计安全画布右下，不随 expanded 画布漂移。
- `3440x1440`：右下按钮组应保持在居中的 `16:9` 设计区域右下。
- HUD 五个入口按钮点击行为应不变。

## 下一阶段候选

完成本轮后，优先从以下二选一继续：

- 迁移 `ToastPanel` 或 `ConfirmDialog`，验证 overlay 类 UI。
- 迁移 `JyPanel`，为背包、商店、储物箱等主面板打基础。
