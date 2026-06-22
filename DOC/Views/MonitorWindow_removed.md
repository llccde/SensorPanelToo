# MonitorWindow (已移除)

## 原始作用
genTask1/genTask2 创建的硬件监控窗口，以 ListView + GridView 展示所有传感器原始数据。

## 移除原因
- 功能已被 `HardwareService` + 组件渲染控件体系取代
- `MonitorWindow` 独立内嵌 Computer 实例，与 `HardwareService` 单例重复
- 精简项目，后续以仪表盘组件方式展示传感器数据
