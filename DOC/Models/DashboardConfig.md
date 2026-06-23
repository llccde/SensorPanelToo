# DashboardConfig

## 作用
仪表盘配置文件的根模型，包含画布属性、背景图设置和组件列表。

## 职责边界
- 负责：持有主题名、画布尺寸、背景色/背景图、组件列表
- 不负责：渲染、数据采集、绑定验证

## 依赖
- `Component` 及其派生类

## 被谁使用
- `ConfigService` — JSON 序列化/反序列化
- `ThemeEditorWindow` — 保存/加载主题
- `RenderWindow` — 渲染主题
- `ThemeSelector` — 预览主题

## 公开 API

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Version` | `string` | `"1.0"` | 配置文件版本号 |
| `ThemeName` | `string` | `"Default"` | 主题名称 |
| `CanvasWidth` | `double` | `1280` | 画布宽度 |
| `CanvasHeight` | `double` | `720` | 画布高度 |
| `BackgroundColor` | `string` | `"#0A0A0F"` | 画布背景色（HEX） |
| `BackgroundImagePath` | `string` | `""` | 背景图片路径（支持相对路径 exe → themeImg/） |
| `BackgroundImageScale` | `double` | `1.0` | 背景图片缩放 |
| `BackgroundImageOffsetX` | `double` | `0` | 背景图片 X 偏移 |
| `BackgroundImageOffsetY` | `double` | `0` | 背景图片 Y 偏移 |
| `Components` | `List<Component>` | `new()` | 组件列表 |

## 关键设计决策
- 颜色使用 string/HEX 格式，GUID 使用 `Guid` 类型，确保 JSON 可读性
- `Components` 使用 `List<Component>` 基类类型，`[JsonDerivedType]` 实现多态反序列化
- 背景图片绘制在 ZIndex=-100 层（背景色之上、组件之下）
- 图片路径支持相对路径，自动从 exe 目录解析

## 示例
```json
{
  "version": "1.0",
  "themeName": "Gaming",
  "canvasWidth": 1280,
  "canvasHeight": 720,
  "backgroundColor": "#0A0A0F",
  "backgroundImagePath": "themeImg/bg_20250101.jpg",
  "backgroundImageScale": 1.0,
  "backgroundImageOffsetX": 0,
  "backgroundImageOffsetY": 0,
  "components": [
    { "$type": "ProgressBar", "x": 100, "y": 50 },
    { "$type": "CircularGauge", "x": 300, "y": 50 }
  ]
}
```
