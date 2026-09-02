using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class ExpandEventExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CtaLabel",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtaLabelEn",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "InPerson");

            migrationBuilder.AddColumn<string>(
                name: "RegistrationUrl",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "America/Toronto");

            migrationBuilder.CreateTable(
                name: "EventCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventSpeakers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSpeakers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSpeakers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventOrganizers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventOrganizers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventOrganizers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_DisplayOrder",
                table: "EventCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_Slug",
                table: "EventCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventOrganizers_EventId_DisplayOrder",
                table: "EventOrganizers",
                columns: new[] { "EventId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSpeakers_EventId_DisplayOrder",
                table: "EventSpeakers",
                columns: new[] { "EventId", "DisplayOrder" });

            var seededAt = new DateTime(2026, 9, 2, 17, 42, 1, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "EventCategories",
                columns: new[] { "Id", "Slug", "Name", "NameEn", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { Guid.Parse("e1010000-0000-0000-0000-000000000001"), "workshop", "Atelier", "Workshop", true, 0, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000002"), "conference", "Conférence", "Conference", true, 1, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000003"), "webinar", "Webinaire", "Webinar", true, 2, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000004"), "professional-development", "Développement professionnel", "Professional development", true, 3, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000005"), "diplomatic-community-meeting", "Rencontre diplomatique et communautaire", "Diplomatic and community meeting", true, 4, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000006"), "business-investment", "Affaires et investissement", "Business and investment", true, 5, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000007"), "networking", "Réseautage", "Networking", true, 6, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000008"), "training", "Formation", "Training", true, 7, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000009"), "cultural-festival", "Festival et culture", "Cultural festival", true, 8, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000010"), "national-celebration", "Célébration nationale et civique", "National and civic celebration", true, 9, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000011"), "fundraiser-solidarity", "Collecte et solidarité", "Fundraiser and solidarity", true, 10, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000012"), "memorial-tribute", "Hommage et commémoration", "Memorial and tribute", true, 11, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000013"), "social", "Activité sociale", "Social event", true, 12, seededAt, seededAt },
                    { Guid.Parse("e1010000-0000-0000-0000-000000000014"), "other", "Autre", "Other", true, 13, seededAt, seededAt }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventCategories");

            migrationBuilder.DropTable(
                name: "EventOrganizers");

            migrationBuilder.DropTable(
                name: "EventSpeakers");

            migrationBuilder.DropColumn(
                name: "CtaLabel",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CtaLabelEn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationUrl",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Events");
        }
    }
}
