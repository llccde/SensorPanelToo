# DigitalDisplayComponent

## 作用
数字显示组件的配置模型，继承 `Component`。

## 职责边界
- 负责：持有数字显示的格式、描边配置
- 不负责：渲染逻辑（由 `DigitalDisplayControl` 负责）

## 依赖
- `Component`（基类）

## 被谁使用
- `DigitalDisplayControl` —— 读取配置并渲染

## 公开 API

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ShowPrefix` | `bool` | `false` | 是否显示单位前缀 |
| `ShowSuffix` | `bool` | `true` | 是否显示单位后缀 |
| `DecimalPlaces` | `int` | 1 | 小数位数 |
| `StrokeColor` | `string` | `"#CCCCCC"` | 文字描边颜色 |
| `StrokeThickness` | `double` | 1 | 文字描边厚度 |
| `FontWeight` | `FontWeightOption` | `Bold` | Normal / Bold |

### 基础尺寸
`BaseWidth = 250`, `BaseHeight = 80`（Scale=1.0 时）

## 关键设计决策
- 描边效果通过 `FormattedText.BuildGeometry` + `DrawGeometry` 实现，先画描边再画填充
- 固定基础尺寸 250×80

## 示例
```csharp
var comp = new DigitalDisplayComponent
{
    ForegroundColor = "#0066CC",
    StrokeColor = "#CCC", StrokeThickness = 1,
    ShowSuffix = true, DecimalPlaces = 1
};
```
