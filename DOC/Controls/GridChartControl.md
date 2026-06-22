# GridChartControl

## 作用
折线图/历史图组件的 WPF 渲染控件，通过 `OnRender` + `DrawingContext` 自绘网格、折线和填充区域。

## 职责边界
- 负责：根据配置和历史数据绘制折线图
- 不负责：数据采集、历史数据存储（由外部追加 `HistoryValues`）

## 依赖
- `GridChartComponent`（配置模型）
- `SensorValue`（数据模型，用于获取 Y 轴范围）

## 被谁使用
- `ComponentDemoWindow`（验证）
- `SensorPanelViewModel`（后续）—— 追加历史数据并触发刷新

## 公开 API

| 成员 | 类型 | 说明 |
|------|------|------|
| `ComponentData` | `GridChartComponent?` | 依赖属性，图表配置 |
| `SensorValue` | `SensorValue?` | 依赖属性，用于确定 Y 轴上下限 |

## 关键设计决策
- 使用 `OnRender` + `DrawingContext` 自绘，所有图形元素直接绘制到控件表面
- `ForegroundColor` → 图表区域（网格区域）填充色，`BackgroundColor` → 控件整体背景
- `SmoothFactor > 0` 时对每个点向前（历史方向）取 `SmoothFactor * 2` 秒的采样窗口，以固定 0.05s 间隔采样，线性插值后距离平方倒数加权求均值，直线段连接
- 填充区域为折线到 X 轴的闭合区域（`StreamGeometry`）
- Y 轴刻度标签基于 `SensorValue.UpperBound` 和 `LowerBound`
- 不持有定时器，由外部决定刷新频率
- 线程安全：调用方通过 `InvalidateVisual()` 触发重绘，WPF 内部调度到 UI 线程

## 示例
```xml
<controls:GridChartControl
    ComponentData="{Binding ChartConfig}"
    SensorValue="{Binding CpuLoad}" />
```

```csharp
// 追加数据点
chartCtrl.ComponentData.HistoryValues.Add((DateTime.Now, 45.2f));
chartCtrl.InvalidateVisual();
```
