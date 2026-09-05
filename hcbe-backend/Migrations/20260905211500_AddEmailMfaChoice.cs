using HcbeApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcbeApi.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260905211500_AddEmailMfaChoice")]
public partial class AddEmailMfaChoice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MfaMethod",
            table: "Users",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CodeHash",
            table: "MfaChallenges",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeliveryMethod",
            table: "MfaChallenges",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "Authenticator");

        migrationBuilder.AddColumn<DateTime>(
            name: "LastSentAtUtc",
            table: "MfaChallenges",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql("UPDATE \"Users\" SET \"MfaMethod\" = 'Authenticator' WHERE \"MfaEnabledAtUtc\" IS NOT NULL AND \"MfaMethod\" IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MfaMethod", table: "Users");
        migrationBuilder.DropColumn(name: "CodeHash", table: "MfaChallenges");
        migrationBuilder.DropColumn(name: "DeliveryMethod", table: "MfaChallenges");
        migrationBuilder.DropColumn(name: "LastSentAtUtc", table: "MfaChallenges");
    }
}
