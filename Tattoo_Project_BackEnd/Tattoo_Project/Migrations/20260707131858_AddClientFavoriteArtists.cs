using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddClientFavoriteArtists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientFavoriteArtists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    TattooArtistId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientFavoriteArtists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientFavoriteArtists_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientFavoriteArtists_TattooArtists_TattooArtistId",
                        column: x => x.TattooArtistId,
                        principalTable: "TattooArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientFavoriteArtists_ClientId_TattooArtistId",
                table: "ClientFavoriteArtists",
                columns: new[] { "ClientId", "TattooArtistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientFavoriteArtists_TattooArtistId",
                table: "ClientFavoriteArtists",
                column: "TattooArtistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientFavoriteArtists");
        }
    }
}
