using System.Reflection;
using JustGo.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustGo.Api.Migrations;

[DbContext(typeof(ApiDbContext))]
[Migration("20260324170000_QuartzSchema")]
public partial class QuartzSchema : Migration
{
    private const string SqlScriptResourceName = "JustGo.Api.Migrations.quartz_postgres.sql";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(ReadEmbeddedSqlScript());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS qrtz_fired_triggers;
            DROP TABLE IF EXISTS qrtz_paused_trigger_grps;
            DROP TABLE IF EXISTS qrtz_scheduler_state;
            DROP TABLE IF EXISTS qrtz_locks;
            DROP TABLE IF EXISTS qrtz_simprop_triggers;
            DROP TABLE IF EXISTS qrtz_simple_triggers;
            DROP TABLE IF EXISTS qrtz_cron_triggers;
            DROP TABLE IF EXISTS qrtz_blob_triggers;
            DROP TABLE IF EXISTS qrtz_triggers;
            DROP TABLE IF EXISTS qrtz_job_details;
            DROP TABLE IF EXISTS qrtz_calendars;
            """);
    }

    private static string ReadEmbeddedSqlScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(SqlScriptResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{SqlScriptResourceName}' was not found.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
