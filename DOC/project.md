# SensorPanelToo 项目概览

## 技术栈

| 项 | 值 |
|----|----|
| 框架 | .NET 8 + WPF |
| 硬件监控 | LibreHardwareMonitorLib 0.9.6 |
| 测试 | xUnit 2.5.3 |
| UI 模式 | Code-behind（MVVM 计划中） |

## 当前进度

已完成的模块：

| 已完成 | 文件 |
|--------|------|
| 数据模型 | `Models/SensorValue.cs`, `Models/Component.cs` + 5 派生类, `Models/DashboardConfig.cs` |
| 硬件服务 | `Services/HardwareService.cs` |
| 配置服务 | `Services/ConfigService.cs` |
| 测试项目 | `SensorPanelToo.Tests/` |
| 组件渲染控件 | `Controls/ProgressBarControl`, `CircularGaugeControl`, `DigitalDisplayControl`, `GridChartControl`, `SensorLabelControl`, `ComponentDebugPanel`, `ThemeSelector`, `SensorTreeSelector`, `ColorPalettePopup` |
| 值转换器 | `Converters/BoolToVisibilityConverter.cs` |
| 主题编辑器 | `Views/ThemeEditorWindow.xaml/.cs` |
| 单组件调试 | `Views/ComponentEditorWindow.xaml/.cs` |
| 硬件选择 | `Views/HardwareSelectDialog.xaml/.cs` |
| 主控制台 | `MainWindow.xaml/.cs`（双标签页：主题/调试） |
| 渲染窗口 | `Views/RenderWindow.xaml/.cs` |
| 主题选择组件 | `Controls/ThemeSelector.xaml/.cs` |

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

### 6. 主控制台 + 渲染分离

应用启动入口改为 `MainWindow`（控制台），从控制台呼出：
- **主题编辑器**（`ThemeEditorWindow`）：编辑主题、添加/排列/配置组件
- **渲染窗口**（`RenderWindow`）：纯渲染，全屏/窗口化，支持多显示器定位
- **组件调试**（`ComponentEditorWindow`）：单组件属性调试

### 7. 多显示器全屏

渲染窗口全屏时使用 Win32 `SetWindowPos` 定位到目标显示器，避免 WPF `Left`/`Top` 设置时机导致的非主屏定位失败。

## 项目结构

```
SensorPanelToo/
├── agentPleaseRead.md            # AI 协作约定
├── DOC/                          # 所有文档
│   ├── project.md                # ← 本文件
│   ├── task3_SensorPanelToo.md   # Task 3 实现总结
│   ├── GenTask/                  # 原始任务计划
│   ├── Models/                   # 模型文档
│   ├── Services/                 # 服务文档
│   ├── Controls/                 # 控件文档
│   ├── Converters/               # 转换器文档
│   └── Views/                    # 视图文档
├── Models/
│   ├── SensorValue.cs
│   ├── Component.cs              # 组件基类
│   ├── ProgressBarComponent.cs
│   ├── CircularGaugeComponent.cs
│   ├── DigitalDisplayComponent.cs
│   ├── GridChartComponent.cs
│   ├── SensorLabelComponent.cs
│   └── DashboardConfig.cs
├── Services/
│   ├── HardwareService.cs
│   └── ConfigService.cs
├── Controls/
│   ├── ProgressBarControl.xaml/.cs
│   ├── CircularGaugeControl.xaml/.cs
│   ├── DigitalDisplayControl.xaml/.cs
│   ├── GridChartControl.xaml/.cs
│   ├── SensorLabelControl.xaml/.cs
│   ├── ComponentDebugPanel.xaml/.cs
│   ├── ThemeSelector.xaml/.cs
│   ├── SensorTreeSelector.xaml/.cs
│   └── ColorPalettePopup.xaml/.cs
├── Converters/
│   └── BoolToVisibilityConverter.cs
├── Views/
│   ├── ThemeEditorWindow.xaml/.cs     # 主题编辑器
│   ├── ComponentEditorWindow.xaml/.cs # 单组件调试
│   ├── RenderWindow.xaml/.cs          # 纯渲染窗口
│   └── HardwareSelectDialog.xaml/.cs  # 硬件选择
├── SensorPanelToo.Tests/
│   └── HardwareServiceTests.cs
├── MainWindow.xaml/.cs            # 主控制台（应用入口）
└── App.xaml/.cs
```

## 文档约定

参见 `agentPleaseRead.md`：
- 每个 `.cs` 类对应一个 `DOC/` 下的 `.md` 文档
- `.md` 是理解类的首选入口，源码仅用于确认细节
