# RetakeSpawnEditor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Créer un plugin CounterStrikeSharp qui permet aux admins CS2 de visualiser (piliers CBeam + labels CPointWorldText), ajouter, modifier et supprimer les spawns du plugin CS2Retake-Agora.

**Architecture:** Plugin indépendant qui lit/écrit les mêmes fichiers JSON que CS2Retake (`spawns/de_*.json`). Les entités de visualisation sont créées à la demande et nettoyées automatiquement. Le HUD texte (PrintToCenter) est envoyé par-joueur sur OnTick.

**Tech Stack:** C# / .NET 8.0, CounterStrikeSharp.API 1.0.228, System.Text.Json

## Global Constraints

- MinimumApiVersion: 228 (même que CS2Retake)
- .NET 8.0 (TargetFramework net8.0)
- Toutes les commandes requièrent `@css/admin`
- Pas de dépendances NuGet additionnelles sauf CounterStrikeSharp.API
- Les spawns CS2Retake sont dans `spawns/{mapName}.json` dans le dossier du plugin CS2Retake
- Équipes : 2=Terrorist, 3=CounterTerrorist
- Sites : 0=A, 1=B
- Nommage : camelCase variables/méthodes, PascalCase classes, UPPER_SNAKE_CASE constantes
- Fonctions < 50 lignes, fichiers < 800 lignes
- Immutabilité : créer de nouveaux objets, ne pas muter l'existant

---

### Task 1: Scaffolding projet

**Files:**
- Create: `RetakeSpawnEditor/RetakeSpawnEditor.csproj`
- Create: `RetakeSpawnEditor/SpawnEditorPlugin.cs`

**Interfaces:**
- Consumes: rien
- Produces: classe `SpawnEditorPlugin : BasePlugin` avec `ModuleName = "RetakeSpawnEditor"`, `ModuleVersion = "1.0.0"`, `ModuleAuthor = "local"`

- [ ] **Step 1: Créer le .csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>RetakeSpawnEditor</RootNamespace>
    <AssemblyName>RetakeSpawnEditor</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CounterStrikeSharp.API" Version="1.0.228" />
  </ItemGroup>
</Project>
```

Écrire dans `RetakeSpawnEditor/RetakeSpawnEditor.csproj`.

- [ ] **Step 2: Créer SpawnEditorPlugin.cs**

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;

namespace RetakeSpawnEditor;

[MinimumApiVersion(228)]
public class SpawnEditorPlugin : BasePlugin
{
    public override string ModuleName => "RetakeSpawnEditor";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "local";

    public override void Load(bool hotReload)
    {
        Console.WriteLine("[RetakeSpawnEditor] Plugin loaded.");
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine("[RetakeSpawnEditor] Plugin unloaded.");
    }
}
```

- [ ] **Step 3: Compiler**

```
cd RetakeSpawnEditor
dotnet build
```

Résultat attendu : `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 4: Commit**

```
git add RetakeSpawnEditor/
git commit -m "feat: scaffold RetakeSpawnEditor plugin project"
```

---

### Task 2: Modèle SpawnPoint + SpawnFileManager

**Files:**
- Create: `RetakeSpawnEditor/Models/SpawnPoint.cs`
- Create: `RetakeSpawnEditor/Config/SpawnEditorConfig.cs`
- Create: `RetakeSpawnEditor/Managers/SpawnFileManager.cs`
- Create: `RetakeSpawnEditor.Tests/RetakeSpawnEditor.Tests.csproj`
- Create: `RetakeSpawnEditor.Tests/SpawnFileManagerTests.cs`

**Interfaces:**
- Consumes: rien
- Produces:
  - `SpawnPoint` : `SpawnId` (Guid), `Team` (int), `BombSite` (int), `IsInBombZone` (bool), `PositionX/Y/Z` (float), `QAngleX/Y/Z` (float), computed `TeamLabel` (string), `SiteLabel` (string)
  - `SpawnFileManager(string spawnsFolder)`
  - `SpawnFileManager.LoadSpawns(string mapName) : List<SpawnPoint>`
  - `SpawnFileManager.SaveSpawns(string mapName, IReadOnlyList<SpawnPoint> spawns) : void`
  - `SpawnEditorConfig : BasePluginConfig` avec `SpawnFolderPath`, `AdminFlag`, `BeamHeight`, `MaxHighlightDistance`, `MaxDisplayDistance`

- [ ] **Step 1: Créer Models/SpawnPoint.cs**

```csharp
using System.Text.Json.Serialization;

namespace RetakeSpawnEditor.Models;

public class SpawnPoint
{
    public Guid SpawnId { get; set; } = Guid.Empty;
    public int Team { get; set; }
    public int BombSite { get; set; }
    public bool IsInBombZone { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float QAngleX { get; set; }
    public float QAngleY { get; set; }
    public float QAngleZ { get; set; }

    [JsonIgnore]
    public string TeamLabel => Team == 2 ? "T" : "CT";

    [JsonIgnore]
    public string SiteLabel => BombSite == 0 ? "A" : "B";
}
```

- [ ] **Step 2: Créer Config/SpawnEditorConfig.cs**

```csharp
using CounterStrikeSharp.API.Core;

namespace RetakeSpawnEditor.Config;

public class SpawnEditorConfig : BasePluginConfig
{
    public string SpawnFolderPath { get; set; } = "../CS2Retake/spawns";
    public string AdminFlag { get; set; } = "@css/admin";
    public float BeamHeight { get; set; } = 72.0f;
    public float MaxHighlightDistance { get; set; } = 150.0f;
    public float MaxDisplayDistance { get; set; } = 2000.0f;
}
```

- [ ] **Step 3: Créer Managers/SpawnFileManager.cs**

```csharp
using System.Text.Json;
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor.Managers;

public class SpawnFileManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _spawnsFolder;

    public SpawnFileManager(string spawnsFolder)
    {
        _spawnsFolder = spawnsFolder;
    }

    public List<SpawnPoint> LoadSpawns(string mapName)
    {
        var path = GetSpawnPath(mapName);
        if (!File.Exists(path)) return new List<SpawnPoint>();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<SpawnPoint>>(json) ?? new List<SpawnPoint>();
    }

    public void SaveSpawns(string mapName, IReadOnlyList<SpawnPoint> spawns)
    {
        Directory.CreateDirectory(_spawnsFolder);
        foreach (var sp in spawns.Where(sp => sp.SpawnId == Guid.Empty))
            sp.SpawnId = Guid.NewGuid();
        File.WriteAllText(GetSpawnPath(mapName), JsonSerializer.Serialize(spawns, JsonOptions));
    }

    private string GetSpawnPath(string mapName) =>
        Path.Combine(_spawnsFolder, $"{mapName}.json");
}
```

- [ ] **Step 4: Créer le projet de tests RetakeSpawnEditor.Tests/RetakeSpawnEditor.Tests.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <ProjectReference Include="../RetakeSpawnEditor/RetakeSpawnEditor.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Écrire RetakeSpawnEditor.Tests/SpawnFileManagerTests.cs**

```csharp
using RetakeSpawnEditor.Managers;
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor.Tests;

public class SpawnFileManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SpawnFileManager _manager;

    public SpawnFileManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _manager = new SpawnFileManager(_tempDir);
    }

    [Fact]
    public void LoadSpawns_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        var result = _manager.LoadSpawns("de_nonexistent");
        Assert.Empty(result);
    }

    [Fact]
    public void SaveAndLoad_PreservesSpawnData()
    {
        var spawns = new List<SpawnPoint>
        {
            new SpawnPoint
            {
                SpawnId = Guid.NewGuid(), Team = 2, BombSite = 0,
                IsInBombZone = false,
                PositionX = 100f, PositionY = 200f, PositionZ = -10f,
                QAngleX = 0f, QAngleY = 90f, QAngleZ = 0f
            }
        };
        _manager.SaveSpawns("de_test", spawns);
        var loaded = _manager.LoadSpawns("de_test");

        Assert.Single(loaded);
        Assert.Equal(2, loaded[0].Team);
        Assert.Equal(0, loaded[0].BombSite);
        Assert.Equal(100f, loaded[0].PositionX);
        Assert.Equal(90f, loaded[0].QAngleY);
    }

    [Fact]
    public void SaveSpawns_AssignsNewGuid_WhenSpawnIdIsEmpty()
    {
        var spawn = new SpawnPoint { SpawnId = Guid.Empty, Team = 3, BombSite = 1 };
        _manager.SaveSpawns("de_test", new List<SpawnPoint> { spawn });
        var loaded = _manager.LoadSpawns("de_test");
        Assert.NotEqual(Guid.Empty, loaded[0].SpawnId);
    }

    [Fact]
    public void SpawnPoint_TeamLabel_ReturnsTForTeam2() =>
        Assert.Equal("T", new SpawnPoint { Team = 2 }.TeamLabel);

    [Fact]
    public void SpawnPoint_TeamLabel_ReturnsCTForTeam3() =>
        Assert.Equal("CT", new SpawnPoint { Team = 3 }.TeamLabel);

    [Fact]
    public void SpawnPoint_SiteLabel_ReturnsAForSite0() =>
        Assert.Equal("A", new SpawnPoint { BombSite = 0 }.SiteLabel);

    [Fact]
    public void SpawnPoint_SiteLabel_ReturnsBForSite1() =>
        Assert.Equal("B", new SpawnPoint { BombSite = 1 }.SiteLabel);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
```

- [ ] **Step 6: Lancer les tests**

```
cd RetakeSpawnEditor.Tests
dotnet test --verbosity normal
```

Attendu : `Passed! - 7 tests passed.`

- [ ] **Step 7: Commit**

```
git add RetakeSpawnEditor/Models/ RetakeSpawnEditor/Config/ RetakeSpawnEditor/Managers/SpawnFileManager.cs RetakeSpawnEditor.Tests/
git commit -m "feat: add SpawnPoint model, SpawnEditorConfig, SpawnFileManager with tests"
```

---

### Task 3: AdminSessionManager

**Files:**
- Create: `RetakeSpawnEditor/Managers/AdminSessionManager.cs`
- Create: `RetakeSpawnEditor.Tests/AdminSessionManagerTests.cs`

**Interfaces:**
- Consumes: `SpawnPoint` (Task 2)
- Produces:
  - `AdminSessionManager.EnableVisualization(ulong steamId) : void`
  - `AdminSessionManager.DisableVisualization(ulong steamId) : void`
  - `AdminSessionManager.IsVisualizationEnabled(ulong steamId) : bool`
  - `AdminSessionManager.GetAdminsWithVisualization() : IReadOnlyList<ulong>`
  - `AdminSessionManager.SetNearestSpawn(ulong steamId, SpawnPoint? spawn) : void`
  - `AdminSessionManager.GetNearestSpawn(ulong steamId) : SpawnPoint?`
  - `AdminSessionManager.HasUnsavedChanges : bool`
  - `AdminSessionManager.MarkUnsaved() : void`
  - `AdminSessionManager.MarkSaved() : void`
  - `AdminSessionManager.Clear() : void`

- [ ] **Step 1: Écrire RetakeSpawnEditor.Tests/AdminSessionManagerTests.cs**

```csharp
using RetakeSpawnEditor.Managers;
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor.Tests;

public class AdminSessionManagerTests
{
    private readonly AdminSessionManager _manager = new();

    [Fact]
    public void IsVisualizationEnabled_ReturnsFalse_WhenNotEnabled() =>
        Assert.False(_manager.IsVisualizationEnabled(12345UL));

    [Fact]
    public void EnableVisualization_MakesIsVisualizationEnabledReturnTrue()
    {
        _manager.EnableVisualization(12345UL);
        Assert.True(_manager.IsVisualizationEnabled(12345UL));
    }

    [Fact]
    public void DisableVisualization_MakesIsVisualizationEnabledReturnFalse()
    {
        _manager.EnableVisualization(12345UL);
        _manager.DisableVisualization(12345UL);
        Assert.False(_manager.IsVisualizationEnabled(12345UL));
    }

    [Fact]
    public void GetAdminsWithVisualization_ReturnsOnlyEnabledAdmins()
    {
        _manager.EnableVisualization(1UL);
        _manager.EnableVisualization(2UL);
        _manager.DisableVisualization(1UL);
        var result = _manager.GetAdminsWithVisualization();
        Assert.Single(result);
        Assert.Equal(2UL, result[0]);
    }

    [Fact]
    public void GetNearestSpawn_ReturnsNull_WhenNotSet() =>
        Assert.Null(_manager.GetNearestSpawn(12345UL));

    [Fact]
    public void SetAndGetNearestSpawn_RoundTrips()
    {
        var spawn = new SpawnPoint { Team = 2, BombSite = 0 };
        _manager.SetNearestSpawn(12345UL, spawn);
        Assert.Equal(spawn, _manager.GetNearestSpawn(12345UL));
    }

    [Fact]
    public void HasUnsavedChanges_IsFalseByDefault() =>
        Assert.False(_manager.HasUnsavedChanges);

    [Fact]
    public void MarkUnsaved_SetsHasUnsavedChangesToTrue()
    {
        _manager.MarkUnsaved();
        Assert.True(_manager.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSaved_ClearsHasUnsavedChanges()
    {
        _manager.MarkUnsaved();
        _manager.MarkSaved();
        Assert.False(_manager.HasUnsavedChanges);
    }

    [Fact]
    public void Clear_ResetsAllState()
    {
        _manager.EnableVisualization(1UL);
        _manager.SetNearestSpawn(1UL, new SpawnPoint());
        _manager.MarkUnsaved();
        _manager.Clear();
        Assert.False(_manager.IsVisualizationEnabled(1UL));
        Assert.Null(_manager.GetNearestSpawn(1UL));
        Assert.False(_manager.HasUnsavedChanges);
    }
}
```

- [ ] **Step 2: Lancer les tests — vérifier FAIL**

```
dotnet test --filter "AdminSessionManagerTests" --verbosity normal
```

Attendu : FAIL (classe non définie)

- [ ] **Step 3: Implémenter RetakeSpawnEditor/Managers/AdminSessionManager.cs**

```csharp
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor.Managers;

public class AdminSessionManager
{
    private readonly HashSet<ulong> _activeAdmins = new();
    private readonly Dictionary<ulong, SpawnPoint?> _nearestSpawn = new();
    private bool _hasUnsavedChanges;

    public bool HasUnsavedChanges => _hasUnsavedChanges;

    public void EnableVisualization(ulong steamId) => _activeAdmins.Add(steamId);

    public void DisableVisualization(ulong steamId)
    {
        _activeAdmins.Remove(steamId);
        _nearestSpawn.Remove(steamId);
    }

    public bool IsVisualizationEnabled(ulong steamId) => _activeAdmins.Contains(steamId);

    public IReadOnlyList<ulong> GetAdminsWithVisualization() => _activeAdmins.ToList();

    public void SetNearestSpawn(ulong steamId, SpawnPoint? spawn) =>
        _nearestSpawn[steamId] = spawn;

    public SpawnPoint? GetNearestSpawn(ulong steamId) =>
        _nearestSpawn.TryGetValue(steamId, out var spawn) ? spawn : null;

    public void MarkUnsaved() => _hasUnsavedChanges = true;
    public void MarkSaved() => _hasUnsavedChanges = false;

    public void Clear()
    {
        _activeAdmins.Clear();
        _nearestSpawn.Clear();
        _hasUnsavedChanges = false;
    }
}
```

- [ ] **Step 4: Lancer les tests — vérifier PASS**

```
dotnet test --filter "AdminSessionManagerTests" --verbosity normal
```

Attendu : `Passed! - 10 tests passed.`

- [ ] **Step 5: Commit**

```
git add RetakeSpawnEditor/Managers/AdminSessionManager.cs RetakeSpawnEditor.Tests/AdminSessionManagerTests.cs
git commit -m "feat: add AdminSessionManager with tests"
```

---

### Task 4: VisualizationManager

**Files:**
- Create: `RetakeSpawnEditor/Managers/VisualizationManager.cs`

**Interfaces:**
- Consumes: `SpawnPoint` (Task 2), `SpawnEditorConfig` (Task 2)
- Produces:
  - `VisualizationManager(SpawnEditorConfig config)`
  - `VisualizationManager.RebuildMarkers(IReadOnlyList<SpawnPoint> spawns) : void`
  - `VisualizationManager.ClearAll() : void`
  - `VisualizationManager.UpdateHighlight(SpawnPoint? nearest) : void`

Note: Code CSS in-game uniquement — pas de tests unitaires automatisés. Validation manuelle en Task 6.

- [ ] **Step 1: Créer RetakeSpawnEditor/Managers/VisualizationManager.cs**

```csharp
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakeSpawnEditor.Config;
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor.Managers;

public class VisualizationManager
{
    private readonly SpawnEditorConfig _config;
    private readonly List<CBeam> _pillars = new();
    private readonly List<CPointWorldText> _labels = new();
    private readonly List<CBeam> _highlights = new();

    private static readonly Color TerroristColor = Color.FromArgb(255, 255, 60, 60);
    private static readonly Color CounterTerroristColor = Color.FromArgb(255, 60, 130, 255);
    private static readonly Color HighlightColor = Color.FromArgb(255, 255, 255, 0);

    public VisualizationManager(SpawnEditorConfig config)
    {
        _config = config;
    }

    public void RebuildMarkers(IReadOnlyList<SpawnPoint> spawns)
    {
        ClearAll();
        for (var i = 0; i < spawns.Count; i++)
        {
            CreatePillar(spawns[i]);
            CreateLabel(spawns[i], i);
        }
    }

    public void ClearAll()
    {
        ClearList(_pillars);
        ClearList(_labels);
        ClearList(_highlights);
    }

    public void UpdateHighlight(SpawnPoint? nearest)
    {
        ClearList(_highlights);
        if (nearest != null) CreateHighlightRing(nearest);
    }

    private void CreatePillar(SpawnPoint spawn)
    {
        var beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null || !beam.IsValid) return;

        var baseColor = spawn.Team == 2 ? TerroristColor : CounterTerroristColor;
        beam.Render = spawn.BombSite == 1
            ? Color.FromArgb(200, baseColor.R, baseColor.G, baseColor.B)
            : baseColor;
        beam.Width = 3.0f;
        beam.EndWidth = 3.0f;
        beam.Amplitude = 0;
        beam.Speed = 0;

        var bottom = new Vector(spawn.PositionX, spawn.PositionY, spawn.PositionZ);
        var top = new Vector(spawn.PositionX, spawn.PositionY, spawn.PositionZ + _config.BeamHeight);
        beam.Teleport(bottom, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        beam.SetStartPos(bottom);
        beam.SetEndPos(top);
        beam.DispatchSpawn();
        _pillars.Add(beam);
    }

    private void CreateLabel(SpawnPoint spawn, int index)
    {
        var text = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        if (text == null || !text.IsValid) return;

        text.MessageText = $"[{spawn.TeamLabel}][{spawn.SiteLabel}] #{index:D2}";
        text.Enabled = true;
        text.FontSize = 80;
        text.Color = spawn.Team == 2 ? TerroristColor : CounterTerroristColor;
        text.WorldUnitsPerPx = 0.25f;
        text.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
        text.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
        text.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        var pos = new Vector(spawn.PositionX, spawn.PositionY, spawn.PositionZ + _config.BeamHeight + 20f);
        text.Teleport(pos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        text.DispatchSpawn();
        _labels.Add(text);
    }

    private void CreateHighlightRing(SpawnPoint spawn)
    {
        const float radius = 30f;
        const int segments = 8;

        for (var i = 0; i < segments; i++)
        {
            var a1 = 2 * Math.PI * i / segments;
            var a2 = 2 * Math.PI * (i + 1) / segments;

            var beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam == null || !beam.IsValid) continue;

            beam.Render = HighlightColor;
            beam.Width = 2.0f;
            beam.EndWidth = 2.0f;
            beam.Amplitude = 0;

            var p1 = new Vector(
                spawn.PositionX + radius * (float)Math.Cos(a1),
                spawn.PositionY + radius * (float)Math.Sin(a1),
                spawn.PositionZ + 5f);
            var p2 = new Vector(
                spawn.PositionX + radius * (float)Math.Cos(a2),
                spawn.PositionY + radius * (float)Math.Sin(a2),
                spawn.PositionZ + 5f);

            beam.Teleport(p1, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            beam.SetStartPos(p1);
            beam.SetEndPos(p2);
            beam.DispatchSpawn();
            _highlights.Add(beam);
        }
    }

    private static void ClearList<T>(List<T> entities) where T : CBaseEntity
    {
        foreach (var e in entities.Where(e => e.IsValid)) e.Remove();
        entities.Clear();
    }
}
```

Note: si `PointWorldTextJustifyHorizontal_t` n'est pas disponible dans la version CSS, remplacer par le cast entier :
```csharp
text.JustifyHorizontal = (PointWorldTextJustifyHorizontal_t)0;
text.JustifyVertical = (PointWorldTextJustifyVertical_t)0;
text.ReorientMode = (PointWorldTextReorientMode_t)0;
```

- [ ] **Step 2: Compiler**

```
cd RetakeSpawnEditor
dotnet build
```

Attendu : `Build succeeded. 0 Error(s).`

- [ ] **Step 3: Commit**

```
git add RetakeSpawnEditor/Managers/VisualizationManager.cs
git commit -m "feat: add VisualizationManager with CBeam pillars and CPointWorldText labels"
```

---

### Task 5: Commandes + plugin complet

**Files:**
- Create: `RetakeSpawnEditor/SpawnCommandLogic.cs`
- Modify: `RetakeSpawnEditor/SpawnEditorPlugin.cs`
- Create: `RetakeSpawnEditor.Tests/SpawnCommandLogicTests.cs`

**Interfaces:**
- Consumes: `SpawnPoint`, `SpawnEditorConfig`, `SpawnFileManager`, `AdminSessionManager`, `VisualizationManager`
- Produces:
  - `SpawnCommandLogic.FindNearestSpawn(IReadOnlyList<SpawnPoint>, float px, float py, float pz, float maxDist) : SpawnPoint?`
  - Commandes : `css_se`, `css_se_add`, `css_se_del`, `css_se_set`, `css_se_zone`, `css_se_tp`, `css_se_list`, `css_se_save`, `css_se_reload`

- [ ] **Step 1: Créer RetakeSpawnEditor/SpawnCommandLogic.cs**

```csharp
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor;

public static class SpawnCommandLogic
{
    public static SpawnPoint? FindNearestSpawn(
        IReadOnlyList<SpawnPoint> spawns,
        float px, float py, float pz,
        float maxDistance)
    {
        SpawnPoint? nearest = null;
        var minDist = float.MaxValue;

        foreach (var spawn in spawns)
        {
            var dx = spawn.PositionX - px;
            var dy = spawn.PositionY - py;
            var dz = spawn.PositionZ - pz;
            var dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dist < minDist && dist <= maxDistance)
            {
                minDist = dist;
                nearest = spawn;
            }
        }

        return nearest;
    }
}
```

- [ ] **Step 2: Écrire RetakeSpawnEditor.Tests/SpawnCommandLogicTests.cs**

```csharp
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor.Tests;

public class SpawnCommandLogicTests
{
    [Fact]
    public void FindNearestSpawn_ReturnsNull_WhenListIsEmpty()
    {
        var result = SpawnCommandLogic.FindNearestSpawn(new List<SpawnPoint>(), 0f, 0f, 0f, 150f);
        Assert.Null(result);
    }

    [Fact]
    public void FindNearestSpawn_ReturnsNearest()
    {
        var spawns = new List<SpawnPoint>
        {
            new SpawnPoint { PositionX = 0, PositionY = 0, PositionZ = 0 },
            new SpawnPoint { PositionX = 100, PositionY = 0, PositionZ = 0 },
            new SpawnPoint { PositionX = 50, PositionY = 0, PositionZ = 0 }
        };
        var result = SpawnCommandLogic.FindNearestSpawn(spawns, 40f, 0f, 0f, 200f);
        Assert.Equal(50f, result!.PositionX);
    }

    [Fact]
    public void FindNearestSpawn_ReturnsNull_WhenAllBeyondMaxDistance()
    {
        var spawns = new List<SpawnPoint>
        {
            new SpawnPoint { PositionX = 1000, PositionY = 1000, PositionZ = 0 }
        };
        var result = SpawnCommandLogic.FindNearestSpawn(spawns, 0f, 0f, 0f, 50f);
        Assert.Null(result);
    }

    [Fact]
    public void FindNearestSpawn_ReturnsSpawnWithinMaxDistance()
    {
        var spawns = new List<SpawnPoint>
        {
            new SpawnPoint { PositionX = 100, PositionY = 0, PositionZ = 0 }
        };
        var result = SpawnCommandLogic.FindNearestSpawn(spawns, 0f, 0f, 0f, 150f);
        Assert.NotNull(result);
    }
}
```

- [ ] **Step 3: Lancer les tests SpawnCommandLogicTests**

```
dotnet test --filter "SpawnCommandLogicTests" --verbosity normal
```

Attendu : `Passed! - 4 tests passed.`

- [ ] **Step 4: Remplacer SpawnEditorPlugin.cs par la version complète**

```csharp
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakeSpawnEditor.Config;
using RetakeSpawnEditor.Managers;
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor;

[MinimumApiVersion(228)]
public class SpawnEditorPlugin : BasePlugin, IPluginConfig<SpawnEditorConfig>
{
    public override string ModuleName => "RetakeSpawnEditor";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "local";

    public SpawnEditorConfig Config { get; set; } = new();

    private SpawnFileManager _fileManager = null!;
    private VisualizationManager _vizManager = null!;
    private AdminSessionManager _adminSession = null!;
    private List<SpawnPoint> _spawns = new();
    private string _currentMap = string.Empty;

    public void OnConfigParsed(SpawnEditorConfig config) => Config = config;

    public override void Load(bool hotReload)
    {
        _fileManager = new SpawnFileManager(Path.Combine(ModuleDirectory, Config.SpawnFolderPath));
        _vizManager = new VisualizationManager(Config);
        _adminSession = new AdminSessionManager();

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.OnTick>(OnTick);

        AddCommand("css_se", "Toggle spawn visualization", CmdToggleVis);
        AddCommand("css_se_add", "Add spawn at current position [T|CT] [A|B]", CmdAdd);
        AddCommand("css_se_del", "Delete nearest spawn", CmdDel);
        AddCommand("css_se_set", "Edit nearest spawn [T|CT] [A|B]", CmdSet);
        AddCommand("css_se_zone", "Toggle IsInBombZone on nearest spawn", CmdZone);
        AddCommand("css_se_tp", "Teleport to spawn by index", CmdTeleport);
        AddCommand("css_se_list", "List all spawns", CmdList);
        AddCommand("css_se_save", "Save spawns to JSON", CmdSave);
        AddCommand("css_se_reload", "Reload spawns from JSON", CmdReload);

        if (hotReload) LoadCurrentMapSpawns();
        Console.WriteLine("[RetakeSpawnEditor] Loaded.");
    }

    public override void Unload(bool hotReload)
    {
        _vizManager.ClearAll();
        _adminSession.Clear();
    }

    private void OnMapStart(string mapName)
    {
        _vizManager.ClearAll();
        _adminSession.Clear();
        _currentMap = mapName;
        _spawns = _fileManager.LoadSpawns(mapName);
        Console.WriteLine($"[RetakeSpawnEditor] Map {mapName}: {_spawns.Count} spawns chargés.");
    }

    private void OnMapEnd()
    {
        _vizManager.ClearAll();
        _adminSession.Clear();
    }

    private void OnTick()
    {
        foreach (var steamId in _adminSession.GetAdminsWithVisualization())
        {
            var player = FindPlayerBySteamId(steamId);
            if (player?.PlayerPawn?.Value?.AbsOrigin == null) continue;

            var pos = player.PlayerPawn.Value.AbsOrigin;
            var nearest = SpawnCommandLogic.FindNearestSpawn(
                _spawns, pos.X, pos.Y, pos.Z, Config.MaxHighlightDistance);

            if (nearest?.SpawnId != _adminSession.GetNearestSpawn(steamId)?.SpawnId)
            {
                _adminSession.SetNearestSpawn(steamId, nearest);
                _vizManager.UpdateHighlight(nearest);
            }

            var totalT = _spawns.Count(s => s.Team == 2);
            var totalCT = _spawns.Count(s => s.Team == 3);
            var line1 = $"[SpawnEditor ON] {_spawns.Count} spawns | T:{totalT} CT:{totalCT}";

            string line2;
            if (nearest != null)
            {
                var idx = _spawns.IndexOf(nearest);
                var dx = nearest.PositionX - pos.X;
                var dy = nearest.PositionY - pos.Y;
                var dz = nearest.PositionZ - pos.Z;
                var dist = (int)MathF.Sqrt(dx * dx + dy * dy + dz * dz);
                line2 = $"Proche: #{idx:D2} [{nearest.TeamLabel}][{nearest.SiteLabel}] InZone:{(nearest.IsInBombZone ? "oui" : "non")} Dist:{dist}u";
            }
            else
            {
                line2 = "Aucun spawn proche (css_se_add pour ajouter)";
            }

            player.PrintToCenter($"{line1}\n{line2}");
        }
    }

    private void CmdToggleVis(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        var steamId = player!.SteamID;

        if (_adminSession.IsVisualizationEnabled(steamId))
        {
            _adminSession.DisableVisualization(steamId);
            if (!_adminSession.GetAdminsWithVisualization().Any()) _vizManager.ClearAll();
            player.PrintToChat("[SpawnEditor] Visualisation désactivée.");
        }
        else
        {
            _adminSession.EnableVisualization(steamId);
            _vizManager.RebuildMarkers(_spawns);
            player.PrintToChat($"[SpawnEditor] Visualisation activée — {_spawns.Count} spawns.");
        }
    }

    private void CmdAdd(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        if (player!.PlayerPawn?.Value?.AbsOrigin == null) return;

        var team = info.GetArg(1).ToUpper() == "CT" ? 3 : 2;
        var site = info.GetArg(2).ToUpper() == "B" ? 1 : 0;
        var pos = player.PlayerPawn.Value.AbsOrigin;
        var rot = player.PlayerPawn.Value.AbsRotation;

        var spawn = new SpawnPoint
        {
            SpawnId = Guid.NewGuid(),
            Team = team, BombSite = site,
            IsInBombZone = player.PlayerPawn.Value.InBombZone ?? false,
            PositionX = pos.X, PositionY = pos.Y, PositionZ = pos.Z,
            QAngleX = rot?.X ?? 0f, QAngleY = rot?.Y ?? 0f, QAngleZ = rot?.Z ?? 0f
        };

        _spawns.Add(spawn);
        _adminSession.MarkUnsaved();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn #{_spawns.Count - 1:D2} ajouté [{spawn.TeamLabel}][{spawn.SiteLabel}]. Sauvegarde: css_se_save");
    }

    private void CmdDel(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        var nearest = _adminSession.GetNearestSpawn(player!.SteamID);
        if (nearest == null) { player.PrintToChat("[SpawnEditor] Aucun spawn proche."); return; }

        _spawns.Remove(nearest);
        _adminSession.SetNearestSpawn(player.SteamID, null);
        _adminSession.MarkUnsaved();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn supprimé. Total: {_spawns.Count}. Sauvegarde: css_se_save");
    }

    private void CmdSet(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        var nearest = _adminSession.GetNearestSpawn(player!.SteamID);
        if (nearest == null) { player.PrintToChat("[SpawnEditor] Aucun spawn proche."); return; }

        var teamArg = info.GetArg(1).ToUpper();
        var siteArg = info.GetArg(2).ToUpper();
        if (teamArg is "T" or "CT") nearest.Team = teamArg == "CT" ? 3 : 2;
        if (siteArg is "A" or "B") nearest.BombSite = siteArg == "B" ? 1 : 0;

        _adminSession.MarkUnsaved();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn modifié → [{nearest.TeamLabel}][{nearest.SiteLabel}]. Sauvegarde: css_se_save");
    }

    private void CmdZone(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        var nearest = _adminSession.GetNearestSpawn(player!.SteamID);
        if (nearest == null) { player.PrintToChat("[SpawnEditor] Aucun spawn proche."); return; }
        nearest.IsInBombZone = !nearest.IsInBombZone;
        _adminSession.MarkUnsaved();
        player.PrintToChat($"[SpawnEditor] IsInBombZone → {nearest.IsInBombZone}. Sauvegarde: css_se_save");
    }

    private void CmdTeleport(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        if (!int.TryParse(info.GetArg(1), out var idx) || idx < 0 || idx >= _spawns.Count)
        {
            player!.PrintToChat($"[SpawnEditor] Usage: css_se_tp <0-{_spawns.Count - 1}>");
            return;
        }
        var spawn = _spawns[idx];
        player!.PlayerPawn?.Value?.Teleport(
            new Vector(spawn.PositionX, spawn.PositionY, spawn.PositionZ),
            new QAngle(spawn.QAngleX, spawn.QAngleY, spawn.QAngleZ),
            new Vector(0, 0, 0));
        player.PrintToChat($"[SpawnEditor] TP spawn #{idx:D2} [{spawn.TeamLabel}][{spawn.SiteLabel}]");
    }

    private void CmdList(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        player!.PrintToChat($"[SpawnEditor] {_spawns.Count} spawns — {_currentMap}:");
        for (var i = 0; i < _spawns.Count; i++)
        {
            var s = _spawns[i];
            player.PrintToChat($"  #{i:D2} [{s.TeamLabel}][{s.SiteLabel}] InZone:{s.IsInBombZone} ({s.PositionX:F0},{s.PositionY:F0},{s.PositionZ:F0})");
        }
    }

    private void CmdSave(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        _fileManager.SaveSpawns(_currentMap, _spawns);
        _adminSession.MarkSaved();
        player!.PrintToChat($"[SpawnEditor] {_spawns.Count} spawns sauvegardés → {_currentMap}.json");
    }

    private void CmdReload(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        _spawns = _fileManager.LoadSpawns(_currentMap);
        _adminSession.MarkSaved();
        if (_adminSession.IsVisualizationEnabled(player!.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] {_spawns.Count} spawns rechargés depuis {_currentMap}.json");
    }

    private bool IsAdmin(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid) return false;
        if (!AdminManager.PlayerHasPermissions(player, Config.AdminFlag))
        {
            player.PrintToChat("[SpawnEditor] Accès refusé — @css/admin requis.");
            return false;
        }
        return true;
    }

    private void LoadCurrentMapSpawns()
    {
        _currentMap = Server.MapName;
        _spawns = _fileManager.LoadSpawns(_currentMap);
    }

    private static CCSPlayerController? FindPlayerBySteamId(ulong steamId) =>
        Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
}
```

- [ ] **Step 5: Compiler**

```
cd RetakeSpawnEditor
dotnet build
```

Attendu : `Build succeeded. 0 Error(s).`

Erreurs fréquentes et corrections :
- `InBombZone` non disponible → remplacer par `false`
- `AdminManager.PlayerHasPermissions` avec mauvaise signature → utiliser `AdminManager.PlayerHasPermissions(player, Config.AdminFlag)`

- [ ] **Step 6: Tous les tests**

```
dotnet test --verbosity normal
```

Attendu : `Passed! - 21 tests passed.` (7 + 10 + 4)

- [ ] **Step 7: Commit**

```
git add RetakeSpawnEditor/SpawnCommandLogic.cs RetakeSpawnEditor/SpawnEditorPlugin.cs RetakeSpawnEditor.Tests/SpawnCommandLogicTests.cs
git commit -m "feat: complete SpawnEditorPlugin with all commands and OnTick HUD"
```

---

### Task 6: Solution + build release + test manuel

**Files:**
- Create: `SpawnEditor.sln`

**Interfaces:**
- Consumes: tous fichiers précédents
- Produces: DLL release déployable

- [ ] **Step 1: Créer la solution**

```
dotnet new sln -n SpawnEditor
dotnet sln SpawnEditor.sln add RetakeSpawnEditor/RetakeSpawnEditor.csproj
dotnet sln SpawnEditor.sln add RetakeSpawnEditor.Tests/RetakeSpawnEditor.Tests.csproj
```

- [ ] **Step 2: Build release**

```
cd RetakeSpawnEditor
dotnet build -c Release
```

DLL produite : `RetakeSpawnEditor/bin/Release/net8.0/RetakeSpawnEditor.dll`

- [ ] **Step 3: Copier vers CS2**

Copier `RetakeSpawnEditor.dll` vers :
```
game/csgo/addons/counterstrikesharp/plugins/RetakeSpawnEditor/RetakeSpawnEditor.dll
```

- [ ] **Step 4: Test manuel — checklist complète**

Lancer CS2 local sur `de_dust2` avec CS2Retake actif :

| Test | Commande | Résultat attendu |
|------|----------|-----------------|
| Chargement | démarrage | Console: `Map de_dust2: N spawns chargés` |
| Toggle vis | `css_se` | Piliers rouge (T) et bleu (CT) apparaissent |
| HUD | rester immobile | PrintToCenter: nombre de spawns |
| Approche spawn | marcher | HUD: `Proche: #XX [T][A]` + anneau jaune au sol |
| Add T-A | `css_se_add T A` | Nouveau pilier rouge aux pieds |
| Add CT-B | `css_se_add CT B` | Nouveau pilier bleu |
| Del | approcher + `css_se_del` | Pilier disparu |
| Set team | `css_se_set CT B` | Couleur pilier change rouge→bleu |
| Zone | `css_se_zone` | HUD InZone toggle |
| TP | `css_se_tp 5` | Téléportation spawn #5 |
| List | `css_se_list` | Liste dans le chat |
| Save | `css_se_save` | `de_dust2.json` mis à jour |
| Reload | `css_se_reload` | Spawns rechargés |
| Map change | changer de map | Piliers disparus, nouveaux spawns chargés |
| Non-admin | commande sans flag | Message: `@css/admin requis` |

- [ ] **Step 5: Commit final**

```
git add SpawnEditor.sln
git commit -m "feat: complete RetakeSpawnEditor — add solution file, plugin ready for deployment"
```

---

## Résumé des tests automatisés

| Suite | Tests | Couverture |
|-------|-------|-----------|
| SpawnFileManagerTests | 7 | SpawnFileManager load/save/guid, SpawnPoint labels |
| AdminSessionManagerTests | 10 | enable/disable, nearest, unsaved, clear |
| SpawnCommandLogicTests | 4 | FindNearestSpawn (vide, plus proche, hors distance) |
| **Total** | **21** | |

VisualizationManager et commandes in-game → test manuel Task 6.
