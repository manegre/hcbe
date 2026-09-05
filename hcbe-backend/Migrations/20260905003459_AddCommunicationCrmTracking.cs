using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationCrmTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunicationConsentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationConsentEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunityJourneyStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyType = table.Column<string>(type: "text", nullable: false),
                    LastTriggeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggerCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityJourneyStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Recipient = table.Column<string>(type: "text", nullable: false),
                    TrackingToken = table.Column<string>(type: "text", nullable: false),
                    QueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstOpenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastOpenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenCount = table.Column<int>(type: "integer", nullable: false),
                    UnsubscribedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsletterDeliveries_NewsletterCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "NewsletterCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationConsentEvents_Category_OccurredAtUtc",
                table: "CommunicationConsentEvents",
                columns: new[] { "Category", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationConsentEvents_Email_OccurredAtUtc",
                table: "CommunicationConsentEvents",
                columns: new[] { "Email", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityJourneyStates_UserId_JourneyType",
                table: "CommunityJourneyStates",
                columns: new[] { "UserId", "JourneyType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterDeliveries_CampaignId_Recipient",
                table: "NewsletterDeliveries",
                columns: new[] { "CampaignId", "Recipient" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterDeliveries_TrackingToken",
                table: "NewsletterDeliveries",
                column: "TrackingToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunicationConsentEvents");

            migrationBuilder.DropTable(
                name: "CommunityJourneyStates");

            migrationBuilder.DropTable(
                name: "NewsletterDeliveries");
        }
    }
}
