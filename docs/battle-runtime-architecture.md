# 战斗运行时架构

## 分层方向

依赖固定为：

```text
Game.Core battle runtime
        ↓
Game.Application battle use cases
        ↓
Game.Godot battle presentation
```

Core 不依赖 Application 或 Godot。Application 负责根据 request 建立战斗、结算和 carryover；
Godot 只调用命令、播放消息并维护界面状态。

## Core

`BattleEngine` 保留命令入口和战斗时间线，具体职责下沉到：

- `BattleSkillExecutor`：技能计划；
- `BattleEffectExecutor`：统一 Effect 执行；
- 各状态 resolver：伤害、恢复、Buff、资源与成长；
- `BattleOutcomeEvaluator`：阵营存活结果；
- `BattleTargetResolver` / `BattleSkillTargeting`：Effect 与技能空间目标；
- `BattleExecutionScope`：Effect 链顺序和递归保护。

`BattleUnit` 继续直接引用 `CharacterInstance`，战斗成长立即写入角色实例。

## 命令与消息

所有改变状态的引擎命令返回 `BattleCommandResult<T>`：

- `Success` 和失败信息；
- 命令业务结果；
- 本次命令产生的单一有序 `BattleMessage` 列表。

消息分为：

- `BattleFact`：伤害、恢复、移动、Buff、成长等已发生事实；
- `BattleCue`：飘字、语音等宿主表现请求；
- `BattleTrace`：Hook 触发与 execution scope 诊断。

`BattleState` 不保存 UI 事件队列，调用方也不能再从 action result 和 state 两处消费事件。

## Application

`BattleService` 是 `GameSession` 的薄门面，对外接收 `SpecialBattleRequest`。内部由：

- `BattleStateFactory` 装配普通、竞技场和珍珑战斗；
- `ProceduralBattleCharacterFactory` 生成随机敌人和难度强化；
- `ZhenlongqijuBattleFactory` 处理珍珑专属内容；
- `BattleSettlementService` 预览并应用奖励；
- `BattleCarryoverService` 回写角色资源并恢复队伍资源。

不保留字符串/数组构建重载或测试专用公开入口。

## Godot

`BattleScreen` 保留为场景根脚本和控制器装配点。普通类分别负责：

- `BattleBoardController`：棋盘交互与目标高亮；
- `BattleActionPanelController`：行动按钮、技能和物品/状态面板；
- `BattleEventPresenter`：Fact、Cue 的宿主呈现；Trace 当前保留给按需诊断，不进入玩家表现；
- `BattleSkillPresentationController`：技能与移动演出；
- `BattleSettingsController`：速度、自动战斗和设置；
- `BattleSettlementController`：carryover、奖励和结束 Task。

`BattleFlowOrchestrator` 只负责命令调用、时间线循环和自动回合。胜负由 Core evaluator 输出；
Core 的平局目前由外层按我方失败处理，投降仍是宿主强制失败。
