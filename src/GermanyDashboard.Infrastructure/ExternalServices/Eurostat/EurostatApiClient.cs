using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GermanyDashboard.Infrastructure.ExternalServices.Eurostat;

public interface IEurostatApiClient
{
    /// <summary>
    /// Fetches a Eurostat dataset filtered to the given dimension values (e.g. geo, time,
    /// sex, age, unit) — no registration or API key required. See
    /// https://ec.europa.eu/eurostat/web/main/data/web-services
    /// </summary>
    Task<JsonStatDataset> GetDatasetAsync(string datasetCode, IReadOnlyDictionary<string, IReadOnlyList<string>> filters, CancellationToken ct = default);
}

public class EurostatApiClient : IEurostatApiClient
{
    private const string BaseUrl = "https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data";
    private readonly HttpClient _httpClient;
    private readonly ILogger<EurostatApiClient> _logger;

    public EurostatApiClient(HttpClient httpClient, ILogger<EurostatApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<JsonStatDataset> GetDatasetAsync(
        string datasetCode,
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        CancellationToken ct = default)
    {
        var query = new List<string> { "format=JSON", "lang=EN" };
        foreach (var (key, values) in filters)
        {
            query.AddRange(values.Select(v => $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(v)}"));
        }

        var url = $"{BaseUrl}/{datasetCode}?{string.Join("&", query)}";
        _logger.LogInformation("Fetching Eurostat dataset {DatasetCode}...", datasetCode);

        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var dataset = await JsonSerializer.DeserializeAsync<JsonStatDataset>(stream, cancellationToken: ct);

        return dataset ?? throw new InvalidOperationException($"Eurostat returned an empty response for '{datasetCode}'.");
    }
}
