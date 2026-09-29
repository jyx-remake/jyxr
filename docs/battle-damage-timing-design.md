# 战斗伤害 Timing 设计

> 本文说明伤害阶段的职责划分；具体入口存在差异，不能将下述推荐管线视为所有伤害的统一实现。
> 普通技能、直接伤害和毒伤的当前行为见下节；阶段操作受能力型 Context 限制。
> Effect、Context 和消息流总览见 `battle-runtime-architecture.md` 与
> `battle-effect-phase-hook-design.md`。

## 目标

本文只定义伤害相关 timing 的职责边界，不讨论完整战斗状态机。

本文目标：

- 明确每个 timing 允许放哪些类型的效果
- 明确普通伤害与真实伤害分别走哪些 timing
- 避免把“命中前拦截”“防御修正”“扣血后触发”混在同一个 timing

## 当前实现与设计边界

普通技能执行命中与伤害步骤。`ApplyDirectDamage` 直接调用公共伤害 resolver，跳过命中、普通攻防计算与 `BeforeDamageApplied`，保留实际扣血后的响应。毒伤额外关闭承伤后响应，并限制为至少保留 1 HP。

公共承伤后路径已实现 `OnDamageTaken`、受击回怒、`BeforeDefeated`、`OnDefeated` 和攻击方的 `OnDamageDealt`；关闭该路径的伤害不会执行这些阶段。下文的真实伤害推荐流程用于表达设计方向，不表示每项已按该分类接入。统一伤害来源与阶段策略的待办见 [TODO](../TODO.md) 和 [战斗后续议题](battle-runtime-refactoring-backlog.md)。

## 核心原则

- 命中判断与伤害数值结算分开
- 防御体系与受伤事实分开
- 真实伤害绕过防御，不绕过受伤事实
- timing 按职责划分，不按某个具体技能命名

## 四个核心 Timing

### BeforeHitResolved

语义：

- 这一下是否算命中
- 这一下是否被直接拦截为 MISS / 无效命中

允许放的效果类型：

- 闪避
- 必中
- 破闪避
- 易容
- 溜须拍马
- 其他“命中是否成立”的判定

不应放的效果类型：

- 按攻击/防御/暴击修改伤害值
- 依赖“本次伤害数值”的减伤
- 受伤后触发

典型结果：

- 命中
- MISS
- 命中成立但禁止后续附加效果

当前适合挂载的 effect 原语：

- `cancel_hit`
  - 用于闪避、易容、溜须拍马这类直接让本次命中失效的效果
- `remove_buff`
  - 仅在“命中判定成立后立刻破功”这类场景使用
  - 典型例子是 `易容` 奇袭后移除自身 buff
- 其他 effect 原语默认不建议挂在这一层

### BeforeDamageCalculation

语义：

- 命中已经成立
- 现在进入普通伤害公式的数值修正

允许放的效果类型：

- 攻击乘区
- 防御乘区
- 暴击率修正
- 暴击倍率修正
- `final_damage` 修正
- 常规增伤
- 常规减伤
- 乾坤大挪移
- 宝甲
- 神经病
- 鲁莽

不应放的效果类型：

- 闪避
- 斗转星移
- 不老长春
- 受伤回怒

说明：

- 该 timing 只服务普通伤害
- 真实伤害不走这一层
- `final_damage` 只表示普通伤害公式内部的最后一层纯数值修正
- `final_damage` 不负责分摊、转移、溢出转资源、清零并回血这类带副作用的落血改写

当前适合挂载的 effect 原语：

- `modify_damage_context`
  - 这是这一层的主力原语
  - 常见字段：
    - `source_attack`
    - `source_attack_low`
    - `source_attack_high`
    - `target_defence`
    - `critical_chance`
    - `critical_multiplier`
    - `final_damage`
- 不建议在这一层挂：
  - `cancel_hit`
  - `add_rage`
  - `set_rage`
  - `set_action_gauge`
  - `add_hp`
  - `add_mp`

### BeforeDamageApplied

语义：

- 命中已经成立
- 当前已经得到一个伤害值
- 但目标还没有真正扣血

允许放的效果类型：

- 依赖“本次伤害值”的防御性改写
- 伤害封顶
- 伤害减半
- 伤害分摊
- 伤害转移
- 斗转星移
- 不老长春
- 其他“拿到伤害值后，再决定怎么落血”的效果

不应放的效果类型：

- 闪避
- 攻击/防御/暴击的公式修正
- 受伤后触发

说明：

- 这一层本质上是“伤害落地前的防御与反制”
- 如果某个机制需要读取本次最终伤害值，但又会改写伤害落点，应放在这里

当前适合挂载的 effect 原语：

- 当前运行时已提供 `BeforeDamageApplied`，并以 `modify_damage` 和少量 custom effect 承载落血前改写
- 后续主要会落到这里的 effect 方向：
  - 伤害封顶
  - 溢出转资源
  - 分摊 / 转移
  - 反弹 / 反制
  - 清零并回血
- 已有原语中，原则上只有“确实依赖最终伤害值并带副作用”的将来专用 effect 才应进入这一层
- `modify_damage_context` 不应继续承担这类职责

### OnDamageTaken

语义：

- 目标已经实际扣血
- 现在处理“受伤之后”的反应

允许放的效果类型：

- 受击回怒
- 受到伤害后触发被动
- 受伤叠层
- 受伤后回血/回蓝/回能量
- 受伤统计
- 受伤后的表现事件

不应放的效果类型：

- 闪避
- 命中拦截
- 普通伤害公式修正
- 斗转星移
- 不老长春

说明：

- 这一层看的是“最终实际损失了多少 HP”
- 如果伤害被完全拦截或被改写成 0，则不应进入这一层

当前适合挂载的 effect 原语：

- `add_rage`
  - 受击回怒
- `apply_buff`
  - 受伤后叠层或追加状态
- `add_hp`
  - 受伤后回血
- `add_mp`
  - 受伤后回蓝
- `remove_buff`
  - 受伤后消耗某个状态
- `remove_negative_buffs`
  - 极少数“挨打后清异常”机制
- `remove_positive_buffs`
  - 极少数“挨打后破状态”机制
- 原则上不建议在这一层挂：
  - `cancel_hit`
  - `modify_damage_context`

## 普通伤害与真实伤害

### 普通伤害

普通伤害的推荐管线：

1. `BeforeHitResolved`
2. `BeforeDamageCalculation`
3. `BeforeDamageApplied`
4. 扣血
5. `OnDamageTaken`

适用对象：

- 外功
- 招式
- 大部分依赖攻击/防御/暴击公式的伤害

### 真实伤害

真实伤害的推荐管线：

1. `BeforeHitResolved`
2. 跳过 `BeforeDamageCalculation`
3. 跳过 `BeforeDamageApplied`
4. 直接扣血
5. `OnDamageTaken`

适用对象：

- 火枪
- 襄儿的心愿
- 同归剑法
- 其他明确声明为真实伤害的技能效果

设计含义：

- 真实伤害绕过防御体系
- 真实伤害仍然算“真正受伤”
- 因此真实伤害会触发扣血后的受伤逻辑

## 效果归类规则

判断某个效果该挂在哪个 timing，可按下面的顺序判断：

1. 它是在判定“打中没打中”吗？
   如果是，放 `BeforeHitResolved`

2. 它是在改普通伤害公式里的攻击、防御、暴击或最终倍率吗？
   如果是，放 `BeforeDamageCalculation`

3. 它需要先拿到本次伤害值，再决定是否减伤、分摊、转移、反制吗？
   如果是，放 `BeforeDamageApplied`

4. 它要求目标已经真实掉血后才生效吗？
   如果是，放 `OnDamageTaken`

## 典型映射

### 命中前拦截

- 易容：`BeforeHitResolved`
- 溜须拍马：`BeforeHitResolved`
- 闪避类天赋：`BeforeHitResolved`
- 必中类天赋：`BeforeHitResolved`

### 普通伤害公式修正

- 乾坤大挪移：`BeforeDamageCalculation`
- 宝甲：`BeforeDamageCalculation`
- 神经病：`BeforeDamageCalculation`
- 鲁莽：`BeforeDamageCalculation`

### 落血前防御/反制

- 不老长春：`BeforeDamageApplied`
- 斗转星移：`BeforeDamageApplied`
- 分摊/替挡：`BeforeDamageApplied`
- 伤害封顶：`BeforeDamageApplied`
- 溢出转资源：`BeforeDamageApplied`

### 扣血后反应

- 受击回怒：`OnDamageTaken`
- 受到伤害后触发的被动：`OnDamageTaken`

## 非目标

本文不解决以下问题：

- 毒伤、灼烧、自残是否复用同一套 timing
- 治疗是否需要独立 `BeforeHealApplied / OnHealed`
- 死亡阶段如何覆盖不同伤害来源（当前已有 `BeforeDefeated` 和 `OnDefeated`）
- 多段伤害与一次技能多目标命中的事件聚合

这些问题后续再拆文档。

## 落地建议

- 运行时必须保持 `BeforeDamageCalculation -> BeforeDamageApplied -> TakeDamage -> OnDamageTaken` 的顺序
- 真武七截阵的挡刀属于 `BeforeDamageApplied`；乾坤大挪移和宝甲继续使用 `BeforeDamageCalculation + final_damage`
- 当前真实伤害 effect 后续应明确走“命中判定 -> 直接扣血 -> 扣血后触发”的窄管线
- `BeforeDamageApplied` 只保留真正依赖“本次伤害值”的防御与反制效果，不要再次混入闪避或普通攻防修正
