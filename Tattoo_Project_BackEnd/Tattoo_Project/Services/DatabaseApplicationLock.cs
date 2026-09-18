using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tattoo_Project.Data;

namespace Tattoo_Project.Services;

internal static class DatabaseApplicationLock
{
    public static async Task AcquireAsync(TattooDbContext context, string resource, string conflictCode, string conflictMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentException("Lock resource is required.", nameof(resource));
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Application lock requires an active database transaction.");

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
        parameter.Value = resource;
        command.Parameters.Add(parameter);
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        var raw = await command.ExecuteScalarAsync(cancellationToken);
        var result = raw == null || raw == DBNull.Value ? -999 : Convert.ToInt32(raw);
        if (result < 0) throw new DomainConflictException(conflictCode, conflictMessage);
    }
}
