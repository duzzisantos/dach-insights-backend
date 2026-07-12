namespace GermanyDashboard.Domain.Entities;

public class DataPoint
{
    public long Id { get; set; }

    public int IndicatorId { get; set; }
    public Indicator? Indicator { get; set; }

    public int RegionId { get; set; }
    public Region? Region { get; set; }

    public int Year { get; set; }
    public decimal Value { get; set; }
}
