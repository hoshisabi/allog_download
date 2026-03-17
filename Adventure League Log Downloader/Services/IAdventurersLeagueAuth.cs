using System.Net.Http;
using System.Threading.Tasks;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Abstraction for logging into Adventurers League and obtaining an authenticated HttpClient and user id.
/// Implementations should handle cookies/session and any CSRF or form workflow.
/// </summary>
public interface IAdventurersLeagueAuth
{
    /// <summary>
    /// Returns an <see cref="HttpClient"/> that carries the authenticated session (cookies/headers).
    /// The client lifetime is owned by the implementation; callers should not dispose it.
    /// </summary>
    Task<HttpClient> GetAuthenticatedClientAsync();

    /// <summary>
    /// Returns the authenticated user's numeric id as used in Adventurers League URLs.
    /// </summary>
    Task<string> GetUserIdAsync();
}
