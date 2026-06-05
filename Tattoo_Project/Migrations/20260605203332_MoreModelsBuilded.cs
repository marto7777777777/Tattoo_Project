using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class MoreModelsBuilded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedPrice",
                table: "TattooRequests");

            migrationBuilder.DropColumn(
                name: "RequiredHours",
                table: "TattooRequests");

            migrationBuilder.AddColumn<int>(
                name: "DurationHours",
                table: "TattooSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "TattooSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "TattooArtists",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "TattooArtists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OffersOnlineConsultation",
                table: "TattooArtists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "TattooArtists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDeposit",
                table: "TattooArtists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Consultations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Consultations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ArtistRequirement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TattooArtistId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistRequirement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtistRequirement_TattooArtists_TattooArtistId",
                        column: x => x.TattooArtistId,
                        principalTable: "TattooArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArtistResponse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TattooRequestId = table.Column<int>(type: "int", nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedHours = table.Column<int>(type: "int", nullable: false),
                    ResponseMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistResponse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtistResponse_TattooRequests_TattooRequestId",
                        column: x => x.TattooRequestId,
                        principalTable: "TattooRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TattooArtistId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioImage_TattooArtists_TattooArtistId",
                        column: x => x.TattooArtistId,
                        principalTable: "TattooArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtistRequirement_TattooArtistId",
                table: "ArtistRequirement",
                column: "TattooArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistResponse_TattooRequestId",
                table: "ArtistResponse",
                column: "TattooRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioImage_TattooArtistId",
                table: "PortfolioImage",
                column: "TattooArtistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistRequirement");

            migrationBuilder.DropTable(
                name: "ArtistResponse");

            migrationBuilder.DropTable(
                name: "PortfolioImage");

            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "TattooSessions");

            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "TattooSessions");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "OffersOnlineConsultation",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "RequiresDeposit",
                table: "TattooArtists");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Clients");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedPrice",
                table: "TattooRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredHours",
                table: "TattooRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
