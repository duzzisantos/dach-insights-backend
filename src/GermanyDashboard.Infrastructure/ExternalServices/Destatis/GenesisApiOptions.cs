namespace GermanyDashboard.Infrastructure.ExternalServices.Destatis;

/// <summary>
/// Bound from configuration section "Destatis". Credentials must come from environment
/// variables / user-secrets — never hardcode them or commit them to appsettings.json.
/// Register at https://www-genesis.destatis.de/genesis/online to obtain an account.
/// </summary>
public class GenesisApiOptions
{
    public const string SectionName = "Destatis";

    public string BaseUrl { get; set; } = "https://www-genesis.destatis.de/genesisWS/rest/2020";
    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>GENESIS table codes to import, e.g. "12411-0010" for population by state.</summary>
    public string[] TableCodes { get; set; } = Array.Empty<string>();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
