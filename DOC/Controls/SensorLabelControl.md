# SensorLabelControl

## 作用
传感器标签组件的 WPF 渲染控件，通过 `OnRender` + `DrawingContext` 自绘传感器路径名称。

## 职责边界
- 负责：根据配置格式化并显示传感器路径
- 不负责：数据采集、交互编辑

## 依赖
- `SensorLabelComponent`（配置模型）
- `SensorValue`（数据模型，读取 BindingId）

## 被谁使用
- `ComponentEditorWindow`（预览编辑）
- `SensorPanel`（后续）

## 公开 API

| 成员 | 类型 | 说明 |
|------|------|------|
| `ComponentData` | `SensorLabelComponent?` | 依赖属性，显示配置 |
| `SensorValue` | `SensorValue?` | 依赖属性，传感器实时值（用于读取 BindingId） |
| `Refresh()` | `void` | 公开方法，重新读取模型并重绘 |

## 关键设计决策
- 使用 `OnRender` + `DrawingContext` 自绘
- 描边效果与 DigitalDisplay 一致：`FormattedText.BuildGeometry` + `DrawGeometry`（`LineJoin = Round`）
- 路径取自 `SensorValue.BindingId`，按 `/` 分割后取末尾 N 层
- 无实时值时回退使用 `ComponentData.BindingId`（配置中的绑定 ID）
- `HierarchyLevels = 0` 时仅显示最后一段

## 示例
```xml
<controls:SensorLabelControl
    ComponentData="{Binding LabelConfig}"
    SensorValue="{Binding CpuTemp}" />
```
