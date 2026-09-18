using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiProviderBillingEntitlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppleBillingAppAccountToken",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleBillingObfuscatedAccountId",
                table: "AspNetUsers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActiveProvider",
                table: "ArtistSubscriptions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AppleAppAccountToken",
                table: "ArtistSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstActivatedAt",
                table: "ArtistSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleObfuscatedAccountId",
                table: "ArtistSubscriptions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasUsedTrial",
                table: "ArtistSubscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAt",
                table: "ArtistSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiProjectStorePurchases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiTattooProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PurchasePayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EncryptedPurchasePayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessGrantedUntil = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProjectStorePurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiProjectStorePurchases_AiTattooProjects_AiTattooProjectId",
                        column: x => x.AiTattooProjectId,
                        principalTable: "AiTattooProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderSubscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArtistSubscriptionId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TrialStartsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentPeriodStartsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentPeriodEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "bit", nullable: false),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    PurchaseTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EncryptedPurchasePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderSubscriptions_ArtistSubscriptions_ArtistSubscriptionId",
                        column: x => x.ArtistSubscriptionId,
                        principalTable: "ArtistSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalEventId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorePurchaseBindings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BindingKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginalArtistSubscriptionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePurchaseBindings", x => x.Id);
                });

            // Preserve legacy Stripe state while moving provider-specific data into the
            // normalized provider table. Legacy columns remain temporarily for portal/customer lookup.
            migrationBuilder.Sql("""
                INSERT INTO [ProviderSubscriptions]
                    ([ArtistSubscriptionId], [Provider], [ExternalSubscriptionId], [ExternalTransactionId],
                     [ProductId], [Status], [TrialStartsAt], [TrialEndsAt], [CurrentPeriodStartsAt],
                     [CurrentPeriodEndsAt], [CancelAtPeriodEnd], [IsSandbox], [PurchaseTokenHash],
                     [EncryptedPurchasePayload], [FirstVerifiedAt], [LastVerifiedAt], [CreatedAt], [UpdatedAt])
                SELECT [Id], 'stripe', [StripeSubscriptionId], NULL,
                       'legacy_stripe_artist_monthly', [Status], NULL, [TrialEndsAt], NULL,
                       [CurrentPeriodEndsAt], [CancelAtPeriodEnd], 0, NULL, NULL,
                       [CreatedAt], [UpdatedAt], [CreatedAt], [UpdatedAt]
                FROM [ArtistSubscriptions]
                WHERE [StripeSubscriptionId] IS NOT NULL;

                UPDATE [ArtistSubscriptions]
                SET [ActiveProvider] = CASE
                        WHEN [Status] IN ('trialing', 'active', 'grace_period') THEN 'stripe'
                        WHEN [CancelAtPeriodEnd] = 1 AND [CurrentPeriodEndsAt] > SYSUTCDATETIME() THEN 'stripe'
                        ELSE NULL END,
                    [HasUsedTrial] = CASE WHEN [TrialEndsAt] IS NOT NULL THEN 1 ELSE 0 END,
                    [FirstActivatedAt] = CASE
                        WHEN [Status] IN ('trialing', 'active', 'grace_period') THEN [CreatedAt]
                        ELSE NULL END,
                    [LastVerifiedAt] = [UpdatedAt];

                INSERT INTO [ProviderWebhookEvents]
                    ([Provider], [ExternalEventId], [EventType], [PayloadHash], [ProcessingStatus],
                     [Error], [ReceivedAt], [ProcessedAt])
                SELECT 'stripe', [StripeEventId], [EventType],
                       LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', [StripeEventId]), 2)),
                       [Status], [Error], [ReceivedAt], [ProcessedAt]
                FROM [StripeWebhookEvents];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AppleBillingAppAccountToken",
                table: "AspNetUsers",
                column: "AppleBillingAppAccountToken",
                unique: true,
                filter: "[AppleBillingAppAccountToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GoogleBillingObfuscatedAccountId",
                table: "AspNetUsers",
                column: "GoogleBillingObfuscatedAccountId",
                unique: true,
                filter: "[GoogleBillingObfuscatedAccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistSubscriptions_AppleAppAccountToken",
                table: "ArtistSubscriptions",
                column: "AppleAppAccountToken",
                unique: true,
                filter: "[AppleAppAccountToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistSubscriptions_GoogleObfuscatedAccountId",
                table: "ArtistSubscriptions",
                column: "GoogleObfuscatedAccountId",
                unique: true,
                filter: "[GoogleObfuscatedAccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AiProjectStorePurchases_AiTattooProjectId",
                table: "AiProjectStorePurchases",
                column: "AiTattooProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiProjectStorePurchases_Provider_ExternalTransactionId",
                table: "AiProjectStorePurchases",
                columns: new[] { "Provider", "ExternalTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiProjectStorePurchases_PurchasePayloadHash",
                table: "AiProjectStorePurchases",
                column: "PurchasePayloadHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSubscriptions_ArtistSubscriptionId",
                table: "ProviderSubscriptions",
                column: "ArtistSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSubscriptions_Provider_ExternalSubscriptionId",
                table: "ProviderSubscriptions",
                columns: new[] { "Provider", "ExternalSubscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSubscriptions_Provider_ExternalTransactionId",
                table: "ProviderSubscriptions",
                columns: new[] { "Provider", "ExternalTransactionId" },
                unique: true,
                filter: "[ExternalTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSubscriptions_PurchaseTokenHash",
                table: "ProviderSubscriptions",
                column: "PurchaseTokenHash",
                unique: true,
                filter: "[PurchaseTokenHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderWebhookEvents_Provider_ExternalEventId",
                table: "ProviderWebhookEvents",
                columns: new[] { "Provider", "ExternalEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchaseBindings_Provider_BindingKeyHash",
                table: "StorePurchaseBindings",
                columns: new[] { "Provider", "BindingKeyHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiProjectStorePurchases");

            migrationBuilder.DropTable(
                name: "ProviderSubscriptions");

            migrationBuilder.DropTable(
                name: "ProviderWebhookEvents");

            migrationBuilder.DropTable(
                name: "StorePurchaseBindings");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AppleBillingAppAccountToken",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GoogleBillingObfuscatedAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_ArtistSubscriptions_AppleAppAccountToken",
                table: "ArtistSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ArtistSubscriptions_GoogleObfuscatedAccountId",
                table: "ArtistSubscriptions");

            migrationBuilder.DropColumn(
                name: "AppleBillingAppAccountToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GoogleBillingObfuscatedAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActiveProvider",
                table: "ArtistSubscriptions");

            migrationBuilder.DropColumn(
                name: "AppleAppAccountToken",
                table: "ArtistSubscriptions");

            migrationBuilder.DropColumn(
                name: "FirstActivatedAt",
                table: "ArtistSubscriptions");

            migrationBuilder.DropColumn(
                name: "GoogleObfuscatedAccountId",
                table: "ArtistSubscriptions");

            migrationBuilder.DropColumn(
                name: "HasUsedTrial",
                table: "ArtistSubscriptions");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "ArtistSubscriptions");
        }
    }
}
