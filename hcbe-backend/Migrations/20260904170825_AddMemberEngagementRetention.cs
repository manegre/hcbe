using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberEngagementRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DigestFrequency",
                table: "MemberPreferences",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Off");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDigestSentAtUtc",
                table: "MemberPreferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberBlocks_Members_BlockedMemberId",
                        column: x => x.BlockedMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberBlocks_Members_BlockerMemberId",
                        column: x => x.BlockerMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedMemberItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedMemberItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedMemberItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberBlocks_BlockedMemberId",
                table: "MemberBlocks",
                column: "BlockedMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberBlocks_BlockerMemberId_BlockedMemberId",
                table: "MemberBlocks",
                columns: new[] { "BlockerMemberId", "BlockedMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedMemberItems_UserId_EntityType_EntityId",
                table: "SavedMemberItems",
                columns: new[] { "UserId", "EntityType", "EntityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberBlocks");

            migrationBuilder.DropTable(
                name: "SavedMemberItems");

            migrationBuilder.DropColumn(
                name: "DigestFrequency",
                table: "MemberPreferences");

            migrationBuilder.DropColumn(
                name: "LastDigestSentAtUtc",
                table: "MemberPreferences");
        }
    }
}
