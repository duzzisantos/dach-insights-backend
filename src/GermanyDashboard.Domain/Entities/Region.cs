using GermanyDashboard.Domain.Enums;

namespace GermanyDashboard.Domain.Entities;

public class Region
{
    public int Id { get; set; }
    public required string Code { get; set; } // ISO 3166-2, e.g. "DE-BY", or "DE" for national
    public required string Slug { get; set; } // e.g. "bayern"
    public required string Name { get; set; }
    public required string NameEnglish { get; set; }
    public RegionType Type { get; set; }
    public int? ParentRegionId { get; set; }
    public Region? ParentRegion { get; set; }
    public long? Population { get; set; }
    public double? AreaKm2 { get; set; }
    public string? Capital { get; set; }
    public string? GeoJsonKey { get; set; } // key matching the states GeoJSON feature property

    public ICollection<Region> ChildRegions { get; set; } = new List<Region>();
    public ICollection<DataPoint> DataPoints { get; set; } = new List<DataPoint>();
}
