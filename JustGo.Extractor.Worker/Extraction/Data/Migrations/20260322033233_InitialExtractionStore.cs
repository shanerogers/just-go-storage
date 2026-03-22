using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustGo.Extractor.Worker.Extraction.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialExtractionStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "justgo_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtractedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtSourceUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_justgo_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "justgo_extraction_run_state",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    LastSuccessfulBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSuccessfulCompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_justgo_extraction_run_state", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_justgo_documents_DocumentType_SourceId",
                table: "justgo_documents",
                columns: new[] { "DocumentType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_justgo_documents_ExtractedAtUtc",
                table: "justgo_documents",
                column: "ExtractedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "justgo_documents");

            migrationBuilder.DropTable(
                name: "justgo_extraction_run_state");
        }
    }
}
