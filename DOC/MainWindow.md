# MainWindow

## 作用
应用程序主控制台，左侧双标签页：主题管理 + 组件调试。

## 职责边界
- 负责：主题列表/预览（通过 ThemeSelector）、新建/编辑主题（启动 ThemeEditorWindow）、渲染主题（启动 RenderWindow）、组件调试（内嵌 ComponentDebugPanel）、硬件服务管理
- 不负责：主题详情编辑、组件渲染逻辑、传感器数据采集
- 限制：同时只允许一个渲染窗口

## 依赖
- `Controls.ThemeSelector` — 主题列表+预览
- `Controls.ComponentDebugPanel` — 嵌入式调试面板
- `Views.ThemeEditorWindow` — 独立编辑器窗口
- `Views.RenderWindow` — 独立渲染窗口
- `Views.HardwareSelectDialog` — 硬件选择
- `Services.ConfigService` — 主题持久化
- `Services.HardwareService` — 传感器数据
- `System.Windows.Forms.Screen` — 显示器枚举

## 被谁使用
- App.xaml（启动窗口）

## 公开 API
构造函数，无公开属性。

## 关键设计决策

### 双标签布局
- **主题页**：`ThemeSelector`（上）+ 底部工具栏（下）
  - 工具栏：新建 / 编辑 / | / 全屏 / 显示器 / 硬件服务 / 渲染
- **调试页**：`ComponentDebugPanel` 全区域嵌入

### 新建流程
输入名称 → 创建空 `DashboardConfig` → 保存 → 列表选中 → 打开编辑器

### 编辑流程
选中主题 → 以路径构造 `ThemeEditorWindow(path)` → 关闭刷新列表

### 渲染互斥
`_renderWindow` 跟踪，重复触发先 `Close()` 旧窗口

## 示例
```csharp
// App.xaml.cs 自动启动
// 双击主题列表项直接渲染
```
