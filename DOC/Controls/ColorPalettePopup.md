# ColorPalettePopup

## 作用
预设调色盘取色器 Popup，90 色网格 + HEX 输入。已被 `ColorPickerPopup` 替代为编辑器默认取色器，保留作为快速取色备选。

## 职责边界
- 负责：显示颜色网格、接收用户选择、输出 HEX 字符串
- 不负责：取色后的应用逻辑

## 当前状态
- **已被替代**：`ThemeEditorWindow` 和 `ComponentDebugPanel` 已改用 `ColorPickerPopup`（HSV 色盘）
- **仍保留**：作为可用的预设取色器组件，可在其他场景使用

## 示例
```csharp
var popup = new ColorPalettePopup();
popup.ColorSelected += hex => targetBox.Text = hex;
popup.PlacementTarget = targetBox;
popup.IsOpen = true;
```
