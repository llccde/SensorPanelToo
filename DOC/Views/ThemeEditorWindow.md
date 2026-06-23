# ThemeEditorWindow

## 作用
多组件界面主题编辑器。画布上添加/编辑/排列传感器组件，支持背景图/背景色编辑（作为可选中实体），以主题名称保存/加载。

## 职责边界
- 负责：组件 CRUD、属性编辑、拖拽定位、背景图/背景色编辑、主题保存/加载/另存为、传感器绑定
- 不负责：组件渲染逻辑、硬件监控

## 依赖
- 数据模型：`Component` 及 5 派生类、`DashboardConfig`
- 渲染控件：5 个组件控件 + `SensorTreeSelector` + `ColorPickerPopup`
- 服务：`HardwareService`、`ConfigService`
- 视图：`HardwareSelectDialog`

## 被谁使用
- `MainWindow`（"编辑"按钮 → `new ThemeEditorWindow(path)`）

## 公开 API

| 成员 | 说明 |
|------|------|
| `ThemeEditorWindow(string? themePath = null)` | 传路径则自动加载 |

## 关键设计决策

### 主题路径感知
- 构造函数传入路径自动加载
- 保存：有路径直接覆盖 → 弹窗"保存完成"
- 另存为：输入名称 → `Themes/{name}.json`
- 加载：列表对话框，仅显示主题名

### 颜色编辑
- 每个颜色输入框左侧 16×16 色块，实时反映当前色
- 点击色块/聚焦输入框 → `ColorPickerPopup`（HSV 色盘）
- 拖拽选色实时生效，确定/取消/ESC 关闭
- 背景色独立处理：色块 + TextChanged 联动画布

### 背景图（选中式编辑）
- 背景作为"伪组件"可选中：点击画布上图片区域或工具栏「背景图」
- 选中后在右侧显示属性面板（与组件属性互斥）
- 属性：背景色、图片路径、缩放（平方映射 0.01~36x）、水平/垂直偏移
- 快捷按钮：原比例、宽适应、高适应、水平居中、垂直居中
- 「存至软件目录」→ `themeImg/bg_时间戳.ext`，已在内则忽略
- 图片路径在 exe 子目录下自动转相对路径
- 画布 Image 层 ZIndex=-100

### 画布操作
- Zoom 滑块（`LayoutTransform` 缩放，ScrollViewer 联动）
- Ctrl+滚轮缩放、拖拽平移、拖拽组件

## 示例
```csharp
new ThemeEditorWindow().Show();
new ThemeEditorWindow("Themes/my_theme.json").Show();
```
