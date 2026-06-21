# AI Agent Instructions

When working on this project, follow these rules to keep complexity manageable.

All documentation lives under the `DOC/` folder, mirroring the source file structure:
most importantly, please read `DOC/project.md` to konw the overall project structure and current progress before making any change.
```
SensorPanelToo/
├── agentPleaseRead.md        ← this file
├── DOC/
│   ├── task3_SensorPanelToo.md   ← overall project summary
│   ├── GenTask/                  ← task plans
│   ├── Models/
│   │   └── SensorValue.md
│   └── Services/
│       └── HardwareService.md
├── Models/
│   └── SensorValue.cs
├── Services/
│   └── HardwareService.cs
└── ...
```

## Core Rule: One .md per class

Every time you create or significantly modify a `.cs` file, maintain a corresponding `.md` file under `DOC/` at the same relative path. The `.md` file is the **single source of truth** for understanding what that class does — for both the human developer and future AI sessions.

### Naming

```
Models/SensorValue.cs    →  DOC/Models/SensorValue.md
Services/ConfigService.cs → DOC/Services/ConfigService.md
Views/MonitorWindow.xaml.cs → DOC/Views/MonitorWindow.md
```

### Required sections in each .md

```markdown
# ClassName

## 作用
一句话说清楚这个类是干什么的。

## 职责边界
- 它负责什么
- 它不负责什么

## 依赖
- 引用了哪些本项目内的类
- 依赖了哪些外部 NuGet 包

## 被谁使用
- 哪些类会调用它

## 公开 API
列出 public 方法、属性、事件，每个一行，附简短说明。
如果是数据模型(POCO)，列出所有属性及其含义。

## 关键设计决策
记录为什么这样设计，尤其是反直觉的决策。例如：
- "使用 sensor.Identifier.ToString() 而非自定义拼接，避免硬件名含 - 解析出错"
- "Stop() 用 Interlocked，防止引用计数变负数"

## 示例
简短的使用示例代码片段。
```

### When to update

| 场景 | 动作 |
|------|------|
| 新建 .cs 文件 | 同时创建 .md |
| 修改 .cs 的 public API | 更新 .md |
| 修改 .cs 的内部逻辑但 API 不变 | 不更新 |
| 修改 .cs 的依赖关系 | 更新 .md 的"依赖"和"被谁使用" |
| 删除 .cs 文件 | 不删除对应 .md,而是改为xxx_removed.md |

### For the human developer

If you're reading this and feel lost, start with:
1. `DOC/task3_SensorPanelToo.md` — overall project summary
2. `DOC/*/xxx.md` — per-class documentation
3. Only then read `*.cs` source files

You don't need to memorize everything. You just need to know which class does what and where to find it.

### For AI agents

Before making any change, read the relevant `.md` files first — they are much faster to parse than full source code. Do NOT read every `.cs` file on every request.
