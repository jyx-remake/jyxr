# Attachment Design Note

本文记录被删除前的 attachment 设计方向，作为后续重构的设计备忘。当前代码库不再保留 attachment 运行时代码、定义类型或 resolver；后续重建时应以本文为边界，而不是兼容已删除实现。

## 当前代码状态

- 当前仓库保留的是轻量 `AffixDefinition` 系统，用于角色静态投影，不等同于本文完整 attachment 方案。
- 装备、天赋、Buff 通过 `IAffixProvider` 提供普通 affix。
- 外功、内功通过 `SkillAffixDefinition` 提供带来源条件的技能 affix：
  - `MinimumLevel`
  - `RequiresEquippedInternalSkill`
  - `Effect`
- `GrantTalentAffix` 等需要仓储引用的 affix 当前由 `JsonContentLoader.ResolveAffixes(...)` 集中解析。
- `CharacterInstance.RebuildSnapshot()` 负责从角色自身运行态收集当前生效 affix，再由 `TalentResolver` 展开授予天赋。
- battle hook runtime 当前不存在；本文中的 hook 章节是后续重构设计方向。

## 目标

- 用统一模型表达“某个内容定义会给角色或战斗对象附加能力”。
- 让装备、外功、内功、特殊技能、天赋、Buff 等来源都能以同一套规则暴露被动能力。
- 区分稳定定义、角色持久化实例、战斗运行态投影，避免把运行时缓存写进存档。
- 在装配阶段解析 definition 引用，高频路径不反复按字符串查仓储。
- 条件、静态修饰和战斗 hook 分层建模，避免把不同生命周期的效果压进一个宽泛 effect 列表。

## 非目标

- 不为旧实现保留兼容层。
- 不把 `CharacterInstance` 或 `GameSession` 改成单例。
- 不在存档中保存 runtime definition 引用。
- 不在 attachment 层直接承载完整战斗命令、地图事件或演出流程。

## 核心概念

`AttachmentSourceDefinition` 表达“可以产出 attachment 的定义”。候选来源包括：

- 装备定义
- 外功定义
- 内功定义
- 特殊技能定义
- 天赋定义
- Buff 定义

source 本身只说明能力来源，不直接说明能力是否正在生效。是否生效由当前角色实例、装备状态、技能等级、Buff 层数、战斗上下文等运行态输入共同决定。

`Attachment` 是 source 产出的能力条目。它应该按生命周期拆分：

- 静态修饰：进入角色或战斗投影的数值、标签、表现层标记。
- 战斗 hook：在固定 phase 上响应战斗事件，读取上下文并执行规则操作。
- 展开型修饰：例如授予额外天赋，会把新 source 加入解析队列，直到闭包稳定。

## Source Context

resolver 不应只拿 definition。它还需要 source 的运行态上下文：

- source definition ID
- source 类型
- 技能等级
- 是否装备
- Buff 层数与剩余回合
- 是否来自闭包展开
- owner 角色或 combatant

条件判断只读取明确传入的 context，不隐式访问全局仓储或宿主状态。

## 条件模型

attachment 条件应分为两层：

- source 条件：判断 attachment 是否从该来源生效，例如最低技能等级、内功是否装备、Buff 层数。
- hook 条件：判断某个战斗 phase 中是否执行操作，例如随机概率、生命值阈值、是否击杀目标、使用次数是否剩余。

source 条件可以在角色投影或战斗初始化时预先求值；hook 条件必须在对应 phase 执行时读取战斗上下文。

## 静态修饰

静态修饰用于构建角色属性或战斗入场投影。推荐的最小集合：

- 属性修饰：对 `StatType` 做加算或倍率修正。
- 标签修饰：给角色或 combatant 增加可查询标签。
- 天赋授予：把另一个天赋 source 加入 resolver 队列。
- 表现修饰：给宿主层提供动画、特效或 UI 标记投影。

属性计算应显式定义叠加顺序。基础建议：

1. 读取 `CharacterDefinition.Stats` 与 `CharacterInstance.AllocatedStats` 得到基础属性。
2. 汇总所有生效的加算修饰。
3. 汇总所有生效的倍率修饰。
4. 计算结果向外暴露为不可变投影。

天赋授予必须有循环保护。以稳定 ID 去重，不允许同一解析批次无限展开。

## 战斗 Hook

hook 不应和静态修饰混成同一种 effect。它的生命周期是战斗 phase，而不是角色属性投影。

推荐结构：

- `Phase`
- `Conditions`
- `Operations`
- `UsageKey` 或等价的次数作用域

phase 应是封闭枚举，并维护集中式合法矩阵，限定哪些 condition 和 operation 可以出现在某个 phase。这样内容错误能在加载阶段暴露，而不是战斗中半路失败。

候选 phase：

- TurnEnd
- BeforeDamageResolve
- BeforeDamageApply
- BeforeDeath
- AfterDamageApplied
- AfterAction

候选 condition：

- RandomChance
- SelfHealthBelowPercent
- KilledAnyTargetThisAction
- UsageRemaining

候选 operation：

- AddFinalDamagePercent
- ClampFinalDamageMax
- GrantExtraAction
- ConsumeUsage
- CancelDeath
- RestoreHealthFull
- ModifyResource
- ApplyDamage

战斗运行态应按 phase 预索引 hook，避免每次执行 phase 时全量扫描所有 source。

## 内容装配

后续重建时应优先使用真正二阶段加载：

1. 先注册所有 runtime definition 空壳。
2. 再统一解析 attachment 中的 definition 引用。
3. 最后运行仓储级校验，包括缺失引用、循环引用、phase 合法矩阵和条件参数范围。

JSON 输入可以继续保留 `modifiers` 与 `hooks` 两个显式字段，但不要把它们压成一个宽泛 `effects` 字段。字段名反映生命周期边界：

- `modifiers` 表达静态或投影期能力。
- `hooks` 表达战斗 phase 能力。

## 存档边界

存档只保存稳定 ID 与实例状态：

- 角色 ID
- 角色 definition ID
- 已学技能 ID、等级、经验、启用状态
- 装备实例 ID 与装备 definition ID
- 已解锁天赋 ID
- Buff 实例状态

存档不保存 attachment 解析结果。读档后由当前内容仓储重新构建角色实例，再由 resolver 生成新的投影。

## 当前删除范围

本次删除的是旧 attachment 实现，不是删除设计方向：

- source interface
- attachment condition definitions
- modifier definitions
- attachment resolver 与 resolution result
- definition 上的 attachment `Modifiers` 字段
- 依赖 attachment resolver 的属性与天赋闭包测试

后续重构应重新建模，不从旧类型名和旧 JSON 形状出发做兼容补丁。
