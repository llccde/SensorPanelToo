# ThemeSelector

## 作用
主题列表 + 预览组合控件，左侧列出已保存主题，右侧实时渲染选中主题的 Canvas 预览（含背景图）。

## 职责边界
- 负责：列出主题名、选中时加载 DashboardConfig 并渲染组件+背景图、暴露选中路径/名称、发出选中/双击事件
- 不负责：主题编辑、传感器数据轮询、渲染窗口管理

## 依赖
- `ConfigService` — 列出主题、加载 DashboardConfig
- 五个渲染控件 + 六个数据模型

## 被谁使用
- `MainWindow`（主题标签页内嵌）

## 公开 API

| 成员 | 说明 |
|------|------|
| `SelectedThemeName` | 当前选中的主题名称 |
| `SelectedThemePath` | 当前选中主题的完整文件路径 |
| `RefreshList()` | 刷新主题列表 |
| `SelectTheme(string name)` | 按名称选中主题 |
| `ThemeSelected` 事件 | 列表选中项变更（路由事件） |
| `ThemeDoubleClicked` 事件 | 列表双击（路由事件） |

## 关键设计决策
- 左侧 240px ListBox + 右侧 Viewbox Canvas（640×360）
- 选择时即时渲染预览（含背景色和背景图，无传感器轮询）
- 背景图片支持相对路径
- 双击事件用于父级快速操作（直接渲染）

## 示例
```xml
<controls:ThemeSelector x:Name="ThemeSelector"
                        ThemeSelected="OnThemeSelected"
                        ThemeDoubleClicked="OnThemeDoubleClicked"/>
```
