# 战斗运行时后续重构议题

本文记录当前战斗内核中值得继续收敛、但不纳入第一轮伤害阶段迁移的设计议题。

第一轮仍以 `battle-damage-timing-design.md` 为准，只修正：

- `BeforeDamageCalculation`
- `BeforeDamageApplied`
- `TakeDamage()`
- `OnDamageTaken`
- 真武七截阵与相关测试、数据迁移

本文不是当前实现规范，也不要求第一轮顺带建立下述抽象。后续开始对应工作前，应结合届时的正式技能和战场需求重新确认最小接口。

## 1. Ability 与 Effect 执行边界

一次技能释放应逐步收敛为“动作编排 + 有序 Effect”，而不是让 `CastSkill()` 直接承担所有效果语义。

建议方向：

- `BattleEngine` 保留对外动作入口和动作级编排。
- 普通技能默认生成伤害 Effect 与技能 Buff Effect。
- 治疗、改变敌我位置等特殊技能可只生成自己的 Effect，不必经过伤害管线。
- 混合技能通过有序 Effect 组合表达先后关系。
- 现有绝技数据尚未完整表达治疗、真实伤害和位置变化；未完成逐项规则确认前，不直接切换为 effects-only。

## 2. 统一状态变更入口

当前普通伤害、追加攻击、周期伤害和自定义 Hook 存在直接修改单位资源的路径。后续应让同类变化进入统一 resolver，避免不同调用点拥有不同 timing、事件和边界语义。

可考虑的职责：

- `BattleDamageResolver`
- `BattleRecoveryResolver`
- `BattleBuffResolver`
- `BattleRelocationResolver`
- `BattleResourceResolver`

`BattleUnit.TakeDamage()`、`RestoreHp()` 等仍可作为最终状态原语，但不应继续成为任意规则代码都能绕过流程调用的业务入口。

### 待确认：间接伤害的扣血后反应

需要明确毒伤、灼烧、自残等间接伤害是否触发 `OnDamageTaken`：

- 若 `OnDamageTaken` 表示“任何实际 HP 损失”，间接伤害应触发。
- 若它表示“技能命中造成的受击”，间接伤害应跳过。

当前统一伤害入口会让毒伤在实际扣血后触发 `OnDamageTaken`。在规则语义确定前，不将这一行为视为已确认缺陷，也不据此扩展其他间接伤害。

## 3. 拆分阶段 Context

当前 `BattleHookContext` 同时承载行动、技能消耗、命中、伤害、恢复、Buff 和表现请求，字段大量可空，并反向持有 `BattleEngine`。

后续应按阶段能力拆分 Context：

- 命中阶段只能修改命中结果。
- 伤害计算阶段只能修改公式字段。
- 落血前阶段只能修改待应用伤害和最终承伤目标。
- 扣血后阶段只能读取实际损失并追加后续反应。
- 恢复、Buff、行动开始和技能消耗使用各自的窄上下文。

目标不是为每个 timing 建立形式化 DTO，而是通过类型边界阻止某个阶段执行不属于它的操作。

## 4. 统一目标选择

当前特殊技能和 Hook 分别维护目标选择逻辑，新增 Effect 时容易同时修改多处分派。

后续可提取统一的 `BattleTargetResolver`，输入由两部分组成：

- `BattleTargetQuery`：自身、主目标、覆盖目标、全体友军、全体敌军、附近友军等。
- `BattleTargetContext`：施法者、主目标、技能覆盖单位和战斗状态。

需要明确区分：

- 技能空间范围决定哪些格子和初始单位被覆盖。
- Effect 目标查询决定某个具体 Effect 最终作用于谁。

这样可以自然表达“对覆盖敌人造成伤害，同时治疗施法者”等组合，而不需要让所有 Effect 共用同一目标列表。

## 5. 战斗事实与 Cue 分离

当前 `BattleEvent` 同时承载状态事实、流程事实、Hook 调试信息以及飘字、语音等表现请求。

后续应区分：

- Battle Fact：已经发生的领域事实，例如受伤、Buff 应用、技能升级。
- Battle Cue：宿主需要播放的表现，例如技能动画、飘字和语音。
- Battle Trace：Hook 条件、操作和数值变化等诊断信息。

Fact 是规则结果，Cue 是表现请求，Trace 是诊断数据。三者不应继续依赖一个带大量可空 payload 的统一记录类型。

## 6. 命令级事件批次

当前事件永久追加到 `BattleState.Events`，Godot 侧通过事件下标消费增量；同时 `BattleActionResult` 又携带部分事件，形成两个结果来源。

后续应让每次改变战斗状态的命令拥有独立 execution scope，并直接返回该命令产生的有序 Fact/Cue。`BattleState` 只保存战斗状态；如果需要回放，应显式建立 `BattleHistory`，不要让 UI 消费队列兼任历史系统。

## 7. Effect 链的确定性与递归保护

伤害、反伤、追击、转移和治疗可能继续产生新的 Effect。后续 execution scope 应至少记录：

- Effect 序号
- 来源 Effect 与父 Effect
- 稳定的 Hook 执行顺序
- 最大链深度
- 同一规则在单条链中的触发次数

这不要求引入完整 GAS 或 Tag 体系，只用于保证结果可复现并阻止反伤、追击等机制形成无限递归。

## 8. 死亡与战斗结果边界

当前 Godot 宿主根据双方存活单位推断战斗结束。后续若加入免死、复活、击杀触发或特殊胜负条件，应在 Core 中建立明确的死亡处理和战斗结果判定边界。

推荐顺序：

```text
落血
→ 死亡前规则
→ 单位败退事实
→ 死亡/击杀后规则
→ 战斗结果判定
```

该边界只负责战斗运行态结果，不必同时承担奖励和正式结算。

## 9. 战斗内成长职责

保留 `BattleUnit -> CharacterInstance` 的直接关联和战斗中即时成长，但后续可把技能经验、角色经验与升级事件从 `CastSkill()` 提取到独立的成长 resolver。

提取后仍应满足：

- 成长立即写入 `CharacterInstance`。
- 本次战斗中后续行动能看到升级结果。
- 成长规则和表现事件不再由技能主流程逐项拼装。

## 10. 暂不纳入

除非后续需求明确，以上重构仍不扩展到：

- 改变 `BattleUnit -> CharacterInstance` 的关联方式
- 禁止战斗中成长
- 重做 `BattleUnit` 内部结构
- 调整 `BattleEngine` 公共构造方式
- 统一 AI 与预览执行
- 战斗物品原子性
- 完整 ASC、AttributeSet 或 Tag 体系
