---
title: Configuration Settings Documentation
---

This document provides an overview of the configuration settings used in the `appsettings.json` file for the Gibbon Git Server.

## Structure Overview

The `appsettings.json` file contains various sections for configuring the application, including paths, Git settings, mail settings, and logging. Below is an explanation of each section and its respective keys.

```json
{
  "AppSettings": {
    "DataPath": "~\\App_Data",
    "DemoModeActive": false,
    "IsPushAuditEnabled": false,
    "AllowDbReset": true,
    "RepositoryPath": "~\\App_Data\\Repositories"
  },
  "GitSettings": {
    "BinaryPath": "~\\App_Data\\Git",
    "HomePath": "~\\App_Data",
    "Version": "2.6.1",
    "Architecture": "32"
  },
  "MailSettings": {
    "Host": "",
    "DefaultCredentials": false,
    "Port": 587,
    "Name": "Your sender name",
    "EmailId": "Your email ID",
    "UserName": "Your username",
    "Password": "Your password",
    "UseSSL": true
  },
  "ConnectionStrings": {
    "GibbonGitServerContext": "Data Source=App_Data\\Gibbon.Git.Server.db;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.Hosting": "Warning"
    }
  },
  "DetailedErrors": true
}
```

## Configuration Sections

### 1. `AppSettings`
This section contains the general configuration for the Gibbon Git Server.

- **`DataPath`**: Specifies the directory where application data is stored. The default is `"~\\App_Data"`.
- **`DemoModeActive`**: If set to `true`, the application runs in demo mode. Default is `false`.
- **`IsPushAuditEnabled`**: If set to `true`, audits all push operations for detailed logging. Default is `false`.
- **`AllowDbReset`**: Enables or disables the ability to reset the database. Default is `true`.
- **`RepositoryPath`**: Specifies the directory where repositories are stored. Default is `"~\\App_Data\\Repositories"`.
- **`LogToFile`**: Writes daily log files. Default is `true`. See [Data Directory](documentation/data-directory#log-files).
- **`LogPath`**: Directory for the log files. Default is `Logs` in the data directory.
- **`LogRetainedFileCount`**: Number of daily log files to keep, `0` keeps all. Default is `31`.

### 2. `GitSettings`
This section configures Git-related settings, including the binary path, home path, version, and architecture.

- **`BinaryPath`**: Path to the Git binary. Default is `"~\\App_Data\\Git"`.
- **`HomePath`**: The home directory for Git. Default is `"~\\App_Data"`.
- **`Version`**: The version of Git being used. Default is `"2.6.1"`.
- **`Architecture`**: The architecture of the Git binary, e.g., `32` for 32-bit or `64` for 64-bit. Default is `"32"`.

### 3. `MailSettings`
This section configures email settings used for notifications and alerts.

- **`Host`**: The mail server's hostname.
- **`DefaultCredentials`**: If `true`, uses the system's default credentials. Default is `false`.
- **`Port`**: The port number for the mail server. Default is `587`.
- **`Name`**: The sender's name that appears in the email.
- **`EmailId`**: The sender's email address.
- **`UserName`**: The username for authenticating with the mail server.
- **`Password`**: The password for the mail server.
- **`UseSSL`**: Whether to use SSL when connecting to the mail server. Default is `true`.

### 4. `ConnectionStrings`
This section defines the connection string for the database used by the Gibbon Git Server.

- **`GibbonGitServerContext`**: Specifies the SQLite database connection string. The default is `"Data Source=App_Data\\Gibbon.Git.Server.db;"`.

### 5. `Logging`
This section controls the logging levels for various parts of the application.

- **`Default`**: The default logging level. Set to `Debug` for detailed logs.
- **`Microsoft`**: The logging level for Microsoft-related logs. Set to `Information`.
- **`Microsoft.Hosting`**: The logging level for hosting-related logs. Set to `Warning`.

### 6. `DetailedErrors`
This boolean setting enables or disables detailed error messages in the application. Default is `true`.

---

### Example Usage

To customize the settings for your own environment, modify the values in the `appsettings.json` file as needed.

For example, to enable the push audit and set up your email settings:

```json
{
  "AppSettings": {
    "IsPushAuditEnabled": true
  },
  "MailSettings": {
    "Host": "smtp.yourmailserver.com",
    "UserName": "your-email@example.com",
    "Password": "your-secure-password"
  }
}
```

Make sure to keep sensitive information like email credentials secure. In production environments, it's recommended to store these settings in a secure place such as environment variables or Azure Key Vault.
