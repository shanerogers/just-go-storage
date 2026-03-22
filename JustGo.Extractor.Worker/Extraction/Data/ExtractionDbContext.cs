using JustGo.Extractor.Worker.Extraction.Documents;
using Microsoft.EntityFrameworkCore;

namespace JustGo.Extractor.Worker.Extraction.Data;

public sealed class ExtractionDbContext(DbContextOptions<ExtractionDbContext> options) : DbContext(options)
{
    public DbSet<JustGoDocumentEntity> JustGoDocuments => Set<JustGoDocumentEntity>();
    public DbSet<ExtractionRunStateEntity> ExtractionRunState => Set<ExtractionRunStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<JustGoDocumentEntity>();
        entity.ToTable("justgo_documents");

        entity.HasKey(x => x.Id);
        entity.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
        entity.Property(x => x.SourceId).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        entity.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
        entity.Property(x => x.Version).HasDefaultValue(1);
        entity.Property(x => x.ExtractedAtUtc).IsRequired();

        entity.HasIndex(x => new { x.DocumentType, x.SourceId }).IsUnique();
        entity.HasIndex(x => x.ExtractedAtUtc);

        var runState = modelBuilder.Entity<ExtractionRunStateEntity>();
        runState.ToTable("justgo_extraction_run_state");
        runState.HasKey(x => x.Id);
        runState.Property(x => x.Id).ValueGeneratedNever();
        runState.Property(x => x.LastSuccessfulCompletedAtUtc).IsRequired();
    }
}
