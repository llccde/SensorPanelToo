# ComponentEditorWindow

## 作用
单组件调试独立窗口，包装 `ComponentDebugPanel` 为独立窗体。

## 职责边界
- 负责：提供独立窗口容器、转发 `StopTimers()` 调用
- 不负责：组件编辑逻辑（由 `ComponentDebugPanel` 负责）

## 依赖
- `Controls/ComponentDebugPanel` — 内嵌的调试面板 UserControl

## 被谁使用
- `MainWindow`（调试标签页 → 也可通过菜单打开独立窗口）
- 可单独启动用于快速调试

## 公开 API
构造函数，无公开属性。

## 关键设计变更（genTask6）
- 从原 230 行代码精简为 16 行，所有编辑逻辑移至 `ComponentDebugPanel`
- 仅负责窗口生命周期：构造时 `InitializeComponent()`，关闭时 `DebugPanel.StopTimers()`

## 示例
```csharp
new ComponentEditorWindow().Show();
```
