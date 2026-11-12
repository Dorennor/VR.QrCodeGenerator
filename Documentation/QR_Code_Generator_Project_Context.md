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
| `VR.QrCodeGenerator.Service` | `QrCodeGeneratorService` (the pipeline), `Generation/` (encoder, matrix helpers), `Styles/` (styles and shapes), `Rendering/` (PNG/JPG/SVG), `Verification/QrScanVerifier`, `LoggingService`, `SettingsService` (load/save settings.json), `AppPaths` (settings and logs folders), service interfaces, `OutputHelper` (output folder, save, clipboard), `ImageHelper` (bytes to bitmap), `UriHelper` (http/https check) |
| `VR.QrCodeGenerator.ViewModel` | `MainViewModel`: the UI state and the Generate, Copy, Save, Reset and Open folder commands. `CustomCodesViewModel`: the same plus a list of styles. `DrawingCodesViewModel`: the same with the user's drawing. `QrStyleOption`: a combobox entry. `SettingsViewModel`: the Settings page |
| `VR.QrCodeGenerator.WPF` | `App` (host, DI, crash handling), `MainWindow` (NavigationView), `Controls/QrGeneratorView` (the generator layout the three generator pages host), `Controls/DrawingCanvas`, `Views/GeneratorPage`, `Views/CustomCodesPage`, `Views/DrawingPage`, `Views/SettingsPage`, `ServiceProviderPageProvider`, `WpfDialogService`, `Controls/SettingsCard`, `Styles/AppStyles.xaml`, `Assets/AppIcon.ico`, publish profile |
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
- **PNG and JPG:** `RasterImageRenderer` (SkiaSharp). **SVG:** `SvgImageRenderer`, a true vector image with the same width and height as the other formats.
- **All formats render the same module matrix**, which is not always square, so the image keeps its aspect ratio (`ImageLayout.Fit`) and gets margin instead of being stretched.

### The generation pipeline (Service project)

`QrCodeGeneratorService.Generate` runs four steps, each replaceable on its own:

| Step | Code | Does |
| --- | --- | --- |
| 1. Encode | `Generation/QrMatrixEncoder` | QRCoder → `bool[,]` module matrix (true = dark), no quiet zone |
| 2. Style | `Styles/IQrStyle` | The matrix → the final drawing. `ClassicStyle` returns it as is |
| 3. Quiet zone | `Generation/ModuleMatrix.AddMargin` | 4 light modules around, when `DrawQuietZones` |
| 4. Render | `Rendering/IQrImageRenderer` | One renderer per `EnumImageFormat` |

`Generate(content, options, format)` takes the built-in style from `options.Style` via `QrStyles`. `Generate(content, options, IQrStyle style, format)` takes a style object made at runtime.

### Custom codes and shape styles

The **Custom codes** tab is the generator with a **Style** combobox (Kurkod, Cat, Dog). It has its own `CustomCodesViewModel`, so its content, options and preview are separate from the Generator tab.

All three animals are a `ShapeStyle` with an `IShape` (`Styles/Shapes`). A shape only answers "what goes at point (u, v)?", in code widths: (0, 0)–(1, 1) is the code itself. The possible parts are `None`, `Textured` (QR-like texture with a dark outline), `Solid`, or `Light` (with a dark outline). A shape also gives its margins and eye positions. `ShapeGeometry` has the hit tests (circle, ellipse, polygon, thick polyline, grid-snapped band). `ShapedCodeBuilder` does everything else:

- The QR code's own modules are never changed, so no error correction is spent.
- **Finder gap:** a 2-module light gap is kept beside the three finder patterns, where scanners look for them.
- **No fake finders:** the texture never forms a solid 3 x 3 dark block (the centre of a finder pattern). Random look-alikes once made a scanner lock onto the wrong spot.
- **Checked before use:** `QrScanVerifier` decodes every result with ZXing (TryHarder off) at 3, 5 and 8 px per module, because decoding depends on the pixel size. On failure the builder tries other textures, then a 3-module gap, then a full 4-module quiet zone. If nothing scans, the plain code is returned, so a drawing that does not scan is never returned. In a sweep of 120 contents x 3 animals x 5 pixel sizes (2 to 12 px), every image decoded, none fell back to the plain code, and generating took about 14 ms.
- **Stable texture:** it is seeded from a stable hash of the content, so the preview and the saved file are identical.
- **Small codes:** shapes are in fractions of the code's size, so short content (21 to 25 modules) gives a blockier animal with less readable faces than long content.

To add a style:

1. Add a value to `EnumQrStyle`.
2. Write an `IShape` in `Styles/Shapes`, like `CatShape`. For something that is not a shape around the code, write an `IQrStyle` instead.
3. Register `[EnumQrStyle.X] = new ShapeStyle(new XShape())` in `QrStyles`.
4. Add a `QrStyleOption` (name and description) to `CustomCodesViewModel.Styles`.

`ShapeStyleTests` picks up every value of `EnumQrStyle` automatically. It checks scanning (all error correction levels, formats, pixel sizes and many contents), size, determinism, registration, and that the style is offered on the page.

### Drawing tab

The **Drawing** tab is the generator with a canvas (`Controls/DrawingCanvas`) in place of the style picker. Pages put their own section above the options through `QrGeneratorView.TopContent`.

- **Canvas:** a 360 x 360 white sheet with WPF's `InkCanvas` and one black pen, 10 px, round, no pressure. **Undo** removes the last line, **Clear** removes them all. A dashed square in the centre shows where the code goes: `DrawingCodesViewModel.CodeFraction`, 40 % of the canvas.
- **To the view model:** after every change the strokes are rendered on white and turned into a `bool[,]` mask, one value per canvas pixel. It reaches `DrawingCodesViewModel.Drawing` through a `OneWayToSource` binding.
- **Generate:** `DrawingCodesViewModel.CreateStyle` (a `MainViewModel` hook) returns `new ShapeStyle(new MaskShape(copy of the mask, CodeFraction))`. `MainViewModel` keeps the style it generated with, so Save matches the preview even after the drawing changes. An empty canvas gives the plain code.
- **`MaskShape`:**
  - A module where the pen covers at least 30 % is `Solid`.
  - A module at least half inside an enclosed area is `Textured`. Enclosed means it cannot be reached from the canvas edge without crossing a line or the code's square, so a head drawn on top of the code fills too.
  - Before the flood fill, lines are closed morphologically (dilate, then erode, 6 px), so small slips in an outline still fill.
  - Coverage is read from summed-area tables, so each module costs the same whatever its size.
- **Scanning:** the drawing gets the same finder gaps, texture rules and scan check as the animals, so a drawing anywhere, even scribbles over the whole canvas, still gives a code that scans.
- **Line weight:** the pen is fixed in canvas pixels while modules shrink as content grows. Lines are about 2 modules thick for a typical link (29 modules) and get bolder for long content.

Tests: `DrawingTests` (filling, solid lines, gap closing, a head on the code, scanning, empty canvas, and the view model's Generate and Save).

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