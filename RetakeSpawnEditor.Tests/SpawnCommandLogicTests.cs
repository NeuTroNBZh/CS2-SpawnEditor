using Xunit;
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
