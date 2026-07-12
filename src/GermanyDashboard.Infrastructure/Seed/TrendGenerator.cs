namespace GermanyDashboard.Infrastructure.Seed;

/// <summary>
/// Projects a single 2023-baseline value into a 2015-2024 series using a shared national
/// trend shape per indicator kind, so the demo data has plausible year-over-year movement
/// (growth, a 2020 dip, etc.) instead of a flat line.
/// </summary>
public static class TrendGenerator
{
    public const int FirstYear = 2015;
    public const int LastYear = 2024;
    public const int BaselineYear = 2023;

    // Cumulative multiplier relative to the 2023 baseline (1.00), oldest year first.
    private static readonly IReadOnlyDictionary<int, double> PopulationIndex = new Dictionary<int, double>
    {
        [2015] = 0.968, [2016] = 0.975, [2017] = 0.981, [2018] = 0.987, [2019] = 0.992,
        [2020] = 0.995, [2021] = 0.993, [2022] = 0.997, [2023] = 1.000, [2024] = 1.004,
    };

    private static readonly IReadOnlyDictionary<int, double> GdpPerCapitaIndex = new Dictionary<int, double>
    {
        [2015] = 0.780, [2016] = 0.805, [2017] = 0.832, [2018] = 0.858, [2019] = 0.878,
        [2020] = 0.853, [2021] = 0.894, [2022] = 0.948, [2023] = 1.000, [2024] = 1.034,
    };

    // Additive percentage-point deltas relative to the 2023 baseline (0.0), oldest year first.
    private static readonly IReadOnlyDictionary<int, double> UnemploymentDelta = new Dictionary<int, double>
    {
        [2015] = 1.8, [2016] = 1.5, [2017] = 1.1, [2018] = 0.6, [2019] = 0.2,
        [2020] = 1.0, [2021] = 0.8, [2022] = 0.1, [2023] = 0.0, [2024] = -0.1,
    };

    // Additive years-of-life deltas relative to the 2023 baseline (0.0), oldest year first.
    private static readonly IReadOnlyDictionary<int, double> LifeExpectancyDelta = new Dictionary<int, double>
    {
        [2015] = -0.9, [2016] = -0.7, [2017] = -0.6, [2018] = -0.4, [2019] = -0.2,
        [2020] = -0.6, [2021] = -0.7, [2022] = -0.3, [2023] = 0.0, [2024] = 0.15,
    };

    public static IEnumerable<(int Year, decimal Value)> Population(long baseline2023)
        => PopulationIndex.OrderBy(kv => kv.Key)
            .Select(kv => (kv.Key, (decimal)Math.Round(baseline2023 * kv.Value)));

    public static IEnumerable<(int Year, decimal Value)> GdpPerCapita(double baseline2023)
        => GdpPerCapitaIndex.OrderBy(kv => kv.Key)
            .Select(kv => (kv.Key, (decimal)Math.Round(baseline2023 * kv.Value / 100.0) * 100));

    public static IEnumerable<(int Year, decimal Value)> UnemploymentRate(double baseline2023)
        => UnemploymentDelta.OrderBy(kv => kv.Key)
            .Select(kv => (kv.Key, (decimal)Math.Round(Math.Max(1.5, baseline2023 + kv.Value), 1)));

    public static IEnumerable<(int Year, decimal Value)> LifeExpectancy(double baseline2023)
        => LifeExpectancyDelta.OrderBy(kv => kv.Key)
            .Select(kv => (kv.Key, (decimal)Math.Round(baseline2023 + kv.Value, 1)));
}
