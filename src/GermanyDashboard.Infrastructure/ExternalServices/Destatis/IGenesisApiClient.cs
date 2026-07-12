namespace GermanyDashboard.Infrastructure.ExternalServices.Destatis;

public interface IGenesisApiClient
{
    /// <summary>
    /// Fetches a GENESIS-Online table in flat ("ffcsv") format and parses it into rows of
    /// (region code, region name, year, value). Throws <see cref="GenesisApiException"/>
    /// on a non-success status from the GENESIS API.
    /// </summary>
    Task<IReadOnlyList<GenesisDataRow>> GetTableDataAsync(string tableCode, CancellationToken ct = default);
}

public class GenesisApiException : Exception
{
    public GenesisApiException(string message) : base(message)
    {
    }
}
