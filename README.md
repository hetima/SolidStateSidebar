<h1>Solid State Sidebar</h1>

A simple sidebar for Windows desktop that displays hardware diagnostic information.

This is currently under development.

### Download

Download from the <a href="https://github.com/hetima/SolidStateSidebar/releases">releases page</a>.


### Features
- Monitors CPU, RAM, GPU, network, and logical drives
- Window monitor to list and select preferred application windows
- Claude and Codex usage monitor
- Allows for lots of customization.


### Install

This application does not include an installer. Simply extract and place it in any folder of your choice. The recommended location is:

```
C:\Users\YOURNAME\AppData\Local\Programs\SolidStateSidebar
```

The .NET 10 Desktop Runtime is required to run this application. If it is not installed, install it from [here](https://dotnet.microsoft.com/download/dotnet/10.0) or run the following command:

```
winget install Microsoft.DotNet.DesktopRuntime.10
```

[PawnIO](https://pawnio.eu/) is required to retrieve all hardware information. Install from official site or run the following command:

```
winget install namazso.PawnIO
```

You can create your own icon themes. Just place a set of SVG files in the `C:\Users\<YOURNAME>\AppData\Local\SolidStateSidebar\IconThemes` folder. Please refer to the `Default` folder there for the file names.

### Supported OS

- Windows 10?/11

### License

GNU General Public License v3.0


### Info

Written in C#, .NET10, WPF, built with Visual Studio 2026.

This software is based on [SidebarDiagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics)

Data provided by [Libre Hardware Monitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).

![Screenshot](https://raw.githubusercontent.com/hetima/SolidStateSidebar/main/assets/sss01.jpg)

![Screenshot](https://raw.githubusercontent.com/hetima/SolidStateSidebar/main/assets/sss02.jpg)

![Screenshot](https://raw.githubusercontent.com/hetima/SolidStateSidebar/main/assets/sss03.jpg)


