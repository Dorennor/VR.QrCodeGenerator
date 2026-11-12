# QR Code Generator

A simple WPF desktop app to generate and customize QR codes.

## Features

- Generate QR codes from text or URLs
- Customize size, colors, and error correction level
- Save as PNG, JPG, or SVG
- Copy QR codes to clipboard
- Custom codes tab with fun styles, picked from a list. Kurkod 🐔 (a chicken, like the meme), Cat 🐱 and Dog 🐶. The code itself stays intact and every result is checked to scan
- Drawing tab ✏️: draw around the code with the mouse and get a QR code shaped like your drawing. Closed shapes are filled with a QR-like texture, lines stay solid, and the result is checked to scan
- Settings page: choose the output folder and logging options, applied without a restart
- Windows 11 style interface that follows the system light or dark theme
- Ships as a single portable `.exe`

## Requirements

- Windows 10 or later (x64)
- Nothing else to install for the published `.exe`: the .NET runtime is bundled
- .NET 10 SDK to build from source

## Files

- **QR codes:** the `Output` folder beside the `.exe`, or the folder chosen in Settings
- **Settings:** `%LOCALAPPDATA%\VR.QrCodeGenerator\settings.json`
- **Logs:** `%LOCALAPPDATA%\VR.QrCodeGenerator\Logs\` (Settings → Open logs folder)

## Build

```
dotnet publish VR.QrCodeGenerator.WPF -p:PublishProfile=FolderProfile
```

The single-file app is written to `VR.QrCodeGenerator.WPF\bin\Publish\QR code generator.exe`. Publishing from Visual Studio with `FolderProfile` gives the same result.

## Documentation

See [the project context](Documentation/QR_Code_Generator_Project_Context.md) for the architecture, styling conventions, build steps and known issues.

---

Made for a friend. Feel free to use and modify! 🎯
