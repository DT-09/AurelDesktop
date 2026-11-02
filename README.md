# AUREL Desktop

AUREL is an independent Windows desktop environment companion. It does not use Rainmeter, Rainmeter skins, or third-party widget hosts.

## First release

- Live horizontal desktop clock.
- Draggable, position-persistent widget.
- Three built-in palettes: Signature, Nocturne, and Ivory.
- Position lock and always-on-top controls.
- Settings saved locally in `%AppData%\AUREL\settings.json`.
- Right-click the widget to open its menu and exit the application.

## Signature design

The default AUREL Signature palette uses:
- Ebony and espresso surfaces.
- Warm ivory typography.
- Champagne borders and secondary text.
- Restrained teal accent.
- Times New Roman display typography.
- A translucent panel with approximately 50% background opacity.

The panel uses transparency, not backdrop blur. Windows desktop composition and other windows may affect the perceived result.

## Requirements

- Windows 10 or Windows 11.
- .NET 8 SDK to build from source.
- The GitHub Actions workflow builds a self-contained Windows x64 release artifact.

## Run from source

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

## Publish locally

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The executable is written under `bin/Release/net8.0-windows/win-x64/publish/`.

## Controls

- **Drag:** hold the left mouse button and move the widget.
- **Right-click:** themes, position lock, always-on-top, and exit.
- **Lock position:** prevents accidental movement.
- **Always on top:** keeps the clock above ordinary windows.

## Project direction

AUREL is being developed as a modular Windows desktop companion. Future modules may include calendar, system status, focus tools, and desktop presentation controls. Those are not claimed as implemented in this first release.
