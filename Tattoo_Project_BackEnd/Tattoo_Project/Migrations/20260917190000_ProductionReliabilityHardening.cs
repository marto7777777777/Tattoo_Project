using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations
{
    [DbContext(typeof(TattooDbContext))]
    [Migration("20260917190000_ProductionReliabilityHardening")]
    public partial class ProductionReliabilityHardening : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastProviderEventAt",
                table: "ProviderSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProviderEventId",
                table: "ProviderSubscriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiGenerationOperations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiTattooProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BaseVersionId = table.Column<int>(type: "int", nullable: true),
                    Instruction = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResultVersionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiGenerationOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiGenerationOperations_AiTattooProjects_AiTattooProjectId",
                        column: x => x.AiTattooProjectId,
                        principalTable: "AiTattooProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileCleanupTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileCleanupTasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationOperations_AiTattooProjectId_RequestKey",
                table: "AiGenerationOperations",
                columns: new[] { "AiTattooProjectId", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationOperations_AiTattooProjectId_Status",
                table: "AiGenerationOperations",
                columns: new[] { "AiTattooProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationOperations_OperationId",
                table: "AiGenerationOperations",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileCleanupTasks_CompletedAt_UpdatedAt",
                table: "FileCleanupTasks",
                columns: new[] { "CompletedAt", "UpdatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AiGenerationOperations");
            migrationBuilder.DropTable(name: "FileCleanupTasks");
            migrationBuilder.DropColumn(name: "LastProviderEventAt", table: "ProviderSubscriptions");
            migrationBuilder.DropColumn(name: "LastProviderEventId", table: "ProviderSubscriptions");
        }
    }
}
