namespace JustGo.Integrations.JustGo.Services;

public interface IJustGoTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    void InvalidateToken();
}
