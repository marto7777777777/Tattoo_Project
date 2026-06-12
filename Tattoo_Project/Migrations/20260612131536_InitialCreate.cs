using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TattooArtists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StudioName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StudioAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    OffersOnlineConsultation = table.Column<bool>(type: "bit", nullable: false),
                    RequiresDeposit = table.Column<bool>(type: "bit", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TattooArtists", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    TattooArtistId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_TattooArtists_TattooArtistId",
                        column: x => x.TattooArtistId,
                        principalTable: "TattooArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TattooRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Placement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    TattooArtistId = table.Column<int>(type: "int", nullable: false),
                    RemainingSessionsToBook = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TattooRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TattooRequests_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TattooRequests_TattooArtists_TattooArtistId",
                        column: x => x.TattooArtistId,
                        principalTable: "TattooArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArtistResponses",
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
                    table.PrimaryKey("PK_ArtistResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtistResponses_TattooRequests_TattooRequestId",
                        column: x => x.TattooRequestId,
                        principalTable: "TattooRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consultations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TattooRequestId = table.Column<int>(type: "int", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultations_TattooRequests_TattooRequestId",
                        column: x => x.TattooRequestId,
                        principalTable: "TattooRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TattooReferenceImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TattooRequestId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TattooReferenceImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TattooReferenceImages_TattooRequests_TattooRequestId",
                        column: x => x.TattooRequestId,
                        principalTable: "TattooRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TattooSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TattooRequestId = table.Column<int>(type: "int", nullable: false),
                    FinalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DurationHours = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TattooSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TattooSessions_TattooRequests_TattooRequestId",
                        column: x => x.TattooRequestId,
                        principalTable: "TattooRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtistRequirement_TattooArtistId",
                table: "ArtistRequirement",
                column: "TattooArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistResponses_TattooRequestId",
                table: "ArtistResponses",
                column: "TattooRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_TattooRequestId",
                table: "Consultations",
                column: "TattooRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioImage_TattooArtistId",
                table: "PortfolioImage",
                column: "TattooArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TattooArtistId",
                table: "Schedules",
                column: "TattooArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_TattooReferenceImages_TattooRequestId",
                table: "TattooReferenceImages",
                column: "TattooRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TattooRequests_ClientId",
                table: "TattooRequests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TattooRequests_TattooArtistId",
                table: "TattooRequests",
                column: "TattooArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_TattooSessions_TattooRequestId",
                table: "TattooSessions",
                column: "TattooRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistRequirement");

            migrationBuilder.DropTable(
                name: "ArtistResponses");

            migrationBuilder.DropTable(
                name: "Consultations");

            migrationBuilder.DropTable(
                name: "PortfolioImage");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "TattooReferenceImages");

            migrationBuilder.DropTable(
                name: "TattooSessions");

            migrationBuilder.DropTable(
                name: "TattooRequests");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "TattooArtists");
        }
    }
}
