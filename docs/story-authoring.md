# 剧情源码编写指南

剧情使用 UTF-8 编码的 `.story` 文件，放在 MOD 的 `data/stories` 下，可按子目录组织。游戏加载内容时自动调用 `Game.StoryDsl` 编译源码，按 MOD 顺序装配生效剧情段。编辑完成后，重新启动游戏并加载该 MOD 组合即可验证。

编译器入口为 [StoryCompiler](../src/Game.StoryDsl/StoryCompiler.cs)，表达式语法统一复用 `Game.Expressions`。

完整命令和查询见 [游戏内容 DSL v3](game-dsl-v3.md)，目录、加载顺序和已有剧情的修改方式见 [MOD 数据补丁](mod-data-patching.md)。

已有剧情 XML 可通过 .NET 工具 `tools/StoryXmlConverter` 转成 `.story`，用法与分支复核要求见[旧 MOD 迁移指南](legacy-mod-migration-guide.md#5-将剧情-xml-转成-story)。

## 最小示例

将下面内容保存为 `data/stories/客栈.story`。地图事件的 `action` 可以通过 `story('客栈_问候')` 发起这段剧情。

```text
# 客栈_问候
掌柜：客官需要什么？
- 打听消息
  call 客栈_消息
- 离开
  return

# 客栈_消息
掌柜：听说城外来了位侠客。
已听客栈消息 = true
return
```

MOD 装配后的脚本 ID 为 MOD ID 加上相对路径并去掉 `.story`，例如 MOD `inn-addon` 的 `支线/客栈.story` 对应 `inn-addon/支线/客栈`。剧情执行入口使用段名，例如 `客栈_问候`。调用和跳转可以跨文件，也可以引用依赖 MOD 的段。

每个 MOD 内段名必须唯一。addon 可以提供同名段，整体替换先加载 MOD 的该段，其他段继续生效。加载报告记录每次替换的双方 MOD 和源文件。最终运行目录每个段名只对应一个生效段，详细装配规则见 [剧情源码目录](mod-data-patching.md#剧情源码目录)。

## 空白、注释与剧情段

- 段头以顶格 `#` 开始，段名取其后整行并裁剪首尾空白。
- `# 开始`、`#   开始` 和行末带空格的 `# 开始` 具有相同段名。
- 段内顶层语句也顶格，分支体使用 2 个空格缩进一级。空行可用于组织内容。
- 代码行禁止 Tab，缩进必须是 2 个空格的整数倍。嵌套深度上限为 128 层。
- 字符串外的 `//` 开始行注释，字符串中的 `//` 保留。文件可以带 UTF-8 BOM，也支持 LF 和 CRLF 换行。

```text
// 客栈入口
# 客栈_入口
if silver >= 10
  change_silver(-10) // 扣除住宿费
```

段名本身是稳定 ID。更名时需要同步检查地图、世界触发、指令和存档完成记录中的引用。

## 对白与展示

```text
胡斐：飞天狐狸！
胡斐 : 飞天狐狸！
：只有文本
胡斐：
```

对白支持英文和中文冒号，冒号左右的空白会被裁剪，角色名和正文都可以为空。正文及选项文本直接传递给游戏展示层，Godot BBCode 可以写成 `[color=red]文本[/color]`，换行使用 `[br]`。

对白后紧跟同级选项时，该对白成为整组选择的提示。选择样式可以在提示正文开头声明，当前游戏支持 `regular` 和 `bold`。

```text
掌柜：[#style=bold]客官需要什么？
- 打听消息
- 离开
```

标签中的 `style`、`=` 与样式 ID 周围可以有空白。同一条选择提示最多配置一个标签。普通对白或选项自身配置展示标签时会产生编译错误。

## 命令与控制流

命令必须写成完整函数调用，参数中的字符串支持单引号和双引号。单引号通常更便于阅读。

```text
change_silver(100)
change_item('小还丹', 2)
```

控制流使用独立语句，目标段名直接写在关键字之后。

```text
call 公共片段
jump 结局

# 公共片段
旁白：这段内容可以重复调用。
return

# 结局
旁白：故事结束。
```

- `call` 进入目标段，该段自然结束或执行 `return` 后回到调用点的下一条语句。
- `jump` 强制转移到目标段。
- `return` 结束当前调用段，顶层执行时结束当前剧情流程。
- `jump` 和 `return` 后继续出现同一块中的语句会产生不可达代码错误。

`maxlevel('技能名', 等级)` 在编译时向调用模型补充第三个参数，值为 `'当前剧情段名_技能名'`。技能名必须是字符串字面量。显式填写第三个参数可以固定奖励 key，编译器会保留该值。

```text
maxlevel('独孤九剑', 1)
maxlevel('独孤九剑', 1, '客栈支线_武学奖励')
```

## 变量与条件分支

```text
任务进度 = 1
门派声望 += 2
门派声望 -= 1
del 临时标记

if item_count('小刀') >= 1 and silver > 100
  旁白：行囊充足。
elif silver > 10
  旁白：还能买些补给。
else
  旁白：需要先赚些盘缠。
```

赋值和删除只能作为独立语句。变量的可写范围、类型及缺失值处理由游戏运行时决定。复合赋值在编译时归一化为读取原值后再写入，例如 `count += 2` 生成值表达式 `count + (2)`。

表达式可用于条件、赋值值和函数参数，运算符、内建变量及纯函数以 [条件表达式参考](game-dsl-v3.md#条件表达式) 为准。`if / elif / else` 依次判断，最多执行其中一个分支，分支内可以嵌套其他语句。

## 选择分支

`- 文本` 必须依附前面的提示对白。提示和选项处于同一缩进层级，选项体再缩进一级。空选项体合法。

```text
掌柜：客官需要什么？
- 离开
  return
- 打听消息 if not has_var('已听客栈消息')
  call 客栈_消息
if silver >= 10
  - 住宿
    change_silver(-10)
elif silver > 0
  - 询问便宜住处
else
  - 请求赊账

if has_var('secret_shop')
  - 查看隐藏商品
```

普通选项、单项尾缀 `if` 和条件分支块按源码顺序组成整组选择。每个新的 `if` 开启独立条件链，紧随的 `elif / else` 属于同一链。用于选择的条件分支体只能包含缩进的 `- 选项`，每个选项可以继续包含命令、条件、对白或其他选择。

尾缀 `if` 需要与前面的文本用空白分隔。引号或 BBCode 方括号内的 `if` 保持为文本。所有条件过滤后没有可见选项时，运行时会报告剧情执行失败。

## 战斗结果分支

```text
battle 入门试炼
- win
  旁白：试炼通过。
- lose
  旁白：再接再厉。
- timeout
  旁白：试炼结束。
```

战斗 ID 必须对应已装配的战斗定义。结果名只支持 `win`、`lose`、`timeout`，每种结果最多声明一次。结果分支体缩进一级，可以为空，也可以省略全部结果分支。

## 加载、诊断与内部表示

`Game.Content` 的两个目录加载入口共用 [StorySourceReader](../src/Game.Content/Loading/StorySourceReader.cs)。MOD 加载通过 [StoryContentCatalog](../src/Game.Content/Loading/StoryContentCatalog.cs) 装配类型化剧情段，按同名段整体替换。内容装配完成后，统一校验最终生效段的跨文件引用、内容引用和主 MOD 的开局剧情入口。

编译遇到首个错误即中止该次内容装配，错误包含源文件路径、行列，并在语句解析阶段附带剧情段名。修正源码后再次加载即可重新编译。

编译器直接返回 `Game.Core.Story.StoryScript` 和各类 Step。命令中的 `ParsedCall`、变量值与条件中的 `ParsedExpression` 复用 `Game.Expressions` 的解析结果，并保留源码位置上下文。命令的 `Source` 保留作者输入，`maxlevel` 的隐含奖励 key 直接补入调用语法树。普通分支对应 `BranchStep`，选择由有序的 `ChoiceOptionsBlock` 和 `ChoiceBranchBlock` 组成。

`ContentPackage` 承载 JSON 定义。目录与 MOD 加载入口分别读取定义文件和剧情源码，在 `LoadFromPackage` 中将两者装配成最终仓储。剧情运行模型只保存段和步骤。

开发验证使用以下命令。

```powershell
dotnet test tests/Game.Tests/Game.Tests.csproj --filter 'FullyQualifiedName~StoryCompilerTests|FullyQualifiedName~ModSystemTests'
dotnet build src/Game.Godot/engine-free-rpg.csproj
```
