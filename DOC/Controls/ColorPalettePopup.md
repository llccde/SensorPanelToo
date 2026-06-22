# ColorPalettePopup

## 作用
取色板弹出控件，继承 `Popup`，提供预设调色盘 + HEX 输入。

## 职责边界
- 负责：显示颜色网格、接收用户选择、输出 HEX 字符串
- 不负责：取色后的应用逻辑（由父窗口处理 `ColorSelected` 事件）

## 依赖
- `System.Windows.Controls.Primitives.Popup`

## 被谁使用
- `ComponentEditorWindow` —— 颜色输入框获得焦点时弹出

## 公开 API

| 成员 | 签名 | 说明 |
|------|------|------|
| `ColorSelected` | `event Action<string>?` | 用户选择颜色时触发，参数为 HEX 字符串（含 #） |
| `SetCurrentColor(string hex)` | `void` | 弹出前设置当前颜色到 HEX 框和预览块 |

## 关键设计决策
- 继承 `Popup`（非 `UserControl`），`StaysOpen="True"`，由父窗口手动管理开关
- 90 种预设颜色分 10 列网格排列（含灰度系）
- 底部 HEX 输入框支持回车提交
- 点击颜色 → 触发 `ColorSelected` → 关闭 Popup

## 示例
```csharp
var popup = new ColorPalettePopup();
popup.ColorSelected += hex => targetBox.Text = hex;
popup.SetCurrentColor("#FF6644");
popup.PlacementTarget = targetBox;
popup.IsOpen = true;
```
