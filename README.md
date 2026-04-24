# HSK-VFE-Collection

HSK/CE-patched conversions of **Vanilla Factions Expanded** sub-mods by Oskar Potocki et al., tuned for the [Hardcore SK modpack](https://github.com/skyarkhangel/Hardcore-SK) and bundled with the community CE compatibility patches.

This single repository contains **four separate mods**, each in its own subfolder. The HSK launcher (and RimWorld's mod manager) scan for `About/About.xml` files, so each subfolder is picked up as an independent mod.

## Included mods

| Folder | Upstream | Status | Notes |
|---|---|---|---|
| [`HSK-VFE-Pirates/`](./HSK-VFE-Pirates) | [VFE - Pirates](https://steamcommunity.com/sharedfiles/filedetails/?id=2723801948) | Stable | Warcasket loot + cremator salvage loop, Uranium→DepletedUranium swap, CE patches |
| [`HSK-VFE-Insectoid2/`](./HSK-VFE-Insectoid2) | [VFE - Insectoids 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3309003431) | Stable | Geological Landforms compat fix, HSK material tuning, CE patches |
| [`HSK-VFE-Mechanoids/`](./HSK-VFE-Mechanoids) | [VFE - Mechanoids](https://steamcommunity.com/sharedfiles/filedetails/?id=2329011599) | Stable | HSK architect integration, 21 overlap-with-HSK buildings disabled, CE patches |
| [`HSK-VFE-Deserters/`](./HSK-VFE-Deserters) | [VFE - Deserters](https://steamcommunity.com/sharedfiles/filedetails/?id=3025493377) | Stable | Imperial turrets hidden from architect, 4 missing CE ammo calibers bundled, CE patches |

## Requirements

All four mods require at minimum:

- **Hardcore SK** (Core_SK and its ecosystem)
- **Combat Extended**
- **Harmony**
- **Vanilla Expanded Framework**
- Mod-specific dependencies listed in each subfolder's `About/About.xml` (e.g. VFE Pirates needs the Empire framework, VFE Insectoids 2 needs Biotech, etc.)

## Install

Clone or download this repo. Place each subfolder in your RimWorld `Mods/` directory (or point your HSK launcher at this repo URL — it will scan the subfolders automatically). Enable the individual mods in your modlist.

Do **not** enable the upstream Oskar Potocki versions alongside these — each HSK conversion is marked `incompatibleWith` its upstream ID in its `About.xml`.

## Authorship

- Original mods: Oskar Potocki, Sarg Bjornson, Taranchuk, Sir Van, Xrushha, Kikohi, Chowder, erdelf, Kentington, ISOREX
- HSK/CE conversion, warcasket rework, compat fixes, integration patches: **CarbineAction**
- CE patches by the **Combat Extended community**

## License

Each subfolder follows the original mod author's license where applicable. The HSK conversion / compatibility work (XML patches, C# DLLs in `Source/` and `1.5/Assemblies/HSK*Compat.dll`) is released under the same terms as the upstream mods — free use, modification, and redistribution, credit appreciated.

## Contact

Issues / suggestions / PRs → open an issue on this repo.
