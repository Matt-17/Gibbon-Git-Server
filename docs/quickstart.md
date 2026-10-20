---
title: Quickstart Guide
---

Welcome to the Gibbon Git Server! Follow these steps to get your server up and running quickly.

## 1. Installation

### Prerequisites:
- Windows Server or Windows 10/11
- IIS (Internet Information Services) installed
- ASP.NET Core 10 Hosting Bundle installed
- Git for Windows installed

### Steps:

1. **Download the Latest Release**: 
   Download the latest version of Gibbon Git Server from [here](https://github.com/Matt-17/Gibbon-Git-Server/releases). Until the first release is published, build it from source as described in the [installation guide](documentation/installation).

2. **Extract Files**: 
   Extract the contents of the downloaded zip file to your desired directory.

3. **Configure IIS**:
   - Open IIS Manager.
   - Add a new site and point the physical path to the directory where you extracted the Gibbon Git Server files.
   - Bind the site to a hostname or IP address, and configure SSL if necessary.

4. **Configure Permissions**:
   Ensure that the application pool has the necessary permissions to read/write to the directory where the repositories and configuration files are stored.

5. **Database Setup**:
   - Set `Database:Provider` to `Sqlite` in `appsettings.json` for a lightweight SQLite database.
   - Run the server for the first time to automatically set up the database using EF Migrations.

6. **Update `appsettings.json`**:
   Customize your configuration by modifying the `appsettings.json` file, including repository paths, server options, and email settings (if notifications are used).

## 2. Initial Setup

1. **Open the Web Interface**: 
   Navigate to the URL where your Gibbon Git Server is hosted (e.g., `http://localhost`).

2. **Log In as Administrator**:
   - Log in with the default account `admin` and password `admin`.
   - Change the password right away in your account settings.

3. **Set Up Users**:
   - Navigate to the **Users** section.
   - Create additional users and assign them a role (Administrator or Member).

## 3. Create Your First Repository

1. **Go to Repository Management**:
   - In the admin interface, navigate to the **Repositories** section.
   
2. **Add a New Repository**:
   - Click **Create New Repository**.
   - Provide a name and description for the repository.
   - Configure the visibility (Public or Private) and assign user permissions.

3. **Clone the Repository**:
   - Clone your new repository using Git on your local machine:
     ```bash
     git clone http://<your-server-url>/<repository-name>.git
     ```

4. **Push Your Code**:
   - Add files to the repository, commit them, and push to the server:
     ```bash
     git add .
     git commit -m "Initial commit"
     git push origin main
     ```

## 4. Manage Users and Permissions

1. **User Management**:
   - In the **Users** section, add new users to the system.
   - Assign the role Administrator or Member, depending on their access needs.

2. **Repository Permissions**:
   - Grant users and teams pull, push or administration rights for each repository.
   - Repositories can allow anonymous access if needed.

## 5. Explore Additional Features

- **Teams**: Group users and grant repository permissions to the whole team.
- **Repository Browser**: Browse files, commits, branches and tags in the web interface.
- **Server Settings**: Adjust settings such as the default language through the admin interface.

For more detailed documentation, check out the [full user guide](documentation).

---

By following these steps, you’ll have your Gibbon Git Server up and running, managing repositories, users, and permissions with ease!
