namespace GermanyDashboard.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Slug { get; set; } // e.g. "economy"
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; } // icon identifier used by the frontend
    public string? ColorSlot { get; set; } // maps to a fixed categorical palette slot, e.g. "blue"
    public int SortOrder { get; set; }

    public ICollection<Indicator> Indicators { get; set; } = new List<Indicator>();
}
