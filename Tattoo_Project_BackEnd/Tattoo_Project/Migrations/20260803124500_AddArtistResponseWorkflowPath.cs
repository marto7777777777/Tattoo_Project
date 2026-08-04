using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations
{
    [DbContext(typeof(TattooDbContext))]
    [Migration("20260803124500_AddArtistResponseWorkflowPath")]
    public partial class AddArtistResponseWorkflowPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowPath",
                table: "ArtistResponses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkflowPath",
                table: "ArtistResponses");
        }
    }
}
