# RenderWindow

## 作用
纯渲染窗口，从主题 JSON 文件加载配置，全屏或窗口化渲染所有仪表盘组件和背景图，轮询实时传感器数据。

## 职责边界
- 负责：加载 DashboardConfig、渲染背景色/背景图、创建并渲染组件控件、定时拉取硬件数据、全屏/窗口化、多显示器定位
- 不负责：组件编辑、属性修改、配置文件保存

## 依赖
- `ConfigService` — 加载主题配置
- `HardwareService` — 获取实时传感器数据
- 五个渲染控件 + 六个数据模型
- `System.Windows.Forms.Screen` — 显示器边界
- Win32 `SetWindowPos` — 跨显示器全屏定位

## 被谁使用
- `MainWindow`（"渲染"按钮 → `new RenderWindow(path, fullscreen, monitorIndex)`）

## 公开 API

| 成员 | 说明 |
|------|------|
| `RenderWindow(string configPath, bool fullscreen, int monitorIndex)` | 配置路径、是否全屏、目标显示器索引 |

## 关键设计决策
- 使用 `Loaded` 事件进行窗口布局（窗口句柄就绪后）
- 全屏定位使用 Win32 `SetWindowPos`
- Canvas 置于 Viewbox（`Stretch=Uniform`）自适应窗口
- 背景图片支持相对路径，从 exe 目录解析
- 传感器轮询 500ms，ESC 关闭
- 关闭时停止所有 Timer

## 示例
```csharp
var render = new RenderWindow("Themes/my_theme.json", fullscreen: true, monitorIndex: 1);
render.Show();
```
