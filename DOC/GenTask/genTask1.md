y# genTask1 - Hardware Monitor Window

## 概述
创建 MonitorWindow 使用 LibreHardwareMonitor 读取硬件传感器数据并显示，MainWindow 控制监控窗口的字体颜色。

## 新增文件

### MonitorWindow.xaml
- 硬件监控窗口，900x600，深色背景 (#1E1E1E)
- ListView + GridView 显示六列：Hardware、Sensor、Type、Value、Min、Max
- DataTemplate 绑定 SensorItem 属性及 Foreground 颜色
- ListViewItem 样式：Consolas 字体，13号，前景色绑定数据项

### MonitorWindow.xaml.cs
- `Computer` 实例启用 CPU/GPU/Memory/Motherboard/Storage/Network/Controller 监控
- `UpdateVisitor` 实现 `IVisitor` 接口遍历硬件树并调用 `Update()`
- `DispatcherTimer` 每秒刷新传感器数据
- `SensorItem` 类实现 `INotifyPropertyChanged`，封装传感器值、名称、类型、颜色
- `ForegroundColor` 依赖属性，外部修改时批量更新所有 `SensorItem.Foreground`
- 窗口关闭时停止定时器并关闭 `Computer`

## 修改文件

### SensorPanelToo.csproj
- 添加 `LibreHardwareMonitorLib` 版本 `0.9.6` 的 PackageReference

### MainWindow.xaml
- 添加标题文字 "Hardware Monitor Control"
- 添加 ComboBox（7 种预设颜色：Lime Green、Deep Sky Blue、Orange Red、Gold、Hot Pink、Cyan、White）
- 添加 "Open Monitor Window" 按钮
- 添加状态提示 TextBlock

### MainWindow.xaml.cs
- 管理 `MonitorWindow` 单例（同时只开一个）
- 点击按钮创建或聚焦监控窗口
- ComboBox 选择颜色后通过 `ForegroundColor` 依赖属性同步到监控窗口
- 状态栏显示当前操作结果

## 编译结果
- 0 错误，0 警告
