# task3_plan - Custom Dashboard Editor Implementation Plan

## 一、总体架构

```
SensorPanelToo/
├── Models/                          # 数据模型层
│   ├── DataValue.cs                 # 数据值定义
│   ├── DataValueType.cs             # 枚举：Continuous/Discrete/Enum
│   ├── EnumValueMapping.cs          # 枚举值映射
│   └── SensorBindingRegistry.cs     # 绑定注册表，管理所有传感器数据源
├── Components/                      # 组件模型层 (全部存本地配置用)
│   ├── ComponentBase.cs             # 组件基类（位置、大小、图层、颜色、边框等）
│   ├── ProgressBarComponent.cs      # 进度条
│   ├── DigitalComponent.cs          # 数字显示
│   ├── CircularGaugeComponent.cs    # 环形/扇形仪表
│   ├── LineChartComponent.cs        # 网格折线图
│   ├── ComponentFactory.cs          # 组件工厂，根据类型创建
│   ├── BorderStyle.cs               # 枚举：Solid/Dashed
│   ├── PointerStyle.cs              # 枚举：Retro/Plastic/Metal/Scanline/Finger
│   └── GaugeLineStyle.cs            # 枚举：Solid/Dashed
├── Rendering/                       # WPF 渲染层 (自定义绘制)
│   ├── ComponentViewFactory.cs      # 根据组件模型创建对应的 WPF UserControl/FrameworkElement
│   ├── ProgressBarView.xaml/.cs     # 进度条 WPF 渲染控件
│   ├── DigitalView.xaml/.cs         # 数字显示 WPF 渲染控件
│   ├── CircularGaugeView.xaml/.cs   # 环形仪表 WPF 渲染控件
│   └── LineChartView.xaml/.cs       # 折线图 WPF 渲染控件
├── Services/                        # 服务层
│   ├── HardwareService.cs           # 封装 LibreHardwareMonitor，提供统一传感器查询接口
│   ├── ConfigService.cs             # JSON 序列化/反序列化，保存/加载仪表盘配置
│   ├── SensorMetadataProvider.cs    # 构建多级传感器树，供模态选择器使用
│   └── BindingValidator.cs          # 校验属性ID是否适合组件类型，不合规则返回警告信息
├── Converters/                      # XAML 数据绑定转换器
│   ├── EnumToDisplayConverter.cs
│   └── FloatToProgressConverter.cs
├── Controls/                        # 自定义 WPF 控件
│   ├── ResizableCanvas.cs           # 支持拖拽/缩放的编辑画布
│   ├── DesignAdorner.cs             # 选中组件的缩放手柄/拖拽装饰器
│   ├── SensorPickerDialog.xaml/.cs  # 多级属性选择模态框
│   └── PropertyEditor.xaml/.cs      # 属性编辑面板 (右侧或浮动)
├── MainWindow.xaml/.cs              # 主窗口：编辑模式/预览模式切换，主题选择，工具栏
├── DashboardRenderer.cs             # 将 ComponentBase 列表渲染到 Canvas 上
├── DashboardDocument.cs             # 当前编辑的仪表盘文档（组件列表+全局设置）
└── App.xaml/.cs                     # 启动，主题资源加载
```

---

## 二、数据流

```
LibreHardwareMonitor              SensorBindingRegistry
    ↓                                    ↓
HardwareService    ──定时轮询──→   更新所有已注册 DataValue.CurrentValue
    ↓                                    ↓
ComponentBase.BindingId  ──查找──→   DataValue.GetValue() / GetText()
    ↓
ComponentView 订阅 PropertyChanged →  WPF 渲染层更新显示
```

- **只需要注册被绑定的传感器**，没被任何组件引用的传感器不轮询，节省开销。
- 轮询间隔由全局配置控制（默认 500ms）。

---

## 三、核心类设计

### 3.1 DataValue（数据值）

```csharp
public class DataValue : INotifyPropertyChanged
{
    public string Id { get; }                    // "cpu-core1-occupancyRate"
    public float UpperLimit { get; set; }        // 上限
    public float LowerLimit { get; set; }        // 下限
    public DataValueType Type { get; set; }      // Continuous | Discrete | Enum

    // 离散/枚举相关
    public List<float>? DiscreteValues { get; set; }      // 离散可能值
    public Dictionary<float, string>? EnumMap { get; set; } // 数值→枚举文本

    public string DefaultUnitSuffix { get; set; }   // "°C", "MB/s", "RPM", "%", "GB"
    public string DefaultSymbolPrefix { get; set; } // 保留字段

    public float CurrentValue { get; set; }          // 硬件服务更新

    public Func<float>? ValueProvider { get; set; }  // 硬件服务注入的取值函数
    public Func<string>? TextProvider { get; set; }  // 硬件服务注入的取文字函数

    public void Refresh()
    {
        if (ValueProvider != null)
            CurrentValue = ValueProvider();
    }

    public string GetDisplayText()
    {
        // 枚举类型：查 EnumMap 返回文本
        // 离散类型：取 int 然后转字符串
        // 连续类型：返回 TextProvider() 或 CurrentValue.ToString()
    }
}
```

### 3.2 ComponentBase（组件基类）

```csharp
public class ComponentBase : INotifyPropertyChanged
{
    public string Id { get; set; }                  // GUID，唯一标识
    public string BindingId { get; set; }           // 绑定的 DataValue.Id
    public double X { get; set; }                   // Canvas.Left
    public double Y { get; set; }                   // Canvas.Top
    public double ScaleX { get; set; }              // 宽度
    public double ScaleY { get; set; }              // 高度
    public int ZIndex { get; set; }                 // 图层
    public Color ForegroundColor { get; set; }      // 主要颜色
    public double BorderThickness { get; set; }
    public Color BorderColor { get; set; }
    public BorderStyle BorderStyle { get; set; }    // Solid | Dashed
    public bool TransparentBackground { get; set; }
    public Color BackgroundColor { get; set; }
    public double Roundness { get; set; }           // 0.0 ~ 1.0，各组件自有解读
}
```

### 3.3 派生组件

**ProgressBarComponent**
```csharp
public class ProgressBarComponent : ComponentBase
{
    // 继承全部基类属性，进度条不需要额外属性
    // "圆润" → 进度条两端圆角半径
}
```

**DigitalComponent**
```csharp
public class DigitalComponent : ComponentBase
{
    public bool ShowPrefixSuffix { get; set; }      // 是否显示前后缀
    public string FontFamily { get; set; }
    public double FontSize { get; set; }
    public int DecimalPlaces { get; set; }           // 小数点保留位数
    public Color StrokeColor { get; set; }           // 描边颜色
    public double StrokeThickness { get; set; }      // 描边厚度 (响应 Roundness)
    // "圆润" → 描边厚度比例、字体加粗程度
}
```

**CircularGaugeComponent**
```csharp
public class CircularGaugeComponent : ComponentBase
{
    public GaugeLineStyle LineStyle { get; set; }   // Solid | Dashed
    public double StartAngle { get; set; }
    public double SweepAngle { get; set; }           // 扇形角度 0-360
    public bool ShowPointer { get; set; }
    public Color PointerColor { get; set; }
    public double PointerWidth { get; set; }
    public PointerStyle PointerStyle { get; set; }   // Retro|Plastic|Metal|Scanline|Finger
    // "圆润" → 仪表弧线端点样式
}
```

**LineChartComponent**
```csharp
public class LineChartComponent : ComponentBase
{
    public double Duration { get; set; }             // X轴宽度(时间)
    public int GridXDensity { get; set; }
    public int GridYDensity { get; set; }
    public double GridLineWidth { get; set; }
    public Color GridLineColor { get; set; }
    public double LineWidth { get; set; }
    // "圆润" → 折线拐角圆滑程度（贝塞尔过渡 vs 直线连接）
}
```

---

## 四、关键服务

### 4.1 HardwareService
- 初始化 `LibreHardwareMonitor.Hardware.Computer`
- 提供 `RegisterBinding(string id, Func<float> provider)` 注册方法
- 定时轮询 Computer.Accept(UpdateVisitor)，触发所有已注册 dataValue.Refresh()
- 传感器 ID 命名规则：`{hardwareName}-{sensorName}-{sensorType}`
  - 例子: `CPU Core #1-Load-Load`, `Generic Hard Disk-Used Space-Load`
- 构建多级传感器树（Hardware → SensorGroup → Sensor）供 UI 选择器使用

### 4.2 ConfigService
- 序列化整个仪表盘为 JSON 文件，保存到 `%AppData%/SensorPanelToo/dashboards/`
- JSON 根结构：
  ```json
  {
    "name": "My Dashboard",
    "width": 1920,
    "height": 1080,
    "backgroundColor": "#00000000",
    "refreshIntervalMs": 500,
    "components": [ ... ]
  }
  ```
- 支持保存、加载、删除配置文件
- 主题文件也是同样的格式，保存到 `themes/` 子目录

### 4.3 SensorMetadataProvider
- 扫描 HardwareService 中所有可用传感器
- 构建三层树形结构：
  - 第一层：硬件类型 (CPU / GPU / RAM / Storage / Network / Motherboard)
  - 第二层：具体硬件实例 (CPU Core #1, Generic Hard Disk, ...)
  - 第三层：传感器+类型 (Temperature, Load, Clock, ...)
- 每个叶子节点存储完整属性ID字符串

### 4.4 BindingValidator
- 校验规则：
  - 进度条 → 适合绑定 Load、占用率等 0-100% 类型的传感器
  - 环形仪表 → 适合 Temperature、Clock、Fan 等有明确上下限的值
  - 数字显示 → 所有类型都适合
  - 折线图 → 不适合 Enum 类型，适合连续变化的值
- 返回 `{ IsValid: bool, Warning: string }`

---

## 五、WPF 渲染实现

### 5.1 编辑模式 vs 预览模式
- **编辑模式**：Canvas 上渲染组件，选中显示 Adorner（拖拽手柄 + 缩放手柄），右侧 PropertyEditor 面板可编辑属性
- **预览模式**：隐藏所有编辑 UI，纯粹渲染仪表盘，透明背景全屏覆盖

### 5.2 拖拽 & 缩放
- 使用 `Thumb` 控件实现拖拽移动（改 Canvas.Left/Top）
- 使用 `ResizeAdorner` 实现四角/四边缩放（改 ScaleX/ScaleY）
- 所有操作直接修改 ComponentBase 模型属性 → PropertyChanged → 自动同步配置文件

### 5.3 各组件渲染方式

| 组件 | 渲染方式 |
|------|---------|
| 进度条 | Rectangle + 绑定宽度百分比，LinearGradientBrush |
| 数字显示 | FormattedText 或 TextBlock，带描边用多个偏移 TextBlock 叠加 |
| 环形仪表 | DrawingVisual + Arc 绘制，旋转用 RotateTransform，指针用 Line/Polygon |
| 折线图 | PolyLine + Canvas，维护历史数据环形缓冲区，定时推进 |

### 5.4 主题系统
- 主题 = 仪表盘配置文件的子集，只包含全局设置（背景、默认颜色、默认字体等）
- 加载主题 → 覆盖当前仪表盘的全局属性
- 主题不覆盖组件位置/大小/绑定，只覆盖颜色/风格

---

## 六、UI 布局 (MainWindow)

```
┌─────────────────────────────────────────────────────┐
│ [编辑模式] [预览模式] | 主题: [下拉框] | [保存] [另存为] │  ← 工具栏
├────────────┬────────────────────────────────────────┤
│ 组件面板    │                                        │
│            │                                        │
│ [进度条]   │         Canvas 编辑区                   │
│ [数字]     │     (ResizableCanvas)                  │
│ [环形表]   │                                        │
│ [折线图]   │    组件拖入后显示在此                    │
│            │                                        │
│────────────│                                        │
│ 属性面板    │                                        │
│            │                                        │
│ 选中组件的  │                                        │
│ 属性编辑    │                                        │
│ 绑定ID: []│                                        │
│ 位置: X,Y │                                        │
│ 大小: W,H │                                        │
│ 颜色: [■] │                                        │
│ ...       │                                        │
│            │                                        │
├────────────┴────────────────────────────────────────┤
│ 状态栏: 当前仪表盘名 | 组件数 | 传感器状态            │
└─────────────────────────────────────────────────────┘
```

- 左侧栏可折叠
- 组件面板：拖拽按钮到 Canvas 创建新组件
- 属性面板：选中组件时显示，未选中时隐藏或显示画布属性
- Canvas 支持滚轮缩放、平移

---

## 七、绑定属性选择器 (SensorPickerDialog)

模态框结构：
```
┌── 选择传感器 ──────────────────────────┐
│ 🔍 搜索: [________________]           │
│                                        │
│ TreeView:                              │
│  ├─ CPU                                │
│  │  ├─ CPU Core #1                     │
│  │  │  ├─ Temperature   [✓]            │
│  │  │  ├─ Load                       │
│  │  │  └─ Clock                       │
│  │  └─ CPU Core #2                     │
│  ├─ GPU                                │
│  │  └─ NVIDIA GeForce RTX 3060        │
│  ├─ RAM                                │
│  ├─ Storage                            │
│  └─ Motherboard                        │
│                                        │
│ 绑定类型: Load [连续值 0-100]          │
│ 适用组件: ✓进度条 ✓数字 ✓环形表 ✓折线图  │
│                                        │
│              [确认] [取消]              │
└────────────────────────────────────────┘
```

- 选中某传感器后底部显示其类型、单位和适用组件建议
- 搜索框实时过滤
- 确认后返回属性ID字符串写回 ComponentBase.BindingId

---

## 八、配置文件格式 (JSON)

```json
{
  "version": 1,
  "name": "My Dashboard",
  "canvasWidth": 1920,
  "canvasHeight": 1080,
  "backgroundColor": "#00000000",
  "refreshIntervalMs": 500,
  "components": [
    {
      "$type": "ProgressBar",
      "id": "a1b2c3d4",
      "bindingId": "cpu-core1-load",
      "x": 100, "y": 80,
      "scaleX": 300, "scaleY": 30,
      "zIndex": 0,
      "foregroundColor": "#00FF00",
      "borderThickness": 1.0,
      "borderColor": "#333333",
      "borderStyle": "Solid",
      "transparentBackground": true,
      "backgroundColor": "#00000000",
      "roundness": 0.8
    },
    {
      "$type": "CircularGauge",
      "id": "e5f6g7h8",
      "bindingId": "cpu-core1-temperature",
      "x": 500, "y": 50,
      "scaleX": 200, "scaleY": 200,
      "zIndex": 1,
      "foregroundColor": "#FF4500",
      "startAngle": 140,
      "sweepAngle": 260,
      "showPointer": true,
      "pointerColor": "#FF0000",
      "pointerWidth": 3.0,
      "pointerStyle": "Metal",
      "lineStyle": "Solid",
      "roundness": 0.5,
      ...
    }
  ],
  "dataValues": {
    "cpu-core1-load": {
      "type": "Continuous",
      "upperLimit": 100, "lowerLimit": 0,
      "defaultUnitSuffix": "%"
    },
    "cpu-core1-temperature": {
      "type": "Continuous",
      "upperLimit": 100, "lowerLimit": 0,
      "defaultUnitSuffix": "°C"
    }
  }
}
```

- `$type` 用于反序列化时确定具体子类
- `dataValues` 存储每个绑定值的元信息（上下限、类型、单位）
- 加载时，`dataValues` 中的项自动注册到 `SensorBindingRegistry`

---

## 九、实现顺序 (建议)

| 阶段 | 内容 | 产出 |
|------|------|------|
| 1 | 数据模型层 Models | DataValue, SensorBindingRegistry, 枚举定义 |
| 2 | HardwareService | 封装 LHM，构建传感器树 |
| 3 | 组件模型层 Components | ComponentBase + 4个派生类 |
| 4 | ConfigService | JSON 序列化/反序列化，文件读写 |
| 5 | WPF 渲染控件 | 4种 ComponentView，预览模式先跑通 |
| 6 | Canvas 编辑区 | ResizableCanvas + Drag/Resize + 组件创建 |
| 7 | PropertyEditor + SensorPicker | 属性编辑面板 + 模态选择器 |
| 8 | BindingValidator | 校验 + 警告 |
| 9 | 主题系统 + 保存/加载 UI | 工具栏、主题切换 |
| 10 | 打磨 | 键盘操作、撤销重做、样式细节 |

---

## 十、技术要点备忘

- `DrawingVisual` 性能优于大量 UIElement，折线图和环形表优先使用
- 组件 `Roundness` 属性是 `0.0~1.0` 的 double，各组件自行映射到具体样式参数
- 保存/加载使用 `System.Text.Json` + `polymorphic serialization`（`$type` 判别器）
- 数据库不持久化 Color 作为 `System.Windows.Media.Color`，而是存 `#RRGGBBAA` 字符串，用 TypeConverter 互转
- 传感器 ID 不能包含空格/特殊字符，统一做 slug 化处理
