---
title: Data Directory
---

Gibbon Git Server keeps all of its data below one directory, configured with `Application:DataPath`.
The default is `App_Data` in the installation folder.

## Contents

| Path | Content |
|---|---|
| `Repositories` | The Git repositories |
| `Gibbon.Git.Server.db` | The SQLite database (users, teams, permissions, settings) |
| `DataProtection-Keys` | Keys for login cookies and form tokens |
| `Logs` | Daily log files `gibbon-yyyyMMdd.log` |
| `Git` | Optional Git download (Windows) |
| `.gitconfig` | Git configuration created by Git itself, because `HOME` points to the data directory |

## Moving the data outside the installation folder

Keeping the data separate from the program files makes updates simpler: the installation folder can be replaced completely.
Set the data directory in `appsettings.json`:

```json
"Application": {
  "DataPath": "C:\\ProgramData\\Gibbon"
}
```

or with the environment variable `Application__DataPath`, e.g. `/data` in a container.

- `Application:RepositoryPath`, `Application:LogPath`, `Git:HomePath`, `Git:BinaryPath` and a SQLite database without connection string follow `DataPath` unless they are set explicitly.
- Relative paths are resolved against the installation folder. Existing configurations with explicit paths keep working unchanged.
- The account running the server (e.g. the IIS application pool identity) needs write access to the data directory.
- Each installation needs its own data directory. Two instances sharing one directory would share database and keys.

## Log files

The server writes its log to `Logs/gibbon-yyyyMMdd.log` in the data directory, a new file every day.
This is useful for IIS and the Windows service, which have no console. The console output stays unchanged.

```json
"Application": {
  "LogToFile": true,
  "LogPath": "",
  "LogRetainedFileCount": 31
}
```

- `LogToFile`: set to `false` to write no log files, e.g. in a container that collects the console output.
- `LogPath`: a different directory for the log files. Empty means `Logs` in the data directory.
- `LogRetainedFileCount`: number of daily files to keep; older ones are deleted. `0` keeps all files.
- What is logged is controlled by the usual `Logging:LogLevel` section. The default logs warnings and errors.
- The diagnostics page (`/Home/Diagnostics`, administrators on the server itself) shows the end of the newest log file.

## Keys for login cookies

ASP.NET Core signs and encrypts the login cookie, the form tokens (CSRF protection) and temporary messages with keys stored in `DataProtection-Keys`.
They contain no user data; passwords, database and repositories do not depend on them.

- Keeping them in the data directory means users stay logged in when the IIS application pool recycles, the server restarts or a container is replaced.
- On Windows the keys are encrypted with DPAPI for the machine. On a new server they cannot be read, new keys are created and all users have to log in again.
- On Linux they are stored unencrypted; protect the data directory accordingly.
- Deleting the folder is safe: all users are logged out and open forms have to be reloaded.

## Updating from earlier versions

Nothing has to be changed. Earlier versions did not store these keys, so all users have to log in once after the update.
