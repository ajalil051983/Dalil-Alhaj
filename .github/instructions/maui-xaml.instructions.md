---
description: "Use when writing or modifying .NET MAUI XAML pages. Covers compiled bindings, layout controls, performance-safe controls, and this app's RTL/theming conventions."
applyTo: "**/*.xaml"
---

# MAUI XAML Best Practices

## Compiled bindings are required

Always set `x:DataType` — on the page **and** inside every `DataTemplate`:

```xml
<ContentPage x:Class="KhayratAlhaj.Pages.MyPage"
             x:DataType="vm:MyViewModel"
             xmlns:vm="clr-namespace:KhayratAlhaj.ViewModels">

<DataTemplate x:DataType="models:DhikrItem">
    <Label Text="{Binding Name}" />
</DataTemplate>
```

Bind to a parent ViewModel command from a template with
`{Binding Source={RelativeSource AncestorType={x:Type vm:MyViewModel}}, Path=MyCommand}`.

## Controls

- **Never use** `ListView`, `TableView`, or `Frame` (deprecated/performance).
- Lists > ~20 items → `CollectionView` (with `EmptyView`). Small static lists → `BindableLayout` on a `VerticalStackLayout`.
- Cards/containers → `Border` with `StrokeShape="RoundRectangle 8"`.
- Root layout → `Grid` with explicit `RowDefinitions="Auto,*,Auto"`. Put `ScrollView` inside a `Grid` row, never as root.
- Images: prefer `.png` assets; set `Aspect` explicitly.

## Binding performance

- `Mode=OneTime` for static data (`{Binding Id, Mode=OneTime}`).
- Don't bind compile-time constants — write `Text="Title"`, not `{Binding Title}` when it never changes.
- Use `DataTrigger` (not code-behind) for simple visual states like `IsEnabled`/`Opacity`.

## App-specific conventions

- UI is Arabic-first **RTL**: don't hard-code `FlowDirection`; test layouts in RTL.
- Use theme colors from `ThemeColors.cs` / resource dictionaries — no hard-coded hex in pages; the app supports theme switching via `ThemeChangedMessage`.
- Keep page code-behind thin: layout + lifecycle only, logic belongs in ViewModels/Services.
