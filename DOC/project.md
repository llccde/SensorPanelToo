# SensorPanelToo 项目概览

## 技术栈

| 项 | 值 |
|----|----|
| 框架 | .NET 8 + WPF |
| 硬件监控 | LibreHardwareMonitorLib 0.9.6 |
| 测试 | xUnit 2.5.3 |
| UI 模式 | MVVM（计划中） |

## 当前进度

已完成 Task 3 的前半部分：

| 已完成 | 文件 |
|--------|------|
| 数据模型 | `Models/SensorValue.cs` |
| 硬件服务 | `Services/HardwareService.cs` |
| 测试项目 | `SensorPanelToo.Tests/`（7 个用例，全部通过） |

原有窗口（`MainWindow`, `MonitorWindow`）仍保留，未被修改。

## 与 genTask3 计划的分歧

### 1. BindingId 标识符系统

| | 原计划 | 实际实现 |
|----|--------|----------|
| 格式 | `HardwareType-HardwareName-SensorName` | `sensor.Identifier.ToString()` |
| 示例 | `Cpu-Core#1-Load` | `/intelcpu/0/load/1` |
| 分隔符 | `-` | `/` |

**原因**：

- 硬件名本身可能含 `-`（如 `Intel Core i7-13700K`），用 `-` 做分隔符会导致解析歧义
- `sensor.Identifier` 是 LHM 内建的层级路径，天然唯一，无需自行拼装
- 解析更可靠：`/硬件类型/实例/传感器类型/索引` 结构固定，不依赖硬件名字符集

### 2. 其他设计决策（已实现）

| 决策 | 说明 |
|------|------|
| 单例 + 引用计数 | 多 `SensorPanel` 共享同一 `HardwareService`，最后关闭才释放 |
| 后台线程轮询 | 服务层不依赖 DispatcherTimer，事件在后台线程触发 |
| 类型名冲突 | `SensorValue` 与 LHM 内置类型重名，通过 using alias 消歧义 |

### 3. 未来需要注意

- 配置文件 JSON 中的 `bindingId` 字段格式需与 LHM Identifier 一致（`/xxx/x/x`），不能用原计划的 `Cpu-xx-xx` 格式
- `ConfigService`, `BindingValidationService` 等后续类需知晓此差异
- `getSensor` 绑定选择器 UI 展示给用户的名称仍需取自硬件树（`SensorTreeNode.Name`），与 BindingId 解耦

## 项目结构

```
SensorPanelToo/
├── agentPleaseRead.md         # AI 协作约定
├── DOC/                           # 所有文档
│   ├── project.md                 # ← 本文件
│   ├── task3_SensorPanelToo.md    # Task 3 实现总结
│   ├── GenTask/                   # 原始任务计划
│   ├── Models/                    # 模型文档
│   └── Services/                  # 服务文档
├── Models/
│   └── SensorValue.cs
├── Services/
│   └── HardwareService.cs
├── SensorPanelToo.Tests/
│   └── HardwareServiceTests.cs
├── Views/                         # (待实现)
├── ViewModels/                    # (待实现)
├── Controls/                      # (待实现)
└── Themes/                        # (待实现)
```

## 文档约定

参见 `DOC/agentPleaseRead.md`：
- 每个 `.cs` 类对应一个 `DOC/` 下的 `.md` 文档
- `.md` 是理解类的首选入口，源码仅用于确认细节
