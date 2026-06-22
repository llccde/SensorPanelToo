# SensorPanelToo 项目概览

## 技术栈

| 项 | 值 |
|----|----|
| 框架 | .NET 8 + WPF |
| 硬件监控 | LibreHardwareMonitorLib 0.9.6 |
| 测试 | xUnit 2.5.3 |
| UI 模式 | MVVM（计划中） |

## 当前进度

已完成 Task 3–4：

| 已完成 | 文件 |
|--------|------|
| 数据模型 | `Models/SensorValue.cs` |
| 硬件服务 | `Services/HardwareService.cs` |
| 测试项目 | `SensorPanelToo.Tests/`（7/7 通过） |
| 组件数据模型 | `Models/Component.cs` + 4 派生类 |
| 组件渲染控件 | `Controls/ProgressBarControl` 等 4 个 + `SensorTreeSelector` + `ColorPalettePopup` |
| 配置服务 | `Services/ConfigService.cs` |
| 编辑器窗口 | `Views/ComponentEditorWindow.xaml/.cs` |
| 值转换器 | `Converters/BoolToVisibilityConverter.cs` |

已移除：`MonitorWindow`, `ComponentDemoWindow`。

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

### 3. Scale 缩放系统（deviation from genTask3）

genTask3 原计划组件有独立的 Width/Height，实际改为：
- 基类仅 `Scale`（默认 1.0）
- 每种组件有固定基础尺寸（如 CircularGauge 200×200）
- 通过 `RenderTransform = ScaleTransform(Scale, Scale)` 实现图像级缩放
- 边框/圆角仅对矩形组件有意义，移至 ProgressBar/DigitalDisplay 自身

### 4. 属性精简

基类 `Component` 仅保留所有组件共有的属性（位置、缩放、颜色、字体）。组件特有属性（如边框厚度、弧形角度、网格密度）定义在各自派生类中。

### 5. 传感器边界

`HardwareService.GetSensorTypeBounds()` 按传感器类型返回上下限，不直接使用 LHM 观测极值。

## 项目结构

```
SensorPanelToo/
├── agentPleaseRead.md         # AI 协作约定
├── DOC/                           # 所有文档
│   ├── project.md                 # ← 本文件
│   ├── task3_SensorPanelToo.md    # Task 3 实现总结
│   ├── GenTask/                   # 原始任务计划
│   ├── Models/                    # 模型文档
│   ├── Services/                  # 服务文档
│   ├── Controls/                  # 控件文档
│   ├── Converters/                # 转换器文档
│   └── Views/                     # 视图文档
├── Models/
│   ├── SensorValue.cs
│   ├── Component.cs               # 组件基类
│   ├── ProgressBarComponent.cs
│   ├── CircularGaugeComponent.cs
│   ├── DigitalDisplayComponent.cs
│   ├── GridChartComponent.cs
│   └── DashboardConfig.cs
├── Services/
│   ├── HardwareService.cs
│   └── ConfigService.cs
├── Controls/
│   ├── ProgressBarControl.xaml/.cs
│   ├── CircularGaugeControl.xaml/.cs
│   ├── DigitalDisplayControl.xaml/.cs
│   ├── GridChartControl.xaml/.cs
│   ├── SensorTreeSelector.xaml/.cs
│   └── ColorPalettePopup.xaml/.cs
├── Converters/
│   └── BoolToVisibilityConverter.cs
├── Views/
│   └── ComponentEditorWindow.xaml/.cs
├── SensorPanelToo.Tests/
│   └── HardwareServiceTests.cs
├── MainWindow.xaml/.cs             # 占位（后续改为编辑器入口）
└── App.xaml/.cs
```

## 文档约定

参见 `DOC/agentPleaseRead.md`：
- 每个 `.cs` 类对应一个 `DOC/` 下的 `.md` 文档
- `.md` 是理解类的首选入口，源码仅用于确认细节
