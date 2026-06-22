# genTask4 — 组件独立渲染与编辑器

## 一、需求

完成 ProgressBar / CircularGauge / DigitalDisplay / GridChart 四个组件的独立渲染，并提供单组件编辑与预览窗口。暂不实现 SensorPanel 多组件面板。

## 二、创建的文件

### 数据模型（6 个）

| 文件 | 说明 |
|------|------|
| `Models/Component.cs` | 基类：X, Y, Scale, ZIndex, Foreground/BackgroundColor, FontFamily, FontSize |
| `Models/ProgressBarComponent.cs` | 进度条：Progress/TrackColor, ShowValueText, BorderThickness, BorderColor, Roundness |
| `Models/CircularGaugeComponent.cs` | 环形仪表：SweepAngle, StartAngle, RingThickness, Needle*, ShowCenterValue |
| `Models/DigitalDisplayComponent.cs` | 数字显示：DecimalPlaces, StrokeColor/Thickness, FontWeight, BorderThickness, BorderColor, Roundness |
| `Models/GridChartComponent.cs` | 折线图：Duration, Grid*, LineColor/Width, SmoothFactor, ShowFill, HistoryValues |
| `Models/DashboardConfig.cs` | 仪表盘根配置：Version, ThemeName, CanvasSize, Components |

### 渲染控件（6 个）

| 文件 | 渲染方式 | 说明 |
|------|---------|------|
| `Controls/ProgressBarControl.xaml/.cs` | WPF ProgressBar | 轨道 + 填充 + 叠加文字 |
| `Controls/CircularGaugeControl.xaml/.cs` | OnRender 自绘 (StreamGeometry) | 弧形环 + 指针 + 中心文字 |
| `Controls/DigitalDisplayControl.xaml/.cs` | TextBlock 叠加 | 数值 + 描边层 + 单位 |
| `Controls/GridChartControl.xaml/.cs` | OnRender 自绘 | 网格 + Catmull-Rom 曲线 + 填充 |
| `Controls/SensorTreeSelector.xaml/.cs` | TreeView | 硬件传感器树选择，搜索过滤 |
| `Controls/ColorPalettePopup.xaml/.cs` | Popup | 90 色预设调色板 + HEX 输入 |

### 服务与工具（2 个）

| 文件 | 说明 |
|------|------|
| `Services/ConfigService.cs` | DashboardConfig JSON 读写（System.Text.Json 多态） |
| `Converters/BoolToVisibilityConverter.cs` | WPF bool ↔ Visibility |

### 视图（2 个）

| 文件 | 说明 |
|------|------|
| `Views/ComponentEditorWindow.xaml/.cs` | 单组件编辑预览窗口，左侧 600×600 渲染，右侧属性面板 |
| `Views/ComponentDemoWindow.xaml/.cs` | ~~已删除~~（被 ComponentEditorWindow 取代） |

## 三、修改/删除的文件

| 文件 | 操作 | 原因 |
|------|------|------|
| `MonitorWindow.xaml/.cs` | 删除 | 功能被 HardwareService + 组件体系取代 |
| `MainWindow.xaml/.cs` | 简化 | 移除 MonitorWindow 引用，改为占位窗口 |
| `HardwareService.cs` | 新增方法 | `GetSensorTypeBounds()` 按类型返回合理上下限 |
| `App.xaml` | 修改 StartupUri | 指向 ComponentEditorWindow |
| `DOC/*` | 新增/更新 | 全部模型、控件、服务、视图文档 |

## 四、核心设计决策

### Scale — 图像级缩放

```csharp
ctrl.RenderTransform = new ScaleTransform(comp.Scale, comp.Scale);
ctrl.RenderTransformOrigin = new Point(0, 0);
```

每个组件有固定基础尺寸（如 CircularGauge 200×200），Scale 通过 `RenderTransform` 整体缩放渲染输出，文字、线条、弧线等比例缩放，效果等同于缩放图片。

### 属性精简原则

基类 `Component` 仅保留对所有组件有意义的属性（Scale, X, Y, 颜色, 字体）。边框/圆角仅对矩形组件（ProgressBar, DigitalDisplay）有效，移至各自类。CircularGauge 无边框/圆角。

### 传感器边界

`HardwareService.GetSensorTypeBounds()` 按传感器类型返回上下限：
- Load/Control/Level/Humidity → 固定 0–100
- Temperature → 上界 floor 100°C
- 其他 → 按类型给合理 floor

### 编辑器交互

- 所有数值属性 Slider+TextBox 配对（Tag 关联），Slider 拖动实时预览，TextBox 回车提交
- 颜色输入框 GotFocus 弹出 `ColorPalettePopup`
- 字体使用 ComboBox，每项用自身字体渲染字体名
- `Apply` 按钮一次性提交，Slider 拖动即时预览（调用同一 `ApplyAndRefresh`）
- 传感器选择通过 `SensorTreeSelector` 弹出树，选中即绑定，500ms 实时刷新
- 打开窗口自动 `HardwareService.Start()`，关闭自动 `Stop()`

## 五、构建与测试结果

```
dotnet build SensorPanelToo.slnx   → 0 错误, 0 警告
dotnet test SensorPanelToo.Tests   → 7 通过, 0 失败
```

## 六、下一步 genTask5

修复组件渲染中已发现的 bug：
- 组件窗口关闭时 HardwareService 引用计数异常
- CircularGauge 孤形起始/结束点绘制细节
- GridChart 平滑曲线在某些数值下的渲染偏差
- ColorPalettePopup 焦点管理优化
- 验证所有组件边界情况下 (Scale 极值, SensorValue null) 的行为
