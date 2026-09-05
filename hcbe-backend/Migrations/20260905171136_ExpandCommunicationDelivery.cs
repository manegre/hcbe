using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCommunicationDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailStatus",
                table: "NewsletterDeliveries",
                type: "text",
                nullable: false,
                defaultValue: "Sent");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "NewsletterDeliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InAppStatus",
                table: "NewsletterDeliveries",
                type: "text",
                nullable: false,
                defaultValue: "Skipped");

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "NewsletterDeliveries",
                type: "text",
                nullable: false,
                defaultValue: "fr");

            migrationBuilder.AddColumn<string>(
                name: "PushStatus",
                table: "NewsletterDeliveries",
                type: "text",
                nullable: false,
                defaultValue: "Skipped");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "NewsletterDeliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Channels",
                table: "NewsletterCampaigns",
                type: "text",
                nullable: false,
                defaultValue: "Email");

            migrationBuilder.AddColumn<int>(
                name: "InAppSentCount",
                table: "NewsletterCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PushFailedCount",
                table: "NewsletterCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PushSentCount",
                table: "NewsletterCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetAssociationId",
                table: "NewsletterCampaigns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetMembershipStatus",
                table: "NewsletterCampaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestSentCount",
                table: "NewsletterCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailStatus",
                table: "NewsletterDeliveries");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "NewsletterDeliveries");

            migrationBuilder.DropColumn(
                name: "InAppStatus",
                table: "NewsletterDeliveries");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "NewsletterDeliveries");

            migrationBuilder.DropColumn(
                name: "PushStatus",
                table: "NewsletterDeliveries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "NewsletterDeliveries");

            migrationBuilder.DropColumn(
                name: "Channels",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "InAppSentCount",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "PushFailedCount",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "PushSentCount",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetAssociationId",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetMembershipStatus",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "TestSentCount",
                table: "NewsletterCampaigns");
        }
    }
}
