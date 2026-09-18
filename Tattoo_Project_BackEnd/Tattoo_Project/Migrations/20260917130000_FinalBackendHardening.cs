using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations;

[DbContext(typeof(TattooDbContext))]
[Migration("20260917130000_FinalBackendHardening")]
public partial class FinalBackendHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "TokenVersion", table: "AspNetUsers", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "TimeZoneId", table: "TattooArtists", type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Europe/Sofia");
        migrationBuilder.AddColumn<string>(name: "TimeZoneId", table: "Studios", type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Europe/Sofia");
        migrationBuilder.DropForeignKey(name: "FK_AiProjectStorePurchases_AiTattooProjects_AiTattooProjectId", table: "AiProjectStorePurchases");
        migrationBuilder.AlterColumn<int>(name: "AiTattooProjectId", table: "AiProjectStorePurchases", type: "int", nullable: true, oldClrType: typeof(int), oldType: "int");
        migrationBuilder.AddColumn<string>(name: "ProcessingState", table: "AiProjectStorePurchases", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Granted");
        migrationBuilder.AddForeignKey(name: "FK_AiProjectStorePurchases_AiTattooProjects_AiTattooProjectId", table: "AiProjectStorePurchases", column: "AiTattooProjectId", principalTable: "AiTattooProjects", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddColumn<DateTime>(name: "CancelledAt", table: "TattooRequests", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CancelledByUserId", table: "TattooRequests", type: "nvarchar(450)", maxLength: 450, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CancellationReason", table: "TattooRequests", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "TattooRequests", type: "rowversion", rowVersion: true, nullable: false);
        migrationBuilder.AddColumn<bool>(name: "IsCancelled", table: "Consultations", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime>(name: "CancelledAt", table: "Consultations", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CancelledByUserId", table: "Consultations", type: "nvarchar(450)", maxLength: 450, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CancellationReason", table: "Consultations", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IsCancelled", table: "TattooSessions", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime>(name: "CancelledAt", table: "TattooSessions", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CancelledByUserId", table: "TattooSessions", type: "nvarchar(450)", maxLength: 450, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CancellationReason", table: "TattooSessions", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.CreateTable(
            name: "AccountDeletionRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                State = table.Column<int>(type: "int", nullable: false),
                RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                RetryCount = table.Column<int>(type: "int", nullable: false),
                LastErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AccountDeletionRequests", x => x.Id));
        migrationBuilder.CreateIndex(
            name: "IX_AccountDeletionRequests_UserId",
            table: "AccountDeletionRequests",
            column: "UserId",
            unique: true,
            filter: "[UserId] IS NOT NULL");

        migrationBuilder.CreateTable(
            name: "PendingEmailChanges",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                NewEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                NormalizedNewEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                FailedAttempts = table.Column<int>(type: "int", nullable: false),
                UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PendingEmailChanges", x => x.Id);
                table.ForeignKey("FK_PendingEmailChanges_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_PendingEmailChanges_NormalizedNewEmail", table: "PendingEmailChanges", column: "NormalizedNewEmail");
        migrationBuilder.CreateIndex(name: "IX_PendingEmailChanges_UserId_UsedAt", table: "PendingEmailChanges", columns: new[] { "UserId", "UsedAt" });

        migrationBuilder.CreateTable(
            name: "LegalConsentAudits",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                TermsVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PrivacyVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LegalConsentAudits", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_LegalConsentAudits_UserId_AcceptedAt", table: "LegalConsentAudits", columns: new[] { "UserId", "AcceptedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccountDeletionRequests");
        migrationBuilder.DropTable(name: "LegalConsentAudits");
        migrationBuilder.DropTable(name: "PendingEmailChanges");
        migrationBuilder.DropForeignKey(name: "FK_AiProjectStorePurchases_AiTattooProjects_AiTattooProjectId", table: "AiProjectStorePurchases");
        migrationBuilder.DropColumn(name: "ProcessingState", table: "AiProjectStorePurchases");
        migrationBuilder.AlterColumn<int>(name: "AiTattooProjectId", table: "AiProjectStorePurchases", type: "int", nullable: false, defaultValue: 0, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        migrationBuilder.AddForeignKey(name: "FK_AiProjectStorePurchases_AiTattooProjects_AiTattooProjectId", table: "AiProjectStorePurchases", column: "AiTattooProjectId", principalTable: "AiTattooProjects", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        migrationBuilder.DropColumn(name: "TokenVersion", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "TimeZoneId", table: "TattooArtists");
        migrationBuilder.DropColumn(name: "TimeZoneId", table: "Studios");
        migrationBuilder.DropColumn(name: "CancelledAt", table: "TattooRequests");
        migrationBuilder.DropColumn(name: "CancelledByUserId", table: "TattooRequests");
        migrationBuilder.DropColumn(name: "CancellationReason", table: "TattooRequests");
        migrationBuilder.DropColumn(name: "RowVersion", table: "TattooRequests");
        migrationBuilder.DropColumn(name: "IsCancelled", table: "Consultations");
        migrationBuilder.DropColumn(name: "CancelledAt", table: "Consultations");
        migrationBuilder.DropColumn(name: "CancelledByUserId", table: "Consultations");
        migrationBuilder.DropColumn(name: "CancellationReason", table: "Consultations");
        migrationBuilder.DropColumn(name: "IsCancelled", table: "TattooSessions");
        migrationBuilder.DropColumn(name: "CancelledAt", table: "TattooSessions");
        migrationBuilder.DropColumn(name: "CancelledByUserId", table: "TattooSessions");
        migrationBuilder.DropColumn(name: "CancellationReason", table: "TattooSessions");
    }
}
