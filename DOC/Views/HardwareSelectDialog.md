# HardwareSelectDialog

## 作用
硬件监控选择启动对话框，勾选要监控的硬件类型后启动 `HardwareService`。

## 职责边界
- 负责：展示可选硬件类型、预填当前监控状态、调用 `HardwareService.Start()`、阻塞直到启动完成
- 不负责：后续的传感器数据采集

## 依赖
- `HardwareService` — 通过 `IsCpuEnabled` 等属性读取当前状态、调用 `Start()`

## 被谁使用
- `MainWindow`（"硬件服务"按钮）
- `ThemeEditorWindow`（工具栏 "Setup Hardware" 按钮）
- `ComponentDebugPanel`（"硬件"按钮）
- 任意需要启动硬件监控的地方

## 公开 API
构造函数，无公开属性。通过 `ShowDialog()` 调用。

## 关键设计决策
- **状态感知**：`LoadCurrentState()` 在构造时根据 `HardwareService.IsRunning` 决定初始勾选：
  - 服务运行中 → 从 `IsCpuEnabled` / `IsGpuEnabled` 等属性读取当前监控状态
  - 服务未启动 → 仅默认勾选 CPU 和内存
- 点击"启动"后按钮变为"启动中…"，主线程阻塞执行 `Computer.Open()`
- 点击"跳过"使用默认参数启动（CPU + 内存）
- 异常时弹窗提示但不阻止关闭
- `Topmost="True"` + `WindowStyle="ToolWindow"` 确保在所有窗口之上

## 示例
```csharp
new HardwareSelectDialog().ShowDialog();
// 到此处时 HardwareService 已启动完毕
```
