# UI 自适应重构进度

## 当前阶段

第五轮：迁移 `SystemPanel` 主体内容到 `DesignCanvas`，让系统设置、命令行和存读档入口在拉伸窗口时保持设计区定位。

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
- 第四轮：审计 `JyPanel` 继承风险，并将存档槽选择面板扩展为 30 槽滚动列表。
  - 主要文件：`scenes/ui/system_panel/save_slot_selection_panel.tscn`、`src/Game.Godot/UI/System/SaveSlotSelectionPanel.cs`、`src/Game.Godot/UI/System/SaveSlotCard.cs`、`src/Game.Godot/Persistence/LocalSaveStore.cs`。
  - 存档槽位改为运行时动态生成，手动槽位为 `存档1` 到 `存档30`。

## 第四轮目标

- 审计 `JyPanel` 的继承场景和基础节点路径。
- 不直接移动 `JyPanel` 的 `BackGround`、`CloseButton`，避免破坏继承场景覆盖。
- 先将 `SaveSlotSelectionPanel` 自己的标题、槽位网格、复选框迁入 `DesignCanvas`。
- 将存档槽位网格包进 `ScrollContainer`，为自动存档和后续更多存档槽位预留滚动选择能力。
- 将本地手动存档槽数量从 4 扩展为 30。
- 将存档卡片从场景预放改为运行时动态生成。
- 保持 `SaveSlotSelectionPanel.cs` 存档、读档、删档逻辑不变。
- 保持 `%TitleLabel`、`%SubtitleLabel`、`%SlotsGrid`、`%SkipConfirmationCheckBox` 唯一节点名不变。

## 第四轮不做

- 不迁地图点位坐标。
- 不迁战斗棋盘和行动栏。
- 不重做 `JyPanel` 基础节点。
- 不迁 `system_panel.tscn` 左侧系统菜单。
- 不调整 `SaveSlotSelectionPanel.cs` 的存档执行语义。
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
| P1 | SaveSlotSelectionPanel | 存档槽选择内容按根屏幕固定坐标摆放，且固定 4 个槽位 | 已完成 |
| P1 | SystemPanel | 设置、命令行、返回与存读档入口直接按根屏幕固定坐标摆放 | 已完成 |
| P1 | JyPanel | 背景和关闭按钮使用固定坐标，影响所有继承面板 | 已审计，待设计安全迁移 |

## 第四轮改动记录

- 审计 `JyPanel` 继承场景，确认多个继承场景仍直接覆盖 `BackGround` 或 `CloseButton`。
  - 因此暂不移动 `JyPanel` 基础节点。
- 修改 `scenes/ui/system_panel/save_slot_selection_panel.tscn`。
  - 在 `SaveSlotSelectionPanel` 下新增 `DesignCanvas/DesignRoot`。
  - 将 `TitleLabel`、`SubtitleLabel`、`SkipConfirmationCheckBox` 移入 `DesignRoot`。
  - 新增 `SlotsScroll`，作为存档卡片滚动视口。
  - 将 `SlotsGrid` 移入 `SlotsScroll`。
  - 移除场景中预放的 `SlotCard1` 到 `SlotCard4`。
  - 禁用横向滚动，只保留纵向滚动能力。
  - 保留所有脚本依赖节点的 `unique_name_in_owner = true`。
- 修改 `src/Game.Godot/Persistence/LocalSaveStore.cs`。
  - `SlotCount` 从 4 扩展为 30。
- 修改 `src/Game.Godot/UI/System/SaveSlotSelectionPanel.cs`。
  - 根据 `LocalSaveStore.SlotCount` 动态生成存档卡片。
  - 自动存档卡片仍只在读档模式出现，并插入到列表首位。
- 修改 `src/Game.Godot/UI/System/SaveSlotCard.cs`。
  - 重复实例内部节点获取从 `%TitleLabel` 等唯一节点名改为卡片内部相对路径。
  - 避免 30 个重复卡片实例之间唯一节点名解析不稳定，导致标题都显示为场景默认的“存档1”。

## 第四轮验证记录

- 静态检查确认 `SaveSlotSelectionPanel.cs` 只依赖 `%TitleLabel`、`%SubtitleLabel`、`%SkipConfirmationCheckBox`、`%SlotsGrid`。
- 静态检查确认迁移后这些节点仍保留 `unique_name_in_owner = true`。
- 静态检查确认自动存档卡片仍会通过 `_slotsGrid.AddChild(card)` 加入滚动区域内部。
- 静态检查确认已无 `%SlotCard1` 到 `%SlotCard4` 固定节点依赖。
- 静态检查确认 `SaveSlotCard.Configure(...)` 仍按 `summary.SlotIndex` 设置标题：`存档{summary.SlotIndex}`。
- 本轮改动限于 Godot 宿主层存档槽位数量、UI 生成与场景结构，未改核心游戏规则。

后续验证方式改为：Codex 做静态检查，用户在 Godot 运行时手动验证视觉和交互。

本轮仍需要在 Godot 编辑器或可用运行环境中做一次视觉验证：

- `1920x1080`：存档槽选择内容位置应接近改前效果。
- `1920x1200`：标题、槽位卡片、跳过确认复选框应保持在设计安全画布内，不随 expanded 画布漂移。
- `3440x1440`：存档槽选择内容应保持在居中的 `16:9` 设计区域内。
- 存档/读档/删档模式下应显示 30 个手动存档槽。
- 读档模式下出现自动存档时，列表首位应是自动存档，后面是存档 1 到存档 30。
- 手动槽位标题应按顺序显示为 `存档1`、`存档2`、`存档3`、`存档4`，继续向下滚动直到 `存档30`。
- 列表应可通过纵向滚动查看全部槽位，不直接溢出到底部背景外。
- 存档、读档、删档、自动存档卡片插入、跳过确认复选框行为应不变。

## 第五轮目标

- 将 `SystemPanel` 的设置区、命令行区、返回按钮和底部动作按钮组迁入 `DesignCanvas/DesignRoot`。
- 保留 `SystemPanel` 自身全屏背景图与遮罩在根节点下，用于继续覆盖整个窗口。
- 只调整 Godot 场景节点布局和节点查找路径，不修改设置、命令行、存档、读档、删档、返回主菜单等业务逻辑。
- 继续让 `Save` / `Load` / `Delete` 按钮打开上一轮迁移后的 30 槽存档选择面板。

## 第五轮改动记录

- 修改 `scenes/ui/system_panel/system_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将 `BackButton`、`SettingsVBox`、`ConsoleVBox`、`ActionRow` 移入 `DesignRoot`。
  - 将主菜单、删档、存档、读档四个动作按钮及其图标/文字子节点统一挂到 `DesignCanvas/DesignRoot/ActionRow` 下。
  - 保留 `Background` 与 `Backdrop` 为根节点全屏铺底，不纳入设计画布缩放。
- 修改 `src/Game.Godot/UI/System/SystemPanel.cs`。
  - `_consoleRoot` 从固定路径查找改为 `%ConsoleVBox` 唯一节点查找，适配场景层级移动。

## 第五轮验证记录

- 静态检查确认 `SystemPanel.cs` 中设置、命令行和动作按钮仍通过唯一节点名获取。
- 静态检查确认 `ConsoleVBox` 已设置 `unique_name_in_owner = true`。
- 静态检查确认 `ActionRow` 下面已无旧的 `parent="ActionRow..."` 残留路径。
- 本轮只迁移系统面板 UI 层级，未改核心游戏规则、存档服务、设置保存逻辑或剧情命令逻辑。

本轮手动验证：

- 打开系统面板。
- 拉伸窗口到 `1920x1080`、`1920x1200`、`3440x1440` 或任意宽屏比例。
- 确认命令行区、左侧设置项、右上返回游戏按钮、右下主菜单/删档/存档/读档按钮都保持在居中的 16:9 设计区域内。
- 点击存档、读档、删档，确认仍能打开上一轮的 30 槽滚动存档选择面板。
- 输入一条命令行无效指令，确认错误输出仍显示在命令行输出区。

## 下一阶段候选

完成本轮后，优先从以下二选一继续：

- 迁移 `JyPanel`，为背包、商店、储物箱等主面板打基础。
- 迁移剧情对白框，继续处理底部锚点类 UI。
