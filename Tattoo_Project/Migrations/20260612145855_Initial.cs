using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FinalPrice",
                table: "TattooSessions",
                newName: "PriceForTheSession");

            migrationBuilder.AddColumn<string>(
                name: "PriceForSession",
                table: "TattooRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceForSession",
                table: "TattooRequests");

            migrationBuilder.RenameColumn(
                name: "PriceForTheSession",
                table: "TattooSessions",
                newName: "FinalPrice");
        }
    }
}
