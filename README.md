# QR Code Generator

A simple app to generate and customize QR codes, for Windows (WPF) and Android (.NET MAUI).

## Download

**[Download the latest version](https://github.com/Dorennor/VR.QrCodeGenerator/releases/latest)**:

- **Windows:** one `.exe` for Windows 10/11 (64-bit), nothing to install. The exe is not code-signed, so Windows SmartScreen may warn; click **More info → Run anyway**.
- **Android:** one `.apk` for Android 10 or later (64-bit). Open it on the phone and allow installing from unknown sources when asked.

## Features

- Generate QR codes from text or URLs
- Customize size, colors, and error correction level
- Save as PNG, JPG, or SVG
- Copy QR codes to clipboard
- Custom codes tab with fun styles, picked from a list. Kurkod 🐔 (a chicken, like the meme), Cat 🐱 and Dog 🐶. The code itself stays intact and every result is checked to scan
- Drawing tab ✏️: draw around the code with the mouse (pen size, eraser, undo) and get a QR code shaped like your drawing. Move and resize the code on the sheet. Closed shapes are filled with a QR-like texture, lines stay solid, and the result is checked to scan
- Picture tab 🖼️: choose a picture (or drop one) and it is drawn around the code in black and white dots (dithered), so photos keep their details. Drag and resize the code on the picture, blend it into the picture, and adjust brightness, contrast and detail. Every option explains itself when you hover over it
- Settings page: choose the output folder and logging options, applied without a restart
- Windows 11 style interface that follows the system light or dark theme
- Ships as a single portable `.exe`
- Android app with the same features, adapted to touch: draw with a finger, pinch to resize the code, share a picture into the app, codes saved to the gallery, Copy and Share

## Requirements

- Windows 10 or later (x64)
- Nothing else to install for the published `.exe`: the .NET runtime is bundled
- Android 10 or later (64-bit) for the Android app
- .NET 10 SDK to build from source
- .NET 11 SDK with the MAUI and Android workloads, the Android SDK and a JDK to build the Android app

## Files

- **QR codes:** the `Output` folder beside the `.exe`, or the folder chosen in Settings
- **Settings:** `%LOCALAPPDATA%\VR.QrCodeGenerator\settings.json`
- **Logs:** `%LOCALAPPDATA%\VR.QrCodeGenerator\Logs\` (Settings → Open logs folder)

## Build

```
dotnet publish VR.QrCodeGenerator.WPF -p:PublishProfile=FolderProfile
```

The single-file app is written to `VR.QrCodeGenerator.WPF\bin\Publish\QR code generator.exe`. Publishing from Visual Studio with `FolderProfile` gives the same result.

Android:

```
dotnet publish VR.QrCodeGenerator.MAUI -c Release -p:AndroidSdkDirectory="<sdk>" -p:JavaSdkDirectory="<jdk>"
```

The installable app is `VR.QrCodeGenerator.MAUIinRelease
et11.0-androidpublishm.vr.qrcodegenerator-Signed.apk`. Send it to the phone and open it (allow installing from unknown sources when asked).

## Documentation

See [the project context](Documentation/QR_Code_Generator_Project_Context.md) for the architecture, styling conventions, build steps and known issues.

---

Made for a friend. Feel free to use and modify! 🎯
