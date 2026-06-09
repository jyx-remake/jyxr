# UI 自适应重构进度

## 当前阶段

第三轮最小闭环：复用设计画布迁移 ConfirmDialog 确认弹窗。

## 已完成阶段

- 第一轮：建立 `DesignCanvas`，并将 HUD 右下 `MenueButtonGroup` 迁入设计画布右下锚点。
  - 主要文件：`src/Game.Godot/UI/Layout/DesignCanvas.cs`、`scenes/ui/hud/hud.tscn`。
  - 用户已确认视觉效果可以接受，并已提交。
- 第二轮：将 `ToastPanel` 背景图与文本迁入 `DesignCanvas` 顶部中间区域。
  - 主要文件：`autoload/ui_root.tscn`。
  - 用户已确认拉伸时保持中央，效果可以接受。

## 本轮目标

- 将 `ConfirmDialog` 的背景、内容文本、确认按钮和取消按钮迁入 `DesignCanvas`。
- 保持确认弹窗在 `1920x1080` 设计画布中心区域。
- 保持 `ConfirmDialog.cs` 显隐与等待结果逻辑不变。
- 保持 `%ContentLabel`、`%ConfirmButton`、`%CancelButton` 唯一节点名不变。

## 本轮不做

- 不迁地图点位坐标。
- 不迁战斗棋盘和行动栏。
- 不重做 `JyPanel`。
- 不调整 `ConfirmDialog.cs` 等待结果逻辑。
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
| P0 | HUD 右下按钮组 | 固定 `offset_left = 1730`、`offset_top = 885`，非 16:9 下不是真正贴边 | 已完成 |
| P0 | 地图点位 | 背景 stretch 与点位坐标换算不是同一模型 | 待处理 |
| P0 | 战斗棋盘 | 棋盘、单位、飘字、技能动画必须共享坐标转换 | 待处理 |
| P1 | Toast | 背景与文字固定全屏坐标，宽高比变化后可能不居中 | 已完成 |
| P1 | ConfirmDialog | 弹窗背景虽已居中，但按钮和文本仍按根屏幕固定坐标摆放 | 本轮处理 |
| P1 | JyPanel | 背景和关闭按钮使用固定坐标，影响所有继承面板 | 待处理 |

## 本轮改动记录

- 修改 `scenes/ui/base/confirm_dialog.tscn`。
  - 在 `ConfirmDialog` 下新增 `DesignCanvas/DesignRoot`。
  - 将背景图、`ContentLabel`、`ConfirmButton` 和 `CancelButton` 移入 `DesignRoot`。
  - 保留 `ContentLabel`、`ConfirmButton`、`CancelButton` 的 `unique_name_in_owner = true`。
  - `ConfirmDialog.cs` 不需要修改。

## 本轮验证记录

- 静态检查确认 `ConfirmDialog.cs` 只依赖 `%ContentLabel`、`%ConfirmButton`、`%CancelButton`。
- 静态检查确认迁移后这三个节点仍保留 `unique_name_in_owner = true`。
- 本轮只改场景节点结构，未改 C# 业务逻辑。

本轮仍需要在 Godot 编辑器或可用运行环境中做一次视觉验证：

- `1920x1080`：确认弹窗应显示在屏幕中央，位置接近改前效果。
- `1920x1200`：确认弹窗应保持在设计安全画布中央，不随 expanded 画布漂移。
- `3440x1440`：确认弹窗应保持在居中的 `16:9` 设计区域中央。
- 覆盖存档、删除存档、商店购买或卖出触发确认后，确认/取消按钮行为应不变。

## 下一阶段候选

完成本轮后，优先从以下二选一继续：

- 迁移 `JyPanel`，为背包、商店、储物箱等主面板打基础。
- 迁移剧情对白框，继续处理底部锚点类 UI。
