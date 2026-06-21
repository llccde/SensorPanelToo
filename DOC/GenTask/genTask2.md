# genTask2 - Incremental Sensor Update Optimization

## 问题
MonitorWindow 每秒调用 `Sensors.Clear()` 后重建所有 `SensorItem`，导致：
- 大量对象分配和 GC 压力
- ListView 全量重绘，UI 卡顿

## 修改文件

### MonitorWindow.xaml.cs
- 添加 `Dictionary<string, SensorItem> _sensorMap`，以传感器 `Identifier` 为键缓存已有项
- `RefreshSensors()` 改为增量更新：
  - 遍历硬件传感器，用 `Identifier.ToString()` 做键查找
  - 已存在项：仅更新 `Value/Min/Max` 属性，触发 INotifyPropertyChanged 精确刷新
  - 新项：创建 SensorItem 并加入字典和 ObservableCollection
  - 反向遍历移除不再存在的传感器
- `SensorItem` 新增 `Key` 属性用于反向查找
- 刷新间隔从 `1s` 改为 `500ms`，增量更新开销小可支持更高刷新率
