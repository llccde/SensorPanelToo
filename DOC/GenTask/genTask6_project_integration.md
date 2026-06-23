# genTask6 — Project Integration & Console Redesign

## 一、需求

1. 创建纯渲染窗口（RenderWindow），加载已编辑完成的主题，全屏/窗口化渲染
2. 主窗口（MainWindow）改为控制面板，统一管理：呼出编辑器、渲染、单组件调试、启动硬件服务
3. 渲染窗口支持全屏/窗口模式，多显示器环境下可选择目标显示器
4. 界面中文化
5. 将编辑、调试、渲染分为标签页，侧边竖排切换，平面化设计
6. 渲染页为默认首页，内置主题列表
7. 编辑器自动加载选中主题，保存直接覆盖原文件，"另存为"仅输入名称（不暴露路径）
8. 编辑页新增"新建"按钮 → 输入名称 → 创建空主题 → 打开编辑器
9. 同时只允许一个渲染窗口，重复启动时关闭旧窗口再创建
10. JSON 文件与用户隔离，只以主题名暴露

## 二、理解

### 架构层次

```
App.xaml → MainWindow（控制台）
              ├── 渲染标签页：主题列表 + 渲染选项 → RenderWindow（独立窗口）
              ├── 编辑标签页：主题列表 + 新建/编辑 → ThemeEditorWindow（独立窗口，传入路径）
              └── 调试标签页：内嵌 ComponentDebugPanel
```

### 关键设计

- **组件调试面板提取**：从 `ComponentEditorWindow` 提取核心内容为 `ComponentDebugPanel`（UserControl），既可内嵌于 MainWindow 调试页，也可被 `ComponentEditorWindow` 包装为独立窗口
- **编辑器路径感知**：`ThemeEditorWindow(string? themePath)` 构造时加载主题，保存时覆盖原路径，另存为时写入 `Themes/{name}.json`
- **渲染互斥**：`_renderWindow` 字段跟踪，重复触发先 `Close()` 旧窗口
- **侧边标签**：Button 模拟竖排标签页，`IsEnabled=false` 表示选中态，平面化无阴影
- **多显示器全屏**：Win32 `SetWindowPos` 定位，`Loaded` 事件触发（保证窗口句柄就绪）
- **双列表同步**：渲染页和编辑页各有一个 `ListBox`，通过 `_syncing` 标志双向同步选中项

### WinForms 冲突处理

项目同时启用 `UseWPF` 和 `UseWindowsForms`（为了 `Screen.AllScreens`），在 `.csproj` 中移除 `System.Windows.Forms` 和 `System.Drawing` 的隐式全局 using，避免 `UserControl`/`Color`/`Point` 等类型歧义。

## 三、对话记录摘要

| 轮次 | 内容 |
|------|------|
| 1 | 初始实现：RenderWindow + MainWindow 控制台（双栏布局） |
| 2 | 修复 FullscreenCheck_Changed 初始化阶段 NPE |
| 3 | 修复非主屏幕全屏无反应：OnInitialized → Loaded 事件，Win32 SetWindowPos 定位 |
| 4 | 界面中文化，更新 project.md，新增文档 DOC/MainWindow.md, DOC/Views/RenderWindow.md |
| 5 | 标签页改版 + 渲染互斥：三标签页（编辑/调试/渲染），`_renderWindow` 跟踪 |
| 6 | 提取 ComponentDebugPanel UserControl，侧边标签（渲染首页），嵌入编辑/调试面板 |
| 7 | 主题列表移到渲染页，编辑器路径感知（构造/保存/另存为），编辑页新建按钮 |

## 四、涉及的文件

### 新增文件

| 文件 | 说明 |
|------|------|
| `Views/RenderWindow.xaml` | 纯渲染窗口 XAML（Viewbox + Canvas，全屏/窗口化） |
| `Views/RenderWindow.xaml.cs` | 渲染窗口逻辑：加载配置、创建控件、轮询硬件数据、Win32 定位 |
| `Controls/ComponentDebugPanel.xaml` | 嵌入式组件调试面板 UserControl |
| `Controls/ComponentDebugPanel.xaml.cs` | 调试面板逻辑：类型切换、属性编辑、传感器绑定 |
| `DOC/MainWindow.md` | 主控制台文档 |
| `DOC/Views/RenderWindow.md` | 渲染窗口文档 |
| `DOC/Controls/ComponentDebugPanel.md` | 调试面板文档 |
| `DOC/GenTask/genTask6_project_integration.md` | 本文件 |

### 修改文件

| 文件 | 改动说明 |
|------|----------|
| `MainWindow.xaml` | 完全重写：侧边竖排标签页（渲染/编辑/调试），平面化风格 |
| `MainWindow.xaml.cs` | 重写：双 ListBox 同步、新建/编辑/渲染逻辑、`ShowInputDialog`、渲染互斥 |
| `Views/ThemeEditorWindow.xaml` | 工具栏改为「保存」「另存为」「加载」+ 主题名标签 |
| `Views/ThemeEditorWindow.xaml.cs` | 构造函数传路径、保存/另存为/加载全部改用主题名（屏蔽文件路径）、`LoadConfig` 提取、`ShowInputDialog` |
| `Views/ComponentEditorWindow.xaml` | 精简为仅包装 `<controls:ComponentDebugPanel>` |
| `Views/ComponentEditorWindow.xaml.cs` | 从 230 行精简到 16 行，仅转发 `StopTimers()` |
| `App.xaml` | `StartupUri` 改为 `MainWindow.xaml` |
| `SensorPanelToo.csproj` | 添加 `UseWindowsForms`，移除 `System.Windows.Forms` 和 `System.Drawing` 隐式 using |
| `DOC/project.md` | 更新项目结构、当前进度、设计决策 |
| `DOC/Views/ThemeEditorWindow.md` | 更新保存/加载机制说明 |
| `DOC/Views/ComponentEditorWindow.md` | 更新为轻量包装说明 |

### 未修改文件

| 文件 | 原因 |
|------|------|
| `Controls/ProgressBarControl` 等 5 个渲染控件 | 渲染逻辑未变 |
| `Services/HardwareService.cs` | 硬件服务接口未变 |
| `Services/ConfigService.cs` | JSON 读写逻辑未变 |
| `Models/*.cs` | 数据模型未变 |
| `Controls/SensorTreeSelector.xaml/.cs` | 传感器树未变 |
| `Controls/ColorPalettePopup.xaml/.cs` | 取色器未变 |
| `Converters/BoolToVisibilityConverter.cs` | 转换器未变 |
| `Views/HardwareSelectDialog.xaml/.cs` | 硬件选择对话框未变 |
| `SensorPanelToo.Tests/*` | 测试未变 |

## 五、构建与测试

```
dotnet build   → 0 错误, 2 警告（均为 SensorLabelControl.cs 预存的 CS8600）
```
