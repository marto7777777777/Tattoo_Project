using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtistReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TattooRequestId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    TattooArtistId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtistReviews_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArtistReviews_TattooArtists_TattooArtistId",
                        column: x => x.TattooArtistId,
                        principalTable: "TattooArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArtistReviews_TattooRequests_TattooRequestId",
                        column: x => x.TattooRequestId,
                        principalTable: "TattooRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtistReviews_ClientId",
                table: "ArtistReviews",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistReviews_TattooArtistId",
                table: "ArtistReviews",
                column: "TattooArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistReviews_TattooRequestId",
                table: "ArtistReviews",
                column: "TattooRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistReviews");
        }
    }
}
