# UI 自适应重构进度

## 当前阶段

第四轮：审计 `JyPanel` 继承风险，并迁移存档槽选择面板内容区与滚动区域。

## 已完成阶段

- 第一轮：建立 `DesignCanvas`，并将 HUD 右下 `MenueButtonGroup` 迁入设计画布右下锚点。
  - 主要文件：`src/Game.Godot/UI/Layout/DesignCanvas.cs`、`scenes/ui/hud/hud.tscn`。
  - 用户已确认视觉效果可以接受，并已提交。
- 第二轮：将 `ToastPanel` 背景图与文本迁入 `DesignCanvas` 顶部中间区域。
  - 主要文件：`autoload/ui_root.tscn`。
  - 用户已确认拉伸时保持中央，效果可以接受。
- 第三轮：将 `ConfirmDialog` 确认弹窗迁入 `DesignCanvas` 中央区域。
  - 主要文件：`scenes/ui/base/confirm_dialog.tscn`。
  - 用户已确认这是确认弹窗本体迁移，后面的存档 UI 尚未迁移。

## 本轮目标

- 审计 `JyPanel` 的继承场景和基础节点路径。
- 不直接移动 `JyPanel` 的 `BackGround`、`CloseButton`，避免破坏继承场景覆盖。
- 先将 `SaveSlotSelectionPanel` 自己的标题、槽位网格、复选框迁入 `DesignCanvas`。
- 将存档槽位网格包进 `ScrollContainer`，为自动存档和后续更多存档槽位预留滚动选择能力。
- 保持 `SaveSlotSelectionPanel.cs` 存档、读档、删档逻辑不变。
- 保持 `%TitleLabel`、`%SubtitleLabel`、`%SlotsGrid`、`%SlotCard1` 到 `%SlotCard4`、`%SkipConfirmationCheckBox` 唯一节点名不变。

## 本轮不做

- 不迁地图点位坐标。
- 不迁战斗棋盘和行动栏。
- 不重做 `JyPanel` 基础节点。
- 不迁 `system_panel.tscn` 左侧系统菜单。
- 不调整 `SaveSlotSelectionPanel.cs` 存档业务逻辑。
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
| P1 | ConfirmDialog | 弹窗背景虽已居中，但按钮和文本仍按根屏幕固定坐标摆放 | 已完成 |
| P1 | SaveSlotSelectionPanel | 存档槽选择内容按根屏幕固定坐标摆放 | 本轮处理 |
| P1 | JyPanel | 背景和关闭按钮使用固定坐标，影响所有继承面板 | 已审计，待设计安全迁移 |

## 本轮改动记录

- 审计 `JyPanel` 继承场景，确认多个继承场景仍直接覆盖 `BackGround` 或 `CloseButton`。
  - 因此暂不移动 `JyPanel` 基础节点。
- 修改 `scenes/ui/system_panel/save_slot_selection_panel.tscn`。
  - 在 `SaveSlotSelectionPanel` 下新增 `DesignCanvas/DesignRoot`。
  - 将 `TitleLabel`、`SubtitleLabel`、`SkipConfirmationCheckBox` 移入 `DesignRoot`。
  - 新增 `SlotsScroll`，作为存档卡片滚动视口。
  - 将 `SlotsGrid`、`SlotCard1` 到 `SlotCard4` 移入 `SlotsScroll`。
  - 禁用横向滚动，只保留纵向滚动能力。
  - 保留所有脚本依赖节点的 `unique_name_in_owner = true`。
  - `SaveSlotSelectionPanel.cs` 不需要修改。

## 本轮验证记录

- 静态检查确认 `SaveSlotSelectionPanel.cs` 只依赖 `%TitleLabel`、`%SubtitleLabel`、`%SkipConfirmationCheckBox`、`%SlotsGrid`、`%SlotCard1` 到 `%SlotCard4`。
- 静态检查确认迁移后这些节点仍保留 `unique_name_in_owner = true`。
- 静态检查确认自动存档卡片仍会通过 `_slotsGrid.AddChild(card)` 加入滚动区域内部。
- 本轮只改场景节点结构，未改 C# 业务逻辑。

本轮仍需要在 Godot 编辑器或可用运行环境中做一次视觉验证：

- `1920x1080`：存档槽选择内容位置应接近改前效果。
- `1920x1200`：标题、槽位卡片、跳过确认复选框应保持在设计安全画布内，不随 expanded 画布漂移。
- `3440x1440`：存档槽选择内容应保持在居中的 `16:9` 设计区域内。
- 读档模式下出现自动存档时，第三行卡片应可通过纵向滚动查看，不再直接溢出到底部背景外。
- 存档、读档、删档、自动存档卡片插入、跳过确认复选框行为应不变。

## 下一阶段候选

完成本轮后，优先从以下二选一继续：

- 迁移 `JyPanel`，为背包、商店、储物箱等主面板打基础。
- 迁移剧情对白框，继续处理底部锚点类 UI。
