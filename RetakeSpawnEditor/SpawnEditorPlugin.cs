using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakeSpawnEditor.Config;
using RetakeSpawnEditor.Managers;
using RetakeSpawnEditor.Models;
using SpawnPoint = RetakeSpawnEditor.Models.SpawnPoint;

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
    private int _countT;
    private int _countCT;

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
        UpdateSpawnCounts();
        Console.WriteLine($"[RetakeSpawnEditor] Map {mapName}: {_spawns.Count} spawns charges.");
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
                // Highlight uniquement si un seul admin est en mode vis (evite conflit multi-admin)
                if (_adminSession.GetAdminsWithVisualization().Count == 1)
                    _vizManager.UpdateHighlight(nearest);
                else
                    _vizManager.UpdateHighlight(null);
            }

            var totalT = _countT;
            var totalCT = _countCT;
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
            player.PrintToChat("[SpawnEditor] Visualisation desactivee.");
        }
        else
        {
            _adminSession.EnableVisualization(steamId);
            _vizManager.RebuildMarkers(_spawns);
            player.PrintToChat($"[SpawnEditor] Visualisation activee -- {_spawns.Count} spawns.");
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
            IsInBombZone = false,
            PositionX = pos.X, PositionY = pos.Y, PositionZ = pos.Z,
            QAngleX = rot?.X ?? 0f, QAngleY = rot?.Y ?? 0f, QAngleZ = rot?.Z ?? 0f
        };

        _spawns.Add(spawn);
        UpdateSpawnCounts();
        _adminSession.MarkUnsaved();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn #{_spawns.Count - 1:D2} ajoute [{spawn.TeamLabel}][{spawn.SiteLabel}]. Sauvegarde: css_se_save");
    }

    private void CmdDel(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        var nearest = _adminSession.GetNearestSpawn(player!.SteamID);
        if (nearest == null) { player.PrintToChat("[SpawnEditor] Aucun spawn proche."); return; }

        _spawns.Remove(nearest);
        UpdateSpawnCounts();
        _adminSession.SetNearestSpawn(player.SteamID, null);
        _adminSession.MarkUnsaved();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn supprime. Total: {_spawns.Count}. Sauvegarde: css_se_save");
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
        UpdateSpawnCounts();

        _adminSession.MarkUnsaved();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn modifie -> [{nearest.TeamLabel}][{nearest.SiteLabel}]. Sauvegarde: css_se_save");
    }

    private void CmdZone(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        var nearest = _adminSession.GetNearestSpawn(player!.SteamID);
        if (nearest == null) { player.PrintToChat("[SpawnEditor] Aucun spawn proche."); return; }
        nearest.IsInBombZone = !nearest.IsInBombZone;
        _adminSession.MarkUnsaved();
        player.PrintToChat($"[SpawnEditor] IsInBombZone -> {nearest.IsInBombZone}. Sauvegarde: css_se_save");
    }

    private void CmdTeleport(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        if (_spawns.Count == 0) { player!.PrintToChat("[SpawnEditor] Aucun spawn charge."); return; }
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
        player!.PrintToChat($"[SpawnEditor] {_spawns.Count} spawns -- {_currentMap}:");
        for (var i = 0; i < _spawns.Count; i++)
        {
            var s = _spawns[i];
            player.PrintToChat($"  #{i:D2} [{s.TeamLabel}][{s.SiteLabel}] InZone:{s.IsInBombZone} ({s.PositionX:F0},{s.PositionY:F0},{s.PositionZ:F0})");
        }
    }

    private void CmdSave(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        if (string.IsNullOrEmpty(_currentMap)) { player!.PrintToChat("[SpawnEditor] Aucune carte chargee."); return; }
        _fileManager.SaveSpawns(_currentMap, _spawns);
        _adminSession.MarkSaved();
        player!.PrintToChat($"[SpawnEditor] {_spawns.Count} spawns sauvegardes -> {_currentMap}.json");
    }

    private void CmdReload(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        if (string.IsNullOrEmpty(_currentMap)) { player!.PrintToChat("[SpawnEditor] Aucune carte chargee."); return; }
        _spawns = _fileManager.LoadSpawns(_currentMap);
        UpdateSpawnCounts();
        _adminSession.MarkSaved();
        if (_adminSession.IsVisualizationEnabled(player!.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] {_spawns.Count} spawns recharges depuis {_currentMap}.json");
    }

    private bool IsAdmin(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid) return false;
        if (!AdminManager.PlayerHasPermissions(player, Config.AdminFlag))
        {
            player.PrintToChat($"[SpawnEditor] Acces refuse -- {Config.AdminFlag} requis.");
            return false;
        }
        return true;
    }

    private void UpdateSpawnCounts()
    {
        _countT = _spawns.Count(s => s.Team == 2);
        _countCT = _spawns.Count(s => s.Team == 3);
    }

    private void LoadCurrentMapSpawns()
    {
        _currentMap = Server.MapName;
        _spawns = _fileManager.LoadSpawns(_currentMap);
        UpdateSpawnCounts();
    }

    private static CCSPlayerController? FindPlayerBySteamId(ulong steamId) =>
        Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
}
