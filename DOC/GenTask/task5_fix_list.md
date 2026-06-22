## 修复记录

### 1. Checkbox 即时生效 ✅
- 所有 CheckBox 添加 `Checked="CheckBox_Changed" Unchecked="CheckBox_Changed"` 事件
- `ComponentEditorWindow.xaml.cs` 新增 `CheckBox_Changed` handler，调用 `ApplyAndRefresh()`
- 涉及：PropTransparent, PropShowValueText, PropNeedleEnabled, PropShowCenterValue, PropShowSuffix, PropShowFill

### 2. 图形前景色和文字颜色区分 ✅
- `ProgressBarComponent` 新增 `TextColor` 属性（默认 #FFFFFF），`ProgressBarControl` 使用此颜色渲染文字
- `CircularGaugeComponent` 新增 `TextColor` 属性（默认 #FFFFFF），`CircularGaugeControl` 中心文字使用此颜色
- 编辑界面新增对应颜色选择器

### 3. 进度条圆角 ✅
- `ProgressBarControl.xaml` 重构：外层包裹 `Border`（`OuterBorder`），设置 `CornerRadius`
- `ProgressBarControl.ApplyComponentData()` 将 `Roundness` 应用到 `OuterBorder.CornerRadius`

### 4. 进度条背景/轨道和前景/填充合并 ✅
- 移除 `ProgressColor` 和 `TrackColor` 的独立编辑器
- 轨道颜色 = `BackgroundColor`（透明背景时隐藏）
- 填充颜色 = `ForegroundColor`
- 控件直接使用 `comp.ForegroundColor` 和 `comp.BackgroundColor`

### 5. 进度条边框改为外边框 ✅
- `ProgressBarControl.xaml` 中 `Border`（`OuterBorder`）包裹整个 `Grid`
- 边框厚度和颜色应用到 `OuterBorder`，不再应用到 `ProgressBar` 自身

### 6. 进度条字号不生效 ✅
- `ApplyToEditor` → `ApplyAndRefresh` 新增 `SyncCurrentControl()`，强制控件从模型重新读取所有属性
- `ProgressBarControl.ApplyComponentData()` 改为 `public`，新增 `RefreshValue()`（公开），确保所有属性实时同步

### 7. 进度条大部分属性无效 ✅
- 同上 `SyncCurrentControl()` 机制解决。控件的 `ApplyComponentData()` / `Refresh()` / `InvalidateVisual()` 在每次编辑后强制触发
- 新增 `Orientation` 支持：垂直模式应用 `RotateTransform(-90)`

### 8. 网格前景色 = 网格区域填充色 ✅
- `GridChartControl.DrawChart()` 新增：用 `ForegroundColor` 填充图表区域（chartLeft→chartRight, chartTop→chartBottom）
- `BackgroundColor` 仍作为控件整体背景

### 9. 数字显示描边 → 实际描边 ✅
- `DigitalDisplayControl` 完全重写，改用 `OnRender` + `FormattedText`
- 描边效果：在多个方向偏移绘制 outline（步数根据 `StrokeThickness` 自适应），再覆盖主文字
- 不再是单方向阴影，而是真正的文字轮廓
