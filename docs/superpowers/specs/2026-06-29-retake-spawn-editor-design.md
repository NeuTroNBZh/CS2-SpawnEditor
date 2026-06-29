# RetakeSpawnEditor — Design Spec

## Objectif

Plugin CS2 séparé qui permet aux admins de visualiser, ajouter, modifier et supprimer les spawns du plugin CS2Retake-Agora, avec un rendu 3D en jeu (piliers lumineux, labels texte).

## Contexte

CS2Retake stocke ses spawns dans des fichiers JSON par map (`spawns/de_*.json`). Ce plugin lit et écrit ces mêmes fichiers sans dépendance runtime à CS2Retake.

### Format spawn

```json
{
  "SpawnId": "guid",
  "Team": 2,
  "BombSite": 0,
  "IsInBombZone": false,
  "PositionX": 0.0,
  "PositionY": 0.0,
  "PositionZ": 0.0,
  "QAngleX": 0.0,
  "QAngleY": 0.0,
  "QAngleZ": 0.0
}
```

- `Team`: 2=Terrorist, 3=CounterTerrorist
- `BombSite`: 0=A, 1=B

## Architecture

```
RetakeSpawnEditor/
├── RetakeSpawnEditor.csproj
├── SpawnEditorPlugin.cs
├── Models/SpawnPoint.cs
├── Managers/SpawnFileManager.cs
├── Managers/VisualizationManager.cs
├── Managers/AdminSessionManager.cs
└── Config/SpawnEditorConfig.cs
```

## Stack

- CounterStrikeSharp.API v1.0.228
- .NET 8.0
- System.Text.Json
