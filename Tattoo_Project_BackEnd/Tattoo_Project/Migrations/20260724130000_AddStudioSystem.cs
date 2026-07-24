using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations
{
    [DbContext(typeof(TattooDbContext))]
    [Migration("20260724130000_AddStudioSystem")]
    public class AddStudioSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "TattooArtists",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TattooArtists",
                type: "nvarchar(1200)",
                maxLength: 1200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedStudioOn",
                table: "TattooArtists",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudioId",
                table: "TattooArtists",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Studios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    IsOpenForJoinRequests = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OwnerArtistId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Studios_TattooArtists_OwnerArtistId",
                        column: x => x.OwnerArtistId,
                        principalTable: "TattooArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Preserve existing artist/studio data by creating one studio for every current artist.
            migrationBuilder.Sql(@"
INSERT INTO Studios ([Name], [Description], [Address], [City], [Country], [Latitude], [Longitude], [IsOpenForJoinRequests], [CreatedOn], [OwnerArtistId])
SELECT
    CASE WHEN NULLIF(LTRIM(RTRIM([StudioName])), '') IS NULL THEN CONCAT([FirstName], ' ', [LastName], ' Studio') ELSE [StudioName] END,
    CONCAT('Studio profile for ', CASE WHEN NULLIF(LTRIM(RTRIM([StudioName])), '') IS NULL THEN CONCAT([FirstName], ' ', [LastName]) ELSE [StudioName] END, '.'),
    [StudioAddress],
    [StudioCity],
    [StudioCountry],
    [StudioLatitude],
    [StudioLongitude],
    1,
    SYSUTCDATETIME(),
    [Id]
FROM TattooArtists;

UPDATE a
SET a.StudioId = s.Id,
    a.JoinedStudioOn = s.CreatedOn
FROM TattooArtists a
INNER JOIN Studios s ON s.OwnerArtistId = a.Id;
");

            migrationBuilder.CreateIndex(
                name: "IX_TattooArtists_StudioId",
                table: "TattooArtists",
                column: "StudioId");

            migrationBuilder.CreateIndex(
                name: "IX_Studios_Name_City",
                table: "Studios",
                columns: new[] { "Name", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Studios_OwnerArtistId",
                table: "Studios",
                column: "OwnerArtistId",
                unique: true,
                filter: "[OwnerArtistId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TattooArtists_Studios_StudioId",
                table: "TattooArtists",
                column: "StudioId",
                principalTable: "Studios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateTable(
                name: "StudioJoinRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudioId = table.Column<int>(type: "int", nullable: false),
                    TattooArtistId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudioJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudioJoinRequests_Studios_StudioId",
                        column: x => x.StudioId,
                        principalTable: "Studios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudioJoinRequests_TattooArtists_TattooArtistId",
                        column: x => x.TattooArtistId,
                        principalTable: "TattooArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudioJoinRequests_StudioId_Status",
                table: "StudioJoinRequests",
                columns: new[] { "StudioId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioJoinRequests_TattooArtistId_Status",
                table: "StudioJoinRequests",
                columns: new[] { "TattooArtistId", "Status" });

            migrationBuilder.DropColumn(name: "StudioName", table: "TattooArtists");
            migrationBuilder.DropColumn(name: "StudioAddress", table: "TattooArtists");
            migrationBuilder.DropColumn(name: "StudioCity", table: "TattooArtists");
            migrationBuilder.DropColumn(name: "StudioCountry", table: "TattooArtists");
            migrationBuilder.DropColumn(name: "StudioLatitude", table: "TattooArtists");
            migrationBuilder.DropColumn(name: "StudioLongitude", table: "TattooArtists");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StudioJoinRequests");

            migrationBuilder.DropForeignKey(name: "FK_TattooArtists_Studios_StudioId", table: "TattooArtists");

            migrationBuilder.AddColumn<string>(name: "StudioName", table: "TattooArtists", type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "StudioAddress", table: "TattooArtists", type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "StudioCity", table: "TattooArtists", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "StudioCountry", table: "TattooArtists", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<double>(name: "StudioLatitude", table: "TattooArtists", type: "float", nullable: true);
            migrationBuilder.AddColumn<double>(name: "StudioLongitude", table: "TattooArtists", type: "float", nullable: true);

            migrationBuilder.Sql(@"
UPDATE a SET
    a.StudioName = s.Name,
    a.StudioAddress = s.Address,
    a.StudioCity = s.City,
    a.StudioCountry = s.Country,
    a.StudioLatitude = s.Latitude,
    a.StudioLongitude = s.Longitude
FROM TattooArtists a
LEFT JOIN Studios s ON s.Id = a.StudioId;
");

            migrationBuilder.DropTable(name: "Studios");
            migrationBuilder.DropColumn(name: "JoinedStudioOn", table: "TattooArtists");
            migrationBuilder.DropColumn(name: "StudioId", table: "TattooArtists");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "TattooArtists",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TattooArtists",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1200)",
                oldMaxLength: 1200);
        }
    }
}
