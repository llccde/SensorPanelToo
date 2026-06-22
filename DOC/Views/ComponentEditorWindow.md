# ComponentEditorWindow

## 作用
单组件编辑与预览窗口，左侧实时渲染，右侧属性编辑。

## 职责边界
- 负责：切换组件类型、编辑属性、预览渲染、绑定传感器
- 不负责：多组件布局（由后续 SensorPanel 负责）、配置文件持久化

## 依赖
- 四个渲染控件：`ProgressBarControl`, `CircularGaugeControl`, `DigitalDisplayControl`, `GridChartControl`
- `ColorPalettePopup` —— 取色
- `SensorTreeSelector` —— 传感器选择
- `HardwareService` —— 实时数据
- 六个数据模型：`Component` 及其派生类

## 公开 API
构造函数，无公开属性。

## 关键设计决策
- 打开时自动 `HardwareService.Instance.Start()`，关闭时 `Stop()`
- 所有 Slider+TextBox 通过 `Tag` 配对，使用通用 `SliderVC`/`BoxKD` handler
- 颜色输入框 `GotFocus` 弹出 `ColorPalettePopup`
- `Apply` 按钮一次性提交所有编辑并重绘，Slider 拖动实时预览
- Scale 通过 `RenderTransform` 实现图像级缩放
- 渲染 Canvas 固定 600×600，组件居中放置

## 示例
```csharp
new ComponentEditorWindow().Show();
```
