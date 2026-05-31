using JustGo.Api.Features.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JustGo.Api.Data;

public sealed class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    public DbSet<MemberSyncRecord> Members => Set<MemberSyncRecord>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // TickerQ injects its entities at runtime via IModelCustomizer, which the
        // design-time snapshot cannot capture. Suppress the false-positive warning.
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApiDbContext).Assembly);
    }
}
