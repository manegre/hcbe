using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityParticipation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Availability",
                table: "OpportunityApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Experience",
                table: "OpportunityApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchReasons",
                table: "OpportunityApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchScore",
                table: "OpportunityApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Availability",
                table: "Opportunities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Benefits",
                table: "Opportunities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BenefitsEn",
                table: "Opportunities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Commitment",
                table: "Opportunities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Opportunities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAtUtc",
                table: "Opportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Opportunities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "Opportunities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequirementsEn",
                table: "Opportunities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAtUtc",
                table: "Opportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OpportunityApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityApplicationDocuments_OpportunityApplications_Opp~",
                        column: x => x.OpportunityApplicationId,
                        principalTable: "OpportunityApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateNumber = table.Column<string>(type: "text", nullable: false),
                    ContributionSummary = table.Column<string>(type: "text", nullable: true),
                    ConfirmedHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityCertificates_OpportunityApplications_Opportunity~",
                        column: x => x.OpportunityApplicationId,
                        principalTable: "OpportunityApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerTimeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerTimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerTimeEntries_OpportunityApplications_OpportunityApp~",
                        column: x => x.OpportunityApplicationId,
                        principalTable: "OpportunityApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityApplicationDocuments_OpportunityApplicationId_Cr~",
                table: "OpportunityApplicationDocuments",
                columns: new[] { "OpportunityApplicationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityCertificates_CertificateNumber",
                table: "OpportunityCertificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityCertificates_OpportunityApplicationId",
                table: "OpportunityCertificates",
                column: "OpportunityApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTimeEntries_OpportunityApplicationId_Status_Activi~",
                table: "VolunteerTimeEntries",
                columns: new[] { "OpportunityApplicationId", "Status", "ActivityDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpportunityApplicationDocuments");

            migrationBuilder.DropTable(
                name: "OpportunityCertificates");

            migrationBuilder.DropTable(
                name: "VolunteerTimeEntries");

            migrationBuilder.DropColumn(
                name: "Availability",
                table: "OpportunityApplications");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "OpportunityApplications");

            migrationBuilder.DropColumn(
                name: "MatchReasons",
                table: "OpportunityApplications");

            migrationBuilder.DropColumn(
                name: "MatchScore",
                table: "OpportunityApplications");

            migrationBuilder.DropColumn(
                name: "Availability",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Benefits",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "BenefitsEn",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Commitment",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "EndsAtUtc",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "RequirementsEn",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "StartsAtUtc",
                table: "Opportunities");
        }
    }
}
