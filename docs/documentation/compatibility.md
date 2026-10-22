---
title: Compatibility
---

## Platforms

- Windows with IIS (ASP.NET Core 10 Hosting Bundle) or Kestrel.
- Linux with Kestrel, e.g. in a container.
- Git must be installed; Gibbon Git Server uses `git` from the `PATH` or the path in `Git:Executable`.

## Databases

- **SQLite** for simple setups.
- **SQL Server** by setting `Database:Provider` to `SqlServer` and a connection string.
- The database schema is migrated automatically on startup.

## Updating existing installations

- Existing `appsettings.json` files keep working. Explicitly configured paths (`RepositoryPath`, `Git:HomePath`, SQLite `Data Source`) keep their meaning; see [Data Directory](documentation/data-directory).
- Releases no longer contain an `appsettings.json`, only `appsettings.example.json`. Without a configuration file, or without `Database:Provider`, the server uses SQLite in the data directory. Earlier releases shipped an `appsettings.json` with the in-memory database; an installation that still ran with it had no persistent data before.
- Stored passwords stay valid.
- After updating to a version that stores the keys for login cookies in the data directory, all users have to log in once.

## Bonobo Git Server

Gibbon Git Server is a fork of Bonobo Git Server. A migration of the Bonobo database is not documented yet.
Existing repositories can be copied into the repositories folder; they are picked up when the server starts.
