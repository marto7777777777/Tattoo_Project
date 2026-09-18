namespace Tattoo_Project.Models;

public static class AiGenerationOperationStatuses
{
    public const string Generating = "Generating";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string TimedOut = "TimedOut";
    public const string Abandoned = "Abandoned";
}

public class AiGenerationOperation
{
    public long Id { get; set; }
    public Guid OperationId { get; set; } = Guid.NewGuid();
    public int AiTattooProjectId { get; set; }
    public AiTattooProject AiTattooProject { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string OperationType { get; set; } = null!;
    public string RequestKey { get; set; } = null!;
    public long OperationEpoch { get; set; }
    public int? BaseVersionId { get; set; }
    public string? Instruction { get; set; }
    public string Status { get; set; } = AiGenerationOperationStatuses.Generating;
    public int? ResultVersionId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime LeaseExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? FailureCode { get; set; }
}
