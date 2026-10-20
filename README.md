# Gibbon Git Server

[![GitHub Pages](https://github.com/Code-iX/Gibbon-Git-Server/actions/workflows/jekyll-gh-pages.yml/badge.svg)](https://github.com/Code-iX/Gibbon-Git-Server/actions/workflows/jekyll-gh-pages.yml)

**Gibbon Git Server** is a self-hosted Git server built with **ASP.NET Core**. This fork is currently under heavy development, and branch squashing will occur soon. Feel free to explore, but please refrain from forking for development purposes at this time.

For further information and documentation, please visit the [official website](https://code-ix.github.io/Gibbon-Git-Server/).

This is a fork of the original [Bonobo Git Server](https://github.com/jakubgarfield/Bonobo-Git-Server), which was last 
updated in 2019. The original project was built on .NET Framework, which is now outdated. This fork aims to modernize 
the project by migrating it to ASP.NET Core 8.0. The goal is to provide a more efficient, secure, and cross-platform Git 
server solution. You can find the key changes [below](#key-changes-to-bonobo-git-server).

---

## Features

- Web-based interface for managing Git repositories and users.
- Full Git support (clone, push, pull, etc.).
- Enhanced performance through .NET 8 optimizations.
- Open-source under the **MIT license**.
- Fully cross-platform thanks to .NET Core.

## Prerequisites

- **ASP.NET Core Runtime** (version 8 or higher).
- Windows Server with IIS or use **Kestrel** for cross-platform hosting.
- `git` command-line tool for Git operations.

## Installation Steps

1. **Clone the repository**:
   <code>git clone https://github.com/Code-iX/Gibbon-Git-Server.git</code>

2. **Publish the application** using the .NET Core CLI:
   <code>dotnet publish -c Release -o ./publish</code>

3. **Deploy** the published files on IIS (Windows) or any web server (e.g., Kestrel for Linux/macOS).

4. **Database Setup**:
   - Update the connection string in <code>appsettings.json</code> for your preferred database (SQLite is default).
   - Run Entity Framework migrations (optional):
     <code>dotnet ef database update</code>

5. **Run the Application**:
   - Launch the app via IIS or directly using:
     <code>dotnet Gibbon-Git-Server.dll</code>

6. **Access the Web Interface** at <code>http://localhost:5000</code> (or your configured port).

## Configuration

- Update **`appsettings.json`** to adjust application settings.
- The default database is SQLite, but you can switch to another by updating the connection string.

### Data directory

All data (SQLite database, repositories, keys for login cookies) is stored below `Application:DataPath`, by default `App_Data` in the installation folder.
To keep the data separate from the program files, point it to another folder, either in `appsettings.json` or with the environment variable `Application__DataPath`:

```json
"Application": {
  "DataPath": "C:\\ProgramData\\Gibbon"
}
```

- `Application:RepositoryPath`, `Git:HomePath` and a SQLite database without a connection string follow `DataPath` unless they are set explicitly.
- Relative paths are resolved against the installation folder, so existing configurations keep working.
- The account running the server needs write access to the data directory.
  
## Update

Before each update please read carefully the information about **compatibility issues** between your version and the latest one in [changelog](https://code-ix.github.io/Gibbon-Git-Server/changelog).

- **Backup** your data.
- **Stop the server**.
- Delete all the files in the installation folder **except App_Data**.
- Copy the files from the downloaded archive to the server location.
- **Start the server**.

## Changelog

Stay updated with the latest changes by visiting the [Changelog](https://code-ix.github.io/Gibbon-Git-Server/changelog) on our website.  
You can also view the legacy Bonobo Git Server changelog [here](https://github.com/Code-iX/Gibbon-Git-Server/blob/40135135d314ef25b9f82a9e5664366bc6995c4d/changelog.md).

## Contributing

We welcome contributions! If you plan to make significant changes, please open an issue first to discuss your ideas. Pull requests are always appreciated. For more details, check out our [Contributing Guidelines](./CONTRIBUTING.md).

## License

This project is licensed under the MIT License.

## Still have questions?

If you have any further questions, feel free to check out [Questions](https://code-ix.github.io/Gibbon-Git-Server/questions) on our website.

## Key changes to Bonobo Git Server

- Migrated from .NET Framework to **ASP.NET Core 8**.
- Removed support for **Active Directory (AD) login**.
- Dropped **SQL Server** support in favor of **Entity Framework migrations**.
- Updated the **UI** to a more modern look and feel.
