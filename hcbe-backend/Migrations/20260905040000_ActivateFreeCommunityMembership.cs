using HcbeApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260905040000_ActivateFreeCommunityMembership")]
public sealed class ActivateFreeCommunityMembership : Migration
{
    private static readonly Guid FreePlanId = Guid.Parse("8f0c48d3-0c24-4c39-9f7e-4c8ec4034f11");

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.Sql("""
                UPDATE "MembershipPlans"
                SET "IsActive" = FALSE, "UpdatedAtUtc" = CURRENT_TIMESTAMP;
                """);
        }
        else
        {
            migrationBuilder.Sql("""
                UPDATE "MembershipPlans"
                SET "IsActive" = 0, "UpdatedAtUtc" = CURRENT_TIMESTAMP;
                """);
        }

        migrationBuilder.InsertData(
            table: "MembershipPlans",
            columns:
            [
                "Id", "Name", "NameEn", "Description", "DescriptionEn",
                "AmountCents", "Currency", "BillingMode", "StripePriceId",
                "BenefitsJson", "IsActive", "DisplayOrder", "CreatedAtUtc", "UpdatedAtUtc"
            ],
            columnTypes: ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                ?
                [
                    "uuid", "character varying(160)", "text", "text", "text",
                    "bigint", "character varying(3)", "character varying(20)", "character varying(255)",
                    "text", "boolean", "integer", "timestamp with time zone", "timestamp with time zone"
                ]
                :
                [
                    "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER", "TEXT", "TEXT", "TEXT",
                    "TEXT", "INTEGER", "INTEGER", "TEXT", "TEXT"
                ],
            values:
            [
                FreePlanId,
                "Membre communautaire — Gratuit",
                "Community member — Free",
                "Accès gratuit à la communauté, aux services, aux événements et aux ressources du HCBE Canada.",
                "Free access to the HCBE Canada community, services, events and resources.",
                0L,
                "cad",
                "Free",
                null,
                "[\"Accès à l’espace membre\",\"Carte de membre numérique\",\"Services et ressources communautaires\",\"Renouvellement annuel gratuit\"]",
                true,
                0,
                new DateTime(2026, 9, 5, 4, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 5, 4, 0, 0, DateTimeKind.Utc)
            ]);

        if (!ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)) return;

        migrationBuilder.Sql("""
            UPDATE "MembershipStandings" AS standing
            SET "PlanId" = '8f0c48d3-0c24-4c39-9f7e-4c8ec4034f11',
                "Status" = 'Active',
                "CurrentPeriodStartUtc" = COALESCE(standing."CurrentPeriodStartUtc", CURRENT_TIMESTAMP),
                "CurrentPeriodEndUtc" = CASE
                    WHEN standing."CurrentPeriodEndUtc" > CURRENT_TIMESTAMP THEN standing."CurrentPeriodEndUtc"
                    ELSE CURRENT_TIMESTAMP + INTERVAL '1 year'
                END,
                "GraceEndsAtUtc" = CASE
                    WHEN standing."CurrentPeriodEndUtc" > CURRENT_TIMESTAMP THEN standing."CurrentPeriodEndUtc" + INTERVAL '30 days'
                    ELSE CURRENT_TIMESTAMP + INTERVAL '1 year 30 days'
                END,
                "LastReminderKey" = NULL,
                "UpdatedAtUtc" = CURRENT_TIMESTAMP
            FROM "Users" AS account
            WHERE account."Id" = standing."UserId"
              AND account."IsActive" = TRUE
              AND account."MemberId" IS NOT NULL;

            INSERT INTO "MembershipStandings"
                ("Id", "UserId", "PlanId", "Status", "CurrentPeriodStartUtc", "CurrentPeriodEndUtc", "GraceEndsAtUtc", "AutoRenew", "StripeCustomerId", "StripeSubscriptionId", "LastTransactionId", "LastReminderKey", "LastReminderAtUtc", "UpdatedAtUtc")
            SELECT
                md5(account."Id"::text || ':hcbe-community-membership')::uuid,
                account."Id",
                '8f0c48d3-0c24-4c39-9f7e-4c8ec4034f11',
                'Active', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 year',
                CURRENT_TIMESTAMP + INTERVAL '1 year 30 days', FALSE,
                NULL, NULL, NULL, NULL, NULL, CURRENT_TIMESTAMP
            FROM "Users" AS account
            WHERE account."IsActive" = TRUE
              AND account."MemberId" IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1 FROM "MembershipStandings" AS standing
                  WHERE standing."UserId" = account."Id"
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                ? """
                  UPDATE "MembershipStandings"
                  SET "PlanId" = NULL
                  WHERE "PlanId" = '8f0c48d3-0c24-4c39-9f7e-4c8ec4034f11';
                  """
                : """
                  UPDATE "MembershipStandings"
                  SET "PlanId" = NULL
                  WHERE "PlanId" = '8F0C48D3-0C24-4C39-9F7E-4C8EC4034F11';
                  """);

        migrationBuilder.DeleteData(table: "MembershipPlans", keyColumn: "Id", keyValue: FreePlanId);
    }
}
