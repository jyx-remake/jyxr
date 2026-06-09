# UI 自适应重构进度

## 当前阶段

第六轮已完成代码侧迁移，等待 Godot 运行时手动验证。

本轮范围：

- `JyPanel` 基础背景与关闭按钮进入设计画布。
- 背包、商店、储物箱、队伍、日志、英雄面板主体内容进入各自 `DesignCanvas/DesignRoot`。
- 不做构建验证；本阶段以后由 Codex 做静态检查，用户在 Godot 运行时做视觉和交互验证。

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
- 第五轮：迁移 `SystemPanel` 主体内容到 `DesignCanvas`。
  - 主要文件：`scenes/ui/system_panel/system_panel.tscn`、`src/Game.Godot/UI/System/SystemPanel.cs`。
  - 用户已确认拉伸下视觉正常。
- 第六轮：迁移 `JyPanel` 基础壳与第一批主面板。
  - 主要文件：`scenes/ui/base/jy_panel.tscn`、`scenes/ui/inventory_panel/inventory_panel.tscn`、`scenes/ui/shop_panel/shop_panel.tscn`、`scenes/ui/chest_panel/chest_panel.tscn`、`scenes/ui/party_panel/party_panel.tscn`、`scenes/ui/journal/journal_panel.tscn`、`scenes/ui/hero_panel/hero_panel.tscn`。
  - 等待用户运行时手动验证。

## 验证矩阵

后续每轮 UI 迁移优先验证：

- `1920x1080`
- `1366x768`
- `1920x1200`
- `3440x1440`

验证方式：

- Codex 做静态路径、节点唯一名和场景结构检查。
- 用户在 Godot 运行时手动验证视觉和交互。
- 当前阶段按用户要求不跑 `dotnet build` / `dotnet test`。

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
| P1 | JyPanel | 背景和关闭按钮使用固定坐标，影响所有继承面板 | 基础壳已完成 |
| P1 | InventoryPanel | 背包内容直接挂根节点固定坐标 | 已完成 |
| P1 | ShopPanel | 商店内容直接挂根节点固定坐标 | 已完成 |
| P1 | ChestPanel | 储物箱内容直接挂根节点固定坐标 | 已完成 |
| P1 | PartyPanel | 队伍内容直接挂根节点固定坐标 | 已完成 |
| P1 | JournalPanel | 日志内容直接挂根节点固定坐标 | 已完成 |
| P1 | HeroPanel | 英雄面板内容直接挂根节点固定坐标 | 已完成 |
| P1 | CharacterPanel | 角色主面板仍有根级固定坐标内容 | 待处理 |
| P1 | ItemTargetSelectionPanel | 物品目标选择弹窗仍有根级固定坐标内容 | 待处理 |
| P1 | CharacterEquipmentSelectionPanel | 装备选择弹窗仍有根级固定坐标内容 | 待处理 |
| P1 | CombatantSelectPanel | 出战选择面板仍有根级固定坐标内容 | 待处理 |
| P1 | BattleItemPanel / BattleSettlementPanel | 战斗弹窗仍有根级固定坐标内容 | 待处理 |

## 第六轮改动记录

- 修改 `scenes/ui/base/jy_panel.tscn`。
  - 新增 `PanelBackdropCanvas/DesignRoot`，承载通用 `BackGround`。
  - 新增 `PanelChromeCanvas/DesignRoot`，承载通用 `CloseButton`。
  - `PanelChromeCanvas` 使用较高 `z_index`，确保关闭按钮压在子面板内容之上。
  - 关闭按钮信号连接改为 `PanelChromeCanvas/DesignRoot/CloseButton`。
- 修改继承 `JyPanel` 且覆盖基础节点的场景。
  - `ChestPanel`、`CharacterPanel` 的 `BackGround` 覆盖路径改为 `PanelBackdropCanvas/DesignRoot`。
  - `ShopPanel`、`ChestPanel`、`HeroPanel`、`CombatantSelectPanel`、`BattleSettlementPanel` 的 `CloseButton` 覆盖路径改为 `PanelChromeCanvas/DesignRoot`。
  - 移除 `PartyPanel` 中指向旧根级 `CloseButton` 的残留连接。
- 修改 `scenes/ui/inventory_panel/inventory_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将标题、分类按钮、列表头、滚动列表和空状态标签迁入设计画布。
- 修改 `scenes/ui/shop_panel/shop_panel.tscn`。
  - 保留 `BackgroundTexture` 与 `BackgroundDim` 为根节点全屏铺底。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将掌柜、标题、快速买入、买入/卖出/离开、分类按钮、商品列表、提示文本和货币栏迁入设计画布。
- 修改 `scenes/ui/chest_panel/chest_panel.tscn`。
  - 保留 `BackgroundTexture` 与 `BackgroundDim` 为根节点全屏铺底。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将掌柜、标题、存入/取出/离开、分类按钮、物品列表、提示文本和容量标签迁入设计画布。
- 修改 `scenes/ui/party_panel/party_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将队伍滚动列表、拖拽提示和空状态标签迁入设计画布。
- 修改 `scenes/ui/journal/journal_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将标题、日志滚动列表和空状态标签迁入设计画布。
- 修改 `scenes/ui/hero_panel/hero_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将顶部 tab 按钮组和 `HeroTabContainer` 迁入设计画布。

## 第六轮静态检查

- `JyPanel` 基类信号连接已指向 `PanelChromeCanvas/DesignRoot/CloseButton`。
- 本轮覆盖的 `BackGround`、`CloseButton` 不再使用根级父节点路径。
- 背包、商店、储物箱、队伍、日志、英雄面板根级内容只保留预期的 `DesignCanvas`。
- 商店与储物箱保留根级 `BackgroundTexture` 和 `BackgroundDim`，用于继续铺满整个窗口。
- 背包、商店、储物箱、队伍、日志、英雄面板迁移后无旧的 `CategoryButtons`、`ModeButtons`、`GoodsPanel`、`Header`、`ScrollContainer`、`TabButtonRow`、`HeroTabContainer` 父路径残留。
- 脚本依赖节点仍保留 `unique_name_in_owner = true`。
- 按用户要求，本轮不做构建验证。

## 第六轮手动验证清单

- 打开背包面板，确认标题、分类按钮、列表头、物品列表和空状态都保持在居中 16:9 设计区内。
- 背包分类切换、物品 tooltip、使用物品打开目标选择入口应不变。
- 打开商店，确认背景仍铺满窗口，掌柜、分类、商品列表、提示文本和货币栏保持在设计区内。
- 商店买入、卖出、快速买入、离开行为应不变。
- 打开储物箱，确认背景仍铺满窗口，分类、物品列表、提示文本和容量标签保持在设计区内。
- 储物箱存入、取出、容量显示和离开行为应不变。
- 打开队伍面板，确认队伍卡片列表、拖拽提示、空状态位于设计区内，拖拽排序仍正常。
- 打开日志面板，确认标题、日志列表和空状态位于设计区内，长列表可滚动。
- 打开英雄面板，确认 tab 按钮、江湖历练、成就、武学精通页位于设计区内，tab 切换仍正常。
- 抽查继承 `JyPanel` 但内容尚未迁移的面板，确认关闭按钮仍可点击：角色面板、装备选择、目标选择、出战选择、战斗物品、战斗结算。

## 后续计划

建议下一步继续补齐尚未迁完的 `JyPanel` 子面板，然后再进入剧情 UI：

1. 迁移角色相关面板。
   - `scenes/ui/character_panel/character_panel.tscn`
   - `scenes/ui/character_panel/equipment_selection_panel.tscn`
   - 目标：角色主面板、装备选择弹窗与 `JyPanel` 背景保持同一设计坐标系。
2. 迁移物品目标选择弹窗。
   - `scenes/ui/inventory_panel/item_target_selection_panel.tscn`
   - 目标：背包使用物品后的目标选择不再停留在根级固定坐标。
3. 迁移战斗入口与战斗弹窗。
   - `scenes/ui/battle/combatant_select_panel.tscn`
   - `scenes/ui/battle/battle_item_panel.tscn`
   - `scenes/ui/battle/battle_settlement_panel.tscn`
   - 目标：先处理 `JyPanel` 弹窗类战斗 UI，不碰战斗棋盘坐标系统。
4. 迁移剧情对白与选项。
   - `scenes/ui/story/story_dialogue_panel.tscn`
   - `scenes/ui/story/story_choice_panel.tscn`
   - 目标：对白框贴底部安全区，选项框与对白框关系稳定。
5. 迁移开局流程。
   - 门派选择、输入名字、头像选择、随机属性。
   - 目标：开局 UI 全部进入设计画布。
6. 单独迁地图 UI。
   - 地图背景显示 rect、点位、主角 pin、底部信息区共享坐标转换。
7. 单独迁战斗棋盘和行动栏。
   - 棋盘格、单位、hover、可达区、飘字、技能动画共享同一个 transform。
