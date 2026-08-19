using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations
{
    [DbContext(typeof(TattooDbContext))]
    [Migration("20260812170000_AddPermanentArtistPublicLinks")]
    public partial class AddPermanentArtistPublicLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicProfileSlug",
                table: "TattooArtists",
                type: "nvarchar(140)",
                maxLength: 140,
                nullable: true);

            // Existing links use the immutable artist id plus a random suffix.
            // New profiles receive a readable name prefix in application code.
            migrationBuilder.Sql("""
                UPDATE TattooArtists
                SET PublicProfileSlug = CONCAT('artist-', Id, '-', LOWER(LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 8)))
                WHERE PublicProfileSlug IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PublicProfileSlug",
                table: "TattooArtists",
                type: "nvarchar(140)",
                maxLength: 140,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(140)",
                oldMaxLength: 140,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TattooArtists_PublicProfileSlug",
                table: "TattooArtists",
                column: "PublicProfileSlug",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_TattooArtists_PublicProfileSlug", table: "TattooArtists");
            migrationBuilder.DropColumn(name: "PublicProfileSlug", table: "TattooArtists");
        }
    }
}
