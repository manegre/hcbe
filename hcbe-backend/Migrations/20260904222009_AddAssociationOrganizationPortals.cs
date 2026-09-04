using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAssociationOrganizationPortals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedAssociationId",
                table: "ServiceCases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationType",
                table: "Associations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Association");

            migrationBuilder.CreateTable(
                name: "AssociationCalendarItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    LocationEn = table.Column<string>(type: "text", nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationCalendarItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationCalendarItems_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssociationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Visibility = table.Column<string>(type: "text", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationDocuments_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssociationJoinRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationJoinRequests_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssociationJoinRequests_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssociationJoinRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AssociationMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Permissions = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationMembers_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssociationMembers_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_AssignedAssociationId_Status_UpdatedAt",
                table: "ServiceCases",
                columns: new[] { "AssignedAssociationId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationCalendarItems_AssociationId_StartsAtUtc",
                table: "AssociationCalendarItems",
                columns: new[] { "AssociationId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationDocuments_AssociationId_CreatedAt",
                table: "AssociationDocuments",
                columns: new[] { "AssociationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationJoinRequests_AssociationId_MemberId_Status",
                table: "AssociationJoinRequests",
                columns: new[] { "AssociationId", "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationJoinRequests_MemberId",
                table: "AssociationJoinRequests",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationJoinRequests_ReviewedByUserId",
                table: "AssociationJoinRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationMembers_AssociationId_MemberId",
                table: "AssociationMembers",
                columns: new[] { "AssociationId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssociationMembers_MemberId",
                table: "AssociationMembers",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceCases_Associations_AssignedAssociationId",
                table: "ServiceCases",
                column: "AssignedAssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceCases_Associations_AssignedAssociationId",
                table: "ServiceCases");

            migrationBuilder.DropTable(
                name: "AssociationCalendarItems");

            migrationBuilder.DropTable(
                name: "AssociationDocuments");

            migrationBuilder.DropTable(
                name: "AssociationJoinRequests");

            migrationBuilder.DropTable(
                name: "AssociationMembers");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCases_AssignedAssociationId_Status_UpdatedAt",
                table: "ServiceCases");

            migrationBuilder.DropColumn(
                name: "AssignedAssociationId",
                table: "ServiceCases");

            migrationBuilder.DropColumn(
                name: "OrganizationType",
                table: "Associations");
        }
    }
}
