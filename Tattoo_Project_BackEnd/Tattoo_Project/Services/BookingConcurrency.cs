using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tattoo_Project.Data;

namespace Tattoo_Project.Services;

internal static class BookingConcurrency
{
    public static async Task AcquireArtistLockAsync(
        TattooDbContext context,
        int artistId,
        CancellationToken cancellationToken = default)
    {
        if (artistId <= 0) throw new ArgumentOutOfRangeException(nameof(artistId));
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Booking lock requires an active database transaction.");

        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            SELECT @result;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = $"InkRoute:ArtistBooking:{artistId}";
        command.Parameters.Add(parameter);

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var raw = await command.ExecuteScalarAsync(cancellationToken);
        var result = raw == null || raw == DBNull.Value ? -999 : Convert.ToInt32(raw);
        if (result < 0)
            throw new DomainConflictException(
                "booking_concurrency_conflict",
                "The artist calendar is being updated by another request. Refresh and retry.");
    }
}
