# genTask4 - 组件独立渲染实现

## 一、目标

先完成 ProgressBar / CircularGauge / DigitalDisplay / GridChart 四个组件的**独立渲染能力**，每个组件能接收数据模型并自绘。**暂不实现 SensorPanel 仪表盘窗口**（多组件组合排列、Canvas 布局延后）。

## 二、当前状态

| 已完成 | 文件 |
|--------|------|
| 数据模型 | `Models/SensorValue.cs` |
| 硬件服务 | `Services/HardwareService.cs` |
| 测试项目 | `SensorPanelToo.Tests/` (7/7 通过) |
| 监控窗口 | `MonitorWindow.xaml/.cs` (独立功能，保留不动) |
| 控制窗口 | `MainWindow.xaml/.cs` (后续改造成编辑器，本轮暂不动) |

| 待建 | 说明 |
|------|------|
| 组件数据模型 4+1 个 | Component 基类 + ProgressBar/CircularGauge/DigitalDisplay/GridChart 派生类 |
| 渲染控件 4 个 | 每个组件对应一个 WPF UserControl |
| ConfigService | JSON 读写（依赖 DashboardConfig，本轮一并实现） |
| DashboardConfig | 配置文件根模型 |
| Demo 窗口 | 用于开发阶段验证四个组件渲染效果，不加入最终产品 |

## 三、文件清单

```
SensorPanelToo/
├── Models/
│   ├── Component.cs                     # 组件基类（公共属性）
│   ├── ProgressBarComponent.cs          # 进度条模型
│   ├── CircularGaugeComponent.cs        # 环形仪表模型
│   ├── DigitalDisplayComponent.cs       # 数字显示模型
│   ├── GridChartComponent.cs            # 折线图模型
│   └── DashboardConfig.cs               # 仪表盘配置根模型
│
├── Controls/
│   ├── ProgressBarControl.xaml/.cs      # 进度条渲染
│   ├── CircularGaugeControl.xaml/.cs    # 环形仪表渲染（自绘 Canvas）
│   ├── DigitalDisplayControl.xaml/.cs   # 数字显示渲染
│   └── GridChartControl.xaml/.cs        # 折线图渲染（自绘 Canvas）
│
├── Services/
│   └── ConfigService.cs                 # JSON 配置读写
│
├── Converters/
│   └── BoolToVisibilityConverter.cs     # WPF Bool ↔ Visibility
│
└── Views/
    └── ComponentDemoWindow.xaml/.cs     # 临时 Demo 窗口（独立渲染验证）
```

## 四、实施步骤

### Step 4-1: 基础设施

| # | 文件 | 说明 |
|---|------|------|
| 4-1a | `Converters/BoolToVisibilityConverter.cs` | 基础转换器，被多个 XAML 引用 |
| 4-1b | `Converters/BoolToVisibilityConverter.md` | 对应文档 |

### Step 4-2: 组件数据模型

| # | 文件 | 说明 |
|---|------|------|
| 4-2a | `Models/Component.cs` + `.md` | 基类：Id, ComponentType, BindingId, Position, Size, ZIndex, 颜色/字体/圆角等公共属性 |
| 4-2b | `Models/ProgressBarComponent.cs` + `.md` | 继承 Component，扩展 ProgressColor, TrackColor, Orientation, ShowValueText, ValueTextPosition |
| 4-2c | `Models/CircularGaugeComponent.cs` + `.md` | 继承 Component，扩展 SweepAngle, StartAngle, GaugeStyle, Needle*, RingThickness, ShowCenterValue |
| 4-2d | `Models/DigitalDisplayComponent.cs` + `.md` | 继承 Component，扩展 ShowPrefix, ShowSuffix, DecimalPlaces, StrokeColor, StrokeThickness, FontWeight |
| 4-2e | `Models/GridChartComponent.cs` + `.md` | 继承 Component，扩展 DurationSeconds, GridDensity*, 线条/填充相关, HistoryValues 运行时队列 |
| 4-2f | `Models/DashboardConfig.cs` + `.md` | Version, ThemeName, CanvasWidth, CanvasHeight, BackgroundColor, List<Component> |

### Step 4-3: ConfigService

| # | 文件 | 说明 |
|---|------|------|
| 4-3a | `Services/ConfigService.cs` + `.md` | Save/Load/ListThemes，System.Text.Json 多态序列化（JsonDerivedType） |

### Step 4-4: 四个渲染控件

每个控件：
- 继承 `UserControl`
- 暴露两个依赖属性：`ComponentData`（对应组件模型）和 `SensorValue`（传感器快照）
- 在 DataContext/SensorValue 变化时重绘
- 无交互手柄（拖拽/缩放/选中是编辑器的职责，不在本轮范围）

| # | 文件 | 说明 | 渲染方式 |
|---|------|------|----------|
| 4-4a | `Controls/ProgressBarControl.xaml/.cs` + `.md` | 进度条 | XAML 模板：ProgressBar + TextBlock |
| 4-4b | `Controls/CircularGaugeControl.xaml/.cs` + `.md` | 环形仪表 | Canvas 自绘：弧形 + 指针 + 中心文字 |
| 4-4c | `Controls/DigitalDisplayControl.xaml/.cs` + `.md` | 数字显示 | XAML 模板：TextBlock，描边用 Border/TextBlock 叠加 |
| 4-4d | `Controls/GridChartControl.xaml/.cs` + `.md` | 折线图 | Canvas 自绘：网格线 + 折线/贝塞尔曲线 + 填充区域 |

### Step 4-5: Demo 窗口（验证用）

| # | 文件 | 说明 |
|---|------|------|
| 4-5a | `Views/ComponentDemoWindow.xaml/.cs` | 展示四个控件 + 滑块模拟传感器值变化，用于开发期目视验证。不加入最终产品，后续可删除 |

## 五、关键设计决策

### 5.1 控件数据输入

控件通过两个 `DependencyProperty` 接收数据，不通过 DataContext：

```csharp
// 每个控件都具备：
public static readonly DependencyProperty ComponentDataProperty = ...;
public static readonly DependencyProperty SensorValueProperty = ...;
```

| DP | 类型 | 用途 |
|----|------|------|
| `ComponentData` | `ProgressBarComponent` 等具体类型 | 样式、尺寸、颜色等静态配置 |
| `SensorValue` | `SensorValue?` | 实时数据值，null 时显示占位符 "N/A" |

这样设计的好处：
- 控件可直接在 XAML 中使用 `<local:ProgressBarControl ComponentData="{Binding ...}" SensorValue="{Binding ...}" />`
- 不依赖中间 ViewModel，降低耦合
- Demo 窗口和后续 SensorPanel 都以同样方式使用

### 5.2 自绘控件（CircularGauge, GridChart）

- 使用 Canvas + `DrawingVisual` 或直接在 `OnRender` 中绘制
- 不需要子控件树，性能更好
- 所有坐标系相对于控件自身 Width/Height

### 5.3 SensorValue 为 null 的处理

每个控件在 SensorValue 为 null 时显示灰色"N/A"或空进度条，不崩溃。

### 5.4 不实现的内容（本轮）

- ~~SensorPanel 窗口~~（多组件 Canvas 布局）
- ~~编辑器交互~~（拖拽、缩放、选中高亮）
- ~~ComponentThumb 装饰器~~
- ~~BindingSelector 绑定选择器~~
- ~~PropertyPanel 属性面板~~
- ~~MainWindow 重构~~
- ~~主题配置文件 .json~~

## 六、验证方式

1. 构建通过：`dotnet build SensorPanelToo.slnx`
2. Demo 窗口手动测试：启动后四个组件渲染，拖动滑块改变模拟值，所有组件同步更新
3. 测试用例：编写至少 4 个测试验证 ConfigService 序列化/反序列化、组件模型属性默认值

## 七、与 genTask5 的衔接

genTask5 将基于本轮成果实现：
- SensorPanel 窗口（Canvas 承载多个控件，按 Position 布局）
- SensorPanelViewModel（定时从 HardwareService 取数据 → 投递到各控件）
- ComponentDemoWindow 可退出历史舞台
