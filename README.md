# HSK-VFE-Collection

HSK/CE-patched conversions of **Vanilla Factions Expanded** sub-mods by Oskar Potocki et al., tuned for the [Hardcore SK](https://github.com/skyarkhangel/Hardcore-SK) modpack and bundled with the community Combat Extended compatibility patches.

This repository contains **five separate mods**, each in its own subfolder. RimWorld's mod manager (and the HSK launcher) scan for `About/About.xml`, so each subfolder is recognized as an independent mod.

## Requirements

- **RimWorld 1.5**
- **Harmony**
- **Hardcore SK Modpack**
- **Combat Extended**
- **Vanilla Expanded Framework**
- Mod-specific dependencies listed in each subfolder's `About/About.xml` (e.g. VFE Pirates needs the Empire framework, Insectoid2 needs Biotech, etc.)

## What's Inside

### 🏴‍☠ HSK-VFE-Pirates
Full HSK/CE conversion of **VFE — Pirates**. Warcasket loot drop + cremator salvage loop integrated with HSK's recycling economy. `Uranium → DepletedUranium` swap so warcaskets cost HSK-tier alloy instead of raw ore. Junker raid gating tuned to 800 minTotalPoints via `raidCommonalityFromPointsCurve` so low-point colonies aren't blasted on Day 1.

### 🐛 HSK-VFE-Insectoid2
Full HSK/CE conversion of **VFE — Insectoids 2**. Geological Landforms compat fix for hive spawn terrain, HSK material tuning across all faction defs, Insect raid gating to 800 minTotalPoints.

### 🤖 HSK-VFE-Mechanoids
Full HSK/CE conversion of **VFE — Mechanoids**. 21 buildings that overlap with HSK content are hidden from architect. Mech raid gating to 1500 minTotalPoints (mid-late tier). All 9 Mechanoid turrets now require grid power (previously some were free-running).

### 🪖 HSK-VFE-Deserters
Full HSK/CE conversion of **VFE — Deserters**. Imperial turrets hidden from architect (only spawn at faction bases). 4 missing CE ammo calibers bundled directly in this mod so Deserter weapons fire correctly without a separate compat dependency.

### 🏛 HSK-VFE-Ancients
Full HSK/CE conversion of **VFE — Ancients**. `Steel → SteelBar` and `Uranium → DepletedUranium` HSK-native material swaps. Gene-tailoring biotech parts integrated with HSK biotech recipes. VFEA mending stations hidden (HSK has its own mending workbench in Core_SK). CE turret + soldier patches included.

## Installation

1. Clone or download this repo
2. Place each subfolder in your RimWorld `Mods/` directory (or point your HSK launcher at this repo URL — it will scan the subfolders automatically)
3. Enable the individual mods in your modlist
4. Load **after** Hardcore SK, Combat Extended, Vanilla Expanded Framework, and the upstream VFE mods if you have them disabled

⚠ **Do not enable the upstream Oskar Potocki versions alongside these.** Each HSK conversion is marked `incompatibleWith` its upstream `packageId` in `About.xml` — the launcher will warn you if both are active.

## How It Works

Each conversion is a self-contained replacement of the upstream mod:

- **Recipes** are re-pointed at HSK benches and HSK research gates
- **Materials** are mapped from generic vanilla resources (Steel, Uranium) to HSK-specific alloy tiers (SteelBar, DepletedUranium, ComponentIndustrial)
- **Combat Extended** patches add `Verb_ShootCE`, `CompProperties_AmmoUser`, `AmmoSet`, and proper projectile bindings to every weapon and turret
- **Raid gating** is implemented via vanilla's `raidCommonalityFromPointsCurve` on each faction def — raids of these factions only trigger once your colony hits the threshold
- **Architect-hide patches** mark overlapping buildings with `forceHidden` so HSK's own equivalent shows instead

## Reporting Issues

If you find a bug, please attach your `Player.log` and a description of which subfolder the issue is in. Issues that don't include logs may be closed.

## Authorship

- Original mods: **Oskar Potocki**, **Sarg Bjornson**, **Taranchuk**, **Sir Van**, **Xrushha**, **Kikohi**, **Chowder**, **erdelf**, **Kentington**, **ISOREX**
- HSK/CE conversion, warcasket rework, compat fixes, integration patches: **CarbineAction**
- CE patches by the **Combat Extended community**
- HSK material economy and bench conventions: **Hardcore SK Team**

## License

Each subfolder follows the original mod author's license where applicable. The HSK conversion / compatibility work (XML patches, C# DLLs in `Source/` and `1.5/Assemblies/HSK*Compat.dll`) is released under the same terms as the upstream mods — free use, modification, and redistribution. Credit appreciated.

## Contact

Issues / suggestions / PRs → open an issue on this repo.
