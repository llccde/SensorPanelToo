# Task 3 Summary — HardwareService & Test Infrastructure

## 创建的文件

```
SensorPanelToo/
├── Models/
│   └── SensorValue.cs                      # 数据模型 (SensorValue, SensorTreeNode, SensorValueType)
├── Services/
│   └── HardwareService.cs                  # 硬件监控服务 (单例, 封装 LibreHardwareMonitor)
└── SensorPanelToo.Tests/
    ├── SensorPanelToo.Tests.csproj          # xUnit 测试项目
    └── HardwareServiceTests.cs             # 7 个测试用例
```

## HardwareService 架构

| 特性 | 实现 |
|------|------|
| 生命周期 | 全局单例 (`Lazy<T>`) |
| 共享策略 | 引用计数 Start/Stop，多 SensorPanel 可共用 |
| 数据刷新 | 后台线程，每 500ms 轮询 |
| 线程安全 | `ConcurrentDictionary` 缓存 + `lock` 保护 `Computer` |
| 更新通知 | `event Action? SensorsUpdated` |

### 公开 API

```csharp
HardwareService.Instance          // 单例
Start() / Stop()                  // 引用计数启动/停止
IsRunning / ReferenceCount         // 状态
GetAllSensors() → Dictionary<string, SensorValue>
GetSensor(bindingId) → SensorValue?
GetSensorTree() → List<SensorTreeNode>
```

### BindingId 格式

直接使用 LHM 内置的 `sensor.Identifier.ToString()`，格式为层级路径：

```
/intelcpu/0/load/0                → CPU#0 总负载
/intelcpu/0/load/1                → CPU#0 Core #1 负载
/gpu-nvidia/0/temperature/0       → GPU 温度
/lpc/nct6791d/voltage/0           → 主板传感器电压#0
/hdd/0/load/0                     → 硬盘#0 使用率
```

格式：`/硬件类型/硬件实例/传感器类型/传感器索引`，天然唯一，包含完整层级信息，不会因硬件名含特殊字符而出错。

## 测试结果

```
dotnet test SensorPanelToo.Tests\SensorPanelToo.Tests.csproj

测试总数: 7    通过数: 7    ✅
```

### 单元测试 (1)

| 测试 | 说明 |
|------|------|
| Instance_ReturnsSameSingleton | 单例模式验证 |

### 集成测试 (6)

| 测试 | 说明 |
|------|------|
| GetAllSensors_ReturnsPopulatedCache | 传感器数据填充，验证 BindingId 以 `/` 开头 |
| GetSensor_ByValidBindingId_ReturnsSensor | 有效ID查询传感器 |
| GetSensor_ByInvalidBindingId_ReturnsNull | 无效ID返回null |
| GetSensorTree_ReturnsValidStructure | 传感器树结构完整性 |
| ReferenceCounting_MultipleStartStop | 引用计数正确性 |
| GetAllSensors_ReturnsIndependentSnapshots | 快照独立性 |

## 构建命令

```powershell
# 构建
dotnet build SensorPanelToo.slnx

# 运行测试
dotnet test SensorPanelToo.Tests\SensorPanelToo.Tests.csproj
```

## 注意事项

- `SensorValue` 与 LHM 内置类型同名冲突，通过 using alias (`SensorModel`) 解决
- 测试项目位于主项目子目录，需在 `.csproj` 中添加 `<Compile Remove="SensorPanelToo.Tests\**" />` 排除，否则 WPF 临时编译会报错
- BindingId 已改为 LHM 原生 `Identifier` 格式，消除了自定义拼接中 `-` 冲突的问题
