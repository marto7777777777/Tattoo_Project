using System.Text.Json;
using Tattoo_Project.Models;
using Tattoo_Project.Services;
using Xunit;

namespace InkRoute.Backend.Tests;

public class ReliabilityRulesTests
{
    [Fact]
    public void ActiveAiPass_ExtendsFromExistingEnd()
    {
        var now = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now.AddDays(50), AiPassGrantRules.CalculateEnd(now, now.AddDays(20)));
    }

    [Fact]
    public void ExpiredAiPass_ExtendsFromNow()
    {
        var now = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now.AddDays(30), AiPassGrantRules.CalculateEnd(now, now.AddDays(-1)));
    }

    [Fact]
    public void AiGeneration_HasHardThirtyMinuteTimeout_AndThreeFailureLimit()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), AiGenerationRules.HardTimeout);
        Assert.True(AiGenerationRules.LeaseDuration > AiGenerationRules.HardTimeout);
        Assert.Equal(3, AiGenerationRules.MaxConsecutiveFailures);
        Assert.True(AiGenerationRules.CanStart(new AiTattooProject { ConsecutiveGenerationFailures = 2 }));
        Assert.False(AiGenerationRules.CanStart(new AiTattooProject { ConsecutiveGenerationFailures = 3 }));
    }

    [Fact]
    public void AiGeneration_FencingRejectsOldOperation()
    {
        var active = Guid.NewGuid();
        var project = new AiTattooProject { ActiveOperationId = active, OperationEpoch = 4 };
        Assert.True(AiGenerationRules.OwnsFence(project, active, 4));
        Assert.False(AiGenerationRules.OwnsFence(project, Guid.NewGuid(), 4));
        Assert.False(AiGenerationRules.OwnsFence(project, active, 3));
    }

    [Fact]
    public void FinanciallyUnresolvedPurchase_BlocksDraftCleanup()
    {
        Assert.True(AiPurchaseStateRules.BlocksDraftCleanup(AiPurchaseStates.ConsumptionPending));
        Assert.True(AiPurchaseStateRules.BlocksDraftCleanup(AiPurchaseStates.Consumed));
        Assert.True(AiPurchaseStateRules.BlocksDraftCleanup(AiPurchaseStates.GrantPending));
        Assert.True(AiPurchaseStateRules.BlocksDraftCleanup(AiPurchaseStates.Retryable));
        Assert.True(AiPurchaseStateRules.BlocksDraftCleanup(AiPurchaseStates.Granted));
        Assert.False(AiPurchaseStateRules.BlocksDraftCleanup(AiPurchaseStates.Invalid));
        Assert.False(AiPurchaseStateRules.BlocksDraftCleanup(AiPurchaseStates.Revoked));
    }

    [Fact]
    public void PurchaseStateMachine_DoesNotMoveBackwards()
    {
        Assert.True(AiPurchaseStateRules.CanTransition(AiPurchaseStates.ConsumptionPending, AiPurchaseStates.Consumed));
        Assert.True(AiPurchaseStateRules.CanTransition(AiPurchaseStates.Consumed, AiPurchaseStates.GrantPending));
        Assert.True(AiPurchaseStateRules.CanTransition(AiPurchaseStates.GrantPending, AiPurchaseStates.Granted));
        Assert.False(AiPurchaseStateRules.CanTransition(AiPurchaseStates.Granted, AiPurchaseStates.ConsumptionPending));
        Assert.False(AiPurchaseStateRules.CanTransition(AiPurchaseStates.Consumed, AiPurchaseStates.Verified));
    }

    [Fact]
    public void AppleOneTimePurchase_WithRevocationDate_IsNotGrantable()
    {
        using var doc = JsonDocument.Parse("{\"transactionId\":\"t1\",\"revocationDate\":1770000000000}");
        Assert.Equal(AppleAiPurchaseGrantability.ValidButRevoked, AppleAiPurchaseGrantabilityRules.Evaluate(doc.RootElement));
    }

    [Fact]
    public void AppleOneTimePurchase_WithoutRevocationDate_IsGrantable()
    {
        using var doc = JsonDocument.Parse("{\"transactionId\":\"t1\"}");
        Assert.Equal(AppleAiPurchaseGrantability.ValidAndGrantable, AppleAiPurchaseGrantabilityRules.Evaluate(doc.RootElement));
    }

    [Fact]
    public void OlderProviderEvent_IsRejected()
    {
        var current = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(ProviderEventOrdering.ShouldApply(current, "b", current.AddSeconds(-1), "z"));
        Assert.False(ProviderEventOrdering.ShouldApply(current, "b", current, "a"));
        Assert.True(ProviderEventOrdering.ShouldApply(current, "b", current, "c"));
    }

    [Fact]
    public void SandboxPolicy_DefaultDeny_ExplicitAllow()
    {
        Assert.False(BillingSandboxPolicy.CanGrant(true, false));
        Assert.True(BillingSandboxPolicy.CanGrant(true, true));
        Assert.True(BillingSandboxPolicy.CanGrant(false, false));
    }

    [Fact]
    public void SubscriptionNormalization_DoesNotUpgradePastDue()
    {
        var now = DateTime.UtcNow;
        Assert.Equal(ArtistSubscriptionStatuses.PastDue,
            SubscriptionEntitlementRules.Normalize(ArtistSubscriptionStatuses.PastDue, null, now.AddDays(10), false, false, now));
    }

    [Fact]
    public void StateMachine_RejectAndCancelRemainTerminal()
    {
        Assert.False(TattooRequestStateMachine.CanTransition(RequestStatus.Rejected, RequestStatus.Approved));
        Assert.False(TattooRequestStateMachine.CanTransition(RequestStatus.Cancelled, RequestStatus.InProgress));
    }

    [Fact]
    public void AiStripeCheckout_StateMachine_IsMonotonic()
    {
        Assert.True(AiProjectCheckoutAttemptRules.CanTransition(AiProjectCheckoutAttemptStatuses.Creating, AiProjectCheckoutAttemptStatuses.SessionCreated));
        Assert.True(AiProjectCheckoutAttemptRules.CanTransition(AiProjectCheckoutAttemptStatuses.SessionCreated, AiProjectCheckoutAttemptStatuses.PaymentConfirmed));
        Assert.True(AiProjectCheckoutAttemptRules.CanTransition(AiProjectCheckoutAttemptStatuses.PaymentConfirmed, AiProjectCheckoutAttemptStatuses.GrantPending));
        Assert.True(AiProjectCheckoutAttemptRules.CanTransition(AiProjectCheckoutAttemptStatuses.GrantPending, AiProjectCheckoutAttemptStatuses.Granted));
        Assert.False(AiProjectCheckoutAttemptRules.CanTransition(AiProjectCheckoutAttemptStatuses.Granted, AiProjectCheckoutAttemptStatuses.Creating));
        Assert.False(AiProjectCheckoutAttemptRules.CanTransition(AiProjectCheckoutAttemptStatuses.PaymentConfirmed, AiProjectCheckoutAttemptStatuses.Creating));
    }

    [Fact]
    public void AiStripeCheckout_OnlyFinanciallyLiveStatesAreActive()
    {
        Assert.True(AiProjectCheckoutAttemptStatuses.IsActive(AiProjectCheckoutAttemptStatuses.Creating));
        Assert.True(AiProjectCheckoutAttemptStatuses.IsActive(AiProjectCheckoutAttemptStatuses.SessionCreated));
        Assert.True(AiProjectCheckoutAttemptStatuses.IsActive(AiProjectCheckoutAttemptStatuses.PaymentConfirmed));
        Assert.True(AiProjectCheckoutAttemptStatuses.IsActive(AiProjectCheckoutAttemptStatuses.GrantPending));
        Assert.False(AiProjectCheckoutAttemptStatuses.IsActive(AiProjectCheckoutAttemptStatuses.Granted));
        Assert.False(AiProjectCheckoutAttemptStatuses.IsActive(AiProjectCheckoutAttemptStatuses.Expired));
        Assert.False(AiProjectCheckoutAttemptStatuses.IsActive(AiProjectCheckoutAttemptStatuses.Failed));
    }
}
