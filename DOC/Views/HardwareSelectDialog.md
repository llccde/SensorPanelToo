# HardwareSelectDialog

## 作用
硬件选择启动对话框，独立自包含控件，可被任意窗口调用。

## 职责边界
- 负责：展示可选的硬件类型、调用 `HardwareService.Start()`、阻塞直到启动完成
- 不负责：后续的传感器数据使用

## 依赖
- `HardwareService`

## 被谁使用
- `ComponentEditorWindow`（工具栏 "Setup Hardware" 按钮）
- 任意需要启动硬件监控的地方

## 公开 API
构造函数，无公开属性。通过 `ShowDialog()` 调用，返回时服务已启动完毕。

## 关键设计决策
- 点 Start 后按钮变为 "Starting..."，主线程阻塞执行 `Computer.Open()`
- 完成后自动关闭（`DialogResult = true`）
- CPU/Memory/Storage 默认勾选，其余默认不勾选
- 异常时弹窗提示但不阻止关闭

## 示例
```csharp
new HardwareSelectDialog().ShowDialog();
// 到此处时 HardwareService 已启动完毕
```
