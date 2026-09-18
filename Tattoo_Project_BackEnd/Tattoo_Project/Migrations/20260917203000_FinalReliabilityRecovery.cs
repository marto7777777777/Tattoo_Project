using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations;

[DbContext(typeof(TattooDbContext))]
[Migration("20260917203000_FinalReliabilityRecovery")]
public partial class FinalReliabilityRecovery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AiGenerationOperations_AiTattooProjectId_RequestKey",
            table: "AiGenerationOperations");

        migrationBuilder.AddColumn<long>(name: "OperationEpoch", table: "AiGenerationOperations", type: "bigint", nullable: false, defaultValue: 0L);
        migrationBuilder.AddColumn<DateTime>(name: "StartedAtUtc", table: "AiGenerationOperations", type: "datetime2", nullable: false, defaultValue: new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc));
        migrationBuilder.AddColumn<DateTime>(name: "LeaseExpiresAtUtc", table: "AiGenerationOperations", type: "datetime2", nullable: false, defaultValue: new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc));
        migrationBuilder.AddColumn<DateTime>(name: "CompletedAtUtc", table: "AiGenerationOperations", type: "datetime2", nullable: true);

        migrationBuilder.AddColumn<Guid>(name: "ActiveOperationId", table: "AiTattooProjects", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<long>(name: "OperationEpoch", table: "AiTattooProjects", type: "bigint", nullable: false, defaultValue: 0L);
        migrationBuilder.AddColumn<int>(name: "ConsecutiveGenerationFailures", table: "AiTattooProjects", type: "int", nullable: false, defaultValue: 0);

        migrationBuilder.AddColumn<int>(name: "RetryCount", table: "AiProjectStorePurchases", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTime>(name: "NextRetryAtUtc", table: "AiProjectStorePurchases", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LastAttemptAtUtc", table: "AiProjectStorePurchases", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LastErrorCode", table: "AiProjectStorePurchases", type: "nvarchar(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "DeadLetteredAtUtc", table: "AiProjectStorePurchases", type: "datetime2", nullable: true);

        migrationBuilder.Sql(@"
UPDATE [AiGenerationOperations]
SET [StartedAtUtc] = [CreatedAt],
    [LeaseExpiresAtUtc] = DATEADD(minute, 35, [UpdatedAt]),
    [CompletedAtUtc] = CASE WHEN [Status] = N'Generating' THEN [UpdatedAt] ELSE [UpdatedAt] END,
    [Status] = CASE WHEN [Status] = N'Generating' THEN N'Failed' ELSE [Status] END,
    [FailureCode] = CASE WHEN [Status] = N'Generating' THEN N'migration_recovered_stale_operation' ELSE [FailureCode] END;");

        migrationBuilder.CreateIndex(
            name: "IX_AiGenerationOperations_Status_LeaseExpiresAtUtc",
            table: "AiGenerationOperations",
            columns: new[] { "Status", "LeaseExpiresAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AiProjectStorePurchases_ProcessingState_NextRetryAtUtc",
            table: "AiProjectStorePurchases",
            columns: new[] { "ProcessingState", "NextRetryAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_AiGenerationOperations_Status_LeaseExpiresAtUtc", table: "AiGenerationOperations");
        migrationBuilder.DropIndex(name: "IX_AiProjectStorePurchases_ProcessingState_NextRetryAtUtc", table: "AiProjectStorePurchases");

        migrationBuilder.DropColumn(name: "OperationEpoch", table: "AiGenerationOperations");
        migrationBuilder.DropColumn(name: "StartedAtUtc", table: "AiGenerationOperations");
        migrationBuilder.DropColumn(name: "LeaseExpiresAtUtc", table: "AiGenerationOperations");
        migrationBuilder.DropColumn(name: "CompletedAtUtc", table: "AiGenerationOperations");
        migrationBuilder.DropColumn(name: "ActiveOperationId", table: "AiTattooProjects");
        migrationBuilder.DropColumn(name: "OperationEpoch", table: "AiTattooProjects");
        migrationBuilder.DropColumn(name: "ConsecutiveGenerationFailures", table: "AiTattooProjects");
        migrationBuilder.DropColumn(name: "RetryCount", table: "AiProjectStorePurchases");
        migrationBuilder.DropColumn(name: "NextRetryAtUtc", table: "AiProjectStorePurchases");
        migrationBuilder.DropColumn(name: "LastAttemptAtUtc", table: "AiProjectStorePurchases");
        migrationBuilder.DropColumn(name: "LastErrorCode", table: "AiProjectStorePurchases");
        migrationBuilder.DropColumn(name: "DeadLetteredAtUtc", table: "AiProjectStorePurchases");

        migrationBuilder.CreateIndex(
            name: "IX_AiGenerationOperations_AiTattooProjectId_RequestKey",
            table: "AiGenerationOperations",
            columns: new[] { "AiTattooProjectId", "RequestKey" },
            unique: true);
    }
}
