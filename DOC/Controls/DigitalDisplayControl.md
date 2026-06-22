# DigitalDisplayControl

## 作用
数字显示组件的 WPF 渲染控件，格式化显示传感器数值和单位。

## 职责边界
- 负责：根据配置格式化并显示数值
- 不负责：数据采集、交互编辑

## 依赖
- `DigitalDisplayComponent`（配置模型）
- `SensorValue`（数据模型）

## 被谁使用
- `ComponentEditorWindow`（预览编辑）
- `SensorPanel`（后续）

## 公开 API

| 成员 | 类型 | 说明 |
|------|------|------|
| `ComponentData` | `DigitalDisplayComponent?` | 依赖属性，显示配置 |
| `SensorValue` | `SensorValue?` | 依赖属性，传感器实时值 |
| `Refresh()` | `void` | 公开方法，重新读取模型并重绘 |

## 关键设计决策
- 使用 `OnRender` + `DrawingContext` + `FormattedText` 自绘
- 描边效果：使用 `FormattedText.BuildGeometry` 转换为 Geometry，先用粗 Pen（`LineJoin = Round`，避免锐角尖刺）画描边，再用前景色画填充
- `StrokeThickness = 0` 时跳过描边层
- `ShowPrefix`/`ShowSuffix` 控制单位显示位置
- `DecimalPlaces` 控制 `float.ToString($"F{n}")` 的小数位数

## 示例
```xml
<controls:DigitalDisplayControl
    ComponentData="{Binding DisplayConfig}"
    SensorValue="{Binding CpuTemp}" />
```

