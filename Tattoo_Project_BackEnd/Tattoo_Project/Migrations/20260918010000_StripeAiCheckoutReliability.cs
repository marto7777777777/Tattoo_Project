using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations
{
    [DbContext(typeof(TattooDbContext))]
    [Migration("20260918010000_StripeAiCheckoutReliability")]
    public partial class StripeAiCheckoutReliability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiProjectCheckoutAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AiTattooProjectId = table.Column<int>(type: "int", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ExpectedBaseAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PriceSemantics = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StripeIdempotencyKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripeCheckoutUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProjectCheckoutAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiProjectCheckoutAttempts_AiTattooProjects_AiTattooProjectId",
                        column: x => x.AiTattooProjectId,
                        principalTable: "AiTattooProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiProjectCheckoutAttempts_AiTattooProjectId",
                table: "AiProjectCheckoutAttempts",
                column: "AiTattooProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiProjectCheckoutAttempts_Status_UpdatedAtUtc",
                table: "AiProjectCheckoutAttempts",
                columns: new[] { "Status", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiProjectCheckoutAttempts_StripeCheckoutSessionId",
                table: "AiProjectCheckoutAttempts",
                column: "StripeCheckoutSessionId",
                unique: true,
                filter: "[StripeCheckoutSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AiProjectCheckoutAttempts_StripeIdempotencyKey",
                table: "AiProjectCheckoutAttempts",
                column: "StripeIdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiProjectCheckoutAttempts_UserId_AiTattooProjectId_Product",
                table: "AiProjectCheckoutAttempts",
                columns: new[] { "UserId", "AiTattooProjectId", "Product" },
                unique: true,
                filter: "[Status] IN ('Creating','SessionCreated','PaymentConfirmed','GrantPending')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AiProjectCheckoutAttempts");
        }
    }
}
