# Component (基类)

## 作用
所有仪表盘组件的基类，定义 X/Y 绝对位置 + Scale 缩放 + 公共外观属性。

## 职责边界
- 负责：持有组件 ID、类型、位置、缩放、颜色、字体等通用配置
- 不负责：渲染逻辑、传感器数据

## 依赖
- `System.Text.Json`（JsonDerivedType 多态序列化）

## 被谁使用
- `ProgressBarComponent`, `CircularGaugeComponent`, `DigitalDisplayComponent`, `GridChartComponent`, `SensorLabelComponent` —— 派生
- `DashboardConfig` —— 持有 `List<Component>`
- 各渲染控件 —— 通过 `ComponentData` DP 读取配置

## 公开 API

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Id` | `Guid` | `Guid.NewGuid()` | 组件唯一标识 |
| `ComponentType` | `ComponentType` | — | 组件类型枚举 |
| `BindingId` | `string?` | — | 绑定的传感器 ID（LHM Identifier 格式） |
| `X` | `double` | 0 | Canvas X 位置 |
| `Y` | `double` | 0 | Canvas Y 位置 |
| `Scale` | `double` | 1.0 | 统一缩放系数，通过 RenderTransform 实现图像级缩放 |
| `ZIndex` | `int` | 0 | 图层顺序 |
| `ForegroundColor` | `string` | `"#00FF88"` | 前景色（HEX） |
| `BackgroundColor` | `string` | `"#00000000"` | 背景色 |
| `TransparentBackground` | `bool` | `true` | 透明背景 |
| `FontFamily` | `string` | `"Consolas"` | 字体名 |
| `FontSize` | `double` | 14 | 字号 |

### 枚举

`ComponentType`：ProgressBar, CircularGauge, DigitalDisplay, GridChart, SensorLabel

### 已从基类移除（按组件独立）

| 属性 | 所在组件 |
|------|---------|
| `BorderThickness`, `BorderColor` | ProgressBar |
| `Roundness` | ProgressBar |

## 关键设计决策
- 颜色均用 string/HEX 存储，序列化友好
- `Scale` 不再改变 Width/Height 属性，而是通过 `RenderTransform = ScaleTransform(Scale, Scale)` 对渲染输出做整体缩放
- 每种组件有固定基础尺寸（如 ProgressBar 300×40），Scale=1.0 即基础尺寸
- 边框和圆角仅对矩形组件有意义，移至具体组件类
- 使用 `[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]` + `[JsonDerivedType]` 实现多态序列化

## 示例
```csharp
var comp = new ProgressBarComponent
{
    X = 100, Y = 50,
    Scale = 1.5,
    BindingId = "/intelcpu/0/load/1",
    ForegroundColor = "#00FF88"
};
```
