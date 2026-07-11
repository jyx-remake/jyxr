# 战斗 Effect 与 Phase Hook 设计

本文描述当前已落地的轻量能力执行模型。它参考 GAS 的 Ability、Effect、Context、Cue 概念，
但不引入 ASC、AttributeSet 或 Tag 体系。

## 1. 执行边界

- `BattleSkillExecutor` 负责编排技能的有序步骤。
- 普通技能默认包含命中与伤害步骤；Special Skill 不进入默认伤害流程。
- Special Skill 只执行自身 Buff 和显式 Effect；混合能力通过有序 Effect 表达。
- 技能、Hook 和周期效果最终都经 `BattleEffectExecutor` 执行。
- 物品保持独立事务语义，只复用 Buff、恢复和资源 resolver。

所有带目标的 Effect 实现 `ITargetedBattleEffectDefinition`，由 `BattleTargetResolver` 解释
自身、来源、主目标、全体友军、全体敌军和附近友军等选择器。技能覆盖范围只决定初始目标，
具体 Effect 可以再次选择自己的作用对象。

## 2. Hook 职责

`BattleHookExecutor` 是 `BattleEngine` 公共构造参数所保留的单条 Hook 执行入口，职责拆为：

- `BattleHookRunner`：收集角色与 Buff Hook，保持稳定顺序并记录 Trace；
- `BattleHookEvaluator`：条件求值；
- `BattleHookPreviewPolicy`：拒绝随机、表现和有副作用的预览组合；
- `BattleEffectTimingPolicy`：校验 Effect 是否允许出现在当前 timing；
- `BattleEffectExecutor`：执行通过校验的 Effect。

Hook 按稳定来源顺序收集并执行。错误 timing 或不支持的 Effect 组合直接失败，不做静默兼容。

## 3. 能力型 Context

公共 Hook 信息只表达 timing、所属单位、来源、目标、技能、Buff、随机服务和 execution scope。
Effect handler 通过窄接口申请能力：

- `IHitResultEffectContext`：修改命中结果；
- `IDamageCalculationEffectContext`：修改普通伤害公式；
- `IDamageApplicationEffectContext`：修改落血目标和数值；
- `IDamageTakenEffectContext`：读取实际受伤并追加反应；
- `IRecoveryEffectContext`：修改恢复量；
- `ISkillCostEffectContext`：修改技能消耗；
- `IBuffApplicationEffectContext`：取消或强化 Buff 应用；
- `IActionStartEffectContext`：跳过行动开始。

专用 handler 只能接收声明的能力。例如真武增伤只能访问伤害计算，真武挡刀只能访问落血前
改写，怪病只能访问行动开始。内容装配和运行入口都会拒绝错误组合。

## 4. 状态变化入口

- `BattleDamageResolver`：普通伤害与实际扣血；
- `BattleRecoveryResolver`：恢复量修正和实际恢复，返回请求量、修正量和实际量；
- `BattleBuffResolver`：Buff 应用、抵抗、强化与移除；
- `BattleResourceResolver`：怒气、行动条等资源；
- `BattleGrowthResolver`：技能、内功和角色经验及升级事实。

`BattleUnit.TakeDamage()` 等仍是最终状态原语，但规则代码不应绕过 resolver 自行拼接 timing、
实际变化量和消息。

## 5. Effect 链与保护

`BattleExecutionScope` 覆盖技能步骤、Hook、周期效果和它们产生的后续 Effect，记录序号、父
Effect、深度与规则来源。最大深度为 64；超过限制直接失败，以阻止追击、反伤等形成无限链。

## 6. 表现边界

Effect 和 Hook 只产出战斗消息，不直接依赖 Godot：

- Fact 表示已经发生的规则事实；
- Cue 请求宿主播放飘字、语音等表现；
- Trace 提供 Hook 和 Effect 链诊断。

三类消息在同一命令批次中保持相对顺序，具体播放方式由宿主决定。
