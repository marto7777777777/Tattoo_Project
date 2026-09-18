using Tattoo_Project.Models;

namespace Tattoo_Project.Services;

public static class AiGenerationRules
{
    public static readonly TimeSpan HardTimeout = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(35);
    public const int MaxConsecutiveFailures = 3;

    public static bool CanStart(AiTattooProject project) =>
        project.ActiveOperationId == null && project.ConsecutiveGenerationFailures < MaxConsecutiveFailures;

    public static bool OwnsFence(AiTattooProject project, Guid operationId, long operationEpoch) =>
        project.ActiveOperationId == operationId && project.OperationEpoch == operationEpoch;
}
