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
