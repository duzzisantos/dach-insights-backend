using GermanyDashboard.Infrastructure.Seed;
using Xunit;

namespace GermanyDashboard.Tests;

public class TrendGeneratorTests
{
    [Fact]
    public void Population_ReturnsOnePointPerYearInRange()
    {
        var points = TrendGenerator.Population(1_000_000).ToList();

        Assert.Equal(TrendGenerator.LastYear - TrendGenerator.FirstYear + 1, points.Count);
        Assert.All(points, p => Assert.InRange(p.Year, TrendGenerator.FirstYear, TrendGenerator.LastYear));
    }

    [Fact]
    public void Population_BaselineYearMatchesInputExactly()
    {
        var points = TrendGenerator.Population(1_000_000).ToList();

        var baseline = points.Single(p => p.Year == TrendGenerator.BaselineYear);
        Assert.Equal(1_000_000, baseline.Value);
    }

    [Fact]
    public void UnemploymentRate_NeverGoesBelowThePlausibleFloor()
    {
        var points = TrendGenerator.UnemploymentRate(1.5).ToList();

        Assert.All(points, p => Assert.True(p.Value >= 1.5m));
    }
}
