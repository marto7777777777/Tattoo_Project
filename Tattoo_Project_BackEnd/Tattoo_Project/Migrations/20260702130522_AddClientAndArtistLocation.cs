using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAndArtistLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudioCity",
                table: "TattooArtists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudioCountry",
                table: "TattooArtists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "StudioLatitude",
                table: "TattooArtists",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StudioLongitude",
                table: "TattooArtists",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudioCity",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "StudioCountry",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "StudioLatitude",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "StudioLongitude",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Clients");
        }
    }
}
