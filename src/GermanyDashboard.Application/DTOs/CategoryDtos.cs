namespace GermanyDashboard.Application.DTOs;

public record CategorySummaryDto(
    int Id,
    string Slug,
    string Name,
    string? Description,
    string? Icon,
    string? ColorSlot
);

public record CategoryDetailDto(
    CategorySummaryDto Category,
    IReadOnlyList<IndicatorSummaryDto> Indicators
);

public record IndicatorSummaryDto(
    int Id,
    string Slug,
    string Name,
    string? Description,
    string Unit,
    string? ValueFormat,
    string CategorySlug
);

public record IndicatorSeriesDto(
    IndicatorSummaryDto Indicator,
    IReadOnlyList<IndicatorSeriesPointDto> Points
);

public record IndicatorSeriesPointDto(
    string RegionSlug,
    string RegionName,
    string RegionNameEnglish,
    int Year,
    decimal Value
);
