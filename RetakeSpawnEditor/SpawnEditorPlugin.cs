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
