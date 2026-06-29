using Xunit;
using RetakeSpawnEditor.Managers;
using RetakeSpawnEditor.Models;

namespace RetakeSpawnEditor.Tests;

public class AdminSessionManagerTests
{
    private readonly AdminSessionManager _manager = new();

    [Fact]
    public void IsVisualizationEnabled_ReturnsFalse_WhenNotEnabled() =>
        Assert.False(_manager.IsVisualizationEnabled(12345UL));

    [Fact]
    public void EnableVisualization_MakesIsVisualizationEnabledReturnTrue()
    {
        _manager.EnableVisualization(12345UL);
        Assert.True(_manager.IsVisualizationEnabled(12345UL));
    }

    [Fact]
    public void DisableVisualization_MakesIsVisualizationEnabledReturnFalse()
    {
        _manager.EnableVisualization(12345UL);
        _manager.DisableVisualization(12345UL);
        Assert.False(_manager.IsVisualizationEnabled(12345UL));
    }

    [Fact]
    public void GetAdminsWithVisualization_ReturnsOnlyEnabledAdmins()
    {
        _manager.EnableVisualization(1UL);
        _manager.EnableVisualization(2UL);
        _manager.DisableVisualization(1UL);
        var result = _manager.GetAdminsWithVisualization();
        Assert.Single(result);
        Assert.Equal(2UL, result[0]);
    }

    [Fact]
    public void GetNearestSpawn_ReturnsNull_WhenNotSet() =>
        Assert.Null(_manager.GetNearestSpawn(12345UL));

    [Fact]
    public void SetAndGetNearestSpawn_RoundTrips()
    {
        var spawn = new SpawnPoint { Team = 2, BombSite = 0 };
        _manager.SetNearestSpawn(12345UL, spawn);
        Assert.Equal(spawn, _manager.GetNearestSpawn(12345UL));
    }

    [Fact]
    public void HasUnsavedChanges_IsFalseByDefault() =>
        Assert.False(_manager.HasUnsavedChanges);

    [Fact]
    public void MarkUnsaved_SetsHasUnsavedChangesToTrue()
    {
        _manager.MarkUnsaved();
        Assert.True(_manager.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSaved_ClearsHasUnsavedChanges()
    {
        _manager.MarkUnsaved();
        _manager.MarkSaved();
        Assert.False(_manager.HasUnsavedChanges);
    }

    [Fact]
    public void Clear_ResetsAllState()
    {
        _manager.EnableVisualization(1UL);
        _manager.SetNearestSpawn(1UL, new SpawnPoint());
        _manager.MarkUnsaved();
        _manager.Clear();
        Assert.False(_manager.IsVisualizationEnabled(1UL));
        Assert.Null(_manager.GetNearestSpawn(1UL));
        Assert.False(_manager.HasUnsavedChanges);
    }
}
