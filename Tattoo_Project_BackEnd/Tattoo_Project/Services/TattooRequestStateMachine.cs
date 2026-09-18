using Tattoo_Project.Models;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public static class TattooRequestStateMachine
{
    private static readonly IReadOnlyDictionary<RequestStatus, HashSet<RequestStatus>> Allowed =
        new Dictionary<RequestStatus, HashSet<RequestStatus>>
        {
            [RequestStatus.Submitted] = [RequestStatus.UnderReview, RequestStatus.Approved, RequestStatus.Rejected],
            [RequestStatus.UnderReview] = [RequestStatus.Approved, RequestStatus.Rejected],
            [RequestStatus.Approved] = [RequestStatus.WaitingForConsultation, RequestStatus.TattooBooked, RequestStatus.Cancelled],
            [RequestStatus.WaitingForConsultation] = [RequestStatus.Approved, RequestStatus.ConsultationCompleted, RequestStatus.Cancelled],
            [RequestStatus.ConsultationCompleted] = [RequestStatus.TattooBooked, RequestStatus.Cancelled],
            [RequestStatus.TattooBooked] = [RequestStatus.Approved, RequestStatus.ConsultationCompleted, RequestStatus.InProgress, RequestStatus.Completed, RequestStatus.Cancelled],
            [RequestStatus.InProgress] = [RequestStatus.Completed, RequestStatus.Cancelled],
            [RequestStatus.Completed] = [RequestStatus.InProgress],
            [RequestStatus.Rejected] = [],
            [RequestStatus.Cancelled] = []
        };

    public static bool CanTransition(RequestStatus from, RequestStatus to) =>
        from == to || (Allowed.TryGetValue(from, out var targets) && targets.Contains(to));

    public static ResultService Transition(TattooRequest request, RequestStatus target)
    {
        if (!CanTransition(request.Status, target))
            return ResultService.Fail($"Invalid tattoo request status transition: {request.Status} -> {target}.");
        request.Status = target;
        return ResultService.Ok();
    }
}
