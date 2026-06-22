# CircularGaugeControl

## 作用
环形仪表组件的 WPF 渲染控件，通过 `OnRender` + `DrawingContext` 自绘弧形、指针和文字。

## 职责边界
- 负责：根据配置绘制环形仪表
- 不负责：数据采集、交互编辑

## 依赖
- `CircularGaugeComponent`（配置模型）
- `SensorValue`（数据模型）
- `System.Windows.Media.DrawingContext`（自绘）

## 被谁使用
- `ComponentDemoWindow`（验证）
- `SensorPanel`（后续）

## 公开 API

| 成员 | 类型 | 说明 |
|------|------|------|
| `ComponentData` | `CircularGaugeComponent?` | 依赖属性，仪表配置 |
| `SensorValue` | `SensorValue?` | 依赖属性，传感器实时值 |

## 关键设计决策
- 使用 `OnRender` + `DrawingContext` 自绘，不创建子控件树，性能更好
- 弧形使用 `StreamGeometry` + `ArcTo`，通过 `DrawingContext.DrawGeometry` 渲染
- 角度系统：WPF 坐标（0°=3点方向，顺时针），`StartAngle = -135` 默认从左上角开始
- 指针从中心画线到环形边缘，中心点用实心圆表示
- `NeedleStyle` 枚举预留但当前版本统一绘制实线指针

## 示例
```xml
<controls:CircularGaugeControl
    ComponentData="{Binding GaugeConfig}"
    SensorValue="{Binding CpuTemp}" />
```
