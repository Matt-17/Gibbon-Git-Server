# Gibbon Git Server

[![CI](https://github.com/Matt-17/Gibbon-Git-Server/actions/workflows/pr-test.yml/badge.svg?branch=master)](https://github.com/Matt-17/Gibbon-Git-Server/actions/workflows/pr-test.yml)
[![GitHub Pages](https://github.com/Matt-17/Gibbon-Git-Server/actions/workflows/jekyll-gh-pages.yml/badge.svg)](https://github.com/Matt-17/Gibbon-Git-Server/actions/workflows/jekyll-gh-pages.yml)
[![Release](https://img.shields.io/github/v/release/Matt-17/Gibbon-Git-Server?include_prereleases)](https://github.com/Matt-17/Gibbon-Git-Server/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/github/license/Matt-17/Gibbon-Git-Server)](LICENSE.md)
[![Conderis](https://conderis.com/Matt-17/Gibbon-Git-Server/badge.svg?token=XBpjWsWO7DZ9vo2O5IX9zQ)](https://conderis.com)
[![Docker](https://img.shields.io/badge/docker-coming%20soon-lightgrey)](https://matt-17.github.io/Gibbon-Git-Server/roadmap)
[![Coverage](https://img.shields.io/badge/coverage-pending-lightgrey)](https://github.com/Matt-17/Gibbon-Git-Server/actions/workflows/pr-test.yml)
[![Last commit](https://img.shields.io/github/last-commit/Matt-17/Gibbon-Git-Server)](https://github.com/Matt-17/Gibbon-Git-Server/commits/master)

**Gibbon Git Server** is a self-hosted Git server built with **ASP.NET Core**. This fork is currently under heavy development, and branch squashing will occur soon. Feel free to explore, but please refrain from forking for development purposes at this time.

For further information and documentation, please visit the [official website](https://matt-17.github.io/Gibbon-Git-Server/).

This is a fork of the original [Bonobo Git Server](https://github.com/jakubgarfield/Bonobo-Git-Server), which was last 
updated in 2019. The original project was built on .NET Framework, which is now outdated. This fork aims to modernize 
the project by migrating it to ASP.NET Core 10. The goal is to provide a more efficient, secure, and cross-platform Git 
server solution. You can find the key changes [below](#key-changes-to-bonobo-git-server).

---

## Features

- Web-based interface for managing Git repositories and users.
- Full Git support (clone, push, pull, etc.).
- Enhanced performance through .NET 10 optimizations.
- Open-source under the **MIT license**.
- Fully cross-platform thanks to .NET Core.

## Prerequisites

- **ASP.NET Core Runtime** (version 10).
- Windows Server with IIS or use **Kestrel** for cross-platform hosting.
- `git` command-line tool for Git operations.

## Installation Steps

1. **Clone the repository**:
   <code>git clone https://github.com/Matt-17/Gibbon-Git-Server.git</code>

2. **Publish the application** using the .NET Core CLI:
   <code>dotnet publish Gibbon.Git.Server -c Release -o ./publish</code>

3. **Deploy** the published files on IIS (Windows) or any web server (e.g., Kestrel for Linux/macOS).

4. **Database Setup**:
   - Set <code>Database:Provider</code> in <code>appsettings.json</code> to <code>Sqlite</code>; the shipped configuration uses an in-memory database that is lost on restart.
   - The database is created and migrated automatically on startup.

5. **Run the Application**:
   - Launch the app via IIS or directly using:
     <code>dotnet Gibbon.Git.Server.dll</code>

6. **Access the Web Interface** at <code>http://localhost:5000</code> (or your configured port).

## Configuration

- Update **`appsettings.json`** to adjust application settings.
- Use SQLite for a simple setup; SQL Server is possible by changing the provider and connection string.
- See the [documentation](https://matt-17.github.io/Gibbon-Git-Server/documentation) for all settings and the data directory.
  
## Update

Before each update please read carefully the information about **compatibility issues** between your version and the latest one in [changelog](https://matt-17.github.io/Gibbon-Git-Server/changelog).

- **Backup** your data.
- **Stop the server**.
- Delete all the files in the installation folder **except App_Data, appsettings.json and web.config**.
- Copy the files from the downloaded archive to the server location.
- **Start the server**.

## Changelog

Stay updated with the latest changes by visiting the [Changelog](https://matt-17.github.io/Gibbon-Git-Server/changelog) on our website.  
You can also view the legacy Bonobo Git Server changelog [here](https://github.com/Matt-17/Gibbon-Git-Server/blob/40135135d314ef25b9f82a9e5664366bc6995c4d/changelog.md).

## Contributing

We welcome contributions! If you plan to make significant changes, please open an issue first to discuss your ideas. Pull requests are always appreciated. For more details, check out our [Contributing Guidelines](./CONTRIBUTING.md).

## License

This project is licensed under the MIT License.

## Still have questions?

If you have any further questions, feel free to check out [Questions](https://matt-17.github.io/Gibbon-Git-Server/questions) on our website.

## Key changes to Bonobo Git Server

- Migrated from .NET Framework to **ASP.NET Core 10**.
- Removed support for **Active Directory (AD) login**.
- Database access via **Entity Framework Core** with automatic migrations (SQLite, SQL Server).
- Updated the **UI** to a more modern look and feel.
