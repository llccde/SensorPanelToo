# genTask3 - Custom Dashboard Designer & Renderer

## 一、需求梳理与设计修正

### 1.1 双窗口架构

```
┌──────────────────────────────────────┐     ┌──────────────────────────┐
│           MainWindow                 │     │      SensorPanel         │
│         (样式编辑器)                  │     │    (渲染/运行仪表盘)      │
│                                      │     │                          │
│  ┌──────────────────────────────┐    │     │  ┌────────────────────┐  │
│  │  [新增] [保存] [主题▼] [运行] │    │     │  │                    │  │
│  ├──────────────────┬───────────┤    │     │  │   Canvas 渲染区     │  │
│  │                  │ 属性面板   │    │     │  │   (无交互手柄)     │  │
│  │   Canvas 编辑区   │           │    │     │  │                    │  │
│  │  (带拖拽/缩放)    │ Position  │    │     │  │   LibreHardware   │  │
│  │                  │ Binding   │    │     │  │   Monitor 实时数据  │  │
│  │                  │ Color     │    │     │  │                    │  │
│  │                  │ Border..  │    │ 启动 │  └────────────────────┘  │
│  │                  │           │    │────►│                          │
│  │                  │           │    │     │  可多开:同主题             │
│  │                  │           │    │     │  或不同主题               │
│  └──────────────────┴───────────┘    │     └──────────────────────────┘
│  状态栏: 已保存 / 主题: gaming       │
└──────────────────────────────────────┘
```

- **MainWindow**：编辑器，不运行实时数据。创建/拖拽/配置组件，切换主题，保存 JSON。
- **SensorPanel**：运行时渲染窗口，加载一份仪表盘配置，连接 `HardwareService` 获取实时数据并渲染。可由 MainWindow 打开，也可独立启动。

### 1.3 交互流程

```
MainWindow                        SensorPanel
─────────                        ───────────
选择/编辑主题 ──保存──► .json 文件
                            │
点击[运行] ──传递主题路径───►  new SensorPanel(configPath)
                            │
修改颜色 ──保存──► .json     │  (SensorPanel 下次重启生效，或通过IPC热更新)
                            │
                            └──► 加载 Configuration
                                 HardwareService.Start()
                                 定时刷新 → 渲染所有组件
```

### 1.2 对原始描述的修正建议

| 原描述 | 问题 | 修正 |
|--------|------|------|
| `func->float` 获取值 | 配置文件中无法存储函数 | 配置文件只存绑定ID字符串，运行时由 `HardwareService` 解析 |
| 组件含"当前值"字段 | 数据与视图耦合 | 数据模型(`SensorValue`)与组件模型(`Component`)分离，组件仅存绑定ID引用 |
| 绑定ID如 `disk-C-used-GB` | 需明确ID格式规范 | 格式规范化为 `HardwareType-HardwareName-SensorName`，如 `Cpu-Core#1-Load` |
| "圆润程度"模糊 | 不同组件含义不同 | 拆分为各组件独立属性：数字组件→字体粗细，折线图→贝塞尔平滑度 |
| 描边厚度响应"圆润" | 语义不清 | 删除此关联，描边厚度独立配置 |

### 1.3 修正后的数据模型

```
┌─────────────────────────────────────────────────────┐
│                    SensorValue                       │
│  (运行时的传感器值快照，每秒更新)                      │
├─────────────────────────────────────────────────────┤
│  BindingId: string     // "Cpu-Core#1-Load"         │
│  CurrentValue: float                                 │
│  DisplayText: string   // "45%" 或 "High"(枚举)     │
│  Unit: string          // "%" "°C" "MB/s"           │
│  ValueType: enum       // Continuous/Discrete/Enum  │
│  UpperBound: float                                   │
│  LowerBound: float                                   │
│  DiscreteValues: float[]  // 离散值列表              │
│  EnumMap: Dictionary<float, string> // 枚举映射      │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                   Component                          │
│  (存于配置文件的组件定义)                              │
├─────────────────────────────────────────────────────┤
│  Id: Guid                                            │
│  ComponentType: enum // ProgressBar/CircularGauge/   │
│                      //  DigitalDisplay/GridChart    │
│  BindingId: string?  // 绑定的传感器ID               │
│  Position: (double X, double Y)                      │
│  Size: (double Width, double Height)                 │
│  ZIndex: int                                         │
│  ForegroundColor: Color                              │
│  BorderThickness: double                             │
│  BorderColor: Color                                  │
│  BorderStyle: enum // Solid/Dashed                   │
│  TransparentBackground: bool                         │
│  BackgroundColor: Color                              │
│  FontFamily: string                                  │
│  FontSize: double                                    │
│  Roundness: double  // 圆角半径(通用)                │
│  ...子类特有属性                                     │
└─────────────────────────────────────────────────────┘

组件继承树:
Component (基类，含所有通用属性)
├── ProgressBarComponent
│     ProgressColor: Color
│     TrackColor: Color
│     Orientation: Horizontal/Vertical
│     ShowValueText: bool
│     ValueTextPosition: Inside/Outside
├── CircularGaugeComponent
│     SweepAngle: double     // 扇形角度 120~360
│     StartAngle: double     // 起始角度 (默认 -90 即顶部起步)
│     GaugeStyle: enum       // Solid/Dashed
│     NeedleEnabled: bool
│     NeedleColor: Color
│     NeedleWidth: double
│     NeedleStyle: enum      // Retro/Plastic/Metal/ScanLine/Finger
│     RingThickness: double
│     ShowCenterValue: bool
├── DigitalDisplayComponent
│     ShowPrefix: bool       // 显示传感器前缀
│     ShowSuffix: bool       // 显示传感器后缀(单位)
│     DecimalPlaces: int
│     StrokeColor: Color     // 文字描边
│     StrokeThickness: double
│     FontWeight: enum       // Normal/Bold 取代模糊的"圆润"
├── GridChartComponent
│     DurationSeconds: int   // X轴时间跨度
│     GridDensityX: int      // X网格线数量
│     GridDensityY: int      // Y网格线数量
│     GridLineColor: Color
│     GridLineWidth: double
│     LineWidth: double
│     LineColor: Color
│     SmoothFactor: double   // 0=折线, 1=最大贝塞尔平滑(取代"圆润")
│     ShowFill: bool
│     FillOpacity: double
│     HistoryValues: List<(DateTime Time, float Value)> // 运行时历史
```

---

## 二、文件结构设计

```
E:\CSharp\SensorPanelToo\
├── SensorPanelToo.csproj
├── App.xaml / App.xaml.cs
│
├── Models\                              # 数据模型 (POCO)
│   ├── SensorValue.cs                   # 传感器运行时快照
│   ├── Component.cs                     # 组件基类
│   ├── ProgressBarComponent.cs          # 进度条模型
│   ├── CircularGaugeComponent.cs        # 环形仪表模型
│   ├── DigitalDisplayComponent.cs       # 数字显示模型
│   ├── GridChartComponent.cs            # 折线图模型
│   └── DashboardConfig.cs               # 仪表盘配置文件根模型
│       (Version, ThemeName, List<Component>, CanvasWidth, CanvasHeight, BackgroundColor)
│
├── ViewModels\                          # 视图模型 (MVVM)
│   ├── MainViewModel.cs                 # MainWindow的VM (编辑器)
│   │   - ObservableCollection<Component> Components
│   │   - AddComponentCommand / DeleteComponentCommand
│   │   - SaveCommand / LoadCommand
│   │   - LaunchSensorPanelCommand
│   │   - SelectedComponent
│   │   - 拖拽/缩放状态管理
│   │
│   ├── SensorPanelViewModel.cs          # SensorPanel的VM (渲染器)
│   │   - DashboardConfig CurrentConfig
│   │   - Dictionary<string, SensorValue> SensorCache (由HardwareService填充)
│   │   - DispatcherTimer 驱动刷新
│   │   - Start() / Stop()
│   │
│   ├── PropertyPanelViewModel.cs        # 属性编辑面板VM
│   │   - 绑定到当前选中组件
│   │   - 各属性双向绑定
│   │   - OpenBindingDialogCommand
│   │
│   └── BindingSelectorViewModel.cs      # 传感器选择模态框VM
│       - Tree-structured sensor list
│       - Search/Filter
│       - SelectedSensor → 验证是否适合当前组件类型
│
├── Services\                            # 服务层 (单例)
│   ├── HardwareService.cs               # 封装LibreHardwareMonitor，全局唯一
│   │   - Dictionary<string, SensorValue> GetAllSensors()
│   │   - SensorValue? GetSensor(string bindingId)
│   │   - event Action SensorsUpdated
│   │   - List<SensorTreeNode> GetSensorTree()
│   │   - Start() / Stop()
│   │   - 线程安全，多个SensorPanel可共享同一实例
│   │
│   ├── ConfigService.cs                 # JSON配置读写 (静态方法)
│   │   - static void Save(DashboardConfig config, string path)
│   │   - static DashboardConfig Load(string path)
│   │   - static List<string> ListThemes()
│   │   - static string ThemesDirectory { get; }
│   │
│   └── BindingValidationService.cs      # 绑定合法性检查
│       - static bool IsBindable(string bindingId, ComponentType type)
│       - static string GetValidationError(...)
│
├── Controls\                            # 自定义WPF渲染控件
│   ├── ProgressBarControl.xaml/.cs      # 进度条渲染 (继承 UserControl)
│   ├── CircularGaugeControl.xaml/.cs    # 环形仪表渲染
│   ├── DigitalDisplayControl.xaml/.cs   # 数字显示渲染
│   ├── GridChartControl.xaml/.cs        # 折线图渲染
│   └── ComponentThumb.cs               # 编辑模式装饰器 (Adorner)
│
├── Views\                               # 窗口/对话框/用户控件
│   ├── MainWindow.xaml/.cs              # 样式编辑器窗口
│   ├── SensorPanel.xaml/.cs             # 仪表盘渲染窗口
│   ├── MonitorWindow.xaml/.cs           # (保留，独立快速监控)
│   ├── PropertyPanel.xaml/.cs           # 属性编辑面板 (嵌在MainWindow右侧)
│   └── BindingSelectorDialog.xaml/.cs   # 传感器选择模态框
│
├── Converters\                          # 值转换器
│   ├── ComponentTypeToTemplateConverter.cs
│   ├── BoolToVisibilityConverter.cs
│   └── ColorToBrushConverter.cs
│
├── Helpers\                             # 工具
│   ├── DragDropHelper.cs                # 拖拽辅助
│   └── JsonHelper.cs                    # System.Text.Json 多态序列化配置
│
└── Themes\                              # 预设仪表盘配置 (.json)
    ├── default.json
    ├── gaming.json
    └── minimal.json
```

---

## 三、关键架构决策

### 3.1 MainWindow (编辑器) 职责

```
┌──────────────────────────────────────────────────┐
│  [Add Component ▼]  [Save] [Load] [Theme ▼]     │  ← 工具栏
│  [▶ Run SensorPanel]                              │  ← 启动渲染窗口
├─────────────────────────────────┬────────────────┤
│                                 │  Properties    │
│      Canvas (编辑区)             │ ┌────────────┐ │
│      - 拖拽移动组件              │ │ Position   │ │
│      - 8点缩放                   │ │ X: [120]   │ │
│      - 右键菜单(删除/复制)       │ │ Y: [80]    │ │
│      - 选中高亮 + Thumb装饰器    │ ├────────────┤ │
│                                 │ │ Binding    │ │
│                                 │ │ [Select ▼] │ │
│                                 │ ├────────────┤ │
│                                 │ │ Appearance │ │
│                                 │ │ Color: [■] │ │
│                                 │ └────────────┘ │
├─────────────────────────────────┴────────────────┤
│  Status: 已保存至 gaming.json      Components: 5  │
└──────────────────────────────────────────────────┘
```

- 不连接硬件数据，组件显示占位值/模拟值
- 主题切换：从 Theme 下拉加载不同 .json → 替换组件列表 → 刷新 Canvas
- 点击 [Run SensorPanel] → `new SensorPanel(themePath).Show()`

### 3.2 SensorPanel (渲染器) 职责

```
┌──────────────────────────────────────────┐
│  SensorPanel - "Gaming Dashboard"  [✕]  │
│                                          │
│   ┌────────┐     ┌──────────────┐        │
│   │  CPU    │     │              │        │
│   │  45°C   │     │   GPU Load   │        │
│   │ ████░░  │     │   ╭───╮     │        │
│   └────────┘     │   │72%│     │        │
│                   │   ╰───╯     │        │
│   ┌──────────────────────────┐  │        │
│   │  CPU Temp History        │  │        │
│   │  ╱╲   ╱╲                │  │        │
│   │ ╱  ╲_╱  ╲___           │  │        │
│   └──────────────────────────┘  │        │
│                                          │
└──────────────────────────────────────────┘
```

- 构造时加载一份 `DashboardConfig`
- `Loaded` 事件中启动 `HardwareService`（如未启动）并开始定时刷新
- Canvas 上按 ZIndex 排列渲染控件，无交互手柄
- 所有渲染控件绑定到 `SensorPanelViewModel.SensorCache`
- 关闭时停止定时器（但不关闭 HardwareService，因为可能其他 SensorPanel 还在用）

### 3.3 HardwareService 共享策略

```
App.xaml.cs 启动时:
  HardwareService = new HardwareService();  // 全局单例

MainWindow 打开:
  (不使用 HardwareService)

SensorPanel1 打开:
  HardwareService.Start();   // 首次启动

SensorPanel2 打开:
  HardwareService.Start();   // 已启动，无操作(引用计数)

SensorPanel1 关闭:
  HardwareService.Stop();    // 引用计数 -1，不下线

SensorPanel2 关闭:
  HardwareService.Stop();    // 引用计数为0，调用 computer.Close()
```

### 3.4 拖拽定位实现

```
编辑模式 Canvas:
- 组件放在Canvas上，用Canvas.Left/Top控制位置
- MouseDown → 记录偏移量，开始拖拽
- MouseMove → 更新 Component.Position → Canvas.Left/Top
- MouseUp → 结束拖拽
- 缩放手柄: 8个 Thumb 在选中组件边框四角/四边中点
```

### 3.5 传感器树结构 (用于绑定选择器)

```
HardwareService.GetSensorTree() 构建:
SensorTreeNode {
  Name: string
  BindingId: string?      // 叶子节点有
  SensorType: SensorType?
  Children: List<SensorTreeNode>
  Unit: string?
  ValueRange: (float Min, float Max)?
}

示例:
├── CPU
│   ├── Intel Core i7-13700K
│   │   ├── Core #1  (Load, %)                → 适合进度条/数字/折线图/环形
│   │   ├── Core #1  (Temperature, °C)         → 适合进度条/数字/折线图/环形
│   │   └── ...
│   └── Package (Power, W)                     → 适合数字/折线图
├── GPU
│   └── NVIDIA RTX 4090
│       ├── Core (Load, %)
│       ├── Core (Temperature, °C)
│       └── ...
└── Storage
    └── Samsung SSD 980 Pro
        ├── Used Space (%)
        └── Temperature (°C)
```

### 3.6 绑定合法性验证

| 组件类型 | 允许传感器类型 | 不允许原因 |
|----------|---------------|-----------|
| ProgressBar | 有自然上下限的值(Load%, Temp, Used%) | 无上下限的绝对数值不适合进度条 |
| CircularGauge | 同上 | 同上 |
| DigitalDisplay | 所有类型 | 无限制 |
| GridChart | 连续变化值(排除离散/枚举) | 离散值画折线图无意义 |

---

## 四、配置文件格式 (JSON)

```json
{
  "version": "1.0",
  "themeName": "My Dashboard",
  "canvasWidth": 1280,
  "canvasHeight": 720,
  "backgroundColor": "#0A0A0F",
  "components": [
    {
      "id": "a1b2c3d4-...",
      "componentType": "DigitalDisplay",
      "bindingId": "Cpu-Intel Core i7-Package-Temperature",
      "position": { "x": 100, "y": 50 },
      "size": { "width": 180, "height": 80 },
      "zIndex": 0,
      "foregroundColor": "#00FF88",
      "borderThickness": 1.0,
      "borderColor": "#333333",
      "borderStyle": "Solid",
      "transparentBackground": true,
      "backgroundColor": "#00000000",
      "fontFamily": "Consolas",
      "fontSize": 14.0,
      "roundness": 4.0,

      "showPrefix": false,
      "showSuffix": true,
      "decimalPlaces": 1,
      "strokeColor": "#00AA55",
      "strokeThickness": 0.5,
      "fontWeight": "Bold"
    },
    {
      "id": "e5f6g7h8-...",
      "componentType": "CircularGauge",
      "bindingId": "Gpu-NVIDIA RTX 4090-Core-Load",
      "position": { "x": 300, "y": 50 },
      "size": { "width": 200, "height": 200 },
      "zIndex": 1,
      "foregroundColor": "#FF6644",
      "borderThickness": 0.0,
      "borderColor": "#000000",
      "borderStyle": "Solid",
      "transparentBackground": true,
      "backgroundColor": "#00000000",
      "fontFamily": "Segoe UI",
      "fontSize": 12.0,
      "roundness": 0.0,

      "sweepAngle": 270,
      "startAngle": -135,
      "gaugeStyle": "Solid",
      "needleEnabled": true,
      "needleColor": "#FF0000",
      "needleWidth": 2.5,
      "needleStyle": "Metal",
      "ringThickness": 12.0,
      "showCenterValue": true
    }
  ]
}
```

---

## 五、数据流图

```
App 启动
    │
    ├──► HardwareService (单例, 全局)
    │        │
    │        └──► Dictionary<string, SensorValue> 缓存 (线程安全)
    │
    ├──► MainWindow (编辑器)
    │        │
    │        ├──► 新建/编辑 Component
    │        ├──► 保存 → ConfigService.Save() → .json
    │        ├──► 加载 → ConfigService.Load() ← .json
    │        └──► [运行] → new SensorPanel(themePath)
    │
    └──► SensorPanel (渲染器, 可多开)
             │
             ├──► 加载 DashboardConfig (.json)
             ├──► DispatcherTimer (500ms)
             │        │
             │        └──► 从 HardwareService 获取 SensorValue
             │             按 BindingId 投递到各渲染控件
             │
             └──► Canvas 渲染
                      ├── ProgressBarControl    ← SensorValue
                      ├── CircularGaugeControl ← SensorValue
                      ├── DigitalDisplayControl← SensorValue
                      └── GridChartControl     ← SensorValue + 历史队列
```

---

## 六、实施步骤

| Step | 文件 | 说明 |
|------|------|------|
| 3-1 | Models/*.cs (6个) | 定义所有数据模型、枚举、多态序列化 |
| 3-2 | Helpers/JsonHelper.cs | System.Text.Json 多态反序列化 |
| 3-3 | Services/HardwareService.cs | 封装LHM，全局单例，构建传感器树 |
| 3-4 | Services/ConfigService.cs | 配置文件读写、主题列表 |
| 3-5 | Services/BindingValidationService.cs | 绑定合法性检查 |
| 3-6 | Converters/*.cs | 值转换器 |
| 3-7 | ViewModels/MainViewModel.cs | 编辑器 VM |
| 3-8 | ViewModels/SensorPanelViewModel.cs | 渲染器 VM |
| 3-9 | ViewModels/PropertyPanelViewModel.cs | 属性面板 VM |
| 3-10 | ViewModels/BindingSelectorViewModel.cs | 传感器选择 VM |
| 3-11 | Helpers/DragDropHelper.cs | 拖拽/缩放辅助 |
| 3-12 | Controls/ProgressBarControl.xaml/.cs | 进度条渲染 |
| 3-13 | Controls/CircularGaugeControl.xaml/.cs | 环形仪表渲染 |
| 3-14 | Controls/DigitalDisplayControl.xaml/.cs | 数字显示渲染 |
| 3-15 | Controls/GridChartControl.xaml/.cs | 折线图渲染 |
| 3-16 | Controls/ComponentThumb.cs | 编辑装饰器 |
| 3-17 | Views/MainWindow.xaml/.cs | 编辑器窗口 (重写) |
| 3-18 | Views/SensorPanel.xaml/.cs | 渲染器窗口 |
| 3-19 | Views/PropertyPanel.xaml/.cs | 属性面板 (嵌 MainWindow) |
| 3-20 | Views/BindingSelectorDialog.xaml/.cs | 传感器选择模态框 |
| 3-21 | App.xaml.cs | 初始化 HardwareService 单例 |
| 3-22 | Themes/*.json | 预设主题配置 |
| 3-23 | Views/MonitorWindow (保留不移除) | 已有独立监控窗口 |

---

## 七、待确认事项

1. **SensorPanel 可否多开**：允许多开（每个窗口可显示不同主题或同一主题）
2. **MainWindow 和 SensorPanel 同时打开时，编辑保存后 SensorPanel 是否实时更新**：方案A（简单）需关闭重开；方案B（复杂）IPC/事件通知热更新。先用方案A。
3. **MonitorWindow 去留**：保留，作为快速文本监控，与仪表盘系统功能独立。
4. **折线图历史数据**：运行时内存暂存，不持久化到配置文件。最大条数 = DurationSeconds / 0.5s。
