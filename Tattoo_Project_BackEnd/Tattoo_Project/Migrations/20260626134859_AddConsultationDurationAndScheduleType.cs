using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultationDurationAndScheduleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DurationHoursForSession",
                table: "TattooRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsultationDurationMinutes",
                table: "TattooArtists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleType",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationHoursForSession",
                table: "TattooRequests");

            migrationBuilder.DropColumn(
                name: "ConsultationDurationMinutes",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "ScheduleType",
                table: "Schedules");
        }
    }
}
