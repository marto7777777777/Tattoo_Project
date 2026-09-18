using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public static class TimeZoneSupport
{
    public const string DefaultTimeZoneId = "Europe/Sofia";

    public static ResultService<TimeZoneInfo> Get(string? timeZoneId)
    {
        var id = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId.Trim();
        try { return ResultService<TimeZoneInfo>.Ok(TimeZoneInfo.FindSystemTimeZoneById(id)); }
        catch (TimeZoneNotFoundException) { return ResultService<TimeZoneInfo>.Fail("The artist timezone is invalid."); }
        catch (InvalidTimeZoneException) { return ResultService<TimeZoneInfo>.Fail("The artist timezone is invalid."); }
    }

    public static ResultService<DateTime> ToUtc(DateTime value, string? timeZoneId)
    {
        if (value.Kind == DateTimeKind.Utc) return ResultService<DateTime>.Ok(value);
        var zoneResult = Get(timeZoneId); if (!zoneResult.Success) return ResultService<DateTime>.Fail(zoneResult.ErrorMessage!);
        var local = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        if (zoneResult.Data!.IsInvalidTime(local)) return ResultService<DateTime>.Fail("The selected local time does not exist because of a daylight-saving transition.");
        if (zoneResult.Data.IsAmbiguousTime(local)) return ResultService<DateTime>.Fail("The selected local time is ambiguous because of a daylight-saving transition. Send an ISO-8601 UTC/offset timestamp.");
        return ResultService<DateTime>.Ok(TimeZoneInfo.ConvertTimeToUtc(local, zoneResult.Data));
    }
}
