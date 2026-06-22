# DashboardConfig

## 作用
仪表盘配置文件的根模型，包含一组组件和画布属性。

## 职责边界
- 负责：持有整个仪表盘的主题名、画布尺寸、背景色和组件列表
- 不负责：渲染、数据采集、绑定验证

## 依赖
- `Component` 及其派生类

## 被谁使用
- `ConfigService` —— 序列化/反序列化
- `SensorPanelViewModel`（后续）—— 加载并驱动渲染

## 公开 API

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Version` | `string` | `"1.0"` | 配置文件版本号 |
| `ThemeName` | `string` | `"Default"` | 主题名称 |
| `CanvasWidth` | `double` | `1280` | 画布宽度 |
| `CanvasHeight` | `double` | `720` | 画布高度 |
| `BackgroundColor` | `string` | `"#0A0A0F"` | 画布背景色 |
| `Components` | `List<Component>` | `new()` | 组件列表 |

## 关键设计决策
- 颜色使用 string/HEX 格式，GUID 使用 `Guid` 类型，确保 JSON 可读性
- `Components` 使用 `List<Component>` 基类类型，`[JsonDerivedType]` 实现多态反序列化
- 组件按 ZIndex 排序渲染，ZIndex 小的先绘制

## 示例
```json
{
  "version": "1.0",
  "themeName": "Gaming",
  "canvasWidth": 1280,
  "canvasHeight": 720,
  "backgroundColor": "#0A0A0F",
  "components": [
    { "$type": "ProgressBar", "x": 100, "y": 50, ... },
    { "$type": "CircularGauge", "x": 300, "y": 50, ... }
  ]
}
```
