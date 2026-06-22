# GridChartComponent

## 作用
折线图/历史图组件的配置模型，继承 `Component`。

## 职责边界
- 负责：持有折线图的网格、线条、填充配置 + 历史数据队列
- 不负责：渲染逻辑（由 `GridChartControl` 负责）、数据采集

## 依赖
- `Component`（基类）

## 被谁使用
- `GridChartControl` —— 读取配置并渲染
- `SensorPanelViewModel`（后续）—— 追加历史数据

## 公开 API

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `DurationSeconds` | `int` | 60 | X 轴时间跨度（秒） |
| `GridDensityX` | `int` | 5 | 横向网格线数 |
| `GridDensityY` | `int` | 5 | 纵向网格线数 |
| `GridLineColor` | `string` | `"#E0E0E0"` | 网格线颜色 |
| `GridLineWidth` | `double` | 0.5 | 网格线宽 |
| `LineWidth` | `double` | 2 | 数据线宽 |
| `LineColor` | `string` | `"#0066CC"` | 数据线颜色 |
| `SmoothFactor` | `double` | 0 | 平滑度 |
| `ShowFill` | `bool` | `false` | 是否填充 |
| `FillOpacity` | `double` | 0.2 | 填充透明度 |
| `HistoryValues` | `List<(DateTime, float)>` | `new()` | 运行时历史（不序列化） |

### 基础尺寸
`BaseWidth = 400`, `BaseHeight = 200`（Scale=1.0 时）

## 关键设计决策
- 无边框/圆角（图表不需要矩形边框）
- `SmoothFactor > 0` 时使用 Catmull-Rom → 贝塞尔近似
- 历史数据不持久化到 JSON

## 示例
```csharp
var comp = new GridChartComponent
{
    DurationSeconds = 60, GridDensityX = 6, GridDensityY = 4,
    LineColor = "#FFD700", LineWidth = 2, ShowFill = true
};
comp.HistoryValues.Add((DateTime.Now, 45.2f));
```
