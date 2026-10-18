using Microsoft.Extensions.Options;
using PRN232.LMS.Repositories.Interfaces;
using PRN232.LMS.Services.Options;

namespace PRN232.LMS.API.BackgroundServices;

/// <summary>
/// Background job (Lab 2 hardening): periodically deletes refresh tokens that have expired,
/// so the RefreshToken table does not grow forever.
/// The interval is configurable through Jwt:CleanupIntervalMinutes
/// (environment variable JWT_CLEANUP_INTERVAL_MINUTES in Docker), default 60 minutes.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _interval;

    public RefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<RefreshTokenCleanupService> logger,
        IOptions<JwtOptions> jwtOptions)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var minutes = jwtOptions.Value.CleanupIntervalMinutes <= 0 ? 60 : jwtOptions.Value.CleanupIntervalMinutes;
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Refresh-token cleanup job started (interval: {Interval}).", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Refresh-token cleanup job stopped.");
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            var removed = await repository.DeleteExpiredAsync(DateTime.UtcNow, ct);
            if (removed > 0)
            {
                _logger.LogInformation("Refresh-token cleanup removed {Count} expired token(s).", removed);
            }
        }
        catch (OperationCanceledException)
        {
            // application is shutting down
        }
        catch (Exception ex)
        {
            // never let the background job crash the API
            _logger.LogError(ex, "Refresh-token cleanup failed.");
        }
    }
}
