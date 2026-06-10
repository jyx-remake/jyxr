# UI 自适应重构进度

## 当前阶段

第九轮已完成代码侧迁移，等待 Godot 运行时手动验证。

本轮范围：

- 剧情选项框主体内容进入 `DesignCanvas/DesignRoot`。
- 保留原有选项背景、头像、问题文本和选项列表的设计稿坐标与缩放。
- 不修改 `StoryChoicePanel.cs` 和 `StoryChoiceButton.cs` 行为逻辑，保持选项生成、默认焦点和点击选择行为。
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
- 第七轮：迁移剩余 `JyPanel` 子面板。
  - 主要文件：`scenes/ui/character_panel/character_panel.tscn`、`scenes/ui/character_panel/equipment_selection_panel.tscn`、`scenes/ui/inventory_panel/item_target_selection_panel.tscn`、`scenes/ui/battle/combatant_select_panel.tscn`、`scenes/ui/battle/battle_item_panel.tscn`、`scenes/ui/battle/battle_settlement_panel.tscn`。
  - 等待用户运行时手动验证。
- 第八轮：迁移剧情对白框。
  - 主要文件：`scenes/ui/story/story_dialogue_panel.tscn`。
  - 等待用户运行时手动验证。
- 第九轮：迁移剧情选项框。
  - 主要文件：`scenes/ui/story/story_choice_panel.tscn`。
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

## 后续迁移执行规则

从第八轮开始，剧情、开局流程、地图和战斗主界面都按小步迁移处理，不再把多个高风险区域合并到同一轮。

- 每轮只迁一个明确目标，例如“剧情对白框”、“剧情选项框”、“门派选择界面”、“地图背景与点位 transform”。
- 每轮完成后停止继续开发，先记录改动范围、静态检查结果和手动验证清单。
- Codex 不自动 stage、不自动 commit；每轮结束时只提供一条建议的 Conventional Commit 提示词，由用户手动提交。
- 用户手动验证通过并确认继续后，再进入下一轮迁移。
- 高风险区域优先做结构性迁移，不做按分辨率堆叠的特殊偏移补丁。
- 地图和战斗阶段必须先确认共享坐标模型，再迁移依赖该模型的点击、hover、飘字或动画。

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
| P1 | CharacterPanel | 角色主面板仍有根级固定坐标内容 | 已完成 |
| P1 | ItemTargetSelectionPanel | 物品目标选择弹窗仍有根级固定坐标内容 | 已完成 |
| P1 | CharacterEquipmentSelectionPanel | 装备选择弹窗仍有根级固定坐标内容 | 已完成 |
| P1 | CombatantSelectPanel | 出战选择面板仍有根级固定坐标内容 | 已完成 |
| P1 | BattleItemPanel / BattleSettlementPanel | 战斗弹窗仍有根级固定坐标内容 | 已完成 |
| P1 | StoryDialoguePanel / StoryChoicePanel | 剧情对白和选项仍需进入底部安全布局，长文本与点击继续行为要保持 | 待处理 |
| P1 | StartQuestion / SelectSect | 开局问卷流程场景仍需进入设计画布，不能改变剧情命令流程 | 待处理 |

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

## 第七轮改动记录

- 修改 `scenes/ui/character_panel/character_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将头像区、角色基础属性文本、五个分页按钮、`TabContainer` 和底部队伍快切条迁入设计画布。
  - 保持角色面板背景仍由 `JyPanel` 的 `PanelBackdropCanvas/DesignRoot/BackGround` 承载。
- 修改 `scenes/ui/character_panel/equipment_selection_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将标题、提示、装备列表和空状态标签迁入设计画布。
- 修改 `scenes/ui/inventory_panel/item_target_selection_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将标题、物品目标提示、角色列表和底部提示迁入设计画布。
- 修改 `scenes/ui/battle/combatant_select_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将标题、出战卡片列表、右侧人数信息和出战按钮迁入设计画布。
- 修改 `scenes/ui/battle/battle_item_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将标题、提示、数量、物品列表和空状态迁入设计画布。
- 修改 `scenes/ui/battle/battle_settlement_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将标题、奖励详情、奖励列表、空状态和确认按钮迁入设计画布。

## 第七轮静态检查

- 角色主面板、装备选择、物品目标选择、出战选择、战斗物品、战斗结算场景根级内容只保留预期的 `DesignCanvas`。
- 上述场景迁移后无旧的 `AvatarBox`、`Labels`、`Buttons`、`TabContainer`、`ScrollContainer`、`SidePanel`、`ConfirmButton` 父路径残留。
- 脚本依赖节点仍保留 `unique_name_in_owner = true`。
- 按用户要求，本轮不做构建验证。

## 第七轮手动验证清单

- 打开角色面板，确认头像、基础属性、五个分页按钮、分页内容区和底部队伍快切条都保持在居中 16:9 设计区内。
- 角色面板属性、装备、技能、天赋、传记 tab 切换应不变。
- 角色上一位 / 下一位切换应不变。
- 装备 tab 中点击装备槽位打开装备选择弹窗，确认标题、提示、列表和空状态位于设计区内。
- 背包使用物品打开目标选择弹窗，确认目标列表和提示位于设计区内。
- 地图或剧情进入战斗前打开出战选择，确认卡片列表、人数提示和出战按钮位于设计区内。
- 战斗中打开物品面板，确认标题、列表和空状态位于设计区内。
- 战斗结算面板中奖励详情、奖励列表和确认按钮位于设计区内。

## 第八轮改动记录

- 修改 `scenes/ui/story/story_dialogue_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将 `ShadowPanel`、`Frame`、`AvatarBox`、`SpeakerLabel`、`ContentLabel` 和 `SkipButton` 迁入设计画布。
  - 保留原有设计稿坐标和缩放，避免改变 `1920x1080` 基准视觉。
  - 保留所有 `unique_name_in_owner` 节点，`StoryDialoguePanel.cs` 仍通过 `%ShadowPanel`、`%AvatarBox`、`%SpeakerLabel`、`%ContentLabel`、`%SkipButton` 获取节点。

## 第八轮静态检查

- `StoryDialoguePanel` 根节点下只新增预期的 `DesignCanvas`。
- 对白框可交互节点已迁入 `DesignCanvas/DesignRoot`。
- `Frame` 继续忽略鼠标事件，正文 `ContentLabel` 继续接收鼠标输入。
- 脚本依赖节点仍保留 `unique_name_in_owner = true`。
- 按用户要求，本轮不做构建验证。

## 第八轮手动验证清单

- 进入任意剧情对白，确认对白框整体位于底部设计安全区内。
- 在 `1920x1080` 下确认对白框、头像、说话人、正文和跳过按钮位置与迁移前一致。
- 在 `1366x768`、`1920x1200`、`3440x1440` 下确认对白框整体随设计画布居中缩放，不偏到扩展画布边缘。
- 长对白仍可在正文区域滚轮滚动。
- 左键点击正文区域可以继续对白。
- 左键点击对白底部空白区域可以继续对白。
- 按确认键、选择键或提交键可以继续对白。
- 按住跳过按钮时可以跳过对白，松开后停止跳过。

## 第九轮改动记录

- 修改 `scenes/ui/story/story_choice_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将 `Background`、`AvatarBox`、`QuestionLabel`、`ChoiceScroll` 和 `ChoiceContainer` 迁入设计画布。
  - 保留原有设计稿坐标和缩放，避免改变 `1920x1080` 基准视觉。
  - 保留 `AvatarBox`、`QuestionLabel` 和 `ChoiceContainer` 的 `unique_name_in_owner`，`StoryChoicePanel.cs` 仍通过唯一节点名获取节点。
- 未修改 `scenes/ui/story/story_choice_button.tscn`。
  - 选项按钮仍作为 `ChoiceContainer` 下的动态列表项实例化。
  - 本轮不改变按钮尺寸、贴图、文本布局和点击行为。

## 第九轮静态检查

- `StoryChoicePanel` 根节点下只新增预期的 `DesignCanvas`。
- 选项框可见节点和动态选项容器已迁入 `DesignCanvas/DesignRoot`。
- `Background` 和 `QuestionLabel` 继续忽略鼠标事件，选项按钮点击仍由动态生成的 `StoryChoiceButton` 接收。
- 脚本依赖节点仍保留 `unique_name_in_owner = true`。
- 按用户要求，本轮不做构建验证。

## 第九轮手动验证清单

- 进入任意带选项的剧情，确认选项框整体位于居中的 16:9 设计区内。
- 在 `1920x1080` 下确认背景、头像、问题文本和选项列表位置与迁移前一致。
- 在 `1366x768`、`1920x1200`、`3440x1440` 下确认选项框随设计画布居中缩放，不偏到扩展画布边缘。
- 多个选项时列表仍可滚动。
- 默认焦点仍落在第一个选项上。
- 鼠标点击选项可以正常选择并关闭选项面板。
- 键盘或手柄切换焦点、确认选择行为不变。

## 后续计划

后续从第十轮开始继续推进，每一步都需要用户手动验证通过后再继续。

1. 第十轮：开局流程第一批。
   - 文件：`scenes/ui/select_sect/select_sect_screen.tscn`、`scenes/ui/start_question/input_name_panel.tscn`。
   - 目标：门派选择和输入名字进入设计画布，剧情命令接线不变。
   - 完成后建议提交标题：`重构UI：迁移开局选择布局`。
   - 完成后建议提交描述：`将门派选择和输入名字界面迁入设计画布，保持开局剧情命令流程和输入行为不变。`
2. 第十一轮：开局流程第二批。
   - 文件：`scenes/ui/start_question/select_head_panel.tscn`、`scenes/ui/start_question/select_head_slot.tscn`、`scenes/ui/start_question/roll_stats_panel.tscn`。
   - 目标：头像选择和随机属性界面进入设计画布，头像格与按钮区域稳定。
   - 完成后建议提交标题：`重构UI：迁移开局角色设置布局`。
   - 完成后建议提交描述：`将头像选择、头像格和随机属性界面迁入设计画布，保持头像选择和 roll stats 流程不变。`
3. 第十二轮：地图 UI 坐标模型预备。
   - 文件：`scenes/map/map_screen.tscn`、`src/Game.Godot` 中地图 UI 脚本。
   - 目标：先明确地图背景显示 rect 与点位/pin/click 的统一 transform，不迁无关面板。
   - 完成后建议提交标题：`重构UI：建立地图自适应坐标模型`。
   - 完成后建议提交描述：`明确地图背景显示区域与点位、主角标记、点击命中的统一坐标转换，为后续地图覆盖 UI 迁移做准备。`
4. 第十三轮：地图 UI 内容迁移。
   - 文件：`scenes/map/map_entity_box.tscn`、`scenes/map/map_entity_slot.tscn` 及地图底部信息区。
   - 目标：点位列表、底部信息区、主角 pin 与地图坐标模型一致。
   - 完成后建议提交标题：`重构UI：迁移地图覆盖层布局`。
   - 完成后建议提交描述：`将地图点位列表、底部信息区和主角标记接入统一地图坐标模型，保持地图交互和剧情隐藏行为不变。`
5. 第十四轮：战斗棋盘坐标模型预备。
   - 文件：`scenes/ui/battle/battle_screen.tscn`、`scenes/ui/battle/battle_board_view.tscn`、战斗棋盘脚本。
   - 目标：先统一棋盘格、单位、hover、可达区、飘字、技能动画使用的 transform。
   - 完成后建议提交标题：`重构UI：建立战斗棋盘自适应坐标模型`。
   - 完成后建议提交描述：`统一战斗棋盘格、单位、hover、可达区域、飘字和技能动画的坐标转换，为战斗行动 UI 迁移做准备。`
6. 第十五轮：战斗行动 UI 迁移。
   - 文件：`scenes/ui/battle/battle_skill_box.tscn`、`scenes/ui/battle/battle_skill_view.tscn`、`scenes/ui/battle/battle_legend_overlay.tscn`、`scenes/ui/battle/battle_float_text.tscn`。
   - 目标：技能栏、行动按钮、飘字和 overlay 在共享坐标模型下稳定显示。
   - 完成后建议提交标题：`重构UI：迁移战斗行动布局`。
   - 完成后建议提交描述：`将战斗技能栏、行动按钮、飘字和 overlay 接入设计画布与共享棋盘坐标模型，保持战斗操作行为不变。`
7. 收尾清理。
   - 文件：`scenes/main_menu/main_menu.tscn`、`scenes/ui/game_flow/gameover_screen.tscn`、`scenes/ui/game_flow/game_fin_screen.tscn`、`scenes/ui/character_summary_panel.tscn`、`scenes/ui/hint/hint_box.tscn`。
   - 目标：扫尾剩余固定根级坐标，固化新 UI 场景模板。
   - 完成后建议提交标题：`重构UI：清理剩余固定布局界面`。
   - 完成后建议提交描述：`迁移主菜单、失败通关界面、角色摘要和提示框等剩余固定坐标界面，并固化后续 UI 场景模板。`
