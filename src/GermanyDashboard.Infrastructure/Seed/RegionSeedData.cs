using GermanyDashboard.Domain.Enums;

namespace GermanyDashboard.Infrastructure.Seed;

public record RegionSeed(
    string Code,
    string Slug,
    string Name,
    string NameEnglish,
    RegionType Type,
    long Population2023,
    double AreaKm2,
    string? Capital,
    string GeoJsonKey,
    double GdpPerCapita2023,
    double UnemploymentRate2023,
    double LifeExpectancy2023
);

public record CountrySeed(RegionSeed National, IReadOnlyList<RegionSeed> States);

/// <summary>
/// Approximate 2023-baseline figures for Germany, Austria, and Switzerland and their
/// states/regions, used to seed demo data until a real sync runs (see
/// EurostatSyncService). Values are realistic order-of-magnitude placeholders, not an
/// authoritative statistical release — population is overwritten by the real synced
/// figure the moment "sync-eurostat" runs.
///
/// Germany's 16 Bundesländer are NUTS-1 regions in Eurostat's classification, so `Code`
/// uses the familiar ISO 3166-2 form (DE-BY). Austria's real 9 Bundesländer and
/// Switzerland's 7 "Greater Regions" (Grossregionen — statistical groupings of cantons,
/// not administrative units) are both NUTS-2 in Eurostat, and Switzerland's greater
/// regions have no ISO 3166-2 code at all (that only exists per-canton), so `Code` and
/// `GeoJsonKey` both just use the NUTS code directly for those two countries.
/// </summary>
public static class RegionSeedData
{
    private static readonly RegionSeed GermanyNational = new(
        "DE", "germany", "Deutschland", "Germany", RegionType.Country,
        84_400_000, 357_592, "Berlin", "DE",
        44_000, 5.7, 80.6);

    private static readonly IReadOnlyList<RegionSeed> GermanyStates = new List<RegionSeed>
    {
        new("DE-BW", "baden-wurttemberg", "Baden-Württemberg", "Baden-Württemberg", RegionType.State,
            11_280_000, 35_751, "Stuttgart", "DE-BW", 50_000, 3.6, 81.5),
        new("DE-BY", "bayern", "Bayern", "Bavaria", RegionType.State,
            13_370_000, 70_550, "München", "DE-BY", 53_000, 3.0, 81.3),
        new("DE-BE", "berlin", "Berlin", "Berlin", RegionType.State,
            3_850_000, 891, "Berlin", "DE-BE", 44_000, 8.3, 80.5),
        new("DE-BB", "brandenburg", "Brandenburg", "Brandenburg", RegionType.State,
            2_570_000, 29_654, "Potsdam", "DE-BB", 32_000, 6.5, 79.5),
        new("DE-HB", "bremen", "Bremen", "Bremen", RegionType.State,
            680_000, 419, "Bremen", "DE-HB", 55_000, 9.9, 79.6),
        new("DE-HH", "hamburg", "Hamburg", "Hamburg", RegionType.State,
            1_910_000, 755, "Hamburg", "DE-HH", 74_000, 6.6, 80.6),
        new("DE-HE", "hessen", "Hessen", "Hesse", RegionType.State,
            6_390_000, 21_115, "Wiesbaden", "DE-HE", 50_000, 4.9, 80.9),
        new("DE-MV", "mecklenburg-vorpommern", "Mecklenburg-Vorpommern", "Mecklenburg-Vorpommern", RegionType.State,
            1_610_000, 23_214, "Schwerin", "DE-MV", 30_000, 7.3, 79.0),
        new("DE-NI", "niedersachsen", "Niedersachsen", "Lower Saxony", RegionType.State,
            8_140_000, 47_710, "Hannover", "DE-NI", 40_000, 5.3, 80.3),
        new("DE-NW", "nordrhein-westfalen", "Nordrhein-Westfalen", "North Rhine-Westphalia", RegionType.State,
            17_930_000, 34_113, "Düsseldorf", "DE-NW", 42_000, 6.9, 80.0),
        new("DE-RP", "rheinland-pfalz", "Rheinland-Pfalz", "Rhineland-Palatinate", RegionType.State,
            4_110_000, 19_854, "Mainz", "DE-RP", 40_000, 4.6, 80.8),
        new("DE-SL", "saarland", "Saarland", "Saarland", RegionType.State,
            990_000, 2_570, "Saarbrücken", "DE-SL", 38_000, 6.0, 79.8),
        new("DE-SN", "sachsen", "Sachsen", "Saxony", RegionType.State,
            4_090_000, 18_450, "Dresden", "DE-SN", 33_000, 6.0, 79.9),
        new("DE-ST", "sachsen-anhalt", "Sachsen-Anhalt", "Saxony-Anhalt", RegionType.State,
            2_140_000, 20_452, "Magdeburg", "DE-ST", 31_000, 6.9, 79.0),
        new("DE-SH", "schleswig-holstein", "Schleswig-Holstein", "Schleswig-Holstein", RegionType.State,
            2_950_000, 15_804, "Kiel", "DE-SH", 36_000, 5.4, 80.5),
        new("DE-TH", "thuringen", "Thüringen", "Thuringia", RegionType.State,
            2_110_000, 16_172, "Erfurt", "DE-TH", 31_000, 5.5, 79.3),
    };

    private static readonly RegionSeed AustriaNational = new(
        "AT", "austria", "Österreich", "Austria", RegionType.Country,
        9_100_000, 83_879, "Wien", "AT",
        47_000, 5.1, 81.9);

    private static readonly IReadOnlyList<RegionSeed> AustriaStates = new List<RegionSeed>
    {
        new("AT11", "burgenland", "Burgenland", "Burgenland", RegionType.State,
            300_000, 3_965, "Eisenstadt", "AT11", 36_000, 4.5, 81.5),
        new("AT12", "niederosterreich", "Niederösterreich", "Lower Austria", RegionType.State,
            1_700_000, 19_178, "St. Pölten", "AT12", 42_000, 4.2, 81.7),
        new("AT13", "wien", "Wien", "Vienna", RegionType.State,
            1_980_000, 415, "Wien", "AT13", 52_000, 9.6, 81.0),
        new("AT21", "karnten", "Kärnten", "Carinthia", RegionType.State,
            561_000, 9_536, "Klagenfurt", "AT21", 40_000, 6.0, 81.4),
        new("AT22", "steiermark", "Steiermark", "Styria", RegionType.State,
            1_250_000, 16_401, "Graz", "AT22", 42_000, 5.0, 81.6),
        new("AT31", "oberosterreich", "Oberösterreich", "Upper Austria", RegionType.State,
            1_490_000, 11_982, "Linz", "AT31", 46_000, 4.0, 81.8),
        new("AT32", "salzburg", "Salzburg", "Salzburg", RegionType.State,
            560_000, 7_156, "Salzburg", "AT32", 50_000, 3.8, 82.2),
        new("AT33", "tirol", "Tirol", "Tyrol", RegionType.State,
            760_000, 12_648, "Innsbruck", "AT33", 46_000, 3.9, 82.0),
        new("AT34", "vorarlberg", "Vorarlberg", "Vorarlberg", RegionType.State,
            400_000, 2_601, "Bregenz", "AT34", 48_000, 3.8, 81.9),
    };

    private static readonly RegionSeed SwitzerlandNational = new(
        "CH", "switzerland", "Schweiz", "Switzerland", RegionType.Country,
        8_800_000, 41_285, "Bern", "CH",
        84_000, 4.1, 84.0);

    private static readonly IReadOnlyList<RegionSeed> SwitzerlandStates = new List<RegionSeed>
    {
        new("CH01", "region-lemanique", "Région lémanique", "Lake Geneva Region", RegionType.State,
            1_850_000, 8_754, "Lausanne", "CH01", 85_000, 4.9, 83.8),
        new("CH02", "espace-mittelland", "Espace Mittelland", "Espace Mittelland", RegionType.State,
            1_900_000, 9_918, "Bern", "CH02", 74_000, 3.4, 84.0),
        new("CH03", "nordwestschweiz", "Nordwestschweiz", "Northwestern Switzerland", RegionType.State,
            1_500_000, 1_987, "Basel", "CH03", 105_000, 3.5, 83.9),
        new("CH04", "zurich", "Zürich", "Zurich", RegionType.State,
            1_580_000, 1_729, "Zürich", "CH04", 90_000, 4.1, 84.2),
        new("CH05", "ostschweiz", "Ostschweiz", "Eastern Switzerland", RegionType.State,
            1_100_000, 11_946, "St. Gallen", "CH05", 70_000, 3.3, 84.1),
        new("CH06", "zentralschweiz", "Zentralschweiz", "Central Switzerland", RegionType.State,
            810_000, 4_482, "Luzern", "CH06", 88_000, 2.8, 84.3),
        new("CH07", "ticino", "Ticino", "Ticino", RegionType.State,
            350_000, 2_812, "Bellinzona", "CH07", 62_000, 5.8, 83.9),
    };

    public static readonly IReadOnlyList<CountrySeed> Countries = new List<CountrySeed>
    {
        new(GermanyNational, GermanyStates),
        new(AustriaNational, AustriaStates),
        new(SwitzerlandNational, SwitzerlandStates),
    };
}
