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
