---
title: Installation
---
## Installation Steps for Windows

Installing **Gibbon Git Server** is straightforward and requires no special installer. Follow the steps below to get it up and running on your Windows machine.

### 1. Clone the Repository

First, you'll need to clone the Gibbon Git Server repository to your local machine.

1. Open a terminal (Command Prompt or PowerShell).
2. Run the following command to clone the repository:  
   `git clone https://github.com/Matt-17/Gibbon-Git-Server.git`{:.bash}
 3. Navigate into the project directory:  
    `cd Gibbon-Git-Server`{:.bash}
### 2. Publish the Application

Once you have the code on your machine, you need to publish the application. This step builds the app and prepares the files for deployment.

1. Ensure that you have the .NET Core SDK installed (you can check by running   
   `dotnet --version`).
   
2. Run the following command to publish the application:
	`dotnet publish Gibbon.Git.Server -c Release -o ./publish`  
	This command compiles the project in release mode and outputs the files to a folder named `publish`.

### 3. Configure the Application

Without any configuration the server uses **SQLite** in the [data directory](documentation/data-directory) (`App_Data`), which is enough for a simple setup.

To change settings, copy `appsettings.example.json` in the `publish` folder to `appsettings.json` and adjust it, e.g. for SQL Server, the data directory or the mail settings. See [Configuration Settings](documentation/appsettings) for all keys.
    
### 4. Deploy the Application

Now that the application is published and configured, it's time to deploy it.

#### Option 1: Deploy with **IIS (Internet Information Services)**

1. Open **IIS Manager** on your Windows Server or Windows machine.
2. Add a new website:
    - Set the **physical path** to the `publish` folder you created earlier.
    - Choose a **port** for the site (default is usually port 80).
    - Ensure the **application pool** is set to use **No Managed Code**.
3. Start the site and open a browser to access the Gibbon Git Server at the configured URL (e.g., `http://localhost`).

#### Option 2: Run Directly with **Kestrel**

For simpler setups, you can run Gibbon Git Server directly using the **Kestrel** web server, which comes built-in with ASP.NET Core.

1. Navigate to the `publish` folder in your terminal.
2. Run the following command to start the server:

```
dotnet Gibbon.Git.Server.dll
```

3. By default, Kestrel will host the application on port `5000`. You can access the web interface at `http://localhost:5000` (or the configured port).

### 5. Access the Web Interface

Once the server is running, open your web browser and go to the following URL:

- For IIS deployments: `http://localhost` or the domain/port you configured in IIS.
- For Kestrel deployments: `http://localhost:5000` or the port you configured in `appsettings.json`.

You should see the Gibbon Git Server login page. From here, you can start managing repositories, users, and more.

---

That’s it! With just a few simple steps, you can have Gibbon Git Server running on your Windows machine. For more detailed configuration options or deployment strategies, refer to the official documentation on our [website](https://matt-17.github.io/Gibbon-Git-Server/).