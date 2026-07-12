namespace GermanyDashboard.Domain.Entities;

public class Indicator
{
    public int Id { get; set; }
    public required string Slug { get; set; } // e.g. "unemployment-rate"
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Unit { get; set; } // e.g. "%", "EUR", "persons"
    public string? ValueFormat { get; set; } // e.g. "percent", "currency-eur", "number", "years"

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? DataSourceId { get; set; }
    public DataSource? DataSource { get; set; }

    public ICollection<DataPoint> DataPoints { get; set; } = new List<DataPoint>();
}
