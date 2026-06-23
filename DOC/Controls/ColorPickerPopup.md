# ColorPickerPopup

## 作用
HSV 色盘取色器 Popup，200×200 SV 平面 + 色相条 + HEX 输入，ESC 取消。

## 职责边界
- 负责：HSV 交互、HEX 解析、`ColorChanged`/`ColorSelected`/`ColorCancelled` 事件
- 不负责：取色后应用逻辑

## 依赖
- `System.Windows.Controls.Primitives.Popup`

## 被谁使用
- `ThemeEditorWindow` — 组件颜色 + 背景色
- `ComponentDebugPanel` — 组件颜色

## 公开 API

| 成员 | 说明 |
|------|------|
| `ColorChanged` | 拖拽实时触发，参数 HEX |
| `ColorSelected` | 点击「确定」触发 |
| `ColorCancelled` | 点击「取消」或 ESC 触发，恢复原始色 |
| `SetCurrentColor(string hex)` | 弹出前设初始色 |

## 关键设计
- SV 平面：白/黑渐变覆盖色相底色
- 色相条：红→黄→绿→青→蓝→品红→红 彩虹渐变
- `StaysOpen="True"`，仅按钮/ESC 关闭
- `OnOpened` 自动聚焦以接收键盘事件

## 示例
```csharp
var popup = new ColorPickerPopup();
popup.ColorChanged += hex => preview(hex);
popup.ColorSelected += hex => apply(hex);
popup.ColorCancelled += hex => revert(hex);
popup.SetCurrentColor("#FF6644");
popup.IsOpen = true;
```
