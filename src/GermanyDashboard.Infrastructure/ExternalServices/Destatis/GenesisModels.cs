using System.Text.Json.Serialization;

namespace GermanyDashboard.Infrastructure.ExternalServices.Destatis;

public class GenesisTableResponse
{
    [JsonPropertyName("Status")]
    public GenesisStatus? Status { get; set; }

    [JsonPropertyName("Object")]
    public GenesisTableObject? Object { get; set; }
}

public class GenesisStatus
{
    [JsonPropertyName("Code")]
    public int Code { get; set; }

    [JsonPropertyName("Content")]
    public string? Content { get; set; }

    public bool IsSuccess => Code == 0;
}

public class GenesisTableObject
{
    [JsonPropertyName("Content")]
    public string? Content { get; set; } // raw ";"-delimited table payload (flat/ffcsv format)

    [JsonPropertyName("Code")]
    public string? Code { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }
}

/// <summary>A single parsed row from a GENESIS flat ("ffcsv") table export.</summary>
public record GenesisDataRow(string RegionCode, string RegionName, int Year, decimal Value);
