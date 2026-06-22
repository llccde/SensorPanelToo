# ProgressBarComponent

## 作用
进度条组件的配置模型，继承 `Component`。

## 职责边界
- 负责：持有进度条特有的样式、颜色、边框、圆角配置
- 不负责：渲染逻辑（由 `ProgressBarControl` 负责）

## 依赖
- `Component`（基类）

## 被谁使用
- `ProgressBarControl` —— 读取配置并渲染
- `ConfigService` —— 序列化/反序列化

## 公开 API

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TextColor` | `string` | `"#FFFFFF"` | 数值文字颜色 |
| `Orientation` | `Orientation` | `Horizontal` | 方向 |
| `ShowValueText` | `bool` | `true` | 是否显示数值文本 |
| `ValueTextPosition` | `ValueTextPosition` | `Inside` | 文本位置 |
| `BorderThickness` | `double` | 0 | 边框厚度 |
| `BorderColor` | `string` | `"#CCCCCC"` | 边框颜色 |
| `Roundness` | `double` | 0 | 圆角半径 |
| (继承) `ProgressColor` | `string` | — | 已废弃，改用 ForegroundColor |
| (继承) `TrackColor` | `string` | — | 已废弃，改用 BackgroundColor |

### 基础尺寸
`BaseWidth = 300`, `BaseHeight = 40`（Scale=1.0 时）

## 关键设计决策
- 边框/圆角从基类移至此处（环形仪表不需要这些属性）
- `ForegroundColor`（基类）= 进度条填充色，`BackgroundColor`（基类）= 轨道色，`TextColor` = 文字色，三者独立
- 固定基础尺寸 300×40，通过 `RenderTransform` 缩放
- `ProgressColor` / `TrackColor` 保留字段不再使用，以保持反序列化兼容

## 示例
```csharp
var comp = new ProgressBarComponent
{
    X = 100, Y = 50, Scale = 1.0,
    ProgressColor = "#00AA44", TrackColor = "#E0E0E0",
    BorderThickness = 1, BorderColor = "#CCC", Roundness = 4
};
```
