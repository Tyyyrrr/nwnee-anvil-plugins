This repository contains a set of plugins written in C#, using [Anvil API](https://github.com/nwn-dotnet/Anvil) for Neverwinter Nights: Enhanced Edition persistent world servers.


⚠️ Compatibility Notice

**These plugins were originally designed for a specific NWN:EE server!**

As a result:
- Some plugins contain hardcoded strings (Polish)
- The target server relies heavily on NWScript, so these plugins have a hybrid nature. (not pure C#)
- External MySQL database setup is required.
- Certain gameplay assumptions and conditional logic may not match your server's design.

What this means for you:
- You will need to modify the code to adapt plugins to your own server. The amount of work varies by plugin complexity.
- Simple or interface-only plugins (e.g., EasyConfig, MySQLClient) are fully server-agnostic, and don't require any modifications.
- Complex plugins (e.g., CharacterAppearance, MovementSystem) may require deeper integration changes.
- A significant portion of server‑specific data (especially database schemas) has been abstracted and hidden behind ServerData interface contracts (for more information see [ServerData README](ServerData))
- Because of the hybrid NWScript ↔ C# model, ScriptHandlers, and .nss file names may require revision.

_The architecture is intentionally modular, but __compatibility with the target server is prioritized__,
so adaptation __is__ possible — but __not__ plug‑and‑play._

This repository is licensed under the [Apache License 2.0](LICENSE).

Plugin directories may include their own README files with usage notes / configuration details / integration guidance.
