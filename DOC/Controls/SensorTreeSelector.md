# SensorTreeSelector

## 作用
可复用的传感器树形选择控件，从 `HardwareService` 获取传感器树并展示为三级 TreeView。

## 职责边界
- 负责：展示硬件→传感器树，支持搜索过滤，输出选中传感器的 BindingId
- 不负责：硬件数据采集（由 HardwareService 负责）、绑定验证

## 依赖
- `HardwareService.GetSensorTree()`
- `Models.SensorTreeNode`

## 被谁使用
- `ComponentEditorWindow` —— 弹出选择传感器绑定
- 后续 `BindingSelectorDialog` 可复用

## 公开 API

| 成员 | 签名 | 说明 |
|------|------|------|
| `SelectedBindingId` | `string?` (DP) | 当前选中的传感器 BindingId，支持双向绑定 |
| `SelectionChanged` | `event Action<string?>?` | 选中变化时触发 |
| `PopulateTree()` | `void` | 刷新传感器树（需在 HardwareService 启动后调用） |

## 关键设计决策
- 搜索框按叶子节点名称过滤，匹配时显示为平铺列表（隐藏层级）
- `SensorTreeItem` 实现 `INotifyPropertyChanged`，支持 TreeView 展开/选中绑定
- 选中叶子节点时自动向上展开父节点

## 示例
```xml
<controls:SensorTreeSelector x:Name="TreeSelector"
    SelectionChanged="OnSensorSelected" />
```
