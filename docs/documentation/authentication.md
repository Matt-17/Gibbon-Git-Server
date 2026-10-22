---
title: Authentication
---
Gibbon Git Server manages its own users. There is no external identity provider such as Active Directory or LDAP yet; LDAP support is planned ([#35](https://github.com/Matt-17/Gibbon-Git-Server/issues/35)). Authentication has no settings in `appsettings.json`.

## Web interface

Users log in with their user name and password on the login page. The server then sets a login cookie:

- It is valid for 3 days and is renewed while the user is active.
- It is protected with keys stored in the `DataProtection-Keys` folder of the [data directory](documentation/data-directory), so users stay logged in when the server restarts. Deleting the folder logs out all users.

## Git clients

Git sends the user name and password with every request (HTTP Basic Authentication). Use a credential manager, such as Git Credential Manager on Windows, to avoid typing them again.

Repositories can allow anonymous access. Pulling is then possible without credentials; pushing additionally needs anonymous push, per repository or globally in the server settings.

## HTTPS

Basic Authentication sends the credentials unencrypted unless the connection uses HTTPS. Always run the server behind HTTPS, either with an HTTPS binding in IIS or a reverse proxy in front of Kestrel.
