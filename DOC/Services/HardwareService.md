# HardwareService

## 作用
封装 LibreHardwareMonitor，全局单例，为整个应用提供统一的硬件传感器数据获取入口。

## 职责边界
- 负责：启动/停止 LHM Computer，定时轮询传感器，缓存数据，构建传感器树
- 不负责：UI 渲染、配置读写、绑定验证

## 依赖
- `LibreHardwareMonitor.Hardware`（NuGet 包 `LibreHardwareMonitorLib`）
- `SensorPanelToo.Models.SensorValue`、`SensorValueType`、`SensorTreeNode`

## 被谁使用
- `App.xaml.cs`（后续）—— 持有全局单例引用
- `SensorPanelViewModel`（后续）—— 读取传感器数据
- `BindingSelectorViewModel`（后续）—— 获取传感器树
- 本类的使用者通过 `HardwareService.Instance` 访问

## 公开 API

| 成员 | 签名 | 说明 |
|------|------|------|
| `Instance` | `static HardwareService` | 全局唯一实例 |
| `Start()` | `void` | 引用计数 +1，首次调用启动 LHM 和后台线程 |
| `Stop()` | `void` | 引用计数 -1，归零时关闭 LHM，清空缓存 |
| `IsRunning` | `bool` | 后台线程是否运行中 |
| `ReferenceCount` | `int` | 当前引用计数 |
| `GetAllSensors()` | `Dictionary<string, SensorModel>` | 返回当前缓存的快照副本 |
| `GetSensor(bindingId)` | `SensorModel?` | 按 BindingId 查找单个传感器 |
| `GetSensorTree()` | `List<SensorTreeNodeModel>` | 构建硬件→子硬件→传感器的三层树 |
| `SensorsUpdated` | `event Action?` | 每次轮询完成后触发 |
| `GetUnit(sensorType)` | `static string` (internal) | SensorType 到单位的映射 |

## 关键设计决策

### 单例 + 引用计数
- `Start()` 首次调用时才 `new Computer()` + `Open()`，避免应用启动时不必要的开销
- 多个 `SensorPanel` 可共用，各自调用 Start/Stop，引用计数归零才真正释放资源
- `Stop()` 使用 `Interlocked.Decrement` 后检查返回值，防止并发下计数值变负数

### BindingId 使用 LHM 原生 Identifier
- 不自行拼接（如 `Cpu-Core#1-Load`），因为硬件名可能含 `-` 导致解析歧义
- 直接使用 `sensor.Identifier.ToString()`，格式如 `/intelcpu/0/load/1`
- 天然唯一，包含完整硬件层级

### 线程安全
- `_sensorCache` 用 `ConcurrentDictionary`，读写无锁
- `_computer` 用 `lock(_computerLock)` 保护，LHM 的 Computer 本身不是线程安全的
- 后台线程每 500ms：lock → Accept(UpdateVisitor) → 遍历传感器 → 更新 cache → 释放锁 → 触发事件

### 后台线程而非 DispatcherTimer
- 服务层不应依赖 UI 线程
- System.Threading.Timer 或 Thread + CancellationToken 都可以
- 事件在后台线程触发，消费者需自行 `Dispatcher.Invoke` 回 UI 线程

## 示例

```csharp
// 启动
HardwareService.Instance.Start();

// 读取
var cpuLoad = HardwareService.Instance.GetSensor("/intelcpu/0/load/1");
Console.WriteLine(cpuLoad?.DisplayText);  // "45.2%"

// 获取全部
var all = HardwareService.Instance.GetAllSensors();
foreach (var kvp in all)
    Console.WriteLine($"{kvp.Key} = {kvp.Value.DisplayText}");

// 获取树（用于绑定选择器 UI）
var tree = HardwareService.Instance.GetSensorTree();

// 停止
HardwareService.Instance.Stop();
```
