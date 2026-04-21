using System.Text.Json;
using JustGo.Api.Features.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JustGo.Api.Data;

public sealed class MemberSyncRecordConfiguration : IEntityTypeConfiguration<MemberSyncRecord>
{
    public void Configure(EntityTypeBuilder<MemberSyncRecord> entity)
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

        entity.Property(e => e.MemberInformation)
              .HasColumnName("member_information")
              .HasColumnType("jsonb")
              .HasConversion(
                  new ValueConverter<MemberDetailDto, string>(
                      memberInfo => JsonSerializer.Serialize(memberInfo, (JsonSerializerOptions?)null),
                      memberInfo => JsonSerializer.Deserialize<MemberDetailDto>(memberInfo, (JsonSerializerOptions?)null)!),
                  new ValueComparer<MemberDetailDto>(
                      (a, b) => ReferenceEquals(a, b),
                      v => v.GetHashCode(),
                      v => v))
              .IsRequired();
    }
}
