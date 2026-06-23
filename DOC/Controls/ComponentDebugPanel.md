# ComponentDebugPanel

## 作用
嵌入式单组件调试面板（UserControl），在 MainWindow 调试标签页中内嵌使用，也可被 ComponentEditorWindow 包装为独立窗口。

## 职责边界
- 负责：切换组件类型、编辑属性、预览渲染、绑定传感器、模拟数据驱动
- 不负责：多组件布局、配置文件持久化

## 依赖
- 四个渲染控件：`ProgressBarControl`, `CircularGaugeControl`, `DigitalDisplayControl`, `GridChartControl`
- `ColorPalettePopup` — 取色
- `SensorTreeSelector` — 传感器选择
- `HardwareService` — 实时数据
- `Views.HardwareSelectDialog` — 硬件选择
- 四个数据模型：`Component` 及其派生类

## 被谁使用
- `MainWindow`（调试标签页内嵌）
- `ComponentEditorWindow`（包装为独立窗口）

## 公开 API
| 成员 | 说明 |
|------|------|
| `StopTimers()` | 停止图表和数据轮询 Timer |

## 关键设计变更
- 从原 `ComponentEditorWindow` 提取内容为 `UserControl`，Window 仅作轻量包装
- Canvas 从 600×600 缩小为 480×480 以适配嵌入场景
- 移除 `SensorLabelComponent` 支持（调试面板仅支持四种主要组件）

## 示例
```xml
<!-- 内嵌使用 -->
<controls:ComponentDebugPanel x:Name="DebugPanel"/>

<!-- 独立窗口使用（ComponentEditorWindow） -->
<Window ...>
    <controls:ComponentDebugPanel x:Name="DebugPanel"/>
</Window>
```
