---
title: Prerequisites
---
### Prerequisites for Windows

To successfully run Gibbon Git Server on Windows, you need to meet the following requirements:

#### 1. Operating System

- **Windows Server** (recommended): Windows Server 2016 or higher.
- **Windows 10/11**: If using a development or small-scale setup, a modern desktop version of Windows is also sufficient.

#### 2. .NET Core Runtime

Gibbon Git Server is built using **ASP.NET Core**, so you need the appropriate runtime installed.

- Download and install the **ASP.NET Core Runtime 10** from the [official .NET download page](https://dotnet.microsoft.com/download/dotnet/10.0). For IIS, install the **Hosting Bundle**, which contains the runtime.
- You can check the installed runtimes by opening a terminal and running:
  ```bash
  dotnet --list-runtimes
  ```

You should see `Microsoft.AspNetCore.App 10.x.x` if the runtime is properly installed.

#### 3a. IIS (Internet Information Services)

If you want to host Gibbon Git Server on IIS (Internet Information Services), follow these steps:

- Open **Server Manager** on your Windows Server machine.
- Go to **Manage > Add Roles and Features**.
- Select **Web Server (IIS)** and proceed with the installation, making sure the following features are enabled:
    
    - **Web Server** > **Common HTTP Features** (Static Content, Default Document, etc.)
    - **Management Tools** > **IIS Management Console**
    
    After installing IIS, download and install the **ASP.NET Core 10 Hosting Bundle** from the [official .NET download page](https://dotnet.microsoft.com/download/dotnet/10.0). It adds the IIS module for ASP.NET Core; ASP.NET 4.x is not needed.

#### 3b. Kestrel (Optional)

For lightweight or cross-platform hosting, Gibbon Git Server can also be hosted with **Kestrel**, a web server that runs natively in .NET Core. Kestrel comes built-in with ASP.NET Core, so no extra installation is required if you're using it instead of IIS.

- By default, Gibbon Git Server will use Kestrel if no IIS is configured.
- Kestrel is optimal for scenarios where you don't need a full IIS setup or for testing/development purposes.

#### 4. Git Command-Line Tools

Ensure the **Git command-line tools** are installed on the machine for proper Git operations (clone, push, pull, etc.).

- Download the latest version of **Git for Windows** from [git-scm.com](https://git-scm.com/).
- After installation, verify Git is installed by running the following command in the terminal:

```
git --version
```

- You should see something like `git version 2.x.x`.
- Gibbon Git Server uses `git` from the `PATH`. To use another installation, set `Git:Executable` in `appsettings.json`.

#### 5. Database

Gibbon Git Server requires a database for storing configuration and user data. Use **SQLite** for a simple setup (set `Database:Provider` to `Sqlite`; the shipped configuration still uses an in-memory database). If you're using SQLite:

- No additional setup is required for SQLite as it is embedded.

If you want to use **SQL Server**, you need an active SQL Server instance; set `Database:Provider` to `SqlServer` and adjust the connection string in `appsettings.json` accordingly.

---

Once all prerequisites are installed, you can proceed with the [installation steps](documentation/installation) to set up and run Gibbon Git Server on your Windows machine.