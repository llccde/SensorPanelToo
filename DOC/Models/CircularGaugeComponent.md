# CircularGaugeComponent

## 作用
环形仪表组件的配置模型，继承 `Component`。

## 职责边界
- 负责：持有环形仪表的弧线、指针、环厚度等配置
- 不负责：渲染逻辑（由 `CircularGaugeControl` 负责）

## 依赖
- `Component`（基类）

## 被谁使用
- `CircularGaugeControl` —— 读取配置并渲染

## 公开 API

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SweepAngle` | `double` | 270 | 扇形角度 |
| `StartAngle` | `double` | -135 | 起始角度（度） |
| `GaugeStyle` | `GaugeStyle` | `Solid` | 实线/虚线 |
| `NeedleEnabled` | `bool` | `true` | 是否显示指针 |
| `NeedleColor` | `string` | `"#FF0000"` | 指针颜色 |
| `NeedleWidth` | `double` | 2.5 | 指针线宽 |
| `NeedleStyle` | `NeedleStyle` | `Metal` | 指针样式（预留） |
| `RingThickness` | `double` | 14 | 环厚度 |
| `TrackColor` | `string` | `"#DCDCDC"` | 轨道底色 |
| `HideTrack` | `bool` | `false` | 是否隐藏轨道 |
| `ShowCenterValue` | `bool` | `true` | 是否显示中心数值 |
| `TextColor` | `string` | `"#FFFFFF"` | 中心数值文字颜色 |

### 基础尺寸
`BaseSize = 200`（正方形，Scale=1.0 时 200×200）

## 关键设计决策
- 没有边框/圆角属性（环形不需要）
- 圆形组件，长宽相等
- `NeedleStyle` 枚举预留，当前版本统一绘制实线指针
- 轨道默认浅灰 `#DCDCDC`，可配置颜色和显隐

## 示例
```csharp
var comp = new CircularGaugeComponent
{
    ForegroundColor = "#FF6644",
    SweepAngle = 270, StartAngle = -135,
    RingThickness = 14, ShowCenterValue = true
};
```
