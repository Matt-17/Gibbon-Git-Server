---
title: Update
---

Before each update please read carefully the information about compatibility issues between your version and the latest one in the [changelog](changelog).

- **Back up** the data directory (default `App_Data`, see [Data Directory](documentation/data-directory)).
- **Stop** the site or service.
- Keep `App_Data`, `appsettings.json` and `web.config`; delete all other files in the installation folder.
  - Review changes in the new `web.config` and merge them into your existing one.
- Copy the files from the downloaded archive to the installation folder.
- **Start** the site or service. The database is migrated automatically.
- Review the [compatibility notes](documentation/compatibility).
