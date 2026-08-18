---
description: "Use when writing or modifying C# code in the KhayratAlhaj MAUI app. Covers MVVM with CommunityToolkit.Mvvm, DI registration, Shell navigation, audio/MediaElement cleanup, and SQLite data access."
applyTo: "KhayratAlhaj/**/*.cs"
---

# KhayratAlhaj C# / MVVM Conventions

## MVVM — CommunityToolkit.Mvvm (source generators only)

- ViewModels: `public partial class FooViewModel : ObservableObject` with `[ObservableProperty]` fields (`_camelCase` → `PascalCase` property) and `[RelayCommand]` methods.
- Dependent properties: `[NotifyPropertyChangedFor(nameof(FullProp))]`; expose `IsNotBusy => !IsBusy` rather than negating in XAML.
- Guard async commands with `if (IsBusy) return;` and `try/finally { IsBusy = false; }`.
- Don't hand-write `INotifyPropertyChanged` plumbing — the toolkit generates it.

## Dependency injection

Register everything in `MauiProgram.cs` (`AddSingleton` for services, `AddTransient` for pages/viewmodels) and resolve via constructor injection. Set `BindingContext` in the page constructor, **before or after `InitializeComponent()` consistently** — never in XAML.

## Shell navigation

Register routes with `Routing.RegisterRoute(nameof(DetailPage), typeof(DetailPage))`, navigate with `Shell.Current.GoToAsync($"{nameof(DetailPage)}?id={id}")`, receive via `[QueryProperty]` + `OnXxxChanged` partial method.

## Lifecycle & memory (this app had real bugs here)

- Always clean up `MediaElement` audio on page exit: pause, set `Source = null`, unsubscribe events — see `ContentDetailPage.xaml.cs` for the reference pattern.
- Unsubscribe event handlers in `OnDisappearing`; defer heavy work to `OnAppearing`, not the constructor.
- Use `WeakReferenceMessenger` (see `Messages/`) for cross-page communication (e.g. `ThemeChangedMessage`).

## Data access

- Database is `Resources/Data/appdata.bin`, opened through `Services/DatabaseService.cs` — extend it rather than opening new SQLite connections elsewhere.
- Boolean flags in SQLite are `INTEGER 0/1` (e.g. `HasAudioAr`) — map explicitly to `bool`.
- Audio files load via `FileSystem.OpenAppPackageFileAsync($"audio/{catId}_{subId}.ogg")`, copy to `FileSystem.CacheDirectory`, then play from the file path.

## General

- Async all the way (`async Task`, never `async void` except event handlers, never `.Result`/`.Wait()`).
- Arabic strings may appear in logs — use `Debug.WriteLine` with `[ClassName]` prefixes, matching existing style.
- Platform-specific code goes under `Platforms/` with partial classes, not `#if` sprinkled through shared code.
