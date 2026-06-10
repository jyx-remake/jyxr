# UI 自适应重构进度

## 当前阶段

UI 自适应迁移主线已完成，十六轮迁移均已通过 Godot 运行时手动验证。

最终范围：

- `DesignCanvas` 已成为普通 UI 的默认设计安全画布。
- HUD、toast、confirm、`JyPanel`、主要面板、剧情 UI、开局流程、地图 UI、战斗 UI、主菜单、失败/通关、角色摘要和 hint 已完成代码侧迁移。
- 所有迁移轮次已完成用户运行时验证。
- 后续进入具体视觉细节优化与规则维护，不再按迁移轮次推进。
- 固化规则见 `docs/ui-layout-rules.md`。

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
  - 用户已完成运行时验证。
- 第七轮：迁移剩余 `JyPanel` 子面板。
  - 主要文件：`scenes/ui/character_panel/character_panel.tscn`、`scenes/ui/character_panel/equipment_selection_panel.tscn`、`scenes/ui/inventory_panel/item_target_selection_panel.tscn`、`scenes/ui/battle/combatant_select_panel.tscn`、`scenes/ui/battle/battle_item_panel.tscn`、`scenes/ui/battle/battle_settlement_panel.tscn`。
  - 用户已完成运行时验证。
- 第八轮：迁移剧情对白框。
  - 主要文件：`scenes/ui/story/story_dialogue_panel.tscn`。
  - 用户已完成运行时验证。
- 第九轮：迁移剧情选项框。
  - 主要文件：`scenes/ui/story/story_choice_panel.tscn`。
  - 用户已完成运行时验证。
- 第十轮：迁移开局流程第一批。
  - 主要文件：`scenes/ui/select_sect/select_sect_screen.tscn`、`scenes/ui/start_question/input_name_panel.tscn`。
  - 用户已完成运行时验证。
- 第十一轮：迁移开局流程第二批。
  - 主要文件：`scenes/ui/start_question/select_head_panel.tscn`、`scenes/ui/start_question/roll_stats_panel.tscn`。
  - `scenes/ui/start_question/select_head_slot.tscn` 为动态列表项，本轮确认无需结构迁移。
  - 用户已完成运行时验证。
- 第十二轮：建立地图 UI 坐标模型预备结构。
  - 主要文件：`scenes/map/map_screen.tscn`、`src/Game.Godot/Map/MapScreen.cs`。
  - 用户已完成运行时验证。
- 第十三轮：迁移地图覆盖 UI。
  - 主要文件：`scenes/map/map_screen.tscn`。
  - 用户已完成运行时验证。
- 第十四轮：建立战斗棋盘坐标模型预备结构。
  - 主要文件：`scenes/ui/battle/battle_screen.tscn`、`src/Game.Godot/UI/Battle/Widgets/BattleBoardView.cs`。
  - 用户已完成运行时验证。
- 第十五轮：迁移战斗行动 UI。
  - 主要文件：`scenes/ui/battle/battle_screen.tscn`。
  - 用户已完成运行时验证。
- 第十六轮：收尾清理剩余固定布局界面。
  - 主要文件：`scenes/main_menu/main_menu.tscn`、`scenes/ui/game_flow/gameover_screen.tscn`、`scenes/ui/game_flow/game_fin_screen.tscn`、`scenes/ui/character_summary_panel.tscn`、`scenes/ui/hint/hint_box.tscn`。
  - 用户已完成运行时验证。

## 验证矩阵

后续具体细节优化优先验证：

- `1920x1080`
- `1366x768`
- `1920x1200`
- `3440x1440`

验证方式：

- Codex 做静态路径、节点唯一名和场景结构检查。
- 用户在 Godot 运行时手动验证视觉和交互。
- UI 细节优化仍以 Godot 运行时视觉和交互验证为准；是否跑构建按当次改动风险决定。

## 历史迁移执行规则

迁移主线执行期间，从第八轮开始，剧情、开局流程、地图和战斗主界面都按小步迁移处理，不再把多个高风险区域合并到同一轮。

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
| P0 | 地图点位 | 背景 stretch 与点位坐标换算不是同一模型 | 坐标模型预备已完成 |
| P0 | 战斗棋盘 | 棋盘、单位、飘字、技能动画必须共享坐标转换 | 棋盘模型已完成 |
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
| P1 | StoryDialoguePanel / StoryChoicePanel | 剧情对白和选项仍需进入底部安全布局，长文本与点击继续行为要保持 | 已完成 |
| P1 | StartQuestion / SelectSect | 开局问卷流程场景仍需进入设计画布，不能改变剧情命令流程 | 已完成 |
| P1 | MainMenu / GameFlow / CharacterSummary / Hint | 剩余流程界面和轻量提示框仍有根级固定坐标内容 | 已完成 |

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

## 第十轮改动记录

- 修改 `scenes/ui/select_sect/select_sect_screen.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 保留 `Background` 在根节点下继续全屏铺底，避免门派背景受 16:9 设计画布裁切。
  - 将 `PanelTextureRect`、`MasterAvatarBox`、`SectNameLabel`、`PrevButton`、`NextButton`、`AckButton`、门派信息 `VBoxContainer` 和描述 `ScrollContainer` 迁入设计画布。
  - 保留 `Background`、师父头像、门派名、切换按钮、确认按钮、门派信息和描述文本的唯一节点名。
- 修改 `scenes/ui/start_question/input_name_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将 `Dikuang2` 面板迁入设计画布，输入框、提示文本、确认按钮继续作为面板子节点。
  - 保留 `NameEdit`、`HintLabel`、`AckButton` 的唯一节点名。

## 第十轮静态检查

- `SelectSectScreen` 根节点下只保留预期的全屏 `Background` 和 `DesignCanvas`。
- 门派选择交互节点已迁入 `DesignCanvas/DesignRoot`。
- `InputNamePanel` 根节点下只新增预期的 `DesignCanvas`。
- 输入名字面板内部的输入框、提示文本和确认按钮仍挂在 `Dikuang2` 下，脚本唯一节点查找不变。
- 按用户要求，本轮不做构建验证。

## 第十轮手动验证清单

- 从新游戏开局进入门派选择，确认背景仍铺满整个窗口。
- 在 `1920x1080` 下确认门派信息框、师父头像、门派名、左右切换按钮、确认按钮和描述区位置与迁移前一致。
- 在 `1366x768`、`1920x1200`、`3440x1440` 下确认门派选择交互内容随设计画布居中缩放，背景仍全屏。
- 左右切换门派时，门派名、师父头像、武学信息和描述正常刷新。
- 点击确认后能进入下一步输入名字流程。
- 输入名字面板在各验证分辨率下保持居中设计区位置。
- 输入框默认获得焦点，默认名正常显示。
- 回车提交和点击确认按钮都能继续开局流程。

## 第十一轮改动记录

- 修改 `scenes/ui/start_question/select_head_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将 `Dikuang2`、标题、头像滚动列表、`HeadContainer` 示例项和确认按钮迁入设计画布。
  - 保留 `HeadContainer` 和 `AckButton` 的唯一节点名，`SelectHeadPanel.cs` 的节点查找不变。
- 修改 `scenes/ui/start_question/roll_stats_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将内嵌 `CharacterPanel`、`RollButton` 和 `AckButton` 迁入设计画布。
  - 保留 `CharacterPanel`、`RollButton` 和 `AckButton` 的唯一节点名，`RollStatsPanel.cs` 的节点查找不变。
- 未修改 `scenes/ui/start_question/select_head_slot.tscn`。
  - 头像槽位是 `HeadContainer` 下动态生成的固定尺寸列表项。
  - 本轮不改变头像格尺寸、选中标记和点击行为。

## 第十一轮静态检查

- `SelectHeadPanel` 根节点下只新增预期的 `DesignCanvas`。
- 头像选择面板、头像滚动列表和确认按钮已迁入 `DesignCanvas/DesignRoot`。
- `RollStatsPanel` 根节点下只新增预期的 `DesignCanvas`。
- 随机属性界面的内嵌角色面板、重置按钮和确认按钮已迁入 `DesignCanvas/DesignRoot`。
- 脚本依赖节点仍保留 `unique_name_in_owner = true`。
- 按用户要求，本轮不做构建验证。

## 第十一轮手动验证清单

- 开局流程进入头像选择，确认头像选择面板位于居中的 16:9 设计区内。
- 在 `1920x1080` 下确认标题、头像网格、滚动条和确认按钮位置与迁移前一致。
- 在 `1366x768`、`1920x1200`、`3440x1440` 下确认头像选择整体随设计画布居中缩放。
- 点击头像后选中标记正常出现，切换头像时旧选中标记正常消失。
- 未选择头像时点击确认不会继续；选择头像后点击确认能进入下一步。
- 进入随机属性界面，确认角色面板、重置按钮和确认按钮位于设计区内。
- 点击重置按钮时角色属性刷新。
- 点击确认按钮后开局流程继续。

## 第十二轮改动记录

- 修改 `scenes/map/map_screen.tscn`。
  - 将原大地图坐标层 `MapBigTab/Control` 重命名为 `MapBigTab/MapCoordinateRoot`。
  - 为 `MapCoordinateRoot` 设置 `unique_name_in_owner = true`。
  - 将 `MapCoordinateRoot` 尺寸明确为 `1920x1080` 设计坐标区域，保持原有左上偏移 `(-15, -52)`。
  - 更新子节点路径：
    - `MapBigTab/Control/MapEntitySlots` -> `MapBigTab/MapCoordinateRoot/MapEntitySlots`
    - `MapBigTab/Control/MapPin` -> `MapBigTab/MapCoordinateRoot/MapPin`
    - `MapBigTab/Control/MapPin/PinAvatar` -> `MapBigTab/MapCoordinateRoot/MapPin/PinAvatar`
    - `MapBigTab/Control/MapPin/AnimationPlayer` -> `MapBigTab/MapCoordinateRoot/MapPin/AnimationPlayer`
- 修改 `src/Game.Godot/Map/MapScreen.cs`。
  - 新增 `_mapCoordinateRoot = GetNode<Control>("%MapCoordinateRoot")`。
  - 将原 `LargeMapXScale` / `LargeMapYScale` 收敛为 `LargeMapCoordinateScale`。
  - 新增 `MapPositionToLargeMapPoint(...)` 作为大地图点位和主角 pin 的统一换算入口。
  - 新增 `GetLargeMapDesignRect()`，后续可在第十三轮继续扩展为背景显示 rect / 覆盖 UI 坐标模型。
  - `FillLargeMap(...)` 中点位按钮和 `_mapPin` 都改为调用 `MapPositionToLargeMapPoint(...)`。

## 第十二轮静态检查

- 旧路径 `MapBigTab/Control` 已无残留。
- `MapEntitySlots`、`MapPin`、`PinAvatar` 和 `AnimationPlayer` 都已挂到 `MapBigTab/MapCoordinateRoot` 下。
- `MapCoordinateRoot` 保留原左上偏移，不改变 `1920x1080` 下点位基准位置。
- 大地图点位和主角 pin 坐标换算已共用同一个方法。
- 小地图 `MapSmallTab`、`MapEntityList`、`BottomBox` 本轮未迁移。
- 按用户要求，本轮不做构建验证。

## 第十二轮手动验证清单

- 进入大地图，确认地图点位整体位置与迁移前一致。
- 进入大地图，确认主角 pin 位置与迁移前一致，并且跳动动画仍正常。
- 点击大地图点位，确认仍能触发对应地图事件或进入地点。
- 在大地图触发剧情播放时，云层、点位和主角 pin 仍会按剧情播放模式隐藏，剧情结束后恢复。
- 从大地图进入小地图，确认小地图列表和底部描述区没有变化。
- 在 `1920x1080`、`1366x768`、`1920x1200`、`3440x1440` 下抽查大地图点位和主角 pin 没有出现额外偏移。

## 第十三轮改动记录

- 修改 `scenes/map/map_screen.tscn`。
  - 在 `MapSmallTab` 下新增 `DesignCanvas/DesignRoot`。
  - 将小地图覆盖 UI 迁入设计画布。
  - 更新节点路径：
    - `MapSmallTab/CameraButton` -> `MapSmallTab/DesignCanvas/DesignRoot/CameraButton`
    - `MapSmallTab/MapEntityList` -> `MapSmallTab/DesignCanvas/DesignRoot/MapEntityList`
    - `MapSmallTab/BottomBox` -> `MapSmallTab/DesignCanvas/DesignRoot/BottomBox`
    - `MapSmallTab/BottomBox/DialogueShadowPanel` -> `MapSmallTab/DesignCanvas/DesignRoot/BottomBox/DialogueShadowPanel`
    - `MapSmallTab/BottomBox/Frame` -> `MapSmallTab/DesignCanvas/DesignRoot/BottomBox/Frame`
    - `MapSmallTab/BottomBox/MapDescriptionLabel` -> `MapSmallTab/DesignCanvas/DesignRoot/BottomBox/MapDescriptionLabel`
- 未修改 `src/Game.Godot/Map/MapScreen.cs`。
  - `CameraButton`、`MapEntityList`、`BottomBox`、`MapDescriptionLabel` 均保留唯一节点名，脚本查找路径不变。
  - 小地图点位动态生成仍写入 `%MapEntityList`。

## 第十三轮静态检查

- `MapSmallTab` 下新增预期的 `DesignCanvas/DesignRoot`。
- 旧路径 `MapSmallTab/CameraButton`、`MapSmallTab/MapEntityList`、`MapSmallTab/BottomBox` 已无残留。
- 小地图覆盖 UI 节点已迁入 `MapSmallTab/DesignCanvas/DesignRoot`。
- 脚本依赖节点仍保留 `unique_name_in_owner = true`。
- 大地图 `MapCoordinateRoot` 本轮未改。
- 按用户要求，本轮不做构建验证。

## 第十三轮手动验证清单

- 从大地图进入任意小地图，确认小地图点位列表位于居中的 16:9 设计区内。
- 在 `1920x1080` 下确认相机按钮、小地图点位列表、底部描述框位置与迁移前一致。
- 在 `1366x768`、`1920x1200`、`3440x1440` 下确认小地图覆盖 UI 随设计画布居中缩放，不贴到扩展画布边缘。
- 点击小地图点位，确认仍能触发事件、进入地点或打开商店/储物箱/战斗。
- 底部地图描述文字仍正常显示。
- 剧情播放模式下，小地图点位列表、底部描述区和相机按钮仍会隐藏，剧情结束后恢复。
- 大地图点位和主角 pin 不应发生变化。

## 第十四轮改动记录

- 修改 `scenes/ui/battle/battle_screen.tscn`。
  - 新增 `BoardDesignCanvas/DesignRoot`。
  - 将战斗棋盘实例迁入棋盘设计画布。
  - 更新节点路径：
    - `BattleScreen/BoardGrid` -> `BattleScreen/BoardDesignCanvas/DesignRoot/BoardGrid`
  - `BoardGrid` 保留 `unique_name_in_owner = true`，`BattleScreen.cs` 仍通过 `%BoardGrid` 获取节点。
- 修改 `src/Game.Godot/UI/Battle/Widgets/BattleBoardView.cs`。
  - 将格子 rect 换算入口重命名为 `GridPositionToCellRect(...)`。
  - 将单位锚点换算入口重命名为 `GridPositionToUnitAnchor(...)`。
  - 将鼠标点到格子坐标的命中入口重命名为 `TryGetGridPositionAt(...)`。
  - 棋盘绘制、单位位置、移动演出、技能影响动画、hover 命中和点击命中继续共用这些入口。

## 第十四轮静态检查

- `BattleScreen/BoardGrid` 旧路径已无残留。
- `BoardGrid` 已挂到 `BattleScreen/BoardDesignCanvas/DesignRoot`。
- `BattleScreen.cs` 仍通过唯一节点名 `%BoardGrid` 获取棋盘。
- `BattleBoardView` 中旧方法名 `ResolveCellRect`、`ResolveUnitAnchor`、`TryGetCellAt` 已无残留。
- 战斗行动栏、技能栏、右上按钮、日志和底部 HUD 本轮未迁移。
- 按用户要求，本轮不做构建验证。

## 第十四轮手动验证清单

- 进入战斗，确认棋盘整体位置在 `1920x1080` 下与迁移前一致。
- 在 `1366x768`、`1920x1200`、`3440x1440` 下确认棋盘随 16:9 设计区居中缩放，不贴到扩展画布边缘。
- 鼠标 hover 可交互格子时，高亮和实际格子位置一致。
- 点击移动目标格时，单位移动到正确格子。
- 技能选择目标时，目标格、可能影响范围和实际影响范围与鼠标位置一致。
- 技能动画播放位置与受击格/受击单位一致。
- 伤害/治疗/状态飘字仍出现在对应单位头顶。
- 行动栏、技能栏、右上按钮和日志不应因本轮迁移发生行为变化。

## 第十五轮改动记录

- 修改 `scenes/ui/battle/battle_screen.tscn`。
  - 新增 `UiDesignCanvas/DesignRoot`。
  - `UiDesignCanvas` 使用 `DesignCanvas.cs`，设计尺寸保持默认 `1920x1080`。
  - `UiDesignCanvas` 设置 `z_index = 20`，确保行动 UI 显示在战斗棋盘之上。
  - 将战斗行动 UI、右上按钮、标题时钟和日志迁入 UI 设计画布。
  - 更新节点路径：
    - `BattleScreen/TopClock` -> `BattleScreen/UiDesignCanvas/DesignRoot/TopClock`
    - `BattleScreen/AutoBattleButton` -> `BattleScreen/UiDesignCanvas/DesignRoot/AutoBattleButton`
    - `BattleScreen/SurrenderButton` -> `BattleScreen/UiDesignCanvas/DesignRoot/SurrenderButton`
    - `BattleScreen/SpeedUpButton` -> `BattleScreen/UiDesignCanvas/DesignRoot/SpeedUpButton`
    - `BattleScreen/BattleLogTag` -> `BattleScreen/UiDesignCanvas/DesignRoot/BattleLogTag`
    - `BattleScreen/LogPanel` -> `BattleScreen/UiDesignCanvas/DesignRoot/LogPanel`
    - `BattleScreen/BottomHud` -> `BattleScreen/UiDesignCanvas/DesignRoot/BottomHud`
  - `BoardDesignCanvas/DesignRoot/BoardGrid` 保持不变，继续承载棋盘、单位、飘字和技能动画坐标模型。
  - `OverlayRoot` 保持根级全屏覆盖层，不在本轮迁入设计画布。
  - 脚本依赖的节点继续保留 `unique_name_in_owner = true`，`BattleScreen.cs` 无需修改。

## 第十五轮静态检查

- `TopClock`、`AutoBattleButton`、`SurrenderButton`、`SpeedUpButton`、`BattleLogTag`、`LogPanel`、`BottomHud` 已挂到 `BattleScreen/UiDesignCanvas/DesignRoot`。
- `BattleScreen.cs` 仍通过唯一节点名获取标题、按钮、技能栏、头像、日志和 overlay。
- `BoardGrid` 仍挂到 `BattleScreen/BoardDesignCanvas/DesignRoot`。
- `OverlayRoot` 仍挂在 `BattleScreen` 根节点，用于继续覆盖全屏。
- 本轮未修改 C# 行为代码。
- 按用户要求，本轮不做构建验证。

## 第十五轮手动验证清单

- 进入战斗，确认右上投降、自动、加速按钮在 `1920x1080` 下位置与迁移前一致。
- 在 `1366x768`、`1920x1200`、`3440x1440` 下确认右上按钮、左侧日志、顶部战场标题和底部行动栏随设计区居中缩放。
- 选择可行动角色后，确认移动、技能、物品、休息、结束按钮仍可点击。
- 点击技能按钮后，确认技能列表能正常显示和滚动，技能项点击仍能进入目标选择。
- 选中技能后，确认左下选中技能图标、技能名和招式名正常刷新。
- 自动战斗和加速按钮的红色激活状态仍正常显示和隐藏。
- 战斗日志仍能追加文本，且不会被棋盘或底部 HUD 遮挡。
- 棋盘 hover、点击移动、技能目标选择、飘字和技能动画不应因本轮迁移发生偏移。
- 战斗中打开物品面板、战斗结束打开结算面板，确认 overlay 仍覆盖全屏。

## 规则固化

UI 自适应布局规则已固化到 `docs/ui-layout-rules.md`。

后续具体界面细节优化应继续遵守：

- 普通交互 UI 默认进入 `DesignCanvas/DesignRoot`。
- 全屏背景、遮罩和战斗 overlay 可以留在根节点，但不承载普通内容。
- 新增 UI 需要保留脚本依赖节点的唯一节点名。
- 地图和战斗改动必须先确认坐标模型，再改点击、hover、飘字或动画位置。
- 不做按分辨率堆叠的特殊偏移补丁。

## 第十六轮改动记录

- 修改 `scenes/main_menu/main_menu.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 保留 `Bg` 在根节点下继续全屏铺底。
  - 将 `Cross`、`Title` 和主菜单按钮列表迁入设计画布。
  - 将 `Cross` 从根级全屏锚点偏移换算为 `1920x1080` 设计坐标内的固定 rect，保持基准视觉尺寸不变。
- 修改 `scenes/ui/game_flow/gameover_screen.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 保留 `BackgroundDim` 在根节点下继续全屏铺底。
  - 将失败图、标题、日期、死亡统计和底部按钮组迁入设计画布。
- 修改 `scenes/ui/game_flow/game_fin_screen.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 保留 `BackgroundDim` 在根节点下继续全屏铺底。
  - 将通关文字和返回主菜单按钮迁入设计画布。
- 修改 `scenes/ui/character_summary_panel.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 保留 `Overlay` 在根节点下继续全屏遮罩。
  - 将面板框、关闭按钮、头像、基础信息、装备信息和技能页迁入设计画布。
- 修改 `scenes/ui/hint/hint_box.tscn`。
  - 新增 `DesignCanvas/DesignRoot`。
  - 将提示框背景、内容面板、确认按钮、标题和正文迁入设计画布。

## 第十六轮静态检查

- 本轮五个目标场景均已新增 `DesignCanvas/DesignRoot`。
- 主菜单 `StartButton`、`LoadButton`、`MusicButton` 保留唯一节点名，`MainMenu.cs` 节点查找不变。
- 失败界面 `DateLabel`、`DeathInfoLabel`、`RestartButton`、`LoadGameButton`、`ExitButton` 保留唯一节点名，`GameOverScreen.cs` 节点查找不变。
- 通关界面 `MainMenuButton` 保留唯一节点名，`GameFinScreen.cs` 节点查找不变。
- 角色摘要 `Avatar`、`NameLabel`、`LevelLabel`、`HpLabel`、`MpLabel`、`StatsLabel`、`EquipmentLabel`、`SkillTab`、`CloseButton` 保留唯一节点名，`CharacterSummaryPanel.cs` 节点查找不变。
- hint `AckButton`、`ContentLabel` 保留唯一节点名，`HintBox.cs` 节点查找不变。
- 根节点下保留的节点只用于全屏背景或遮罩：主菜单 `Bg`、失败/通关 `BackgroundDim`、角色摘要 `Overlay`。
- 按用户要求，本轮不做构建验证。

## 第十六轮手动验证清单

- 打开主菜单，确认背景仍铺满整个窗口，标题、装饰和三个按钮在 `1920x1080` 下位置与迁移前一致。
- 在 `1366x768`、`1920x1200`、`3440x1440` 下确认主菜单内容随设计区居中缩放，背景仍全屏。
- 主菜单新游戏、读档、音乐欣赏按钮行为不变。
- 进入失败界面，确认失败图、标题、日期、死亡统计和底部按钮位于居中的 16:9 设计区内。
- 失败界面再战、读档、退出按钮行为不变。
- 进入通关界面，确认“全剧终”和返回主菜单按钮位于设计区内，返回行为不变。
- 打开角色摘要面板，确认遮罩仍铺满窗口，面板内容位于设计区内，关闭按钮可点击。
- 打开 hint 提示框，确认提示框整体位于设计区内，确认按钮可关闭提示。
