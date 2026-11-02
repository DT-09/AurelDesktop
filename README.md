# AUREL Desktop

AUREL is a standalone Windows desktop companion. It does not require Rainmeter or a third-party widget host.

## Included in this build

- Translucent horizontal desktop clock with live time, date and ISO week.
- Three built-in palettes: Signature, Nocturne and Ivory.
- Display typography choices: Times New Roman, Georgia, Garamond, Palatino Linotype and Cambria. These use installed Windows font families; AUREL does not redistribute commercial font files.
- Panel opacity presets: 35%, 50%, 65% and 80%.
- Optional live CPU and RAM status using Windows system APIs.
- AUREL cursor scheme generator and installer, with a saved backup and restore command for the prior Windows cursor settings.
- Draggable widget, saved position, position lock and always-on-top setting.
- Settings stored locally in `%AppData%\AUREL\settings.json`.

## Design

The Signature palette uses ebony and espresso surfaces, warm ivory typography, champagne borders and a restrained teal accent. The default panel opacity is 50%. The panel uses transparency, not backdrop blur.

## Cursor scheme

Right-click the widget, choose **Cursor scheme → Install AUREL cursor scheme**. AUREL creates its cursor files under `%AppData%\AUREL\Cursors`, saves the existing cursor registry values once, applies the selected scheme to the current Windows user, and asks Windows to reload its cursors.

Choose **Restore previous cursor scheme** to restore the saved values. The cursor files must remain in that folder while the scheme is active. If you remove AUREL manually, restore the previous scheme first.

## Requirements

- Windows 10 or Windows 11, x64.
- No separate .NET installation is required for the self-contained GitHub Actions release.
- .NET 8 SDK is required to build from source.

## Run from source

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

## Publish locally

```powershell
dotnet publish AurelDesktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Controls

- Left-drag the widget to move it.
- Right-click for themes, typography, transparency, system status, cursor installation/restoration, position lock, always-on-top and exit.

## Not included yet

Calendar, weather, notifications, taskbar replacement, Start-menu replacement, lock-screen changes, and a full widget marketplace are not part of this build. AUREL does not claim to replace Windows Explorer or Windows shell components.
