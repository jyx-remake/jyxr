# 战斗效果系统设计

## 当前实现状态

本文是 battle hook 后续重构设计记录，不代表当前仓库已有运行时代码。

当前仓库状态：

- 战场运行实现已移除，当前没有 `BattleEngine` / `CombatantState` / battle hook runtime。
- 当前只保留轻量 `AffixDefinition` 角色投影系统；它不包含本文描述的 phase hook 执行。
- 后续重建战场与 hook 时，应按本文重新拆分职责，不为旧实现做兼容补丁。

本文后续章节同时覆盖：

- 后续建议采用的 phase hook 设计
- 可扩展但尚未实现的方向

## 1. 目标

本文讨论单个游戏战斗系统中的“附加效果”建模，不以做通用引擎为目标。

当前希望同时满足：

- 装备、技能、天赋都可以附带效果
- 将来这些来源不一定严格区分
- 常驻属性修改与授予能力要能统一表达
- 某些效果要在固定战斗时机介入
- 多个效果可能共同修改一次伤害或一次行动的结算
- 复杂效果允许直接手写代码

本文结论是：

- 定义层继续保持统一 attachment/source 模型
- 战斗执行层采用固定阶段回调，而不是把所有行为都抽成纯 `timing + action`
- 常驻数值修改与战斗时机介入分开建模
- 数值修改以分阶段聚合为主，流程副作用以固定阶段串行执行为主
- hook 定义以“阶段 + 条件 + 操作”为主，只有少数特殊机制才进入自定义代码

## 2. 问题重述

当前需求实际上包含三类不同语义的效果：

### 2.1 常驻派生

例如：

- 攻击力加算
- 暴击率乘算
- 装备授予一个天赋
- 技能达到某等级时提供额外属性

这类效果的本质不是“一次事件发生后执行行为”，而是“当前有效状态的派生结果”。

### 2.2 来源激活条件

例如：

- 装备必须处于已装备状态
- 技能必须未禁用
- 技能等级必须达到要求
- 天赋必须已拥有

这类条件回答的是“这个附加来源现在是否生效”。

### 2.3 战斗流程介入

例如：

- 命中前修改命中率
- 伤害计算前追加增伤
- 伤害结算后吸血
- 行动结束后追加一次行动机会

这类效果的本质是“介入一次正在进行的战斗流程”。

将三类问题强行压进同一套 `trigger = timing + action` 或“来源对象重写固定接口”模型，都会产生额外复杂度。

## 3. 方案选择

### 3.1 不选择“来源对象自己重写一堆固定接口”

不建议把扩展点放在 `EquipmentDefinition` / `SkillDefinition` / `TalentDefinition` 的继承体系上。

原因：

- 行为的核心不在来源类型，而在附带效果本身
- 将来来源可能不再严格区分
- 一个来源可能同时有常驻效果、伤害改写、行动后追加效果等多种行为
- 如果扩展点放在来源类型上，会把行为和来源身份耦死

### 3.2 不选择“所有行为都用纯 timing + action”

不建议把整个战斗系统统一成“某时机触发一个 action”。

原因：

- 常驻属性派生不属于事件反应
- 修改当前伤害值不只是“看到事件再做一件事”，而是“介入当前结算中间态”
- 为了表达精细结算，`timing` 会不断细分，最终退化成隐式的固定阶段体系
- `action` 会越来越像弱类型战斗脚本，难以约束权限与顺序

### 3.3 不选择“核心依赖 handlerId -> 类”

不建议让定义层通过业务字符串去选择一个运行时代码类作为主模型。

原因：

- 业务逻辑会依赖字符串路由
- 重构不友好
- 参数校验偏弱
- 对单个游戏项目来说，这种灵活度并不是刚需

### 3.4 结论

采用混合方案，但主干应以固定阶段回调为核心：

- 来源层统一
- 常驻效果声明式
- 战斗流程在固定阶段开放回调
- hook 定义以规则式结构为主
- 规则式结构由少量通用条件和通用操作组成
- 只有少数无法表达的机制才进入自定义执行

## 4. 核心原则

### 4.1 来源统一，行为解耦

装备、技能、天赋只是“效果来源”，不应是行为扩展点本身。

### 4.2 常驻派生与流程介入分开

“攻击 +20%”和“行动后追加一次行动机会”不是同一种东西，不应要求统一为一种抽象。

### 4.3 结算阶段固定，阶段内权限明确

战斗流程需要稳定的、少量的固定阶段。  
每个阶段允许做什么，应由引擎定义，而不是由单个效果自由决定。

### 4.4 数值修改优先聚合，副作用优先串行

对于伤害、治疗、命中率等数值，优先采用“收集贡献 -> 统一结算”。  
对于追加行动、追击、刷新冷却、施加状态等副作用，优先采用固定阶段串行执行。

### 4.5 优先复用原语，少写专用类

对高频战斗规则，优先抽成：

- 条件原语
- 操作原语

而不是：

- 每个效果一个 handler 类
- 每个 JSON 节点一个代码类型

## 5. 总体结构

建议把系统拆成以下几层。

### 5.1 Attachment Source

统一表示一个附加效果来源。

来源可以是：

- 装备
- 技能
- 天赋
- Buff
- 将来统一后的任意效果来源

来源只负责描述：

- 自己的激活条件
- 自己提供的常驻 modifier
- 自己注册的 battle hooks

### 5.2 Activation Condition

描述来源或某个附加项当前是否有效。

典型条件：

- 已装备
- 技能等级至少为 N
- 技能未禁用
- 拥有指定标签

这里的条件只依赖拥有者当前状态，不依赖某次具体战斗事件。

### 5.3 Modifier

表示常驻派生项，不直接执行战斗逻辑。

第一版只需要支持：

- `StatAddModifier`
- `StatMulModifier`
- `GrantTalentModifier`

后续如有需要，再增加：

- 标签授予
- 抗性授予
- 表现层标记

### 5.4 Battle Hook

表示“在固定战斗阶段介入的一条规则”。

这里不是纯 `timing + action`，而是固定 phase hook。

一条 hook 由三部分组成：

- `Phase`
- `Conditions`
- `Operations`

必要时允许追加：

- `CustomExecution`

### 5.5 Runtime Engine

运行时并不需要为每条规则生成一个独立类。

更推荐：

- 固定阶段由引擎分发
- 条件由一组通用 evaluator 判定
- 操作由一组通用 operation executor 执行
- 只有少量复杂机制才调用自定义执行器

## 6. 推荐的固定阶段

第一版建议只定义少量阶段，避免一开始枚举爆炸。

### 6.1 行动阶段

- `TurnStart`
- `BeforeAction`
- `AfterAction`
- `TurnEnd`

### 6.2 命中/伤害阶段

- `BeforeHit`
- `BeforeDamageResolve`
- `BeforeDamageApply`
- `AfterDamageApplied`
- `BeforeDeath`

### 6.3 治疗阶段

- `BeforeHealResolve`
- `AfterHealApplied`

如果实现初期希望更保守，也可以先不单独拆治疗阶段，而是先只做伤害阶段。

## 7. 伤害结算规则

这是本设计的重点。

### 7.1 原则

对“修改伤害值”的效果，不建议默认采用“所有效果串行直接改 `ctx.Damage`”。

建议采用：

- 固定阶段
- 阶段内分组聚合
- 少数 override 逻辑单独处理

### 7.2 推荐流程

一次伤害可按如下步骤结算：

1. `BeforeHit`
2. 命中判定
3. `BeforeDamageResolve`
4. 统一计算基础伤害、加算、乘算、暴击等
5. `BeforeDamageApply`
6. 应用护盾、保底、伤害改写、免死等规则
7. 若即将死亡，则进入 `BeforeDeath`
8. 扣减生命或取消死亡
9. `AfterDamageApplied`

### 7.3 聚合桶

在 `BeforeDamageResolve` 阶段，建议只允许提交标准化贡献，而不是任意写过程代码。

第一版可定义如下桶：

- `BaseAdd`
- `BaseMul`
- `FinalAdd`
- `FinalMul`
- `Override`
- `Clamp`

统一规则由结算器决定，例如：

1. 计算基础伤害
2. 应用 `BaseAdd`
3. 应用 `BaseMul`
4. 应用 `FinalAdd`
5. 应用 `FinalMul`
6. 应用 `Override`
7. 应用 `Clamp`
8. 最终 round / clamp

### 7.4 为什么不全部串行改值

如果所有效果都直接串行改 `ctx.Damage`，会很快出现以下问题：

- 结果依赖注册顺序
- 取整时机不稳定
- 一个效果必须知道前面谁改过值
- 调整一个效果顺序可能导致整体平衡变化
- 很难向设计层解释“为什么这次是 137 点伤害”

### 7.5 哪些东西不进聚合桶

以下效果不应进入纯数值聚合：

- 取消本次伤害
- 伤害转治疗
- 先扣护盾再扣血
- 伤害后吸血
- 造成伤害后追加追击
- 行动后追加一次行动机会
- 死亡拦截与复活

这些更适合：

- `BeforeDamageApply` 中的受控 override
- `BeforeDeath` 中的流程拦截
- `AfterDamageApplied` / `AfterAction` 中的串行副作用

## 8. 定义层与运行时的关系

建议继续保留定义层，且定义层仍然统一。

### 8.1 定义层负责什么

定义层负责表达：

- 来源是什么
- 来源何时激活
- 它有哪些 modifier
- 它挂载哪些 hooks

### 8.2 运行时负责什么

运行时负责：

- 根据当前单位状态解析有效来源
- 汇总当前有效 modifier
- 在固定战斗阶段遍历 hooks
- 对 hooks 逐条判定条件并执行操作
- 在需要时进入少量自定义执行

### 8.3 推荐的 hook schema

推荐把 hook 定义成规则式结构：

```json
{
  "phase": "BeforeDamageApply",
  "conditions": [
    { "type": "SelfHealthBelowPercent", "value": 20 }
  ],
  "operations": [
    { "type": "ClampFinalDamageMax", "value": 200 }
  ]
}
```

含义是：

- 在 `BeforeDamageApply` 阶段
- 若所有条件成立
- 则按顺序执行这些操作

这里的重点是：

- 定义层不直接引用代码类名
- 也不直接依赖业务字符串去选择一个 handler 类
- 大多数效果通过少量通用条件和通用操作表达

### 8.4 少量自定义执行

对于复杂机制，允许在 hook 中挂一个自定义执行标记。

例如：

- 一个效果跨多个阶段协同
- 需要复杂条件判断
- 需要读取专门状态
- 需要操作额外战斗队列或行动机会系统

此时可写成：

```json
{
  "phase": "AfterAction",
  "custom": {
    "type": "ChainKillRecast"
  }
}
```

这里的 `custom.type` 只作为反序列化边界的判别值，不作为系统内部的主业务依赖。

## 9. 推荐的接口方向

这里只给结构方向，不在本文中固化全部代码细节。

### 9.1 来源接口

```csharp
public interface IAttachmentSource
{
    IReadOnlyList<IActivationCondition> ActivationConditions { get; }
    IReadOnlyList<IModifierDefinition> Modifiers { get; }
    IReadOnlyList<BattleHookDefinition> Hooks { get; }
}
```

### 9.2 常驻 modifier

```csharp
public interface IModifierDefinition
{
}

public sealed record StatAddModifierDefinition(StatType Stat, int Value) : IModifierDefinition;
public sealed record StatMulModifierDefinition(StatType Stat, double Value) : IModifierDefinition;
public sealed record GrantTalentModifierDefinition(string TalentId) : IModifierDefinition;
```

### 9.3 Hook 定义

```csharp
public sealed record BattleHookDefinition(
    BattleHookPhase Phase,
    IReadOnlyList<HookConditionDefinition> Conditions,
    IReadOnlyList<HookOperationDefinition> Operations,
    CustomHookExecutionDefinition? Custom = null);
```

### 9.4 固定阶段

```csharp
public enum BattleHookPhase
{
    TurnStart,
    BeforeAction,
    AfterAction,
    TurnEnd,
    BeforeHit,
    BeforeDamageResolve,
    BeforeDamageApply,
    BeforeDeath,
    AfterDamageApplied,
    BeforeHealResolve,
    AfterHealApplied
}
```

### 9.5 条件原语

```csharp
public abstract record HookConditionDefinition;

public sealed record SelfHealthBelowPercentConditionDefinition(int Value) : HookConditionDefinition;
public sealed record TargetHealthBelowPercentConditionDefinition(int Value) : HookConditionDefinition;
public sealed record RandomChanceConditionDefinition(double Value) : HookConditionDefinition;
public sealed record KilledAnyTargetThisActionConditionDefinition() : HookConditionDefinition;
public sealed record UsageRemainingConditionDefinition(string Scope, string Key, int Max) : HookConditionDefinition;
```

### 9.6 操作原语

```csharp
public abstract record HookOperationDefinition;

public sealed record AddFinalDamagePercentOperationDefinition(double Value) : HookOperationDefinition;
public sealed record ClampFinalDamageMaxOperationDefinition(int Value) : HookOperationDefinition;
public sealed record GrantExtraActionOperationDefinition(int Count) : HookOperationDefinition;
public sealed record CancelDeathOperationDefinition() : HookOperationDefinition;
public sealed record RestoreHealthFullOperationDefinition() : HookOperationDefinition;
public sealed record ConsumeUsageOperationDefinition(string Scope, string Key) : HookOperationDefinition;
```

### 9.7 自定义执行

```csharp
public abstract record CustomHookExecutionDefinition;
```

这里不要求“每条 hook 都对应一个类”。  
更推荐的执行模型是：

- 按阶段分发
- 统一判断条件
- 统一执行操作
- 只有少数 `CustomHookExecutionDefinition` 进入专门代码

## 10. 为什么选择“条件 + 操作”而不是“handlerId”

### 10.1 `handlerId` 的问题

如果把主模型设计成：

- `handlerId`
- `args`

虽然定义层统一，但系统内部会依赖业务字符串来路由行为。

这会带来：

- 重构不友好
- 参数校验偏弱
- 运行时错误更晚暴露
- 对单个游戏项目来说灵活度过剩

### 10.2 每个效果一个类的问题

如果把主模型设计成：

- 每种 hook 一个 definition 类
- 每种 hook 一个 handler 类

虽然强类型很强，但容易出现：

- 类型爆炸
- 样板代码多
- 大量高频规则只是在搬运参数

### 10.3 当前方案的平衡点

当前方案把主路线收敛为：

- 固定阶段
- 少量条件原语
- 少量操作原语
- 少量自定义执行

这样既避免字符串注册表作为核心依赖，又避免为每个效果写一堆碎类。

## 11. 例子

下面用几个具体例子说明，这套方案里哪些效果属于数值聚合，哪些属于固定阶段副作用，哪些需要进入 `custom`。

### 例 1：10% 几率增伤 50%

效果描述：

- 攻击时有 10% 概率使本次最终伤害提高 50%

推荐归类：

- 阶段：`BeforeDamageResolve`
- 类型：规则式 hook
- 实现方式：`RandomChance` 条件 + `AddFinalDamagePercent` 操作

定义示意：

```json
{
  "phase": "BeforeDamageResolve",
  "conditions": [
    { "type": "RandomChance", "value": 0.1 }
  ],
  "operations": [
    { "type": "AddFinalDamagePercent", "value": 0.5 }
  ]
}
```

### 例 2：生命低于 20%，强制最终伤害不超 200

效果描述：

- 当自身生命低于 20% 时，本次受到的最终伤害上限为 200

推荐归类：

- 阶段：`BeforeDamageApply`
- 类型：规则式 hook
- 实现方式：`SelfHealthBelowPercent` 条件 + `ClampFinalDamageMax` 操作

定义示意：

```json
{
  "phase": "BeforeDamageApply",
  "conditions": [
    { "type": "SelfHealthBelowPercent", "value": 20 }
  ],
  "operations": [
    { "type": "ClampFinalDamageMax", "value": 200 }
  ]
}
```

### 例 3：如果击杀了敌人，可以再次行动一次

效果描述：

- 若本次行动导致击杀，则行动结算后立即再行动一次

推荐归类：

- 阶段：`AfterAction`
- 类型：规则式 hook
- 实现方式：`KilledAnyTargetThisAction` 条件 + `GrantExtraAction` 操作

定义示意：

```json
{
  "phase": "AfterAction",
  "conditions": [
    { "type": "KilledAnyTargetThisAction" }
  ],
  "operations": [
    { "type": "GrantExtraAction", "count": 1 }
  ]
}
```

### 例 4：每场战斗首次死亡时满血复活

效果描述：

- 每场战斗第一次死亡时，不真正死亡，而是立刻恢复满生命并继续战斗

推荐归类：

- 阶段：`BeforeDeath`
- 类型：规则式 hook
- 实现方式：`UsageRemaining` 条件 + `ConsumeUsage` / `CancelDeath` / `RestoreHealthFull` 操作

定义示意：

```json
{
  "phase": "BeforeDeath",
  "conditions": [
    {
      "type": "UsageRemaining",
      "scope": "Battle",
      "key": "first_death_revive",
      "max": 1
    }
  ],
  "operations": [
    {
      "type": "ConsumeUsage",
      "scope": "Battle",
      "key": "first_death_revive"
    },
    { "type": "CancelDeath" },
    { "type": "RestoreHealthFull" }
  ]
}
```

### 例 5：把两个效果放进同一个天赋

如果一个天赋同时拥有：

- 生命低于 20% 时最终伤害不超过 200
- 每场战斗首次死亡时满血复活

推荐写成：

```json
{
  "id": "unyielding_will",
  "name": "Unyielding Will",
  "modifiers": [],
  "hooks": [
    {
      "phase": "BeforeDamageApply",
      "conditions": [
        { "type": "SelfHealthBelowPercent", "value": 20 }
      ],
      "operations": [
        { "type": "ClampFinalDamageMax", "value": 200 }
      ]
    },
    {
      "phase": "BeforeDeath",
      "conditions": [
        {
          "type": "UsageRemaining",
          "scope": "Battle",
          "key": "first_death_revive",
          "max": 1
        }
      ],
      "operations": [
        {
          "type": "ConsumeUsage",
          "scope": "Battle",
          "key": "first_death_revive"
        },
        { "type": "CancelDeath" },
        { "type": "RestoreHealthFull" }
      ]
    }
  ]
}
```

## 12. 设计收益

这套方案的主要收益是：

- 保留来源统一性，不把行为绑在装备/技能/天赋类型上
- 把常驻派生和流程介入分开，避免语义混乱
- 对“多个效果共同修改伤害值”给出可控的聚合规则
- 对“追加行动机会”这类副作用保留足够直接的实现方式
- 避免把核心模型建立在业务字符串注册表上
- 避免为大量高频效果编写过多样板类

## 13. 代价与约束

这套方案并不是没有代价。

需要明确接受以下约束：

- 必须先定义少量稳定的固定阶段
- 每个阶段允许做什么，需要引擎明确限制
- 数值聚合桶的规则必须统一，不能让每个效果自定义公式
- 条件原语和操作原语需要有清晰边界，避免无限膨胀
- 只有少数复杂机制才能进入 `custom`

## 14. 日志与调试策略

规则式 hook 相比“显式重写固定回调方法”，天然更像解释执行。  
因此如果完全不补充可观测性，会更难直接从调用栈理解“哪条规则生效了”。

但这不意味着该方案不适合做日志。  
更合适的做法是：在引擎层输出统一的结构化 trace，而不是依赖每个效果实现自己打印日志。

### 14.1 原则

- 日志不是第一版必须完成项
- 但运行时结构设计应为后续 trace 留出空间
- 日志应当由引擎统一产出，而不是让单条规则自由打印

### 14.2 建议记录的最小信息

如果后续补日志，建议每次 hook 执行至少记录：

- 当前 `Phase`
- 当前规则所属来源
- 当前规则标识
- 条件判定结果
- 执行了哪些操作
- 关键上下文字段的前后变化

### 14.3 推荐的 trace 结构

可考虑引入如下结构：

```csharp
public sealed record BattleHookTrace(
    BattleHookPhase Phase,
    string SourceId,
    string RuleId,
    IReadOnlyList<ConditionTrace> Conditions,
    IReadOnlyList<OperationTrace> Operations);

public sealed record ConditionTrace(
    string ConditionType,
    bool Result,
    string? Detail = null);

public sealed record OperationTrace(
    string OperationType,
    string? Before = null,
    string? After = null,
    string? Detail = null);
```

### 14.4 示例

例如“生命低于 20% 时最终伤害不超过 200”的日志可以表现为：

```text
[BeforeDamageApply] source=talent:unyielding_will rule=unyielding_will_damage_cap
  condition SelfHealthBelowPercent(20) => true
  operation ClampFinalDamageMax(200) => finalDamage 356 -> 200
```

### 14.5 为什么不要求第一版实现

第一版的重点仍然是：

- 阶段边界清晰
- 条件/操作原语清晰
- 运行时索引清晰
- 规则执行稳定

日志 trace 可以在这套结构稳定后再补。  
因为一旦阶段、上下文、操作边界明确，trace 往往是自然加上的，而不是必须一开始就完整做完。

## 15. 不做的事

当前设计不追求：

- 做成可外部扩展的通用引擎脚本系统
- 用一个统一抽象覆盖所有效果语义
- 在第一版就支持任意复杂的依赖层系统
- 让定义层直接依赖代码类名或业务 handler id 作为核心模型

第一版的目标是：

- 结构清晰
- 易于实现
- 能稳定支撑现有需求
- 为以后“来源不再严格区分”保留空间

## 16. 最终结论

最终建议如下：

- 来源定义继续统一
- 扩展点不放在来源类型继承上
- 战斗执行机制以固定阶段回调为主
- 数值修改使用分阶段聚合
- 副作用和流程追加使用固定阶段串行执行
- 定义层的 hook 以“阶段 + 条件 + 操作”为主
- 只有少量复杂机制进入自定义执行

换句话说：

- “定义层保持统一”应继续保留
- “战斗流程实现采用固定阶段回调”应替代纯 `timing + action`
- “规则式 hook”应作为主路线
- “自定义执行”应作为少量逃生口，而不是主模型
