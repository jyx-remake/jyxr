# UI 自适应重构进度

## 当前阶段

第二轮最小闭环：复用设计画布迁移 Toast 覆盖层。

## 已完成阶段

- 第一轮：建立 `DesignCanvas`，并将 HUD 右下 `MenueButtonGroup` 迁入设计画布右下锚点。
  - 主要文件：`src/Game.Godot/UI/Layout/DesignCanvas.cs`、`scenes/ui/hud/hud.tscn`。
  - 用户已确认视觉效果可以接受，并已提交。

## 本轮目标

- 将 `ToastPanel` 的背景图与文本迁入 `DesignCanvas`。
- 让 toast 锚到 `1920x1080` 设计画布的顶部中间区域。
- 保持 `ToastPanel.cs` 队列、显隐、淡入淡出逻辑不变。
- 保持 `%MessageLabel` 唯一节点名不变。

## 本轮不做

- 不迁地图点位坐标。
- 不迁战斗棋盘和行动栏。
- 不重做 `JyPanel`。
- 不调整 `ToastPanel.cs` 播放逻辑。
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
| P1 | Toast | 背景与文字固定全屏坐标，宽高比变化后可能不居中 | 本轮处理 |
| P1 | JyPanel | 背景和关闭按钮使用固定坐标，影响所有继承面板 | 待处理 |

## 本轮改动记录

- 修改 `autoload/ui_root.tscn`。
  - 在 `OverlayLayer/ToastPanel` 下新增 `DesignCanvas/DesignRoot`。
  - 新增 `ToastRoot`，锚到设计画布顶部中间。
  - 将 toast 背景图移动到 `ToastRoot` 下。
  - 将 `MessageLabel` 移动到 `ToastRoot` 下，并保留 `unique_name_in_owner = true`。
  - `ToastPanel.cs` 不需要修改。

## 本轮验证记录

- 静态检查确认 `ToastPanel.cs` 只依赖 `%MessageLabel`。
- 静态检查确认迁移后 `MessageLabel` 仍保留 `unique_name_in_owner = true`。
- 本轮只改场景节点结构，未改 C# 业务逻辑。

本轮仍需要在 Godot 编辑器或可用运行环境中做一次视觉验证：

- `1920x1080`：toast 应显示在屏幕上方居中区域。
- `1920x1200`：toast 应保持在设计安全画布顶部中间，不随 expanded 画布漂移。
- `3440x1440`：toast 应保持在居中的 `16:9` 设计区域顶部中间。
- 获得物品、角色升级、称号解锁触发 toast 后，队列播放和淡入淡出应不变。

## 下一阶段候选

完成本轮后，优先从以下二选一继续：

- 迁移 `ConfirmDialog`，继续验证 overlay 类 UI。
- 迁移 `JyPanel`，为背包、商店、储物箱等主面板打基础。
