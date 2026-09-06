using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityCommerce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CommunityOrganizerId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlatformFeePercent",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SalesModel",
                table: "Events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TicketingEnabled",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CommunityOrganizers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    DisplayNameEn = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: false),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    StripeAccountId = table.Column<string>(type: "text", nullable: true),
                    StripeDetailsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    StripeChargesEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    StripePayoutsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityOrganizers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityOrganizers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventPromoCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    PercentOff = table.Column<int>(type: "integer", nullable: false),
                    AmountOffCents = table.Column<long>(type: "bigint", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    RedemptionCount = table.Column<int>(type: "integer", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPromoCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventPromoCodes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameEn = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    PriceCents = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    MaxPerOrder = table.Column<int>(type: "integer", nullable: false),
                    SalesStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SalesEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketTiers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdvertisingCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdvertiserName = table.Column<string>(type: "text", nullable: false),
                    ContactEmail = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    BodyEn = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    DestinationUrl = table.Column<string>(type: "text", nullable: false),
                    Placements = table.Column<string>(type: "text", nullable: false),
                    TargetLanguage = table.Column<string>(type: "text", nullable: true),
                    TargetProvince = table.Column<string>(type: "text", nullable: true),
                    TargetZone = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    BudgetCents = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ImpressionCount = table.Column<long>(type: "bigint", nullable: false),
                    ClickCount = table.Column<long>(type: "bigint", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertisingCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvertisingCampaigns_CommunityOrganizers_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "CommunityOrganizers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdvertisingCampaigns_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuyerName = table.Column<string>(type: "text", nullable: false),
                    BuyerEmail = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SubtotalCents = table.Column<long>(type: "bigint", nullable: false),
                    DiscountCents = table.Column<long>(type: "bigint", nullable: false),
                    PlatformFeeCents = table.Column<long>(type: "bigint", nullable: false),
                    TotalCents = table.Column<long>(type: "bigint", nullable: false),
                    RefundedAmountCents = table.Column<long>(type: "bigint", nullable: false),
                    PromoCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "text", nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    StripeAccountId = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketOrders_EventPromoCodes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "EventPromoCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventTicketOrders_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventTicketOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierName = table.Column<string>(type: "text", nullable: false),
                    TierNameEn = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceCents = table.Column<long>(type: "bigint", nullable: false),
                    LineTotalCents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketOrderItems_EventTicketOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "EventTicketOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventTicketOrderItems_EventTicketTiers_TierId",
                        column: x => x.TierId,
                        principalTable: "EventTicketTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketCode = table.Column<string>(type: "text", nullable: false),
                    AttendeeName = table.Column<string>(type: "text", nullable: false),
                    AttendeeEmail = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransferredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTickets_EventTicketOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "EventTicketOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventTickets_EventTicketTiers_TierId",
                        column: x => x.TierId,
                        principalTable: "EventTicketTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_CommunityOrganizerId",
                table: "Events",
                column: "CommunityOrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingCampaigns_OrganizerId",
                table: "AdvertisingCampaigns",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingCampaigns_Status_StartsAtUtc_EndsAtUtc",
                table: "AdvertisingCampaigns",
                columns: new[] { "Status", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingCampaigns_SubmittedByUserId",
                table: "AdvertisingCampaigns",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityOrganizers_Status",
                table: "CommunityOrganizers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityOrganizers_StripeAccountId",
                table: "CommunityOrganizers",
                column: "StripeAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityOrganizers_UserId",
                table: "CommunityOrganizers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventPromoCodes_EventId_Code",
                table: "EventPromoCodes",
                columns: new[] { "EventId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrderItems_OrderId",
                table: "EventTicketOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrderItems_TierId",
                table: "EventTicketOrderItems",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_AccessToken",
                table: "EventTicketOrders",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_EventId_Status_CreatedAtUtc",
                table: "EventTicketOrders",
                columns: new[] { "EventId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_OrderNumber",
                table: "EventTicketOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_PromoCodeId",
                table: "EventTicketOrders",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_StripeCheckoutSessionId",
                table: "EventTicketOrders",
                column: "StripeCheckoutSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_UserId",
                table: "EventTicketOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTickets_OrderId_Status",
                table: "EventTickets",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventTickets_TicketCode",
                table: "EventTickets",
                column: "TicketCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTickets_TierId",
                table: "EventTickets",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTiers_EventId_DisplayOrder",
                table: "EventTicketTiers",
                columns: new[] { "EventId", "DisplayOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Events_CommunityOrganizers_CommunityOrganizerId",
                table: "Events",
                column: "CommunityOrganizerId",
                principalTable: "CommunityOrganizers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_CommunityOrganizers_CommunityOrganizerId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "AdvertisingCampaigns");

            migrationBuilder.DropTable(
                name: "EventTicketOrderItems");

            migrationBuilder.DropTable(
                name: "EventTickets");

            migrationBuilder.DropTable(
                name: "CommunityOrganizers");

            migrationBuilder.DropTable(
                name: "EventTicketOrders");

            migrationBuilder.DropTable(
                name: "EventTicketTiers");

            migrationBuilder.DropTable(
                name: "EventPromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_Events_CommunityOrganizerId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CommunityOrganizerId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PlatformFeePercent",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SalesModel",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TicketingEnabled",
                table: "Events");
        }
    }
}
