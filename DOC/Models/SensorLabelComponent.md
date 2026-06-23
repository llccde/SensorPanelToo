# SensorLabelComponent

## 作用
传感器标签组件的配置模型，继承 `Component`，用于显示绑定的传感器路径名称。

## 职责边界
- 负责：持有标签的格式、层级数量、描边配置
- 不负责：渲染逻辑（由 `SensorLabelControl` 负责）

## 依赖
- `Component`（基类）

## 被谁使用
- `SensorLabelControl` —— 读取配置并渲染

## 公开 API

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ShowPrefix` | `bool` | `false` | 是否显示 `[ ` 前缀 |
| `ShowSuffix` | `bool` | `true` | 是否显示 ` ]` 后缀 |
| `HierarchyLevels` | `int` | 3 | 显示的路径层级数量（从末尾算起，传感器索引为第0层） |
| `StrokeColor` | `string` | `"#CCCCCC"` | 文字描边颜色 |
| `StrokeThickness` | `double` | 1 | 文字描边厚度 |
| `FontWeight` | `FontWeightOption` | `Bold` | Normal / Bold |

### 基础尺寸
`BaseWidth = 300`, `BaseHeight = 60`（Scale=1.0 时）

## 关键设计决策
- 层级从末尾计数：`BindingId` 按 `/` 分割后，最后一段为第 0 层（传感器索引），倒数第二段为第 1 层（传感器类型），以此类推
- `HierarchyLevels = 0` 表示仅显示最后一段（传感器索引）
- 描边效果使用 `FormattedText.BuildGeometry` + `DrawGeometry`（与 DigitalDisplay 一致）
- 无传感器绑定时显示 `---`

## 示例
```csharp
var comp = new SensorLabelComponent
{
    ForegroundColor = "#00CC88",
    StrokeColor = "#CCC", StrokeThickness = 1,
    HierarchyLevels = 2,
    ShowPrefix = true, ShowSuffix = false
};
// BindingId = "/intelcpu/0/load/0" → 显示 "[ load/0"
```
