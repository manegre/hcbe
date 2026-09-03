using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionErrorIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StackTrace = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    FirstOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAlertedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorIncidents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorIncidents_Fingerprint",
                table: "ErrorIncidents",
                column: "Fingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorIncidents_ResolvedAtUtc_LastOccurredAtUtc",
                table: "ErrorIncidents",
                columns: new[] { "ResolvedAtUtc", "LastOccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorIncidents");
        }
    }
}
