# SensorValue

## 作用
运行时传感器值快照，由 `HardwareService` 创建并缓存，供 UI 渲染读取。

## 职责边界
- 负责：持有单个传感器的一次采样数据
- 不负责：获取数据（由 HardwareService 负责）、UI 展示逻辑

## 依赖
- 无外部依赖，纯 POCO

## 被谁使用
- `HardwareService` —— 写入
- `SensorPanelViewModel`（后续）—— 读取
- `ProgressBarControl`, `CircularGaugeControl` 等渲染控件（后续）—— 读取

## 公开 API

### SensorValue

| 成员 | 类型 | 说明 |
|------|------|------|
| `BindingId` | `string` | 传感器唯一标识，即 `sensor.Identifier.ToString()`，如 `/intelcpu/0/load/1` |
| `CurrentValue` | `float` | 当前数值 |
| `DisplayText` | `string` | 格式化展示文本，如 `"45.0°C"` |
| `Unit` | `string` | 单位，如 `%` `°C` `RPM` |
| `ValueType` | `SensorValueType` | Continuous / Discrete / Enum |
| `UpperBound` | `float` | 上限值 |
| `LowerBound` | `float` | 下限值 |
| `DiscreteValues` | `float[]?` | 离散值列表（预留） |
| `EnumMap` | `Dictionary<float,string>?` | 枚举映射（预留） |

### SensorValueType
| 值 | 说明 |
|----|------|
| `Continuous` | 连续变化值（温度、负载等） |
| `Discrete` | 离散数值 |
| `Enum` | 枚举状态 |

### SensorTreeNode
| 成员 | 类型 | 说明 |
|------|------|------|
| `Name` | `string` | 节点显示名 |
| `BindingId` | `string?` | 叶子节点有值，非叶子为 null |
| `SensorType` | `string?` | 传感器类型名 |
| `Children` | `List<SensorTreeNode>` | 子节点 |
| `Unit` | `string?` | 单位 |
| `ValueRange` | `(float Min, float Max)?` | 取值范围 |

## 关键设计决策
- `SensorValue` 与 LHM 内置类型 `LibreHardwareMonitor.Hardware.SensorValue` 重名，在 `HardwareService.cs` 中通过 `using SensorModel = SensorPanelToo.Models.SensorValue;` 消歧义
- `DiscreteValues` 和 `EnumMap` 目前预留未使用，当前所有传感器均标记为 `Continuous`

## 示例

```csharp
// 由 HardwareService 内部创建：
var sv = new SensorValue
{
    BindingId = "/intelcpu/0/load/1",
    CurrentValue = 45.2f,
    DisplayText = "45.2%",
    Unit = "%",
    ValueType = SensorValueType.Continuous,
    UpperBound = 100f,
    LowerBound = 0f
};
```
