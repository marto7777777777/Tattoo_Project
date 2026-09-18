using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations;

[DbContext(typeof(TattooDbContext))]
[Migration("20260915160000_HardenProductionRelease")]
public partial class HardenProductionRelease : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PendingProvider",
            table: "ArtistSubscriptions",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PendingProviderExpiresAt",
            table: "ArtistSubscriptions",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "StripeCheckoutAttemptToken",
            table: "ArtistSubscriptions",
            type: "uniqueidentifier",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PendingProvider", table: "ArtistSubscriptions");
        migrationBuilder.DropColumn(name: "PendingProviderExpiresAt", table: "ArtistSubscriptions");
        migrationBuilder.DropColumn(name: "StripeCheckoutAttemptToken", table: "ArtistSubscriptions");
    }
}
