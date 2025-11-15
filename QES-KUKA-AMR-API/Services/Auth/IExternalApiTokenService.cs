namespace QES_KUKA_AMR_API.Services.Auth;

public interface IExternalApiTokenService
{
    /// <summary>
    /// Gets the current valid external API token. Automatically refreshes if expired.
    /// </summary>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a refresh of the external API token.
    /// </summary>
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
}
