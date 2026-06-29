using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakeSpawnEditor.Config;
using RetakeSpawnEditor.Models;
using SpawnPoint = RetakeSpawnEditor.Models.SpawnPoint;

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
        beam.Amplitude = 0f;
        beam.Speed = 0f;

        var bottom = new Vector(spawn.PositionX, spawn.PositionY, spawn.PositionZ);
        var top = new Vector(spawn.PositionX, spawn.PositionY, spawn.PositionZ + _config.BeamHeight);
        beam.Teleport(bottom, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        var endPos = beam.EndPos;
        endPos.X = top.X;
        endPos.Y = top.Y;
        endPos.Z = top.Z;
        beam.DispatchSpawn();
        _pillars.Add(beam);
    }

    private void CreateLabel(SpawnPoint spawn, int index)
    {
        var text = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        if (text == null || !text.IsValid) return;

        text.MessageText = $"[{spawn.TeamLabel}][{spawn.SiteLabel}] #{index:D2}";
        text.Enabled = true;
        text.FontSize = 80f;
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
            beam.Amplitude = 0f;
            beam.Speed = 0f;

            var p1 = new Vector(
                spawn.PositionX + radius * (float)Math.Cos(a1),
                spawn.PositionY + radius * (float)Math.Sin(a1),
                spawn.PositionZ + 5f);
            var p2 = new Vector(
                spawn.PositionX + radius * (float)Math.Cos(a2),
                spawn.PositionY + radius * (float)Math.Sin(a2),
                spawn.PositionZ + 5f);

            beam.Teleport(p1, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            var endPos = beam.EndPos;
            endPos.X = p2.X;
            endPos.Y = p2.Y;
            endPos.Z = p2.Z;
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
