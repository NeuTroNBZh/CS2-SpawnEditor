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
