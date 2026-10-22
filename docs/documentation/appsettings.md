---
title: Configuration Settings Documentation
---

Gibbon Git Server starts without any configuration file: it then uses SQLite in the [data directory](documentation/data-directory) and logs warnings only. To change settings, copy `appsettings.example.json` from the installation folder to `appsettings.json` and adjust it. The release archive contains only the example, so an update never overwrites your `appsettings.json`.

Every setting can also be given as an environment variable, with `__` instead of `:`, e.g. `Database__Provider=SqlServer`.

## Example

```json
{
  "Application": {
    "DataPath": "App_Data",
    "DemoModeActive": false
  },
  "Database": {
    "Provider": "Sqlite",
    "AllowMigration": true,
    "ConnectionStrings": {
      "Sqlite": "Data Source=App_Data/Gibbon.Git.Server.db",
      "SqlServer": "Server=.\\SQLEXPRESS;Database=Gibbon;Integrated Security=true;TrustServerCertificate=true"
    }
  },
  "Git": {
    "Executable": "git"
  },
  "Mail": {
    "Host": "",
    "Port": 587,
    "Name": "Gibbon Git Server",
    "Username": "",
    "Password": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.Hosting": "Information"
    }
  }
}
```

## `Application`

| Key | Default | Description |
|---|---|---|
| `DataPath` | `App_Data` | Folder for the database, repositories, keys and logs, relative to the installation folder. See [Data Directory](documentation/data-directory). |
| `RepositoryPath` | `<DataPath>/Repositories` | Folder of the repositories. |
| `DemoModeActive` | `false` | Demo mode: server settings and the administrator's own account cannot be changed. |
| `LogToFile` | `true` | Writes daily log files. |
| `LogPath` | `<DataPath>/Logs` | Folder of the log files. |
| `LogRetainedFileCount` | `31` | Number of daily log files to keep, `0` keeps all. |

## `Database`

| Key | Default | Description |
|---|---|---|
| `Provider` | `Sqlite` | `Sqlite` or `SqlServer`. `Memory` keeps everything in memory and is meant for tests. |
| `AllowMigration` | `true` | Creates and migrates the database on startup. |
| `ConnectionStrings:Sqlite` | `<DataPath>/Gibbon.Git.Server.db` | Connection string for SQLite. A relative `Data Source` is relative to the installation folder. |
| `ConnectionStrings:SqlServer` | | Connection string for SQL Server. |
| `ConnectionStrings:Default` | | Used when there is no connection string named like the provider. |

## `Git`

| Key | Default | Description |
|---|---|---|
| `Executable` | `git` | The git command line tool, from the `PATH` or as a full path. |
| `HomePath` | `<DataPath>` | `HOME` of the git processes. |

## `Mail`

Needed for password reset emails.

| Key | Default | Description |
|---|---|---|
| `Host` | | SMTP server. |
| `Port` | | SMTP port, e.g. `587`. |
| `Name` | | Sender name. |
| `Username` | | SMTP user, also used as sender address. |
| `Password` | | SMTP password. |

Keep credentials out of files that are shared; environment variables such as `Mail__Password` work as well.

## `Logging`

The standard ASP.NET Core log levels. Without a `Logging` section only warnings are written, plus the start messages of the server.
