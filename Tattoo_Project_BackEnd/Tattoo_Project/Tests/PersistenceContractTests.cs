using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Xunit;

namespace InkRoute.Backend.Tests;

public class PersistenceContractTests
{
    private static TattooDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TattooDbContext>()
            .UseSqlServer("Server=(local);Database=InkRouteModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new TattooDbContext(options);
    }

    [Fact]
    public void ExternalAiTransaction_IsUniquePerProvider()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(AiProjectStorePurchase))!;
        Assert.Contains(entity.GetIndexes(), i => i.IsUnique &&
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(AiProjectStorePurchase.Provider), nameof(AiProjectStorePurchase.ExternalTransactionId) }));
    }

    [Fact]
    public void AiOperationAndVersion_IdempotencyConstraintsExist()
    {
        using var db = CreateContext();
        var operation = db.Model.FindEntityType(typeof(AiGenerationOperation))!;
        Assert.Contains(operation.GetIndexes(), i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == nameof(AiGenerationOperation.OperationId));
        Assert.DoesNotContain(operation.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(AiGenerationOperation.AiTattooProjectId), nameof(AiGenerationOperation.RequestKey) }));

        var version = db.Model.FindEntityType(typeof(AiTattooVersion))!;
        Assert.Contains(version.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(AiTattooVersion.AiTattooProjectId), nameof(AiTattooVersion.VersionNumber) }));
    }

    [Fact]
    public void ArtistResponse_RemainsOnePerTattooRequest()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(ArtistResponse))!;
        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == nameof(ArtistResponse.TattooRequestId));
    }

    [Fact]
    public void AccountDeletionStateMachine_OrderIsStable()
    {
        Assert.Equal(0, (int)AccountDeletionState.DeletionRequested);
        Assert.Equal(1, (int)AccountDeletionState.ExternalSubscriptionsClosed);
        Assert.Equal(2, (int)AccountDeletionState.LocalDeletionCompleted);
        Assert.Equal(3, (int)AccountDeletionState.Completed);
    }

    [Fact]
    public void FileCleanupTask_DefaultIsRetryableUntilCompleted()
    {
        var task = new FileCleanupTask { StorageKey = "private/test.png", Reason = "account_deletion" };
        Assert.Null(task.CompletedAt);
        Assert.Equal(0, task.RetryCount);
    }

    [Fact]
    public void AiStripeCheckoutAttempt_HasDurableIdempotencyAndSingleActiveAttemptConstraints()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(AiProjectCheckoutAttempt))!;
        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == nameof(AiProjectCheckoutAttempt.StripeIdempotencyKey));
        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == nameof(AiProjectCheckoutAttempt.StripeCheckoutSessionId));
        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(AiProjectCheckoutAttempt.UserId), nameof(AiProjectCheckoutAttempt.AiTattooProjectId), nameof(AiProjectCheckoutAttempt.Product) }));
    }

    [Fact]
    public void AiProjectPayment_StripeSessionRemainsUniqueForDoubleGrantProtection()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(AiProjectPayment))!;
        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == nameof(AiProjectPayment.StripeCheckoutSessionId));
    }
}
