<div align="center">

# RetakeSpawnEditor

**A spawn editor plugin for CS2Retake — built for CounterStrikeSharp**

[![Release](https://img.shields.io/github/v/release/NeuTroNBZh/CS2-SpawnEditor?style=flat-square&label=Release&color=brightgreen)](https://github.com/NeuTroNBZh/CS2-SpawnEditor/releases/latest)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)](https://dotnet.microsoft.com/)
[![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-1.0.228-orange?style=flat-square)](https://github.com/roflmuffin/CounterStrikeSharp)
[![CS2](https://img.shields.io/badge/Game-CS2-yellow?style=flat-square)](https://www.counter-strike.net/)

*Companion plugin for [CS2 Retake V3](https://github.com/NeuTroNBZh/CS2-RETAKE) — visualize, add, edit and delete retake spawns directly in-game.*

</div>

---

## Features

### Spawn Visualization
| Feature | Description |
|---------|-------------|
| **CBeam pillars** | Vertical colored beams at every spawn position |
| **Team color coding** | Terrorist spawns in red · Counter-Terrorist spawns in blue |
| **Site differentiation** | Site A at full opacity · Site B slightly dimmed |
| **Nearest-spawn highlight** | Yellow ring around the spawn closest to the admin |
| **Floating labels** | `[T][A] #07` text above each spawn |
| **Per-admin HUD** | Center-screen overlay showing total count, nearest spawn and distance |

### Spawn Editing
| Feature | Description |
|---------|-------------|
| **Add spawns** | Drop a new spawn at your current feet position, any team and site |
| **Delete spawns** | Remove the spawn closest to you |
| **Edit team / site** | Change the team or bombsite of the nearest spawn |
| **Toggle bomb zone** | Mark or unmark a spawn as being inside a plant zone |
| **Teleport** | Jump to any spawn by index for quick review |

### Workflow
| Feature | Description |
|---------|-------------|
| **Auto-pause retake** | Activating the editor automatically pauses the match so you have time to work |
| **Auto-unpause** | Match resumes automatically when all admins deactivate the editor |
| **Non-destructive** | Changes stay in memory until you explicitly `css_se_save` |
| **Hot-reload safe** | Works correctly on plugin hot-reload |
| **SimpleAdmin integration** | Full `!admin` menu support via [CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin) (optional) |

---

## Requirements

| Dependency | Version | Link |
|-----------|---------|------|
| **CS2 Retake V3** | ≥ 1.0.0 | [github.com/NeuTroNBZh/CS2-RETAKE](https://github.com/NeuTroNBZh/CS2-RETAKE) |
| **CounterStrikeSharp** | ≥ 1.0.228 | [github.com/roflmuffin/CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases) |
| **Metamod:Source** | latest dev | [sourcemm.net](https://www.sourcemm.net/downloads.php/?branch=master) |
| **CS2-SimpleAdmin** | any | [github.com/daffyyyy/CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin) *(optional)* |

---

## Installation

### Step 1 — Install dependencies
Make sure **CS2 Retake V3**, **Metamod:Source** and **CounterStrikeSharp** are already installed.

### Step 2 — Download the plugin
Go to the [**Releases**](https://github.com/NeuTroNBZh/CS2-SpawnEditor/releases/latest) page and download the latest `SPAWN-EDITOR-*.zip`.

Two variants are available:

| File | Use case |
|------|----------|
| `SPAWN-EDITOR-1.0.0.zip` | First install — includes plugin DLL **and** default config |
| `SPAWN-EDITOR-1.0.0-no_configs.zip` | Update — DLL only, preserves your existing config |
| `SPAWN-EDITOR-1.0.0-config.zip` | Reset config to defaults only |

### Step 3 — Upload to your server
Extract the archive and copy the **entire contents** into your server's `game/csgo/` directory:

```
game/csgo/
└── addons/
    └── counterstrikesharp/
        ├── plugins/
        │   └── RetakeSpawnEditor/
        │       └── RetakeSpawnEditor.dll
        └── configs/
            └── plugins/
                └── RetakeSpawnEditor/
                    └── RetakeSpawnEditor.json   ← auto-generated on first start
```

### Step 4 — Start the server
Restart your CS2 server. The plugin loads automatically alongside CS2Retake.

---

## Configuration

Config file: `addons/counterstrikesharp/configs/plugins/RetakeSpawnEditor/RetakeSpawnEditor.json`

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `SpawnFolderPath` | string | `../CS2Retake/spawns` | Path to the CS2Retake `spawns/` folder, relative to the plugin DLL |
| `AdminFlag` | string | `@css/admin` | CounterStrikeSharp permission required to use all editor commands |
| `BeamHeight` | float | `72.0` | Height of the CBeam pillar above each spawn (in Source units) |
| `MaxHighlightDistance` | float | `150.0` | Max distance (units) from admin to highlight a spawn as "nearest" |
| `MaxDisplayDistance` | float | `2000.0` | Max distance at which spawns are rendered (reserved for future use) |

---

## Commands

### Admin Commands (`@css/admin` required)

| Command | Args | Description |
|---------|------|-------------|
| `css_se` | — | Toggle visualization and editor ON/OFF · auto-pauses / resumes the retake |
| `css_se_add` | `[T\|CT] [A\|B]` | Add a new spawn at your current feet position |
| `css_se_del` | — | Delete the nearest spawn (within `MaxHighlightDistance`) |
| `css_se_set` | `[T\|CT] [A\|B]` | Change team and/or site of the nearest spawn |
| `css_se_zone` | — | Toggle `IsInBombZone` flag on the nearest spawn |
| `css_se_tp` | `<index>` | Teleport to the spawn at index `#XX` |
| `css_se_list` | — | Print all spawns for the current map in chat |
| `css_se_save` | — | Write in-memory changes to the JSON file |
| `css_se_reload` | — | Reload spawns from the JSON file (discards unsaved changes) |

### SimpleAdmin Menu (`!admin`)

If [CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin) is installed, a **Spawn Editor** category appears automatically in the `!admin` menu with all actions above, no extra configuration required.

---

## How It Works

### Editing workflow

```
1. Type !admin → Spawn Editor → Toggle Visualization
   (or: css_se in console)
   → Match pauses automatically
   → Colored pillars appear at every spawn

2. Walk to the desired position

3. Add spawn:      css_se_add T A      (Terrorist, Site A)
   Delete nearest: css_se_del
   Edit nearest:   css_se_set CT B     (change to CT, Site B)

4. css_se_save     → writes to de_dust2.json (or current map)

5. css_se          → disables visualization
   → Match resumes automatically
```

### Spawn file format

Spawns are stored in the same JSON format as CS2Retake:

```json
[
  {
    "SpawnId": "c9616622-dfa0-4de3-b2a0-c7ef9ad372e5",
    "Team": 2,
    "BombSite": 0,
    "IsInBombZone": false,
    "PositionX": 1485.636,
    "PositionY": 1799.516,
    "PositionZ": -7.804,
    "QAngleX": 0.0,
    "QAngleY": -135.228,
    "QAngleZ": 0.0
  }
]
```

`Team`: 2 = Terrorist · 3 = Counter-Terrorist  
`BombSite`: 0 = Site A · 1 = Site B

---

## Troubleshooting

**Plugin does not load**  
→ Verify CounterStrikeSharp ≥ 1.0.228 and `RetakeSpawnEditor.dll` is in `plugins/RetakeSpawnEditor/`.

**Spawns folder not found / 0 spawns loaded**  
→ Check `SpawnFolderPath` in the config. Default points to `../CS2Retake/spawns` — adjust if your CS2Retake plugin is installed at a different path.

**Match does not pause on css_se**  
→ `mp_pause_match` requires the match to be in progress. In warmup or between maps it is a no-op (normal behavior).

**SimpleAdmin menu does not appear**  
→ CS2-SimpleAdmin must be installed and loaded **before** RetakeSpawnEditor. Verify plugin load order in CounterStrikeSharp.

**Visualization entities remain after map change**  
→ Should not happen — `OnMapStart` clears all entities. If it does, type `css_se` twice to force a cleanup.

---

## Building from Source

```bash
git clone https://github.com/NeuTroNBZh/CS2-SpawnEditor.git
cd CS2-SpawnEditor

dotnet build RetakeSpawnEditor/RetakeSpawnEditor.csproj -c Release
```

On Windows, the post-build script `BuildScripts/Sync-PluginArtifacts.ps1` automatically assembles the release packages under `plugin/` and creates the distribution ZIPs.

---

## Credits

| Project | Author | Role |
|---------|--------|------|
| [CS2 Retake V3](https://github.com/NeuTroNBZh/CS2-RETAKE) | [NeuTroNBZh](https://github.com/NeuTroNBZh) | CS2Retake plugin — spawn format and gameplay loop |
| [CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin) | [daffyyyy](https://github.com/daffyyyy) | Admin menu framework |
| [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) | [roflmuffin](https://github.com/roflmuffin) | Plugin framework |

---

## License

This project is licensed under the **GNU General Public License v3.0** — see the [LICENSE](LICENSE) file for details.



