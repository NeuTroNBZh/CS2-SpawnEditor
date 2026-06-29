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
    public override string ModuleAuthor => "NeuTroNBZh";

    public SpawnEditorConfig Config { get; set; } = new();

    private SpawnFileManager _fileManager = null!;
    private VisualizationManager _vizManager = null!;
    private AdminSessionManager _adminSession = null!;
    private List<SpawnPoint> _spawns = new();
    private string _currentMap = string.Empty;
    private int _countT;
    private int _countCT;

    // Admins qui ont le noclip actif (independant de la visu)
    private readonly HashSet<ulong> _noclipAdmins = new();

    public void OnConfigParsed(SpawnEditorConfig config) => Config = config;

    public override void Load(bool hotReload)
    {
        _fileManager = new SpawnFileManager(Path.Combine(ModuleDirectory, Config.SpawnFolderPath));
        _vizManager = new VisualizationManager(Config);
        _adminSession = new AdminSessionManager();

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.OnTick>(OnTick);

        AddCommand("css_se",         "Toggle spawn visualization",               CmdToggleVis);
        AddCommand("css_se_noclip",  "Toggle noclip (editor mode)",              CmdNoclip);
        AddCommand("css_se_add",     "Add spawn at current position [T|CT] [A|B]", CmdAdd);
        AddCommand("css_se_del",     "Delete nearest spawn",                     CmdDel);
        AddCommand("css_se_set",     "Edit nearest spawn [T|CT] [A|B]",         CmdSet);
        AddCommand("css_se_zone",    "Toggle IsInBombZone on nearest spawn",     CmdZone);
        AddCommand("css_se_tp",      "Teleport to spawn by index",               CmdTeleport);
        AddCommand("css_se_list",    "List all spawns",                          CmdList);
        AddCommand("css_se_save",    "Save spawns to JSON",                      CmdSave);
        AddCommand("css_se_reload",  "Reload spawns from JSON",                  CmdReload);

        if (hotReload) LoadCurrentMapSpawns();
        Console.WriteLine("[RetakeSpawnEditor] Loaded.");
    }

    public override void Unload(bool hotReload)
    {
        // Retirer le noclip de tous les admins avant le decharge
        foreach (var steamId in _noclipAdmins.ToList())
        {
            var p = FindPlayerBySteamId(steamId);
            if (p != null) SetNoclip(p, false);
        }
        _noclipAdmins.Clear();

        if (_adminSession.GetAdminsWithVisualization().Any())
            ExitEditorMode();

        _vizManager.ClearAll();
        _adminSession.Clear();
        SimpleAdminBridge.UnregisterMenus();
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        SimpleAdminBridge.TryInit(this);
        SimpleAdminBridge.RegisterMenus();
    }

    // -- Listeners --

    private void OnMapStart(string mapName)
    {
        _noclipAdmins.Clear();
        if (_adminSession.GetAdminsWithVisualization().Any())
            ExitEditorMode();
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
        _noclipAdmins.Clear();
    }

    private void OnTick()
    {
        var admins = _adminSession.GetAdminsWithVisualization();
        foreach (var steamId in admins)
        {
            var player = FindPlayerBySteamId(steamId);
            if (player?.PlayerPawn?.Value?.AbsOrigin == null) continue;

            var pos = player.PlayerPawn.Value.AbsOrigin;
            var nearest = SpawnCommandLogic.FindNearestSpawn(
                _spawns, pos.X, pos.Y, pos.Z, Config.MaxHighlightDistance);

            if (nearest?.SpawnId != _adminSession.GetNearestSpawn(steamId)?.SpawnId)
            {
                _adminSession.SetNearestSpawn(steamId, nearest);
                _vizManager.UpdateHighlight(admins.Count == 1 ? nearest : null);
            }

            var noclipStatus = _noclipAdmins.Contains(steamId) ? " | Noclip ON" : "";
            var line1 = $"[SpawnEditor ON] {_spawns.Count} spawns | T:{_countT} CT:{_countCT}{noclipStatus}";
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

    // -- Commandes --

    private void CmdToggleVis(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        ToggleVisualizationForAdmin(player!);
    }

    private void CmdNoclip(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        ToggleNoclipForAdmin(player!);
    }

    private void CmdAdd(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        AddSpawnForAdmin(player!, info.GetArg(1).ToUpper(), info.GetArg(2).ToUpper());
    }

    private void CmdDel(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        DeleteNearestForAdmin(player!);
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
        UpdateSpawnCounts();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn modifie -> [{nearest.TeamLabel}][{nearest.SiteLabel}]. Sauvegarde: css_se_save");
    }

    private void CmdZone(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        ToggleBombZoneForAdmin(player!);
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
        SaveSpawnsForAdmin(player!);
    }

    private void CmdReload(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsAdmin(player)) return;
        ReloadSpawnsForAdmin(player!);
    }

    // -- Methodes internes (appelables depuis SimpleAdminBridge) --

    internal void ToggleVisualizationForAdmin(CCSPlayerController player)
    {
        var steamId = player.SteamID;
        if (_adminSession.IsVisualizationEnabled(steamId))
        {
            _adminSession.DisableVisualization(steamId);
            if (!_adminSession.GetAdminsWithVisualization().Any())
            {
                _vizManager.ClearAll();
                ExitEditorMode();
            }
            player.PrintToChat("[SpawnEditor] Visualisation desactivee -- retake repris.");
        }
        else
        {
            var wasFirstAdmin = !_adminSession.GetAdminsWithVisualization().Any();
            _adminSession.EnableVisualization(steamId);
            _vizManager.RebuildMarkers(_spawns);
            if (wasFirstAdmin) EnterEditorMode();
            player.PrintToChat($"[SpawnEditor] Visualisation activee ({_spawns.Count} spawns) -- utilisez css_se_noclip pour voler.");
        }
    }

    internal void ToggleNoclipForAdmin(CCSPlayerController player)
    {
        var steamId = player.SteamID;
        if (_noclipAdmins.Contains(steamId))
        {
            _noclipAdmins.Remove(steamId);
            SetNoclip(player, false);
            player.PrintToChat("[SpawnEditor] Noclip OFF.");
        }
        else
        {
            _noclipAdmins.Add(steamId);
            SetNoclip(player, true);
            player.PrintToChat("[SpawnEditor] Noclip ON -- css_se_noclip pour desactiver.");
        }
    }

    internal void ToggleBombZoneForAdmin(CCSPlayerController player)
    {
        var nearest = _adminSession.GetNearestSpawn(player.SteamID);
        if (nearest == null) { player.PrintToChat("[SpawnEditor] Aucun spawn proche."); return; }
        nearest.IsInBombZone = !nearest.IsInBombZone;
        _adminSession.MarkUnsaved();
        player.PrintToChat($"[SpawnEditor] IsInBombZone -> {nearest.IsInBombZone}. Sauvegarde: css_se_save");
    }

    internal void AddSpawnForAdmin(CCSPlayerController player, string teamArg, string siteArg)
    {
        if (player.PlayerPawn?.Value?.AbsOrigin == null) return;
        var team = teamArg == "CT" ? 3 : 2;
        var site = siteArg == "B" ? 1 : 0;
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
        _adminSession.MarkUnsaved();
        UpdateSpawnCounts();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn #{_spawns.Count - 1:D2} ajoute [{spawn.TeamLabel}][{spawn.SiteLabel}]. Sauvegarde: css_se_save");
    }

    internal void DeleteNearestForAdmin(CCSPlayerController player)
    {
        var nearest = _adminSession.GetNearestSpawn(player.SteamID);
        if (nearest == null) { player.PrintToChat("[SpawnEditor] Aucun spawn proche."); return; }

        _spawns.Remove(nearest);
        _adminSession.SetNearestSpawn(player.SteamID, null);
        _adminSession.MarkUnsaved();
        UpdateSpawnCounts();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] Spawn supprime. Total: {_spawns.Count}. Sauvegarde: css_se_save");
    }

    internal void SaveSpawnsForAdmin(CCSPlayerController player)
    {
        if (string.IsNullOrEmpty(_currentMap)) { player.PrintToChat("[SpawnEditor] Aucune carte chargee."); return; }
        _fileManager.SaveSpawns(_currentMap, _spawns);
        _adminSession.MarkSaved();
        player.PrintToChat($"[SpawnEditor] {_spawns.Count} spawns sauvegardes -> {_currentMap}.json");
    }

    internal void ReloadSpawnsForAdmin(CCSPlayerController player)
    {
        if (string.IsNullOrEmpty(_currentMap)) { player.PrintToChat("[SpawnEditor] Aucune carte chargee."); return; }
        _spawns = _fileManager.LoadSpawns(_currentMap);
        _adminSession.MarkSaved();
        UpdateSpawnCounts();
        if (_adminSession.IsVisualizationEnabled(player.SteamID)) _vizManager.RebuildMarkers(_spawns);
        player.PrintToChat($"[SpawnEditor] {_spawns.Count} spawns recharges depuis {_currentMap}.json");
    }

    // -- Mode editeur (warmup infini) --

    private static void EnterEditorMode()
    {
        // Warmup infini : aucune fin de manche, pas de bombe, mouvement libre
        Server.ExecuteCommand("mp_warmup_start");
        Server.ExecuteCommand("mp_warmup_pausetimer 1");
        Console.WriteLine("[RetakeSpawnEditor] Editor mode ON -- warmup infini.");
    }

    private static void ExitEditorMode()
    {
        Server.ExecuteCommand("mp_warmup_end");
        Console.WriteLine("[RetakeSpawnEditor] Editor mode OFF -- retake repris.");
    }

    // -- Helpers --

    private static void SetNoclip(CCSPlayerController player, bool enabled)
    {
        var pawn = player.PlayerPawn?.Value;
        if (pawn == null) return;
        pawn.MoveType = enabled ? MoveType_t.MOVETYPE_NOCLIP : MoveType_t.MOVETYPE_WALK;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
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

    private void LoadCurrentMapSpawns()
    {
        _currentMap = Server.MapName;
        _spawns = _fileManager.LoadSpawns(_currentMap);
        UpdateSpawnCounts();
    }

    private void UpdateSpawnCounts()
    {
        _countT = _spawns.Count(s => s.Team == 2);
        _countCT = _spawns.Count(s => s.Team == 3);
    }

    private static CCSPlayerController? FindPlayerBySteamId(ulong steamId) =>
        Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
}
