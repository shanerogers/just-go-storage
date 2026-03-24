using JustGo.Api.Features.Members;
using Microsoft.EntityFrameworkCore;

namespace JustGo.Api.Data;

public sealed class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    public DbSet<MemberSyncRecord> Members => Set<MemberSyncRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MemberSyncRecord>(entity =>
        {
            entity.ToTable("member_sync_records");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.JustGoMemberId).HasColumnName("justgo_member_id").IsRequired();
            entity.HasIndex(e => e.JustGoMemberId).IsUnique();

            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.EmailAddress).HasColumnName("email_address");
            entity.Property(e => e.MemberStatus).HasColumnName("member_status");
            entity.Property(e => e.LastSyncedAt).HasColumnName("last_synced_at");

            entity.Property(e => e.RawData)
                  .HasColumnName("raw_data")
                  .HasColumnType("jsonb")
                  .IsRequired();
        });
    }
}
