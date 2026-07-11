# 战斗运行时后续议题

本文只记录当前重构完成后仍未实施或仍需规则确认的事项。现行结构见
`battle-runtime-architecture.md`，伤害顺序见 `battle-damage-timing-design.md`。

## 1. 间接伤害的受伤后语义（待确认）

毒伤当前沿用既有行为：实际扣血后触发 `OnDamageTaken`。这不自动意味着灼烧、自残、
反伤等所有间接伤害都应采用相同语义。后续增加这些来源时，需要逐类确认它们是否属于
“任何实际 HP 损失”还是“技能命中造成的受击”。

## 2. 位置变化 Effect

治疗和资源恢复已经可以作为显式 Effect 执行，但改变敌我单位位置尚未建模。后续有正式
技能数据时，再建立 `BattleRelocationResolver`，并明确：

- 推、拉、交换和传送的目标及落点规则；
- 阻挡、越界、控制区与不可移动状态；
- 位置变化产生的 Fact、Cue 和后续触发时机。

不让位置技能借用伤害流程，也不在没有内容需求时提前设计完整位移体系。

## 3. 死亡与特殊胜负条件

Core 当前只根据阵营存活情况输出 `Ongoing`、胜方或 `Draw`；外层暂把平局按我方失败
处理。死亡前拦截、复活、击杀触发和特殊胜负条件仍未实现。需要这些规则时，再建立明确的
死亡处理阶段和可扩展的 outcome 规则。

## 4. 通用 Effect timing 白名单

当前对真武增伤、真武挡刀、怪病等专用 Effect 同时做能力类型和 timing 校验；通用 Effect
仍沿用现有 timing policy。待内容规模确实需要时，再把所有 Effect 的合法 timing 建成统一
装配校验，不为形式完整性扩充矩阵。

## 5. 回放与诊断

命令结果已经返回有序的 Fact、Cue、Trace，`BattleState` 不保存 UI 消费队列。若后续需要
回放或长期诊断，应建立独立 `BattleHistory`，由宿主或调试设施选择性保存消息批次，不能
恢复状态与 UI 双事件源。

## 6. 仍明确不纳入

- 改变 `BattleUnit -> CharacterInstance` 的直接关联；
- 禁止战斗中即时成长或拆 `BattleUnit` 内部结构；
- 调整 `BattleEngine` 公共构造签名；
- 统一 AI 与预览执行模型；
- 重做战斗物品原子性；
- 引入完整 ASC、AttributeSet 或 Tag 体系；
- 在现有职责仍内聚时继续机械拆分 `BattleBoardView`。
