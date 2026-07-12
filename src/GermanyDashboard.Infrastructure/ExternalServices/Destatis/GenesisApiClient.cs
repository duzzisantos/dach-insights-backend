using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GermanyDashboard.Infrastructure.ExternalServices.Destatis;

/// <summary>
/// Thin client for the GENESIS-Online (Destatis) REST API 2020. Credentials are sent as a
/// form-encoded POST body (never appended to the URL) so they never end up in server logs
/// or browser history. See https://www-genesis.destatis.de/genesisWS/rest/2020/help
/// </summary>
public class GenesisApiClient : IGenesisApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GenesisApiOptions _options;
    private readonly ILogger<GenesisApiClient> _logger;

    public GenesisApiClient(HttpClient httpClient, IOptions<GenesisApiOptions> options, ILogger<GenesisApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GenesisDataRow>> GetTableDataAsync(string tableCode, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException(
                "GENESIS-Destatis credentials are not configured. Set Destatis__Username and " +
                "Destatis__Password environment variables (see README) before syncing.");
        }

        var form = new Dictionary<string, string>
        {
            ["username"] = _options.Username!,
            ["password"] = _options.Password!,
            ["name"] = tableCode,
            ["area"] = "all",
            ["format"] = "ffcsv",
            ["language"] = "de",
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/data/tablefile")
        {
            Content = new FormUrlEncodedContent(form),
        };

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (contentType.Contains("json"))
        {
            var parsed = JsonSerializer.Deserialize<GenesisTableResponse>(raw);
            if (parsed?.Status is { IsSuccess: false })
            {
                throw new GenesisApiException($"GENESIS API error for table '{tableCode}': {parsed.Status.Content}");
            }
        }

        return ParseFlatCsv(raw);
    }

    /// <summary>
    /// Parses the GENESIS "ffcsv" flat table format: semicolon-delimited rows of
    /// region code, region name, year, ..., value. The exact column layout varies by
    /// table, so this is a starting point — adjust the column indices per table code
    /// when wiring up a real GENESIS table in DestatisSyncService.
    /// </summary>
    private IReadOnlyList<GenesisDataRow> ParseFlatCsv(string raw)
    {
        var rows = new List<GenesisDataRow>();
        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines.Skip(1)) // first line is the header
        {
            var columns = line.Split(';');
            if (columns.Length < 4) continue;

            var regionCode = columns[0].Trim('"');
            var regionName = columns[1].Trim('"');

            if (!int.TryParse(columns[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
                continue;

            var rawValue = columns[^1].Trim('"').Replace(",", ".");
            if (!decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                continue;

            rows.Add(new GenesisDataRow(regionCode, regionName, year, value));
        }

        _logger.LogInformation("Parsed {RowCount} rows from GENESIS flat CSV response.", rows.Count);
        return rows;
    }
}
