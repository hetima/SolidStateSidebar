<h1>Solid State Sidebar</h1>

A simple sidebar for Windows desktop that displays hardware diagnostic information.

This is currently under development. Please use the [SidebarDiagnostics fork](https://github.com/hetima/SidebarDiagnostics) until it is completed.

### Download

Coming soon...


### Features
- Monitors CPU, RAM, GPU, network, and logical drives.
- Allows for lots of customization.


### Install

If you have the SidebarDiagnostics installed, you can migrate your settings; however, the configuration file may be deleted when uninstalling the previous version. Please back up the file at the following path before uninstalling.

```
C:\Users\YOURNAME\AppData\Local\SidebarDiagnostics\settings.json
```
Before uninstalling, it is recommended to disable "Run At Startup" to avoid potential issues.

Start Solid State Sidebar once, exit the application, and then move it to the location below

```
C:\Users\YOURNAME\AppData\Local\SolidStateSidebar\settings.json
```

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

### Supported OS

- Windows 10?/11

### License

GNU General Public License v3.0


### Info

Written in C# .NET10 WPF.

You will need to run it as administrator.

This doftware is based on [SidebarDiagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics)

Data provided by [Libre Hardware Monitor]("https://github.com/LibreHardwareMonitor/LibreHardwareMonitor").

<img src="https://i.imgur.com/umREcnW.jpeg" />
