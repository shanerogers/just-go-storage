using JustGo.Extractor.Worker.Extraction.Data;
using Microsoft.EntityFrameworkCore;

namespace JustGo.Extractor.Worker.Extraction.Services;

public sealed class ExtractionStartupService(
    IServiceProvider serviceProvider,
    ILogger<ExtractionStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExtractionDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await EnsureQuartzSchemaAsync(dbContext, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureQuartzSchemaAsync(ExtractionDbContext dbContext, CancellationToken cancellationToken)
    {
        const string existsSql = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name = 'qrtz_job_details'
            );
            """;

        var exists = await dbContext.Database.SqlQueryRaw<bool>(existsSql).SingleAsync(cancellationToken);
        if (exists)
        {
            return;
        }

        logger.LogInformation("Quartz schema not found. Creating Quartz tables.");
        await dbContext.Database.ExecuteSqlRawAsync(QuartzPostgresSchema.Sql, cancellationToken);
    }
}
