namespace Tattoo_Project.Models;

public enum AccountDeletionState
{
    DeletionRequested = 0,
    ExternalSubscriptionsClosed = 1,
    LocalDeletionCompleted = 2,
    Completed = 3
}

public class AccountDeletionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public AccountDeletionState State { get; set; } = AccountDeletionState.DeletionRequested;
    public DateTime RequestedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastErrorCode { get; set; }
}
