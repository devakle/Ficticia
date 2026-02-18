using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Net.Sockets;

namespace Api.Host.Seed;

internal static class SqlServerStartup
{
    public static bool IsDatabaseAlreadyExists(Exception ex)
    {
        if (ex is SqlException sqlEx)
        {
            if (sqlEx.Number == 1801 || sqlEx.Errors.Cast<SqlError>().Any(e => e.Number == 1801))
            {
                return true;
            }
        }

        if (ex.Message.Contains("Database", StringComparison.OrdinalIgnoreCase)
            && (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("ya existe", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return ex.InnerException is not null && IsDatabaseAlreadyExists(ex.InnerException);
    }

    public static async Task RunWithDatabaseLockAsync(
        string? connectionString,
        string dbLabel,
        ILogger logger,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{dbLabel} connection string is missing.");
        }

        var master = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        var lockName = $"ficticia:migrate:{dbLabel}";
        await using var conn = new SqlConnection(master.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using (var acquireCmd = new SqlCommand(
                         "DECLARE @r int; EXEC @r = sp_getapplock @Resource=@res, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=120000; SELECT @r;",
                         conn))
        {
            _ = acquireCmd.Parameters.AddWithValue("@res", lockName);
            var result = Convert.ToInt32(await acquireCmd.ExecuteScalarAsync(cancellationToken));
            if (result < 0)
            {
                throw new TimeoutException($"{dbLabel} migration lock was not acquired. sp_getapplock result: {result}");
            }
        }

        try
        {
            logger.LogInformation("{DbLabel} migration lock acquired", dbLabel);
            await action();
        }
        finally
        {
            await using var releaseCmd = new SqlCommand(
                "EXEC sp_releaseapplock @Resource=@res, @LockOwner='Session';",
                conn);
            _ = releaseCmd.Parameters.AddWithValue("@res", lockName);
            _ = await releaseCmd.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("{DbLabel} migration lock released", dbLabel);
        }
    }

    public static async Task WaitUntilServerReadyAsync(
        string? connectionString,
        string dbLabel,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{dbLabel} connection string is missing.");
        }

        var csb = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };
        csb.ConnectTimeout = Math.Min(csb.ConnectTimeout > 0 ? csb.ConnectTimeout : 15, 5);
        logger.LogInformation("{DbLabel} waiting for SQL server at {DataSource}", dbLabel, csb.DataSource);

        const int maxAttempts = 30;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var conn = new SqlConnection(csb.ConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new SqlCommand("SELECT 1", conn);
                _ = await cmd.ExecuteScalarAsync(cancellationToken);
                logger.LogInformation("{DbLabel} SQL server is ready", dbLabel);
                return;
            }
            catch (Exception ex) when (IsReadinessTransient(ex))
            {
                lastException = ex;
                if (attempt == maxAttempts)
                {
                    break;
                }

                var delaySeconds = Math.Min(attempt * 2, 10);
                var sqlNumber = ex is SqlException sqlEx ? sqlEx.Number : 0;
                logger.LogWarning(
                    ex,
                    "{DbLabel} SQL readiness retry {Attempt}/{MaxAttempts}: transient connection failure {SqlNumber}. Waiting {DelaySeconds}s",
                    dbLabel,
                    attempt,
                    maxAttempts,
                    sqlNumber,
                    delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        throw new TimeoutException(
            $"{dbLabel} SQL server was not ready after retries. See inner exception for details.",
            lastException);
    }

    public static async Task EnsureDatabaseExistsAsync(
        string? connectionString,
        string dbLabel,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{dbLabel} connection string is missing.");
        }

        var original = new SqlConnectionStringBuilder(connectionString);
        var databaseName = original.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException($"{dbLabel} database name is missing in connection string.");
        }

        var master = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        var escapedDbId = databaseName.Replace("'", "''", StringComparison.Ordinal);
        var safeDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        var createIfMissingSql = $"""
BEGIN TRY
    IF DB_ID(N'{escapedDbId}') IS NULL
        EXEC(N'CREATE DATABASE [{safeDatabaseName}]');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 1801
        THROW;
END CATCH
""";

        await ExecuteWithRetriesAsync(
            async () =>
            {
                try
                {
                    await using var conn = new SqlConnection(master.ConnectionString);
                    await conn.OpenAsync(cancellationToken);
                    await using var cmd = new SqlCommand(createIfMissingSql, conn);
                    _ = await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex) when (ex.Number == 1801)
                {
                    logger.LogInformation(
                        ex,
                        "{DbLabel} ensure-database-exists: database already exists (1801), continuing",
                        dbLabel);
                }

                return 0;
            },
            dbLabel,
            logger,
            "ensure-database-exists",
            cancellationToken);
    }

    public static async Task<bool> DatabaseExistsAsync(
        string? connectionString,
        string dbLabel,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{dbLabel} connection string is missing.");
        }

        var original = new SqlConnectionStringBuilder(connectionString);
        var databaseName = original.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException($"{dbLabel} database name is missing in connection string.");
        }

        var master = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        return await ExecuteWithRetriesAsync(
            async () =>
            {
                await using var conn = new SqlConnection(master.ConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new SqlCommand("SELECT DB_ID(@dbName)", conn);
                _ = cmd.Parameters.Add(new SqlParameter("@dbName", SqlDbType.NVarChar, 128) { Value = databaseName });
                var value = await cmd.ExecuteScalarAsync(cancellationToken);
                return value is not null && value != DBNull.Value;
            },
            dbLabel,
            logger,
            "database-exists-check",
            cancellationToken);
    }

    public static async Task<IReadOnlyCollection<string>> GetPendingMigrationsWithRetriesAsync(
        Func<Task<IEnumerable<string>>> pendingMigrationsOperation,
        string dbLabel,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteWithRetriesAsync(
            async () => (await pendingMigrationsOperation()).ToArray(),
            dbLabel,
            logger,
            "pending-migrations",
            cancellationToken);

        return result;
    }

    public static async Task MigrateWithRetriesAsync(
        Func<Task> migrateOperation,
        string dbLabel,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 12;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await migrateOperation();
                return;
            }
            catch (SqlException ex) when (IsTransient(ex) || ex.Number == 1801)
            {
                lastException = ex;
                if (attempt == maxAttempts)
                {
                    break;
                }

                var delaySeconds = Math.Min(attempt * 2, 10);
                logger.LogWarning(
                    ex,
                    "{DbLabel} migration retry {Attempt}/{MaxAttempts}: SQL error {SqlNumber}. Waiting {DelaySeconds}s",
                    dbLabel,
                    attempt,
                    maxAttempts,
                    ex.Number,
                    delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        throw new TimeoutException(
            $"{dbLabel} migration failed after retries. See inner exception for details.",
            lastException);
    }

    private static async Task<T> ExecuteWithRetriesAsync<T>(
        Func<Task<T>> operation,
        string dbLabel,
        ILogger logger,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 12;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (SqlException ex) when (IsTransient(ex) || ex.Number == 1801)
            {
                lastException = ex;
                if (attempt == maxAttempts)
                {
                    break;
                }

                var delaySeconds = Math.Min(attempt * 2, 10);
                logger.LogWarning(
                    ex,
                    "{DbLabel} {OperationName} retry {Attempt}/{MaxAttempts}: SQL error {SqlNumber}. Waiting {DelaySeconds}s",
                    dbLabel,
                    operationName,
                    attempt,
                    maxAttempts,
                    ex.Number,
                    delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        throw new TimeoutException(
            $"{dbLabel} {operationName} failed after retries. See inner exception for details.",
            lastException);
    }

    private static bool IsTransient(SqlException ex)
    {
        if (ex.Message.Contains("pre-login handshake", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // DNS resolution can fail briefly while containers are still joining the compose network.
        if (ex.Number == 11001)
        {
            return true;
        }

        if (ex.InnerException is SocketException socketEx &&
            socketEx.SocketErrorCode is SocketError.HostNotFound or SocketError.TryAgain or SocketError.NoData)
        {
            return true;
        }

        return ex.Number is
            0 or
            2 or
            20 or
            53 or
            64 or
            121 or
            233 or
            258 or
            4060 or
            10053 or
            10054 or
            10060 or
            10928 or
            10929 or
            -2;
    }

    private static bool IsReadinessTransient(Exception ex)
    {
        if (ex is SqlException sqlEx)
        {
            return IsTransient(sqlEx);
        }

        return ex is InvalidOperationException or TimeoutException;
    }
}
