using Xunit;
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
