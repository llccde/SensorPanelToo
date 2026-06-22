# BoolToVisibilityConverter

## 作用
WPF 值转换器，将 bool 值转换为 Visibility 枚举，支持反转模式。

## 职责边界
- 负责：bool ↔ Visibility 转换
- 不负责：其他类型转换

## 依赖
- `System.Windows.Data.IValueConverter`

## 被谁使用
- 所有 XAML 中需要根据 bool 控制可见性的 Binding

## 公开 API

| 成员 | 签名 | 说明 |
|------|------|------|
| `Convert` | `object Convert(object, Type, object, CultureInfo)` | true→Visible, false→Collapsed；参数 "Invert" 反转 |
| `ConvertBack` | `object ConvertBack(object, Type, object, CultureInfo)` | Visible→true, 其他→false |

## 示例
```xml
<TextBlock Visibility="{Binding ShowValueText, Converter={StaticResource BoolToVisibility}}" />
<TextBlock Visibility="{Binding ShowValueText, Converter={StaticResource BoolToVisibility}, ConverterParameter=Invert}" />
```
