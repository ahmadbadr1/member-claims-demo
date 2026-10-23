using MemberClaims.Api.Data;
using MemberClaims.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MemberClaims.Api.Workers;

/// <summary>
/// Background job that auto-approves small claims after a short waiting period.
///
/// A hosted service is a singleton, but AppDbContext is scoped. Injecting the
/// DbContext directly would create a captive dependency (one context shared forever,
/// not thread-safe, growing change tracker). Instead we inject IServiceScopeFactory
/// and create a new scope - and therefore a new DbContext - for each run.
/// </summary>
public sealed class ClaimAutoReviewWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<ClaimAutoReviewWorker> logger) : BackgroundService
{
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(configuration.GetValue("ClaimReview:IntervalSeconds", 30));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await ReviewBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log and keep the worker alive; the next tick will retry.
                logger.LogError(ex, "Claim auto-review run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReviewBatchAsync(CancellationToken cancellationToken)
    {
        var autoApproveLimit = configuration.GetValue("ClaimReview:AutoApproveLimit", 500m);
        var waitMinutes = configuration.GetValue("ClaimReview:WaitMinutes", 1);

        // New scope per unit of work -> new DbContext, disposed at the end of this method.
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = clock.GetUtcNow().UtcDateTime;
        var cutoff = now.AddMinutes(-waitMinutes);

        var claims = await db.Claims
            .Where(c => c.Status == ClaimStatus.Submitted
                        && c.CreatedAtUtc <= cutoff
                        && c.Amount <= autoApproveLimit)
            .OrderBy(c => c.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (claims.Count == 0)
        {
            return;
        }

        foreach (var claim in claims)
        {
            claim.Status = ClaimStatus.Approved;
            claim.ReviewedAtUtc = now;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Auto-approved {Count} claims", claims.Count);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else changed one of these claims (rowversion check). Skip this batch;
            // the next run re-reads current data, so nothing is overwritten silently.
            logger.LogWarning("Concurrency conflict during auto-review; batch skipped");
        }
    }
}
