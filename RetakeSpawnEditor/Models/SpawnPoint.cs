using System.Text.Json.Serialization;

namespace RetakeSpawnEditor.Models;

public class SpawnPoint
{
    public Guid SpawnId { get; set; } = Guid.Empty;
    public int Team { get; set; }
    public int BombSite { get; set; }
    public bool IsInBombZone { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float QAngleX { get; set; }
    public float QAngleY { get; set; }
    public float QAngleZ { get; set; }

    [JsonIgnore]
    public string TeamLabel => Team == 2 ? "T" : "CT";

    [JsonIgnore]
    public string SiteLabel => BombSite == 0 ? "A" : "B";
}
