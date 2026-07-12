using System.Text.Json;
using System.Text.Json.Serialization;

namespace GermanyDashboard.Infrastructure.ExternalServices.Eurostat;

/// <summary>
/// Minimal reader for the JSON-stat 2.0 format Eurostat's REST API returns
/// (https://json-stat.org/full/). Values are stored as a flat object keyed by a
/// row-major linear index over the ordered dimensions in <see cref="Id"/>; this
/// decodes that back into one row per populated combination of category keys.
/// </summary>
public class JsonStatDataset
{
    [JsonPropertyName("id")]
    public required List<string> Id { get; set; }

    [JsonPropertyName("size")]
    public required List<int> Size { get; set; }

    [JsonPropertyName("value")]
    public required Dictionary<string, JsonElement> Value { get; set; }

    [JsonPropertyName("dimension")]
    public required Dictionary<string, JsonStatDimension> Dimension { get; set; }

    /// <summary>Yields one row per populated cell: dimension name -> category code, plus the value.</summary>
    public IEnumerable<(IReadOnlyDictionary<string, string> Coordinates, decimal Value)> Rows()
    {
        var strides = new int[Id.Count];
        var running = 1;
        for (var i = Id.Count - 1; i >= 0; i--)
        {
            strides[i] = running;
            running *= Size[i];
        }

        var reverseIndexes = Id
            .Select(dim => Dimension[dim].Category.Index.ToDictionary(kv => kv.Value, kv => kv.Key))
            .ToList();

        foreach (var (key, jsonValue) in Value)
        {
            if (jsonValue.ValueKind != JsonValueKind.Number || !jsonValue.TryGetDecimal(out var value))
            {
                continue;
            }

            var linearIndex = int.Parse(key);
            var coordinates = new Dictionary<string, string>(Id.Count);

            for (var i = 0; i < Id.Count; i++)
            {
                var position = (linearIndex / strides[i]) % Size[i];
                coordinates[Id[i]] = reverseIndexes[i][position];
            }

            yield return (coordinates, value);
        }
    }
}

public class JsonStatDimension
{
    [JsonPropertyName("category")]
    public required JsonStatCategory Category { get; set; }
}

public class JsonStatCategory
{
    [JsonPropertyName("index")]
    public required Dictionary<string, int> Index { get; set; }

    [JsonPropertyName("label")]
    public Dictionary<string, string>? Label { get; set; }
}
