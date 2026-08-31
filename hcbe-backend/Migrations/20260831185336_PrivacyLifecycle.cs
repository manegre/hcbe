using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class PrivacyLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PrivacyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecuteAfterUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubjectReference = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyRequests_Status_ExecuteAfterUtc",
                table: "PrivacyRequests",
                columns: new[] { "Status", "ExecuteAfterUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyRequests_UserId",
                table: "PrivacyRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivacyRequests");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");
        }
    }
}
