using JustGo.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustGo.Api.Migrations;

[DbContext(typeof(ApiDbContext))]
[Migration("20260324180000_MemberSyncRecords")]
public partial class MemberSyncRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS member_sync_records (
                id                uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
                justgo_member_id  uuid        NOT NULL,
                first_name        text        NULL,
                last_name         text        NULL,
                email_address     text        NULL,
                member_status     text        NULL,
                last_synced_at    timestamptz NOT NULL,
                raw_data          jsonb       NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_member_sync_records_justgo_member_id
                ON member_sync_records (justgo_member_id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS member_sync_records;
            """);
    }
}
