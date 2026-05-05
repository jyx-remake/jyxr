# 角色技能切换设计草案

## 目标

为角色面板中的技能页正式支持技能切换，包括：

- 特殊技能
- 外功
- 内功

当前阶段只记录方案，不直接接入现有逻辑。

## 当前基础

当前底层模型已经具备大部分状态表达能力：

- 外功有独立激活态：`ExternalSkillInstance.CurrentIsActive`
- 特殊技能有独立激活态：`SpecialSkillInstance.CurrentIsActive`
- 内功使用“当前装备内功”语义：`CharacterInstance.EquippedInternalSkillId`
- 存档层已支持保存这些状态：
  - `ExternalSkillRecord.IsActive`
  - `SpecialSkillRecord.IsActive`
  - `InternalSkillRecord.Equipped`

因此不需要额外引入新的技能槽或切换状态表。

当前真正缺少的是：

- 应用层统一的技能切换服务
- 明确的切换规则
- UI 点击到应用层的接线
- 切换后统一事件刷新

## 建议规则

### 外功

- 多选，无上限。
- 点击未激活外功：激活。
- 点击已激活外功：取消激活。
- 切换只影响 `IsActive`，不影响等级、经验、最大等级。

说明：

- 当前外功被动 affix 不跟随激活态，这与现有 core 规则一致。
- 如果未来要改成“外功被动 affix 只对激活外功生效”，应在角色快照收集规则里单独调整，不应混进这次 UI 切换接线。

### 特殊技能

- 多选，无上限。
- 交互与外功一致。
- 切换只修改 `IsActive`。

### 内功

- 单选，始终至多一个装备。
- 点击某个未装备内功：将其设为当前装备内功。
- 点击当前已装备内功：第一版建议不做取消装备，保持 no-op。

说明：

- 不建议第一版支持“点掉后变成无内功”，否则会引入更多边界状态和 UI 判定。
- 当前很多内功 affix 带 `requiresEquippedInternalSkill = true`，因此切换内功后必须刷新角色快照。

### 绝技 / FormSkill

- 不单独持久化状态。
- 状态继续直接跟随父技能。
- 第一版不支持独立切换。

UI 建议：

- 可以保留外框和勾选展示。
- 但点击应禁用，或代理给父技能。

第一版更建议：

- 直接禁用点击，只作为展示项。

### LegendSkill

- 继续不展示。
- 不参与切换。

## 推荐分层

不建议把切换逻辑写进 `SkillBox`、`SkillTab` 或 `CharacterPanel`。

建议在 `Game.Application` 中新增服务：

- `CharacterSkillService`

并挂入 `GameSession`，与当前这些服务同级：

- `InventoryService`
- `MapService`
- `StoryService`

这样切换逻辑可以统一收口在应用层，而不是散落在各个 UI 控件中。

## 建议接口

第一版建议保持显式接口，而不是过早抽象成一个统一 toggle API。

推荐接口：

```csharp
SetExternalSkillActive(string characterId, string skillId, bool isActive)
SetSpecialSkillActive(string characterId, string skillId, bool isActive)
EquipInternalSkill(string characterId, string skillId)
```

不建议第一版直接做：

```csharp
ToggleSkillActive(string characterId, SkillKind kind, string skillId)
```

原因：

- 外功和特殊技能是 toggle 语义。
- 内功是 equip 语义。
- 强行统一会让规则变得模糊。

## 领域层建议

当前 `ExternalSkillInstance` 与 `SpecialSkillInstance` 是 record，激活态放在构造参数中，而不是可变属性。

因此正式实现时，建议在 `CharacterInstance` 上增加明确方法，由角色实例负责原位替换对应条目，而不是让 UI 直接操作列表。

推荐新增方法：

```csharp
SetExternalSkillActive(string skillId, bool isActive)
SetSpecialSkillActive(string skillId, bool isActive)
EquipInternalSkill(string skillId)
```

其中内功装备方法当前已有：

- `CharacterInstance.EquipInternalSkill(string? internalSkillId)`

需要特别注意：

- 替换外功或特殊技能运行时实例时，不能丢失 `Level`
- 不能丢失 `Exp`
- 不能丢失 `MaxLevel`
- 不能丢失 `CurrentCooldown`

也就是说，切换本质上应是：

- 按同定义构造新实例
- 保留已有运行时动态状态
- 仅切换激活位

## 快照刷新策略

推荐按影响范围处理：

- 外功切换：当前规则下可不强制重建快照
- 特殊技能切换：当前规则下可不强制重建快照
- 内功切换：必须重建快照

但从实现稳妥性出发，更建议第一版统一处理为：

- 任意技能切换后都调用一次 `RebuildSnapshot()`

原因：

- 成本低
- 不容易漏规则
- 角色面板操作频率不高

因此第一版建议采用统一重建策略。

## 事件设计

切换完成后，应用层统一发布：

- `CharacterChangedEvent(characterId)`

这样可以复用现有角色刷新通路，而不需要发明新的 UI 专用事件。

## UI 接线建议

建议职责分层如下：

### SkillBox

- 只负责发出“用户点击了激活按钮”的请求
- 不直接改模型

### SkillTab / CharacterPanel

- 接收 `SkillBox` 的点击事件
- 调用 `Game.Session.CharacterSkillService`

### 刷新

- 技能切换完成后，由应用层发布 `CharacterChangedEvent`
- 当前角色面板接到后刷新本角色信息与技能页

这样可以避免：

- `SkillBox` 直接依赖 `Game`
- 各个 UI 控件重复实现切换规则
- 以后角色面板与其他面板出现规则分叉

## 与当前 UI 的对应关系

建议技能卡按钮行为如下：

- 外功卡：可点击
- 内功卡：可点击
- 特殊技能卡：可点击
- FormSkill 卡：不可独立点击，或点击代理父技能
- 未解锁 FormSkill：继续不显示

勾选显示规则建议：

- 外功：跟 `IsActive`
- 特殊技能：跟 `IsActive`
- 内功：跟 `IsEquipped`
- FormSkill：跟父技能当前有效状态

## 实现顺序建议

推荐按以下顺序落地：

1. 在 `Game.Application` 中新增 `CharacterSkillService`
2. 在 `CharacterInstance` 上补外功/特殊技能状态修改方法
3. 把 `CharacterSkillService` 挂到 `GameSession`
4. 切换后统一调用 `RebuildSnapshot()`
5. 切换后发布 `CharacterChangedEvent`
6. 技能页 `SkillBox` 恢复点击接线
7. `CharacterPanel` 订阅角色变更并刷新当前角色页
8. 补测试

## 测试建议

至少补以下测试：

- 外功激活后，存档记录中的 `ExternalSkillRecord.IsActive` 正确
- 外功取消激活后，存档记录中的 `ExternalSkillRecord.IsActive` 正确
- 特殊技能激活/取消激活后，`SpecialSkillRecord.IsActive` 正确
- 切换内功后，`EquippedInternalSkillId` 正确变化
- 切换内功后，`requiresEquippedInternalSkill` 的 affix 与派生天赋立即变化
- 切换完成后会发布 `CharacterChangedEvent`
- FormSkill 不产生独立持久化状态

## 结论

基于当前项目结构，第一版正式技能切换建议定义为：

- 外功：多选开关
- 特殊技能：多选开关
- 内功：单选装备
- 绝技 / FormSkill：继续跟随父技能，不独立存状态
- 切换逻辑统一进入 `Game.Application.CharacterSkillService`
- 切换完成后统一发布 `CharacterChangedEvent`
- 第一版统一调用 `RebuildSnapshot()`

该方案与当前 core、存档结构和角色面板迁移方向一致，不需要额外引入兼容性建模。
