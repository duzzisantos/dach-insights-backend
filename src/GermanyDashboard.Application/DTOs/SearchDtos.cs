namespace GermanyDashboard.Application.DTOs;

public record SearchResultDto(
    string Type, // "region" | "category" | "indicator"
    string Slug,
    string Title,
    string? Subtitle,
    string Href
);
