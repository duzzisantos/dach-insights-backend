namespace GermanyDashboard.Domain.Entities;

public class DataSource
{
    public int Id { get; set; }
    public required string Name { get; set; } // e.g. "Statistisches Bundesamt (Destatis) - GENESIS-Online"
    public string? GenesisTableCode { get; set; } // e.g. "12411-0010" for population tables
    public string? Url { get; set; }
    public string? License { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }

    public ICollection<Indicator> Indicators { get; set; } = new List<Indicator>();
}
