using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations
{
    [DbContext(typeof(TattooDbContext))]
    [Migration("20260812173000_AddArtistPhoneVisibility")]
    public partial class AddArtistPhoneVisibility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowPhoneNumberOnPublicProfile",
                table: "TattooArtists",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowPhoneNumberOnPublicProfile",
                table: "TattooArtists");
        }
    }
}
