# 战斗飘字与喊话样式参考

> 本文记录原版战斗中的飘字、全局提示飘字与角色喊话表现。
>
> 本文只作为后续 Godot 宿主层重建表现的参考，不要求兼容原版 Unity 类名、Prefab 结构或 DOTween API。后续实现应先抽象清楚表现事件，再由 Godot 层消费事件播放对应 UI。

## 1. 参考来源

本次只参考以下来源：

- `jyx-legacy-dll/JyGame/AttackInfo.cs`
- `jyx-legacy-dll/JyGame/BattleSprite.cs`
- `jyx-legacy-dll/JyGame/BattleField.cs`
- `jyx-legacy-dll/JyGame/AttackResult.cs`
- `jyx-legacy-dll/JyGame/AttackCastInfo.cs`
- `jyx-legacy-dll/JyGame/AttackCastInfoType.cs`
- `C:\home\game\武侠\金x\归档\X-Heroes-of-Jin-Yong-main\X-Heroes-of-Jin-Yong-main\Assets\Resources\ui\AttackInfo.prefab`
- `C:\home\game\武侠\金x\归档\X-Heroes-of-Jin-Yong-main\X-Heroes-of-Jin-Yong-main\Assets\Resources\ui\BattleSpritePrefab.prefab`
- `C:\home\game\武侠\金x\归档\X-Heroes-of-Jin-Yong-main\X-Heroes-of-Jin-Yong-main\Assets\Sprite\battle_duihuakuang.asset`

不参考 `legacy_scenes`。

## 2. 表现分类

原版可分为三类表现：

- 战斗飘字：角色头顶的伤害、回血、内力、怒气、Buff、技能名等短文本。
- 全局提示飘字：战场层级中央附近向上浮动的提示文本。
- 角色喊话：角色身旁的带头像小气泡，用于天赋、招式或特殊效果触发时的短对白。

这三类表现都属于战斗 UI 表现，不属于剧情对白系统，也不应复用当前全局 toast。

## 3. 战斗飘字

原版战斗飘字由 `AttackInfo` prefab 创建。

静态样式：

- Unity 组件：`Text`
- 字体：`MSYaHei.ttf`
- 默认字号：`18`
- 对齐：居中
- RichText：开启
- 文本默认颜色：黑色，但运行时会被覆盖
- 阴影效果：黑色，偏移 `(1, -1)`
- RectTransform 尺寸：`583 x 183`

运行时行为：

1. 设置文本内容。
2. 设置文本颜色。
3. 挂到 `BattleField.attackInfoLayer`。
4. 初始位置为目标格屏幕坐标上方 `+90`。
5. 1.5 秒内移动到：
   - `x + random(-50, 50)`
   - `y + 150 + random(0, 50)`
6. 位移动画使用 `Ease.OutElastic`。
7. 同时缩放到 `1.3`，缩放动画使用 `Ease.OutExpo`。
8. 动画结束后销毁。

原版没有显式透明淡出；主要靠弹性位移和放大制造反馈。

## 4. 飘字排队

每个 `BattleSprite` 自带一个 `_attackInfoQueue`。

行为：

- 调用 `AttackInfo(text, color)` 时先入队。
- 队列从空变为非空时，延迟 `0.1` 秒开始播放。
- 每次播放一条。
- 下一条固定延迟 `0.4` 秒播放。

这个排队逻辑只解决同一角色多条飘字堆叠的问题，不负责跨角色避让。

后续 Godot 实现应保留“同一战斗单位局部排队”的语义，而不是把所有飘字放进一个全局串行队列。

## 5. 飘字颜色语义

原版颜色由战斗结算代码直接传入，没有独立样式表。

`AddAttackInfo(...)` 的静态调用点只使用 7 种 Unity 预设色：

| Unity 颜色 | RGB / Hex | 实际文本与场景 |
| --- | --- | --- |
| `Color.white` | `255, 255, 255` / `#FFFFFF` | 普通伤害；多重攻击的普通伤害；斗转星移反伤的气血变化；`MISS`；`自伤1000`；`异常状态解除！`；`增益状态解除！`；`解毒`；一处反噬施加的`晕眩` |
| `Color.yellow` | `255, 255, 0` / `#FFFF00` | 暴击伤害；多重攻击的暴击伤害；八卦阵代伤；气血上限扣减提示；`集气清零`、`-集气`；`怒气清零`；`金刚伏魔圈解除!` |
| `Color.green` | `0, 255, 0` / `#00FF00` | `不老长春回血...`；飞星术施加`中毒` |
| `Color.blue` | `0, 0, 255` / `#0000FF` | 内力增减；斗转星移反弹内力；`吸内`；`-内力` |
| `Color.cyan` | `0, 255, 255` / `#00FFFF` | 攻击结算产生的怒气增减，即 `±...怒气` |
| `Color.red` | `255, 0, 0` / `#FF0000` | `清算中毒`；`攻击力上升！`、`强化！`、`真武七截阵`；多数`晕眩`；`穴位被封！`；`吸血`；`原地满血复活!` |
| `Color.magenta` | `255, 0, 255` / `#FF00FF` | 仅见于`斗转星移`发动提示 |

按代码中的稳定程度，可归纳为：

- 气血伤害默认白色，暴击或需要强调的额外伤害使用黄色。
- 内力统一使用蓝色。
- 常规怒气增减使用青色，但技能直接清空怒气使用黄色。
- 集气变化使用黄色。
- 红色主要是强效果提示，既包含负面状态，也包含强化、吸血和复活，不能等同于“伤害”或“减益”。
- 绿色同时用于回血和中毒，不能简单等同于“治疗”。
- 洋红是斗转星移的专用强调色，不是通用怒气色。

因此原版实际是“资源类型色 + 特殊效果手工指定色”，不是一套严格的状态分类。重建时保留稳定的资源色记忆，但不继承特殊效果在各调用点手工指定颜色的方式。战斗事件只携带表现语义，最终颜色由宿主主题集中映射。

当前统一使用以下 8 种表现语义：

| 表现语义 | 默认颜色 | 用途 |
| --- | --- | --- |
| `Normal` | `#FFFFFF` | 普通伤害、`MISS`、普通技能与中性反馈 |
| `Critical` | `#FFD84D` | 暴击、多重攻击等强命中反馈 |
| `Recovery` | `#55E06F` | 气血恢复、角色与技能成长 |
| `Mana` | `#55A7FF` | 内力增减、内功施展 |
| `Energy` | `#45E0E6` | 怒气、集气等行动资源变化 |
| `Beneficial` | `#8BE28B` | 增益、净化、状态解除与抵抗 |
| `Harmful` | `#FF6262` | 减益、封穴、晕眩与行动阻断 |
| `Special` | `#F06CFF` | 绝技、奥义与特殊天赋触发 |

表现语义不是战斗事件目录。同一种样式可以服务多种事实；自伤、反伤和普通攻击都属于 `Normal`，资源增加或减少也使用同一资源色，由文本中的正负号表达方向。Buff 应根据定义中的正负面属性选择 `Beneficial` 或 `Harmful`，不根据名称猜测。

颜色只允许在 Godot 宿主主题中定义。战斗核心、自定义效果和 Presenter 不直接传递或声明 `Color`；需要新的视觉区别时，应先判断现有 8 种语义是否足够，而不是为单个技能增加颜色。

## 6. 全局提示飘字

全局提示仍复用 `AttackInfo` prefab，但调用的是 `DisplayPopinfo(...)`。

静态样式沿用 `AttackInfo` prefab，运行时覆盖：

- 字号：`30`
- 颜色：通常为黄色
- 初始位置：`(0, 0)`
- 目标位置：`y = 100`
- 动画时长：`2` 秒
- 动画结束后销毁

此类提示由 `BattleField.ShowPopSuggestText(...)` 触发，语义上是战场级提示，不绑定具体角色。

## 7. 角色喊话

角色喊话不是飘字，而是 `BattleSpritePrefab` 下的 `Canvas/Dialog`。

静态样式：

- `Dialog` 默认隐藏。
- `Dialog` 局部位置：`(122.7, 24.2)`
- `Dialog` 尺寸：`178 x 71`
- `Dialog` 使用 `Canvas`，RenderMode 为 World Space。
- `Dialog` override sorting：开启。
- `Dialog` sorting order：`100`
- 背景 sprite：`battle_duihuakuang`
- 背景 sprite 原始尺寸：`178 x 71`

头像：

- 节点名：`Head`
- 位置：`(-60.1, 7.7)`
- 尺寸：`40 x 40`
- 图片来源：角色头像资源。

文本：

- 节点名：`Text`
- 位置：`(21.7, 7.7)`
- 尺寸：`116.8 x 40`
- 字体：`MSYaHei.ttf`
- 字号：`14`
- 颜色：黑色
- 对齐：左对齐
- RichText：开启

运行时行为：

1. 显示 `Dialog`。
2. 设置 `Text` 内容。
3. 设置 `Head` 为角色头像。
4. 等待 `2` 秒。
5. 隐藏 `Dialog`。

原版喊话没有打字机、淡入淡出或排队动画。是否显示由战斗逻辑按概率决定。

## 8. 喊话触发模型

战斗结算中存在两类 `AttackCastInfo`：

- `ATTACK_TEXT`：转成角色飘字。
- `SMALL_DIALOG`：转成角色喊话。

`SMALL_DIALOG` 的处理规则：

- 目标 sprite 不能为空。
- 角色必须仍有生命。
- 同一次 attack result 中，同一个 sprite 最多喊一次。
- 按 `property` 做概率检定。
- 命中后调用 `sprite.Say(info)`。

后续应保留“结算事件表达推荐喊话，表现层决定播放”的边界。战斗结算不应直接依赖 Godot 节点。

## 9. 后续 Godot 重建建议

建议拆成两个宿主表现原语：

- `BattleFloatText`
- `BattleSpeechBubble`

战斗核心只输出结构化事件，例如：

- 单位 ID
- 世界/格子位置
- 文本
- 表现类型
- 8 种表现语义之一
- 是否允许排队
- 可选头像资源 ID

Godot 层负责：

- 坐标转换
- 文本节点创建与回收
- 表现语义到颜色的主题映射
- 同单位飘字排队
- 动画播放
- 字体、阴影、背景和头像资源加载

不要把这套表现接到当前 `ToastRequestedEvent`。toast 是全局应用提示；战斗飘字和喊话是战斗局部表现。

## 10. 不做的事

后续重建时不建议：

- 兼容 Unity `AttackInfo` / `BattleSprite` 类名。
- 复制 DOTween API 形态。
- 在业务代码、效果处理器或 Presenter 中手工指定颜色。
- 让战斗核心直接创建 Godot 节点。
- 用剧情对白面板承载战斗喊话。
- 用全局 toast 承载战斗飘字。

第一版应先重建原版可见语义：短文本、颜色、上浮弹性、同角色排队、头像气泡与 2 秒自动隐藏。
