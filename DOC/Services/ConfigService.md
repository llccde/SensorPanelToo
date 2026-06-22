# ConfigService

## 作用
仪表盘配置文件的 JSON 读写服务。

## 职责边界
- 负责：DashboardConfig 与 .json 文件的序列化/反序列化，主题列表管理
- 不负责：配置校验、绑定合法性检查

## 依赖
- `System.Text.Json`（多态序列化）
- `Models.DashboardConfig`, `Models.Component` 及其派生类

## 被谁使用
- `MainWindow`（后续）—— 保存/加载/切换主题
- `SensorPanel`（后续）—— 加载配置并渲染

## 公开 API

| 成员 | 签名 | 说明 |
|------|------|------|
| `Save` | `static void Save(DashboardConfig, string path)` | 序列化并写入文件 |
| `Load` | `static DashboardConfig Load(string path)` | 读取文件并反序列化 |
| `ListThemes` | `static List<string> ListThemes()` | 枚举 Themes 目录下所有 .json 文件名 |
| `ThemesDirectory` | `static string` | Themes 目录路径（自动创建） |

## 关键设计决策
- 使用 `JsonNamingPolicy.CamelCase`，JSON 中属性名小写开头
- 序列化选项集中定义，保证读写一致
- `ThemesDirectory` 位于 `BaseDirectory/Themes/`，首次访问自动创建目录

## 示例
```csharp
var config = new DashboardConfig
{
    ThemeName = "Gaming",
    Components = { new ProgressBarComponent { X = 100, Y = 50 } }
};

var path = Path.Combine(ConfigService.ThemesDirectory, "gaming.json");
ConfigService.Save(config, path);

var loaded = ConfigService.Load(path);
var themes = ConfigService.ListThemes(); // ["gaming"]
```
