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
