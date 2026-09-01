# XMJH 一键转换流水线

## 日常用法

在 `engine-free-rpg` 目录打开 PowerShell，执行：

```powershell
.\scripts\convert-xmjh.ps1
```

命令成功结束后，`mods/xmjh/data` 已经是 Godot 实际读取的新数据，可直接回到 Godot 运行项目。无需再手工复制 JSON，也不要直接运行某一个转换器来更新正式运行目录。

如果只想查看本次会处理哪些文件，不生成和发布内容：

```powershell
.\scripts\convert-xmjh.ps1 -DryRun
```

如果 XMJH 不在项目同级的默认目录，可明确指定：

```powershell
.\scripts\convert-xmjh.ps1 -Source 'D:\你的目录\XMJH'
```

## 流水线做了什么

1. 枚举 `XMJH/Scripts/*.xml`。每个文件必须登记为定义转换、剧情转换或明确的暂不适配项；以后出现新 XML 会直接报错，不会被静默漏掉。
2. 转换 19 组正式定义。`maps2.xml` 是临时文件，不参与地图构建。
3. 转换、编译并隔离 7 个剧情 XML，并从 `XMJH/lua/rollrole.lua` 生成开局问答。
4. 在临时目录组合完整运行数据，并使用 Godot 同源的 `Game.Content` 加载器检查所有定义和跨文件引用。
5. 转换 `XMJH/Animations/animations.xml` 为 Godot `AnimationLibrary`，缺帧会阻止发布。
6. 只有以上步骤全部通过，才同步到 `mods/xmjh/data` 和生成的动画资源目录。失败时不会用本次无效结果覆盖正式运行数据。

## 失败时怎么看

- `Unregistered XMJH Scripts XML`：新增了 XML，但尚未决定如何转换。应给它增加转换器或明确登记适配策略。
- `Game.Content validation failed`：XML 已能转换，但转换后的跨表引用不完整。错误会给出剧情、角色或地图的具体 ID，应修正源 XML 后重新运行。
- `missing frames`：动画 XML 引用了不存在的 PNG；补齐或修正源路径后重新运行。

转换报告位于 `jyx-legacy-data/reports/xmjh/conversion-report.json` 和同目录的 Markdown 文件。动画报告位于 `mods/xmjh/staging/animation-conversion.json`。
