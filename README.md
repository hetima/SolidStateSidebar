<h1>Solid State Sidebar</h1>

A simple sidebar for Windows desktop that displays hardware diagnostic information.

### Download

Go to the <a href="https://github.com/hetima/SolidStateSidebar/releases">releases tab</a>.

### New Features (This fork)
- Customizable font family for the display
- Aligned labels for improved readability (in English)
- Removed graph window
- Updated to the latest LibreHardwareMonitor
- Reduced excutable size by cleaning up unnecessary DLLs


### Features
- Monitors CPU, RAM, GPU, network, and logical drives.
- Allows for lots of customization.
- Allows alerts for various values.
- Allows binding hotkeys.
- Supports monitors of all DPI types.
- Has a clock at the top.


### Install
If you have the original version installed, you can migrate your settings; however, the configuration file may be deleted when uninstalling the previous version. Please back up the file at the following path before uninstalling, and restore it to its original location once the uninstallation is complete.

```
C:\Users\YOURNAME\AppData\Local\SidebarDiagnostics\settings.json
```

Before uninstalling, it is recommended to disable "Run At Startup" to avoid potential issues.

This application does not include an installer. Simply extract and place it in any folder of your choice. The recommended location is:

```
C:\Users\YOURNAME\AppData\Local\Programs\SidebarDiagnostics
```

The .NET 10 Desktop Runtime is required to run this application. If it is not installed, install it from [here](https://dotnet.microsoft.com/download/dotnet/10.0) or run the following command:

```
winget install Microsoft.DotNet.DesktopRuntime.10
```

[PawnIO](https://pawnio.eu/) is required to retrieve all hardware information. Install from official site or run the following command:

```
winget install namazso.PawnIO
```

### Important

If you are changing your screen's DPI settings, <a href="https://github.com/ArcadeRenegade/SidebarDiagnostics/wiki/DPI-Settings">view this page!</a>


### Supported OS

- Windows 10?/11

### License

GNU General Public License v3.0


### Info

Written in C# .NET10 WPF.

You will need to run it as administrator.

This repository is a modified version of the following original by [ArcadeRenegade/SidebarDiagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics)

It also references the following repository [thewriteway/SidebarDiagnostics](https://github.com/thewriteway/SidebarDiagnostics)

Data provided by [Libre Hardware Monitor]("https://github.com/LibreHardwareMonitor/LibreHardwareMonitor").

<img src="https://i.imgur.com/3It1JlA.jpeg" />
