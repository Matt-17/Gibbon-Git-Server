---
title: Changelog
---

## Version 1.0.0

- **Migrated to .NET 10**: The entire application has been updated to leverage the latest .NET features and performance improvements.
- **Removed Active Directory (AD) Authentication**: AD authentication support has been dropped to streamline user management and reduce complexity.
- **Removed Federation Authentication**: Support for federated authentication systems has been discontinued, allowing for simpler user authentication flows.
- **Configuration now in `appsettings.json`**: All server configurations have been consolidated into the `appsettings.json` file, making it easier to manage and customize settings.
- **Database migrations via Entity Framework**: The database upgrade system has been modernized using Entity Framework (EF) Migrations, enabling smoother, code-first database updates.
- **Server configuration moved to the database**: Server configuration data is now stored in the database, improving flexibility and centralized management.
- **Added user configuration**: New user-specific configuration options have been introduced, providing greater control and personalization for each user.
- **Optimized Performance**: Migrating to .NET 10 has resulted in performance improvements across the board, with faster response times and more efficient resource management.
- **Simplified Deployment**: By consolidating configurations into `appsettings.json` and moving server settings into the database, we've made the deployment process more straightforward, requiring fewer external dependencies.
- **Improved User Management**: The addition of user-specific configuration allows for greater personalization and control for individual users, improving the overall user experience.
- **Documentation Updates**: Along with these changes, the documentation has been updated to reflect the new configurations and usage patterns. Check out the updated guides for setting up and managing the server.
