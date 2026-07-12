namespace GermanyDashboard.Application.DTOs;

public record RegionSummaryDto(
    int Id,
    string Code,
    string Slug,
    string Name,
    string NameEnglish,
    string Type,
    long? Population,
    double? AreaKm2,
    string? Capital,
    string? GeoJsonKey,
    string? CountrySlug
);

public record RegionProfileDto(
    RegionSummaryDto Region,
    IReadOnlyList<HighlightStatDto> Highlights,
    IReadOnlyList<CategorySummaryDto> Categories
);

public record HighlightStatDto(
    string IndicatorSlug,
    string IndicatorName,
    string Unit,
    string? ValueFormat,
    int Year,
    decimal Value,
    decimal? PreviousYearValue,
    string CategorySlug
);
