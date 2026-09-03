using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityGrowthPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Audience",
                table: "NewsletterCampaigns",
                type: "text",
                nullable: false,
                defaultValue: "Newsletter");

            migrationBuilder.AddColumn<string>(
                name: "PreferenceCategory",
                table: "NewsletterCampaigns",
                type: "text",
                nullable: false,
                defaultValue: "newsletter");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAtUtc",
                table: "NewsletterCampaigns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetInterest",
                table: "NewsletterCampaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetLanguage",
                table: "NewsletterCampaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetProvince",
                table: "NewsletterCampaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetZone",
                table: "NewsletterCampaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledPublishAtUtc",
                table: "CmsContentItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerMemberId",
                table: "Associations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssociationClaimRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationClaimRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationClaimRequests_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssociationClaimRequests_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailEvents = table.Column<bool>(type: "boolean", nullable: false),
                    EmailOpportunities = table.Column<bool>(type: "boolean", nullable: false),
                    EmailMentorship = table.Column<bool>(type: "boolean", nullable: false),
                    EmailServiceUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    EmailNewsletter = table.Column<bool>(type: "boolean", nullable: false),
                    PushNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    HasCompletedPreferences = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_MemberPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MentorshipCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    NeedsCommitteeSupport = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorshipCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MentorshipCheckIns_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MentorshipCheckIns_MentorshipMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MentorshipMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MentorshipGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorshipGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MentorshipGoals_MentorshipMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MentorshipMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Organization = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    IsRemote = table.Column<bool>(type: "boolean", nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: true),
                    ApplyUrl = table.Column<string>(type: "text", nullable: true),
                    DeadlineUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityApplications_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpportunityApplications_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterCampaigns_Status_ScheduledAtUtc",
                table: "NewsletterCampaigns",
                columns: new[] { "Status", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CmsContentItems_ScheduledPublishAtUtc",
                table: "CmsContentItems",
                column: "ScheduledPublishAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Associations_OwnerMemberId",
                table: "Associations",
                column: "OwnerMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationClaimRequests_AssociationId_MemberId_Status",
                table: "AssociationClaimRequests",
                columns: new[] { "AssociationId", "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationClaimRequests_MemberId",
                table: "AssociationClaimRequests",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipCheckIns_MatchId_CreatedAt",
                table: "MentorshipCheckIns",
                columns: new[] { "MatchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipCheckIns_MemberId",
                table: "MentorshipCheckIns",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipGoals_MatchId_Status",
                table: "MentorshipGoals",
                columns: new[] { "MatchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_Status_DeadlineUtc",
                table: "Opportunities",
                columns: new[] { "Status", "DeadlineUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityApplications_MemberId",
                table: "OpportunityApplications",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityApplications_OpportunityId_MemberId",
                table: "OpportunityApplications",
                columns: new[] { "OpportunityId", "MemberId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Associations_Members_OwnerMemberId",
                table: "Associations",
                column: "OwnerMemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Associations_Members_OwnerMemberId",
                table: "Associations");

            migrationBuilder.DropTable(
                name: "AssociationClaimRequests");

            migrationBuilder.DropTable(
                name: "MemberPreferences");

            migrationBuilder.DropTable(
                name: "MentorshipCheckIns");

            migrationBuilder.DropTable(
                name: "MentorshipGoals");

            migrationBuilder.DropTable(
                name: "OpportunityApplications");

            migrationBuilder.DropTable(
                name: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_NewsletterCampaigns_Status_ScheduledAtUtc",
                table: "NewsletterCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_CmsContentItems_ScheduledPublishAtUtc",
                table: "CmsContentItems");

            migrationBuilder.DropIndex(
                name: "IX_Associations_OwnerMemberId",
                table: "Associations");

            migrationBuilder.DropColumn(
                name: "Audience",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "PreferenceCategory",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "ScheduledAtUtc",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetInterest",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetLanguage",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetProvince",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetZone",
                table: "NewsletterCampaigns");

            migrationBuilder.DropColumn(
                name: "ScheduledPublishAtUtc",
                table: "CmsContentItems");

            migrationBuilder.DropColumn(
                name: "OwnerMemberId",
                table: "Associations");
        }
    }
}
