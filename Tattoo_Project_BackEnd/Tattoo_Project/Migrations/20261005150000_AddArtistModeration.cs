using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations;

public partial class AddArtistModeration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ModerationStatus",
            table: "TattooArtists",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "ArtistReports",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ReportedArtistId = table.Column<int>(type: "int", nullable: false),
                ReporterUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                ReviewedByAdminId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                DecisionNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ArtistReports", x => x.Id);
                table.ForeignKey("FK_ArtistReports_AspNetUsers_ReporterUserId", x => x.ReporterUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ArtistReports_AspNetUsers_ReviewedByAdminId", x => x.ReviewedByAdminId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ArtistReports_TattooArtists_ReportedArtistId", x => x.ReportedArtistId, "TattooArtists", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ArtistModerationSubmissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TattooArtistId = table.Column<int>(type: "int", nullable: false),
                SourceReportId = table.Column<int>(type: "int", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                ReviewedByAdminId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ArtistModerationSubmissions", x => x.Id);
                table.ForeignKey("FK_ArtistModerationSubmissions_AspNetUsers_ReviewedByAdminId", x => x.ReviewedByAdminId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ArtistModerationSubmissions_ArtistReports_SourceReportId", x => x.SourceReportId, "ArtistReports", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_ArtistModerationSubmissions_TattooArtists_TattooArtistId", x => x.TattooArtistId, "TattooArtists", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ArtistModerationSubmissionImages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SubmissionId = table.Column<int>(type: "int", nullable: false),
                ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ArtistModerationSubmissionImages", x => x.Id);
                table.ForeignKey("FK_ArtistModerationSubmissionImages_ArtistModerationSubmissions_SubmissionId", x => x.SubmissionId, "ArtistModerationSubmissions", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ArtistReports_ReportedArtistId_Status",
            table: "ArtistReports",
            columns: new[] { "ReportedArtistId", "Status" });
        migrationBuilder.CreateIndex(
            name: "IX_ArtistReports_ReporterUserId_ReportedArtistId_Status",
            table: "ArtistReports",
            columns: new[] { "ReporterUserId", "ReportedArtistId", "Status" });
        migrationBuilder.CreateIndex(
            name: "IX_ArtistReports_ReviewedByAdminId",
            table: "ArtistReports",
            column: "ReviewedByAdminId");
        migrationBuilder.CreateIndex(
            name: "IX_ArtistModerationSubmissions_TattooArtistId_Status",
            table: "ArtistModerationSubmissions",
            columns: new[] { "TattooArtistId", "Status" });
        migrationBuilder.CreateIndex(
            name: "IX_ArtistModerationSubmissions_SourceReportId",
            table: "ArtistModerationSubmissions",
            column: "SourceReportId");
        migrationBuilder.CreateIndex(
            name: "IX_ArtistModerationSubmissions_ReviewedByAdminId",
            table: "ArtistModerationSubmissions",
            column: "ReviewedByAdminId");
        migrationBuilder.CreateIndex(
            name: "IX_ArtistModerationSubmissionImages_SubmissionId",
            table: "ArtistModerationSubmissionImages",
            column: "SubmissionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ArtistModerationSubmissionImages");
        migrationBuilder.DropTable(name: "ArtistModerationSubmissions");
        migrationBuilder.DropTable(name: "ArtistReports");
        migrationBuilder.DropColumn(name: "ModerationStatus", table: "TattooArtists");
    }
}
