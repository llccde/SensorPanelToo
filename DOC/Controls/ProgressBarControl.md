# ProgressBarControl

## 作用
进度条组件的 WPF 渲染控件，接收 `ProgressBarComponent` 配置和 `SensorValue` 数据，自包含渲染。

## 职责边界
- 负责：根据配置渲染进度条和数值文本
- 不负责：数据采集、交互编辑、传感器绑定

## 依赖
- `ProgressBarComponent`（配置模型）
- `SensorValue`（数据模型）
- WPF `ProgressBar` 控件

## 被谁使用
- `ComponentEditorWindow`（预览编辑）
- `SensorPanel`（后续）—— Canvas 中承载

## 公开 API

| 成员 | 类型 | 说明 |
|------|------|------|
| `ComponentData` | `ProgressBarComponent?` | 依赖属性，进度条配置 |
| `SensorValue` | `SensorValue?` | 依赖属性，传感器实时值 |
| `ApplyComponentData()` | `void` | 公开方法，从模型重新读取所有外观属性 |
| `RefreshValue()` | `void` | 公开方法，刷新进度数值和显示文本 |

## 关键设计决策
- 用 WPF 内置 `ProgressBar` 控件做底层渲染，TextBlock 叠加显示数值
- 外层 `Border`（`OuterBorder`）提供边框和圆角，边框为外边框
- `ForegroundColor` → 进度条填充，`BackgroundColor` → 轨道背景（透明背景时隐藏轨道），`TextColor` → 文字颜色
- `Roundness` → `OuterBorder.CornerRadius`，实现圆角
- `Orientation.Vertical` 时对 ProgressBar 应用 `RotateTransform(-90)`
- `ValueTextPosition.Outside` 时文本移到进度条下方
- `SensorValue` 为 null 时显示 0% 进度和 "N/A" 文本

## 示例
```xml
<controls:ProgressBarControl
    ComponentData="{Binding ProgressBarConfig}"
    SensorValue="{Binding CpuLoad}" />
```

