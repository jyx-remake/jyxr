# 角色技能切换

角色面板已接入技能切换。`CharacterPanel` 根据技能类型调用 `CharacterService`，由领域对象修改状态；发生变更后应用层发布 `CharacterChangedEvent`，角色面板与名册等订阅者刷新显示。重复设置相同状态不发布变更事件。

| 技能类型 | 应用层入口 | 状态 |
| --- | --- | --- |
| 外功 | `SetExternalSkillActive` | 独立激活开关 |
| 特殊技能 | `SetSpecialSkillActive` | 独立激活开关 |
| 招式 | `SetFormSkillActive` | 按来源技能和招式 ID 独立启停 |
| 内功 | `EquipInternalSkill` | 切换当前装备内功 |

外功和特殊技能切换通过现有实例修改激活态，保留等级、经验和冷却等运行状态；不为此创建新的技能服务或替换技能实例。外功被动 affix 不随激活态关闭。内功装备变化由 `CharacterInstance` 重建角色快照，使依赖装备内功的属性与天赋生效。

招式的启停状态由来源外功或内功持有，禁用列表保存为 `DisabledFormSkillIds`。`CharacterMapper` 在对应的 `ExternalSkillRecord`、`InternalSkillRecord` 中写入并恢复该列表；它不是仅用于显示的临时勾选状态。招式最终能否使用还受来源技能状态等领域规则约束。

实现入口：

- [CharacterService](../src/Game.Application/CharacterService.cs)：应用层操作与角色变更事件。
- [CharacterInstance](../src/Game.Core/Model/Character/CharacterInstance.cs)：激活、装备与快照更新。
- [CharacterMapper](../src/Game.Core/Model/Character/CharacterMapper.cs)：存档转换。
- [CharacterPanel](../src/Game.Godot/UI/Character/CharacterPanel.cs)：技能卡点击与界面刷新。
