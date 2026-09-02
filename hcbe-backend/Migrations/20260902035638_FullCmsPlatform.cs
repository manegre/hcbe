using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class FullCmsPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CmsContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Page = table.Column<string>(type: "text", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    DraftValueFr = table.Column<string>(type: "text", nullable: true),
                    DraftValueEn = table.Column<string>(type: "text", nullable: true),
                    PublishedValueFr = table.Column<string>(type: "text", nullable: true),
                    PublishedValueEn = table.Column<string>(type: "text", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsContentItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CmsContentRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CmsContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ValueFr = table.Column<string>(type: "text", nullable: true),
                    ValueEn = table.Column<string>(type: "text", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsContentRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CmsContentRevisions_CmsContentItems_CmsContentItemId",
                        column: x => x.CmsContentItemId,
                        principalTable: "CmsContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CmsContentItems_Key",
                table: "CmsContentItems",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CmsContentItems_Page_Section",
                table: "CmsContentItems",
                columns: new[] { "Page", "Section" });

            migrationBuilder.CreateIndex(
                name: "IX_CmsContentRevisions_CmsContentItemId_Version",
                table: "CmsContentRevisions",
                columns: new[] { "CmsContentItemId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CmsContentRevisions");

            migrationBuilder.DropTable(
                name: "CmsContentItems");
        }
    }
}
