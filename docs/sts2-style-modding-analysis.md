# STS2 风格代码 MOD 方案评估

## 结论

该项目可以实现类似《杀戮尖塔 2》的 MOD 方案，但不应原样照搬。

当前项目已经是数据驱动 RPG 引擎：角色、武学、物品、地图、战斗、剧情主要由 JSON definition 描述，再由 `Game.Content` 装配进 `InMemoryContentRepository`。这和 STS2 的 class-as-content 模型不同。STS2 更偏向让 MOD DLL 新增模型类并由 `ModelDb` 扫描注册；本项目更适合保留 JSON 内容 MOD 作为主入口，再叠加代码插件层和可选 Harmony。

推荐方向：

- 保留当前 JSON 内容 MOD 作为官方主入口。
- 增加 MOD DLL 插件层，让 MOD 能注册内容变换、剧情命令、谓词、战斗效果和运行时 hook。
- 可选内置 Harmony，作为官方入口覆盖不到时的自由修改能力。
- PCK 继续只负责 Godot 资源，不支持 loose assets 覆盖。

## 当前项目状态

项目已经具备一半基础。

Godot 宿主启用了动态加载：

- `engine-free-rpg.csproj`
  - `AssemblyName` 是 `engine-free-rpg`
  - `EnableDynamicLoading` 是 `true`
  - 宿主引用 `Game.Content` 与 `Game.Application`

项目拆分清晰：

- `Game.Core`
  - 领域模型、definition、战斗、剧情 runtime、存档模型。
- `Game.Content`
  - JSON 内容加载、校验、装配为 `InMemoryContentRepository`。
- `Game.Application`
  - 会话、服务、MOD manifest、launcher 配置。
- `engine-free-rpg`
  - Godot 宿主、场景、UI、资源与启动装配。

`mod.json` 已经预留代码 MOD 字段：

- `packs`
- `assemblies`

但是当前启动流程只使用了 `packs`，没有加载 `assemblies`。

当前启动流程位于 `src/Game.Godot/Bootstrap/GameRuntimeBootstrap.cs`：

1. `LoadResourcePacks(modContext, logger)`
2. `EnsureRuntimeNodes(sceneTree)`
3. `LoadConfig(modContext)`
4. `new JsonContentLoader().LoadFromDirectory(modContext.DataDirectoryPath)`
5. 构造 `GameSession`
6. `Game.Initialize(session, modContext, logger)`

也就是说，当前已经有：

- MOD 发现
- MOD 独立目录
- MOD 独立存档目录
- MOD manifest
- PCK 资源包加载
- loose JSON 内容加载

当前还没有：

- MOD DLL 加载
- MOD initializer
- MOD 代码 API
- MOD 注册表
- Harmony
- 多 MOD 同时启用与合并

## 和 STS2 的关键差异

STS2 的内容模型是 class-as-content。

典型流程是：

1. MOD DLL 新增 `AbstractModel` 子类。
2. 游戏扫描 MOD 程序集。
3. `ModelDb` 实例化这些类型。
4. MOD 调用 `ModHelper.AddModelToPool` 把卡牌、遗物、药水加入现有池。
5. 入口不足时用 Harmony patch。

本项目的内容模型是 definition-as-content。

典型内容类型是：

- `CharacterDefinition`
- `ExternalSkillDefinition`
- `InternalSkillDefinition`
- `ItemDefinition`
- `MapDefinition`
- `BattleDefinition`
- `BuffDefinition`
- `TalentDefinition`

这些 definition 从 JSON 反序列化，再通过 `JsonContentLoader.BuildRepository(...)` 建索引、解析引用、校验，最终进入 `InMemoryContentRepository`。

因此，本项目不适合把官方 MOD 入口设计成“继承角色类 / 武学类 / 物品类并自动扫描”。更自然的入口是：

- 追加或修改 `ContentPackage`
- 注册剧情命令
- 注册剧情/地图谓词
- 注册战斗 hook condition/effect
- 注册服务级生命周期 hook
- 必要时通过 Harmony patch 现有实现

## 推荐架构

### 1. 增加稳定契约程序集

建议新增 `Game.Modding` 或 `Game.ModApi` 项目，只放 MOD 作者需要引用的稳定接口。

核心接口可以是：

```csharp
public interface IGameMod
{
    void Initialize(IModContext context);
}
```

`IModContext` 应提供：

- 当前 `ModManifest`
- 当前 MOD 目录
- logger
- 内容注册入口
- 剧情命令注册入口
- 谓词注册入口
- 战斗扩展注册入口
- session lifecycle hook
- 可选 Harmony 访问入口

示意：

```csharp
public interface IModContext
{
    string ModId { get; }
    IModLogger Logger { get; }
    IContentModRegistry Content { get; }
    IStoryCommandRegistry StoryCommands { get; }
    IGamePredicateRegistry Predicates { get; }
    IBattleExtensionRegistry Battle { get; }
    ISessionHookRegistry Sessions { get; }
}
```

### 2. 实现 assemblies 加载

当前 `ModManifest.ResolvedAssemblies` 已经存在，但没有使用。

启动顺序建议改成：

```text
LoadResourcePacks
LoadModAssemblies
RunModInitializers
ApplyHarmonyPatches
BuildContentPackage
ApplyContentContributors
BuildRepository
BuildSession
RunSessionHooks
Game.Initialize
```

DLL 加载建议在 Godot 宿主层实现，因为它需要处理 Godot、PCK、平台路径和宿主程序集解析。

可采用默认 `AssemblyLoadContext`，先做不可卸载版本。等需求明确后再考虑 collectible context。

### 3. 内容扩展走 ContentPackage contributor

不要让代码 MOD 直接绕过 `JsonContentLoader` 改 `InMemoryContentRepository`，否则会跳过引用解析与校验。

建议提供：

```csharp
public interface IContentPackageContributor
{
    void Configure(ContentPackage package);
}
```

MOD 可以：

- 追加角色
- 追加武学
- 追加物品
- 追加地图
- 追加剧情
- 修改已有 definition

然后仍然统一走：

```text
BuildRepository
ResolveAffixes
definition.Resolve(...)
ValidateRepository
```

这和当前 `Game.Content` 的设计最兼容。

### 4. 剧情命令与谓词改为组合注册表

当前 `StoryCommandBinder` 只扫描一个 target 对象的 `[StoryCommand]` 方法。

当前命令来源：

- `StoryCommandDispatcher`
- `GodotStoryRuntimeHost`

当前谓词来源：

- `ApplicationPredicateLibrary`

这对内置命令足够，但不适合代码 MOD。建议把 binder 改成可以组合多个 command provider。

目标形态：

```csharp
public interface IStoryCommandRegistry
{
    void RegisterObject(object target);
    void Register(string name, StoryCommandDelegate command);
}
```

启动时注册顺序：

1. 内置 application 命令
2. 内置 Godot host 命令
3. MOD 命令

冲突策略必须明确。建议默认禁止覆盖，除非 manifest 或 API 显式声明 override。

### 5. 战斗 hook 扩展改为显式注册

当前战斗 hook 已经存在，但它是固定 JSON 多态和固定 executor。

限制点：

- `BattleEffectDefinition` 的派生类型通过 `[JsonDerivedType]` 固定声明。
- `BattleHookConditionDefinition` 的派生类型通过 `[JsonDerivedType]` 固定声明。
- `BattleHookExecutor.ApplyEffect(...)` 用 `switch` 处理固定类型。
- 未知类型会 `NotSupportedException`。

如果 MOD 想新增新的 hook effect，仅加载 DLL 不够。还需要解决两个问题：

1. JSON 如何反序列化到 MOD 定义的类型。
2. executor 如何执行 MOD 定义的类型。

推荐提供注册表：

```csharp
public interface IBattleExtensionRegistry
{
    void RegisterEffect<TEffect>(string discriminator, IBattleEffectExecutor<TEffect> executor)
        where TEffect : BattleEffectDefinition;

    void RegisterCondition<TCondition>(string discriminator, IBattleConditionEvaluator<TCondition> evaluator)
        where TCondition : BattleHookConditionDefinition;
}
```

同时需要把 `System.Text.Json` 的多态解析从纯 attribute 改成可注册 resolver 或自定义 converter。

### 6. Harmony 作为自由修改层

Harmony 不应替代官方 API。它适合作为：

- 修改官方 API 没覆盖的行为
- 临时兼容旧 MOD
- 高级作者做深层 patch

建议 manifest 显式声明：

```json
{
  "assemblies": ["bin/MyMod.dll"],
  "harmony": true
}
```

加载策略：

- 如果 DLL 中有 `IGameMod`，先执行 `Initialize(...)`。
- 如果 `harmony` 为 true，再对该 assembly 执行 `PatchAll`。
- 不建议默认无条件 `PatchAll`，因为本项目还在快速演化，自动 patch 会让错误定位困难。

## 程序集引用与运行时解析

本项目比 STS2 更适合独立契约程序集。

MOD 编译期建议引用：

- `Game.Modding`
- `Game.Core`
- `Game.Content`
- `Game.Application`
- 必要时引用 `engine-free-rpg`

运行时不要覆盖这些 DLL。MOD DLL 只是额外加载。

需要实现依赖解析，把 MOD 依赖的项目程序集解析到宿主已加载版本：

- `Game.Core`
- `Game.Content`
- `Game.Application`
- `Game.Modding`
- `engine-free-rpg`
- `GodotSharp`
- `0Harmony`

如果不做解析，MOD 作者很容易把这些 DLL 一起打包，造成类型身份不一致。例如 MOD 自带一份 `Game.Core.dll` 时，`CharacterDefinition` 在运行时会变成另一份程序集里的类型，和宿主的 `CharacterDefinition` 不相等。

因此必须明确规则：

- MOD 不允许自带宿主程序集。
- MOD 编译期引用宿主提供的 reference assemblies。
- 运行时统一绑定到游戏已加载程序集。

## 多 MOD 问题

当前 launcher 是“选择一个 active mod”，不是多个 MOD 同时启用。

如果要像 STS2 一样多个 MOD 同时加载，需要新增：

- enabled mod 列表
- 依赖声明
- 加载顺序
- 冲突策略
- 内容合并策略
- 存档记录 active mod 列表
- 运行时错误隔离

内容合并必须先定义规则。

推荐规则：

- `jyxr-base` 是必选基础包。
- 其他 MOD 是 patch package。
- 新 ID 直接追加。
- 同 ID 默认禁止覆盖。
- 覆盖必须显式声明，例如 `replace: true` 或 `patches`。
- story segment 同名默认禁止。
- PCK 按 manifest 顺序加载，后者允许覆盖资源。

## 风险与约束

### 平台约束

桌面端最可行。

Android、iOS、Web 对动态 C# 加载和 JIT/AOT 更敏感。即使 Godot C# 桌面可行，移动端也可能需要禁用代码 MOD，只允许 JSON/PCK MOD。

### 序列化约束

当前 `GameJson.Default` 是普通 `JsonSerializerOptions`，多态由 `[JsonDerivedType]` 固定声明。

这意味着 MOD 新增 JSON 多态类型不会自动工作。需要：

- 自定义 converter
- 或 `DefaultJsonTypeInfoResolver` modifier
- 或显式注册 discriminator 到类型的表

### 服务替换约束

当前 `GameSession` 构造函数里直接 new 各服务：

- `BattleService`
- `StoryService`
- `MapService`
- `InventoryService`
- 等

这不利于 MOD 替换服务实现。若需要服务级扩展，应引入 `GameSessionBuilder` 或 service factory。

### 安全约束

代码 MOD 本质是本机代码权限。必须：

- 显示不可信 MOD 风险提示。
- 标记当前运行是 modded。
- 捕获 initializer 和 patch 异常。
- 在日志中记录 MOD ID、版本、程序集。
- 存档中记录 active mods。

## 建议实施顺序

### 阶段 1：最小代码 MOD

目标：加载 DLL，执行 initializer，不支持 Harmony。

新增：

- `Game.Modding`
- `IGameMod`
- `IModContext`
- assemblies 路径校验
- assembly load
- initializer 调用
- 基础日志

### 阶段 2：内容 contributor

目标：代码 MOD 可以追加/修改 `ContentPackage`。

新增：

- `IContentPackageContributor`
- `ContentPackage` 合并前 hook
- 基础冲突检查
- 测试：追加一个角色/武学/物品

### 阶段 3：剧情扩展

目标：MOD 可以注册 story command 和 predicate。

改造：

- `StoryCommandBinder`
- `GamePredicateBinder`
- `StoryCommandDispatcher`
- `GodotStoryRuntimeHost`
- `ApplicationPredicateLibrary`

### 阶段 4：战斗扩展

目标：MOD 可以注册 battle hook condition/effect。

改造：

- JSON 多态 resolver
- battle effect registry
- battle condition registry
- `BattleHookExecutor`

### 阶段 5：Harmony

目标：高级 MOD 可自由 patch。

新增：

- `0Harmony` 引用
- manifest 字段 `harmony`
- patch id 规则
- patch error logging

### 阶段 6：多 MOD

目标：多个 MOD 同时启用。

新增：

- enabled mods 设置
- 依赖/排序
- 内容合并策略
- 存档 MOD 指纹
- 版本与兼容检查

## 最终建议

本项目应采用：

```text
JSON/PCK 内容 MOD
    +
Game.Modding 官方代码插件 API
    +
可注册内容/剧情/战斗扩展点
    +
可选 Harmony
```

不要把核心内容系统改成 STS2 的“继承模型类自动扫描”。当前 JSON definition 模型更适合金庸群侠传这类内容密集型 RPG，也更利于工具链、热校验、内容合并和非程序 MOD 作者参与。

真正需要从 STS2 借鉴的是：

- 启动时加载 PCK
- manifest 管理
- mod DLL initializer
- 宿主程序集解析
- 可选 Harmony
- 明确的 modded 风险提示

不需要借鉴的是：

- `AbstractModel` 扫描式内容发现
- class-as-content 作为主要内容生产方式
