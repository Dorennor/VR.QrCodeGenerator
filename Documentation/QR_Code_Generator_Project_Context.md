# QR Code Generator - Project Context

Updated 2026-11-01. This is the handoff document for a developer or AI assistant picking up the project. The code is authoritative if it differs from this document.

## What it is

A small portable Windows desktop tool that turns text or a URL into a QR code. The user enters content, adjusts the appearance, clicks Generate, then copies the image or saves it. It was made for a friend, so keep it simple: one window, no accounts, no network access.

## Tech stack

- .NET 10, WPF (`net10.0-windows`). All projects have nullable reference types disabled.
- [WPF-UI](https://github.com/lepoco/wpfui) 4.2.0 for the Fluent window, controls and themes.
- Extended.Wpf.Toolkit 5.0.0, only for `xctk:ColorCanvas`.
- QRCoder for the QR matrix, SkiaSharp for the final PNG and JPG images.
- CommunityToolkit.Mvvm for `[ObservableProperty]` and `[RelayCommand]`.
- Microsoft.Extensions.Hosting for DI, System.Text.Json for the settings file, Serilog for logging.
- NUnit and ZXing.Net in the tests. ZXing reads generated images back, so tests prove the codes scan.

## Solution layout

Dependencies run in one direction: WPF -> ViewModel -> Service -> Model.

| Project | Contents |
| --- | --- |
| `VR.QrCodeGenerator.Model` | `QrCodeOptions`, `EnumContentType` (Uri, Text), `EnumImageFormat` (Png, Jpg, Svg), settings classes |
| `VR.QrCodeGenerator.Service` | `QrCodeGeneratorService`, `LoggingService`, `SettingsService` (load/save settings.json), `AppPaths` (settings and logs folders), service interfaces, `OutputHelper` (output folder, save, clipboard), `ImageHelper` (bytes to bitmap), `UriHelper` (http/https check) |
| `VR.QrCodeGenerator.ViewModel` | `MainViewModel`: the UI state and the Generate, Copy, Save, Reset and Open folder commands. `SettingsViewModel`: the Settings page |
| `VR.QrCodeGenerator.WPF` | `App` (host, DI, crash handling), `MainWindow` (NavigationView), `Views/GeneratorPage`, `Views/SettingsPage`, `ServiceProviderPageProvider`, `WpfDialogService`, `Controls/SettingsCard`, `Styles/AppStyles.xaml`, `Assets/AppIcon.ico`, publish profile |
| `VR.QrCodeGenerator.Tests` | Service tests (formats, scanning, size limits) and view model tests (command states, error paths) |

## How generating works

1. Generate is available once the content is not blank (button or Enter in the text box).
2. `MainViewModel.GenerateQrCode` trims the content and requires an http or https URL when the content type is Uri. Text is not validated.
3. `QrCodeGeneratorService.Generate` renders the code in the requested format and returns the bytes. It never touches the disk.
4. The view model shows the PNG through the bound `QrCodeImage`, remembers the content and options, and saves a file in the selected format. **Every Generate writes a file.**

Output details:

- **Two sizing modes:**
  - **Height and Width (default):** the image has exactly that size. The code is scaled with nearest neighbour sampling, which keeps edges sharp, but squares can differ by a pixel when the size does not divide evenly. A QR code must stay square to scan, so if the two values differ the code is the largest centered square and the rest is background. An image with fewer pixels than the code has modules is refused with `QrCodeTooSmallException`.
  - **Exact pixel size:** `QrCodeOptions.PixelsPerModule` is set, so every square is exactly that many pixels and no scaling happens. The image size is modules x pixels, so it depends on the content, and Height and Width are ignored and disabled. The status line reports the resulting size.
- **PNG and JPG:** drawn by QRCoder, then composed and encoded by SkiaSharp.
- **SVG:** a true vector image built from the QR matrix, with the same width and height as the other formats.
- **All formats render the same module matrix** (`QrCodeGeneratorService.BuildModules`), which is not always square, so the image keeps its aspect ratio and gets margin instead of being stretched.

### Kurkod (chicken-shaped QR codes)

`StyleSettings.ChickenShape` (Settings page, off by default) sets `QrCodeOptions.ChickenShape`. `KurkodBuilder` then draws a chicken around the code: textured head, neck, tail and rounded body with a dark outline, solid beak, wattle, comb and legs, and an eye (a dark pupil in a light square).

- The QR code's own modules are never changed, so no error correction is spent.
- Scanners need light space beside the three finder patterns, so the drawing keeps a gap of 2 modules there (3 if needed). Each result is decoded with ZXing (`TryHarder` off) before it is used. If none scans, the fallback keeps a full 4-module quiet zone around the code, which is standard and always scans. In tests, 300 varied contents all scanned with the smallest gap.
- The texture is seeded with a stable hash of the content, so the preview and the saved file are identical.
- Shapes are defined in fractions of the code's size, so small codes (21 modules) get a blockier chicken than long ones.

Save and Copy use what the preview shows, so settings changed since Generate do not affect them until the next Generate. Save does use the currently selected format. Copy always uses the in-memory PNG, whatever format was saved.

Command availability lives in the view model (`CanExecute`), not in code-behind.

## Files on disk

| What | Where |
| --- | --- |
| Generated QR codes | `Output/` beside the executable, or the custom folder from settings, named `yyyyMMdd_HHmmss_<guid>.<ext>` |
| Settings | `%LOCALAPPDATA%\VR.QrCodeGenerator\settings.json`, written by the Settings page; defaults apply until then |
| Logs | `%LOCALAPPDATA%\VR.QrCodeGenerator\Logs\`: a daily Serilog file, plus `<app>_catastrophic.log` for unhandled exceptions |

Settings and logs live in the user profile (`AppPaths`), so the published app is one .exe and nothing else is written beside it except the default `Output/` folder. That folder uses `AppContext.BaseDirectory`, so it works from a shortcut with any "Start in" folder. In a protected location such as Program Files, saving to the default output folder shows an error; choose a custom folder in Settings.

## Settings

The Settings page (bottom of the navigation pane) edits `settings.json`. Save writes the file and applies the values to the running app at once: the shared `Settings` instance is updated and `ILoggingService.ApplySettings` rebuilds the logger. Without the file the defaults apply: file logging on, the default output folder. An `appsettings.json` beside the exe from an older version is read until settings are saved once. The file has two sections:

- `LoggingSettings`: `File` and `Console` enable those log sinks. `Debug` sets the minimum level to Debug (otherwise Information); it does not enable a debug sink.
- `PathsSettings`: with `UseCustomPath` true and `OutputPath` set, files go to that folder, which is created if missing.

## Error handling

- Expected problems (invalid URL, content too long, size too small) show a warning with what to do.
- Generate, Save, Copy and Open folder catch failures, log them and show an error dialog.
- Anything unexpected on the UI thread reaches `App`'s `DispatcherUnhandledException` handler, which writes the catastrophic log, shows a message and keeps the app open.

## UI and styling

The look comes from the sibling project `VR.ModdingTools` (`UniversalBackupTool.UI`) and follows the Windows 11 Settings app. Keep the two projects visually consistent.

- `MainWindow` is a `ui:FluentWindow` with a Mica backdrop and a `ui:TitleBar`.
- `App.xaml` merges `ui:ThemesDictionary`, `ui:ControlsDictionary` and `Styles/AppStyles.xaml`. `App.OnStartup` applies the Windows theme and `MainWindow` follows later changes through `SystemThemeWatcher`. There is no in-app theme setting.
- The layout is a page title, then two columns: content and preview on the left, option cards on the right. A status line beside Copy and Reset confirms the last action.
- Option rows use `controls:SettingsCard` (icon, header, description, control). `IsStacked="True"` places the control below the text for wide inputs. Rows are grouped under `SectionHeaderTextStyle` headers.
- `AppStyles.xaml` is a subset of the ModdingTools file with the same key names. The differences are a tighter `SectionHeaderTextStyle` margin and `SettingsCard` minimum height and icon margin, plus the additions `ColorSwatchBorderStyle` and `FlyoutBorderStyle`.

Conventions:

- Text must use an `AppStyles.xaml` text style or a theme brush as its `Foreground`. The inherited default is black, which is unreadable in dark mode.
- Use `{DynamicResource ...}` for theme brushes so they update when the theme changes.
- Prefer `ui:` controls. The exception is `xctk:ColorCanvas`, because WPF-UI has no color picker. It sits directly in the flyout rather than using `xctk:ColorPicker`, which would open a second, unthemed popup.
- The `.csproj` excludes a folder named `Style\` from compilation. Shared styles belong in `Styles\`.

## Build, test and publish

```
dotnet build VR.QrCodeGenerator.slnx
dotnet test VR.QrCodeGenerator.Tests
```

`Copy_AfterSavingSvg_PutsImageOnClipboard` overwrites the real Windows clipboard, so running the tests replaces whatever is on it.

Run the app with F5 in Visual Studio or by starting the built .exe.

`Properties/PublishProfiles/FolderProfile.pubxml` creates a self-contained, single-file win-x64 build in `VR.QrCodeGenerator.WPF\bin\Publish\`: just `QR code generator.exe` (about 70 MB, compressed). Native DLLs are bundled (`IncludeNativeLibrariesForSelfExtract`), `.pdb` files are dropped and only English satellite resources are kept. Keep `PublishDir` relative: Visual Studio's publish page resolves `$(MSBuildProjectDirectory)` to the solution folder, which breaks its Navigate link.

```
dotnet publish VR.QrCodeGenerator.WPF -p:PublishProfile=FolderProfile
```

The assembly is named `QR code generator`, so the executable is `QR code generator.exe`.

## Settled decisions

These were the owner's choices on 2026-11-01. Do not change them without asking.

- **Every Generate saves a file**, as well as showing the preview. Save writes another copy, which is how to get a second format.
- **Save does not ask for a name or location.** Files go to the output folder with generated names.

## Known issues

- **Low contrast is not checked.** Similar or inverted colors can produce a code that will not scan.
- **Clicking a color swatch while its popup is open does nothing.** OK or a click elsewhere closes it.
- **Unused code:** `IDialogService.OpenFileDialog` and `SaveFileDialog` (which also ignores its `defaultFileName` argument), the `Serilog.Sinks.Debug` package, and the `Style\` exclusions in the WPF `.csproj`.
- **Dialogs are standard `MessageBox` windows** and do not follow the theme (ModdingTools does the same).
- **`ImageHelper` and `OutputHelper` use WPF types** in the Service layer, so that layer is tied to WPF.