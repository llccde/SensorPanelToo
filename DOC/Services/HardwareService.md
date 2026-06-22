# HardwareService

## 作用
封装 LibreHardwareMonitor，全局单例，为整个应用提供统一的硬件传感器数据获取入口。

## 职责边界
- 负责：启动 LHM Computer、定时轮询传感器、缓存数据、构建传感器树
- 不负责：UI 渲染、配置读写、绑定验证

## 依赖
- `LibreHardwareMonitor.Hardware`（NuGet 包 `LibreHardwareMonitorLib`）
- `SensorPanelToo.Models.SensorValue`、`SensorValueType`、`SensorTreeNode`

## 被谁使用
- `HardwareSelectDialog` —— 启动时选择硬件类型
- `ComponentEditorWindow` —— 传感器树选择 + 实时数据
- 后续 `SensorPanelViewModel` —— 数据源

## 公开 API

| 成员 | 签名 | 说明 |
|------|------|------|
| `Instance` | `static HardwareService` | 全局唯一实例 |
| `Start(cpu, gpu, memory, motherboard, network, storage, controller)` | `void` | 启动服务。首次创建 Computer；再调且 flags 变化则关旧开新；flags 不变则无操作。所有参数有默认值（cpu/memory/storage=true，其余=false） |
| `IsRunning` | `bool` | 后台线程是否运行中 |
| `GetAllSensors()` | `Dictionary<string, SensorModel>` | 返回当前缓存的快照副本 |
| `GetSensor(bindingId)` | `SensorModel?` | 按 BindingId 查找单个传感器 |
| `GetSensorTree()` | `List<SensorTreeNodeModel>` | 构建硬件→子硬件→传感器的三层树 |
| `SensorsUpdated` | `event Action?` | 每次轮询完成后触发 |
| `GetUnit(sensorType)` | `static string` (internal) | SensorType 到单位的映射 |
| `GetSensorTypeBounds(type, min, max)` | `static (float,float)` (internal) | 按传感器类型返回合理上下限 |

## 关键设计决策

### 传感器树缓存 + 原子更新块
- 用 `object` 锁替代 `ReaderWriterLockSlim`，锁粒度更细、更稳定
- 传感器树在 `Start()` 中构建并缓存到 `_cachedTree`，`GetSensorTree()` 返回缓存副本，完全无锁，消除 UI 端阻塞
- `UpdateLoop` 中 `Accept` 和 `RefreshCacheCore` 合并为一个 `lock` 块，消除 TOC/TOU 空隙，杜绝 `Start()` 中途替换 `_computer` 引发的不一致
- `GetAllSensors()` / `GetSensor()` 始终无锁（直接读 `ConcurrentDictionary`）
- `VisitHardware` 中 `h.Update()` 通过 `Task.Run` + 2s 超时执行，防止 StorageDevice 等硬件驱动无限阻塞导致锁死

### 无引用计数，无 Stop
- `Start()` 唯一入口，调用即启动，不跟踪调用方数量
- 无 `Stop()` 方法。进程退出时由 `Dispose()` 或 OS 清理
- 再次调用 `Start(gpu: true)` 若传入了新增硬件类型，自动关闭旧 Computer 并重建

### 所有硬件类型均可选
- `Start()` 接收 7 个 bool 参数：cpu/gpu/memory/motherboard/network/storage/controller
- cpu/memory/storage 默认 true，其余默认 false，兼容旧调用
- 全部取消仍可启动（仅有缓存，无传感器数据）

### BindingId 使用 LHM 原生 Identifier
- 不自行拼接，格式如 `/intelcpu/0/load/1`
- 天然唯一，包含完整硬件层级

### 传感器边界策略
- Load/Control/Level/Humidity → 固定 0–100
- Temperature → 上界 floor 100°C
- 其他 → 按类型给合理 floor

### 后台线程而非 DispatcherTimer
- 服务层不依赖 UI 线程
- 事件在后台线程触发，消费者需自行 `Dispatcher.Invoke` 回 UI 线程

## 示例

```csharp
HardwareService.Instance.Start(gpu: true, motherboard: true);

var cpuLoad = HardwareService.Instance.GetSensor("/intelcpu/0/load/1");
Console.WriteLine(cpuLoad?.DisplayText);

// 运行时加网络监控
HardwareService.Instance.Start(network: true);
```
