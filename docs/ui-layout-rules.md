# UI 布局规则

本文是 Godot 宿主层 UI 的后续维护规则。当前 UI 自适应迁移主线已完成并通过运行时验证；后续具体视觉细节可以继续优化，但新增或重改 UI 时应遵守这里的边界。

## 核心原则

- UI 表现重构只处理 Godot 宿主层场景、节点、资源和脚本接线，不改变应用层服务、领域模型、存档结构和剧情命令语义。
- 交互 UI 默认使用 `1920x1080` 设计安全画布：`DesignCanvas/DesignRoot`。
- `DesignCanvas` 负责在实际 viewport 中等比缩放并居中 `DesignRoot`。
- 固定美术坐标只允许存在于 `DesignRoot` 内，不能直接依赖实际屏幕根节点尺寸。
- 背景、遮罩和特殊全屏效果可以留在根节点，但必须是明确的全屏层。
- 不为具体分辨率堆叠特殊偏移，不写按宽高分支的布局补丁。

## 场景结构

普通全屏 UI 推荐结构：

```text
ScreenRoot
├─ BackgroundRoot 或 BackgroundTexture  # 可选，全屏铺底
├─ Overlay 或 BackgroundDim             # 可选，全屏遮罩
└─ DesignCanvas
   └─ DesignRoot
      ├─ Title / Frame / ButtonGroup
      ├─ PanelContent
      └─ ScrollContainer / VBoxContainer / GridContainer
```

基础面板和弹窗：

- 继承 `JyPanel` 的主面板继续使用 `PanelBackdropCanvas/DesignRoot` 和 `PanelChromeCanvas/DesignRoot` 承载通用背景与关闭按钮。
- 子面板自身内容应放入自己的 `DesignCanvas/DesignRoot`。
- 弹窗、确认框、hint、toast 等覆盖 UI 也应进入设计画布，除非它们本身是全屏特效层。

地图与战斗：

- 地图背景可以全屏铺底，地图点位、主角 pin 和点击命中必须共用同一个坐标模型。
- 小地图覆盖 UI 放入 `DesignCanvas/DesignRoot`。
- 战斗棋盘使用独立 `BoardDesignCanvas/DesignRoot`，棋盘、单位、hover、点击、飘字和技能动画共享棋盘坐标转换。
- 战斗行动 UI 使用独立 `UiDesignCanvas/DesignRoot`。
- 战斗 `OverlayRoot` 可以保留根级全屏，用于模态表现和特效容器。

## 控件规则

- 脚本依赖的节点优先设置 `unique_name_in_owner = true`，脚本中优先用 `%NodeName` 获取。
- 只有节点层级本身是语义边界时，才使用固定 `NodePath`。
- 列表、重复项、按钮组和文本区域优先使用 `VBoxContainer`、`HBoxContainer`、`GridContainer`、`ScrollContainer`、`MarginContainer` 等容器。
- 贴边控件应在设计画布内表达锚点和 margin，不要把接近屏幕边缘的魔法 offset 直接挂在根节点上。
- 动态列表项可以保持固定尺寸，但列表容器必须在设计画布或明确坐标模型内。
- tooltip、选择弹窗、物品目标选择、装备选择等浮层应保持在可视设计区内，低分辨率下必须可滚动或可关闭。

## 允许例外

以下节点可以留在根节点：

- 全屏背景图，例如主菜单背景、商店背景、地图背景。
- 全屏遮罩，例如 `BackgroundDim`、角色摘要 `Overlay`。
- 战斗 overlay 或其他需要覆盖整个 viewport 的表现容器。
- Godot 根节点自身和层级容器，例如 `HudLayer`、`PanelLayer`、`ModalLayer`、`OverlayLayer`。

例外节点不应承载普通按钮、正文、列表、标题等交互内容。

## 新增 UI 检查清单

新增或重做一个 UI 场景时，先确认：

- 根节点是否只承载全屏背景、遮罩、层级容器或 `DesignCanvas`。
- 普通可见内容和交互控件是否在 `DesignCanvas/DesignRoot` 内。
- 脚本需要的节点是否保留唯一节点名。
- 动态生成子节点的父容器是否在设计画布内。
- `1920x1080` 下视觉是否保持设计稿基准。
- `1366x768`、`1920x1200`、`3440x1440` 下是否居中缩放、可点击、无明显裁切。
- 是否避免了分辨率特判和根级贴边魔法 offset。

## 静态检查建议

检查目标场景是否还有普通内容直接挂根节点：

```bash
rg -n 'parent="\\."' scenes/ui scenes/main_menu scenes/map
```

检查新增场景是否接入设计画布：

```bash
rg -n 'DesignCanvas|DesignRoot' scenes/ui scenes/main_menu scenes/map
```

检查明显贴边魔法坐标：

```bash
rg -n 'offset_left = 17[0-9][0-9]|offset_top = 8[0-9][0-9]|offset_top = 9[0-9][0-9]' scenes/ui scenes/main_menu scenes/map
```

这些命令只提供线索，不是机械判定。全屏背景、遮罩、动态列表项和设计画布内部的美术坐标需要人工结合场景语义判断。

## 后续优化边界

后续可以继续优化具体视觉细节、按钮间距、字体、容器尺寸、tooltip 位置、低分辨率滚动体验等。

优化时仍应保持：

- 不改变业务流程和应用层语义。
- 不把已迁入设计画布的 UI 退回根级固定坐标。
- 地图和战斗相关改动先确认坐标模型，再改依赖点击、hover、飘字或动画的位置逻辑。
