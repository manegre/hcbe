using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernanceConsultations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowComments",
                table: "Consultations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosesAtUtc",
                table: "Consultations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommentClosesAtUtc",
                table: "Consultations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EligibilityRule",
                table: "Consultations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ActiveMembers");

            migrationBuilder.AddColumn<string>(
                name: "GovernanceType",
                table: "Consultations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Information");

            migrationBuilder.AddColumn<int>(
                name: "MinimumParticipation",
                table: "Consultations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpensAtUtc",
                table: "Consultations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuorumPercentage",
                table: "Consultations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultsPublishedAtUtc",
                table: "Consultations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VotingMode",
                table: "Consultations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Named");

            migrationBuilder.CreateTable(
                name: "ConsultationAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationAuditEvents_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsultationAuditEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationComments_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsultationComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    LabelEn = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationOptions_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationParticipations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParticipatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationParticipations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationParticipations_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsultationParticipations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationBallots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CastAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationBallots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationBallots_ConsultationOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "ConsultationOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsultationBallots_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsultationBallots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationAuditEvents_ConsultationId_CreatedAtUtc",
                table: "ConsultationAuditEvents",
                columns: new[] { "ConsultationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationAuditEvents_UserId",
                table: "ConsultationAuditEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBallots_ConsultationId_UserId",
                table: "ConsultationBallots",
                columns: new[] { "ConsultationId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBallots_OptionId",
                table: "ConsultationBallots",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBallots_UserId",
                table: "ConsultationBallots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationComments_ConsultationId_CreatedAtUtc",
                table: "ConsultationComments",
                columns: new[] { "ConsultationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationComments_UserId",
                table: "ConsultationComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationOptions_ConsultationId_DisplayOrder",
                table: "ConsultationOptions",
                columns: new[] { "ConsultationId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationParticipations_ConsultationId_UserId",
                table: "ConsultationParticipations",
                columns: new[] { "ConsultationId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationParticipations_UserId",
                table: "ConsultationParticipations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultationAuditEvents");

            migrationBuilder.DropTable(
                name: "ConsultationBallots");

            migrationBuilder.DropTable(
                name: "ConsultationComments");

            migrationBuilder.DropTable(
                name: "ConsultationParticipations");

            migrationBuilder.DropTable(
                name: "ConsultationOptions");

            migrationBuilder.DropColumn(
                name: "AllowComments",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "ClosesAtUtc",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "CommentClosesAtUtc",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "EligibilityRule",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "GovernanceType",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "MinimumParticipation",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "OpensAtUtc",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "QuorumPercentage",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "ResultsPublishedAtUtc",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "VotingMode",
                table: "Consultations");
        }
    }
}
