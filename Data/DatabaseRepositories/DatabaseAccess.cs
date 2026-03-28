using AdoNetCore.AseClient;
using Dapper;
using Dapper.Contrib.Extensions;
using Domain;
using Domain.Enums;
using Domain.Extensions;
using Domain.Models.ApplicationConfigurationModels;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Data.DatabaseRepositories;

public class DatabaseAccess(AppUtils utils)
{
    private readonly AppUtils _utils = utils;
    private DataBaseConnectionModel? _scopedConnection;

    #region Connection Management
    public void Configure(string dataBaseId)
    {
        _scopedConnection = _utils.GetDataBase(dataBaseId);
    }

    private static async Task OpenConnectionAsync(IDbConnection databaseConnection, CancellationToken cancellationToken = default)
    {
        if (databaseConnection.State == ConnectionState.Open)
        {
            return;
        }

        if (databaseConnection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
            return;
        }

        databaseConnection.Open();
    }

    private static IDbConnection GetDatabaseConnection(DataBaseConnectionModel bd)
    {
        ArgumentNullException.ThrowIfNull(bd);

        if (string.IsNullOrWhiteSpace(bd.ConnectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(bd));
        }

        return bd.Type switch
        {
            DataBaseType.SQLSERVER => new SqlConnection(bd.ConnectionString),
            DataBaseType.ORACLE => new OracleConnection(bd.ConnectionString),
            DataBaseType.MYSQL => new MySqlConnection(bd.ConnectionString),
            DataBaseType.MARIADB => new MySqlConnection(bd.ConnectionString),
            DataBaseType.POSTGRESQL => new NpgsqlConnection(bd.ConnectionString),
            DataBaseType.FIREBIRD => new FbConnection(bd.ConnectionString),
            DataBaseType.SYBASE => new AseConnection(bd.ConnectionString),
            _ => throw new NotSupportedException($"Database type '{bd.Type}' is not supported.")
        };
    }

    private DataBaseConnectionModel ResolveConnectionModel()
    {
        return _scopedConnection
            ?? throw new InvalidOperationException("DatabaseAccess is not configured. Call Configure with a valid database id before executing queries.");
    }

    private async Task<(IDbConnection Connection, bool ShouldDispose)> ResolveExecutionConnectionAsync(
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        if (transaction?.Connection is IDbConnection transactionConnection)
        {
            await OpenConnectionAsync(transactionConnection, cancellationToken);
            return (transactionConnection, false);
        }

        var dbConnection = GetDatabaseConnection(ResolveConnectionModel());
        await OpenConnectionAsync(dbConnection, cancellationToken);
        return (dbConnection, true);
    }

    #endregion Connection Management

    public async Task<T?> ProcedureFirstOrDefaultAsync<T>(string query, object? parameter, IDbTransaction? transaction = null)
    {
        return await ExecQueryFirstOrDefault<T>(query, parameter, CommandType.StoredProcedure, transaction);
    }

    public async Task<IEnumerable<T>?> Procedure<T>(string query, object? parameter, IDbTransaction? transaction = null)
    {
        return await ExecQuery<T>(query, parameter, CommandType.StoredProcedure, transaction);
    }

    public IAsyncEnumerable<T> ProcedureStream<T>(string query, object? parameter, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecQueryStream<T>(query, parameter, CommandType.StoredProcedure, transaction, cancellationToken);
    }

    public async Task<IEnumerable<IDictionary<string, object?>>?> ProcedureMultipleTables(string query, object? parameter, IDbTransaction? transaction = null)
    {
        List<Dictionary<string, object?>> objReturn = [];
        var (dbConnection, shouldDispose) = await ResolveExecutionConnectionAsync(transaction);

        try
        {
            using var result = await dbConnection.QueryMultipleAsync(query, parameter, transaction, commandType: CommandType.StoredProcedure);

            do
            {
                var tableData = (await result.ReadAsync())
                    .Select(row => (IDictionary<string, object?>)row)
                    .Select(row => row.ToDictionary(kv => kv.Key, kv => kv.Value))
                    .ToList();

                objReturn.AddRange(tableData);
            }
            while (!result.IsConsumed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
        finally
        {
            if (shouldDispose)
            {
                dbConnection.Dispose();
            }
        }

        return objReturn;
    }

    public async Task<T?> QueryFirstOrDefault<T>(string query, object? parameter, IDbTransaction? transaction = null)
    {
        return await ExecQueryFirstOrDefault<T>(query, parameter, transaction: transaction);
    }

    public async Task<IEnumerable<T>?> Query<T>(string query, object? parameter, IDbTransaction? transaction = null)
    {
        return await ExecQuery<T>(query, parameter, transaction: transaction);
    }

    public IAsyncEnumerable<T> QueryStream<T>(string query, object? parameter, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return ExecQueryStream<T>(query, parameter, transaction: transaction, cancellationToken: cancellationToken);
    }

    #region Cached Methods
    public async Task<IReadOnlyList<T>> QueryPagedCachedAsync<T>(
        string query,
        object? parameter,
        IDbTransaction? transaction = null,
        int page = 1,
        int pageSize = 100,
        TimeSpan? cacheExpiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var queryKey = BuildQueryCacheKey(query, parameter, ResolveConnectionModel());
        var pageKey = BuildPageCacheKey(queryKey, page, pageSize);

        var cachedPage = await _utils.GetFromCache<List<T>>(pageKey);
        if (cachedPage is { Count: > 0 })
        {
            return cachedPage;
        }

        var requestedPageSource = new TaskCompletionSource<IReadOnlyList<T>>(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = Task.Run(async () =>
        {
            try
            {
                var currentPage = 1;
                var currentPageItems = new List<T>(pageSize);

                await foreach (var item in QueryStream<T>(query, parameter, transaction, cancellationToken))
                {
                    currentPageItems.Add(item);

                    if (currentPageItems.Count < pageSize)
                    {
                        continue;
                    }

                    var pageItems = currentPageItems.ToList();
                    await _utils.SetToCache(BuildPageCacheKey(queryKey, currentPage, pageSize), pageItems, cacheExpiration);

                    if (currentPage == page)
                    {
                        requestedPageSource.TrySetResult(pageItems);
                    }

                    currentPage++;
                    currentPageItems.Clear();
                }

                if (currentPageItems.Count > 0)
                {
                    var finalPageItems = currentPageItems.ToList();
                    await _utils.SetToCache(BuildPageCacheKey(queryKey, currentPage, pageSize), finalPageItems, cacheExpiration);

                    if (currentPage == page)
                    {
                        requestedPageSource.TrySetResult(finalPageItems);
                    }
                }

                requestedPageSource.TrySetResult([]);
            }
            catch (Exception ex)
            {
                requestedPageSource.TrySetException(ex);
            }
        }, cancellationToken);

        return await requestedPageSource.Task.WaitAsync(cancellationToken);
    }

    private static string BuildPageCacheKey(string queryCacheKey, int page, int pageSize)
    {
        return $"{queryCacheKey}:page:{page}:size:{pageSize}";
    }

    private static string BuildQueryCacheKey(string query, object? parameter, DataBaseConnectionModel connection)
    {
        var parameterJson = parameter?.ToJson() ?? "null";
        var rawKey = $"{connection.Type}|{connection.ConnectionString}|{query}|{parameterJson}";
        return rawKey.ToSHA256();
    }
    #endregion Cached Methods

    #region Base Methods
    private async Task<IEnumerable<T>?> ExecQuery<T>(string query, object? parameter, CommandType? type = null, IDbTransaction? transaction = null)
    {
        var (dbConnection, shouldDispose) = await ResolveExecutionConnectionAsync(transaction);

        try
        {
            return await dbConnection.QueryAsync<T>(query, parameter, transaction, commandType: type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
        finally
        {
            if (shouldDispose)
            {
                dbConnection.Dispose();
            }
        }

    }

    private async IAsyncEnumerable<T> ExecQueryStream<T>(
        string query,
        object? parameter,
        CommandType? type = null,
        IDbTransaction? transaction = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (dbConnection, shouldDispose) = await ResolveExecutionConnectionAsync(transaction, cancellationToken);

        try
        {
            var command = new CommandDefinition(query, parameter, transaction, commandType: type, cancellationToken: cancellationToken);
            using var reader = await dbConnection.ExecuteReaderAsync(command);
            var parser = reader.GetRowParser<T>();

            if (reader is DbDataReader dbDataReader)
            {
                while (await dbDataReader.ReadAsync(cancellationToken))
                {
                    yield return parser(dbDataReader);
                }

                yield break;
            }

            while (reader.Read())
            {
                yield return parser(reader);
            }
        }
        finally
        {
            if (shouldDispose)
            {
                dbConnection.Dispose();
            }
        }
    }

    private async Task<T?> ExecQueryFirstOrDefault<T>(string query, object? parameter, CommandType? type = null, IDbTransaction? transaction = null)
    {
        var (dbConnection, shouldDispose) = await ResolveExecutionConnectionAsync(transaction);

        try
        {
            return await dbConnection.QueryFirstOrDefaultAsync<T>(query, parameter, transaction, commandType: type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return default;
        }
        finally
        {
            if (shouldDispose)
            {
                dbConnection.Dispose();
            }
        }

    }

    #region Transaction Methods
    public async Task<IDbTransaction> CreateTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        var dbConnection = GetDatabaseConnection(ResolveConnectionModel());
        await OpenConnectionAsync(dbConnection, cancellationToken);

        if (dbConnection is DbConnection dbConnectionAsync)
        {
            return await dbConnectionAsync.BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        return dbConnection.BeginTransaction(isolationLevel);
    }

    public async Task CompleteTransactionAsync(
        IDbTransaction? transaction,
        bool saveChanges,
        CancellationToken cancellationToken = default)
    {
        if (transaction is null)
        {
            return;
        }

        var connection = transaction.Connection;

        try
        {
            if (saveChanges)
            {
                await CommitTransactionAsync(transaction, cancellationToken);
                return;
            }

            await RollbackTransactionAsync(transaction, cancellationToken);
        }
        finally
        {
            if (transaction is IAsyncDisposable asyncTransaction)
            {
                await asyncTransaction.DisposeAsync();
            }
            else
            {
                transaction.Dispose();
            }

            if (connection is IAsyncDisposable asyncConnection)
            {
                await asyncConnection.DisposeAsync();
            }
            else
            {
                connection?.Dispose();
            }
        }
    }

    private static async Task CommitTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.CommitAsync(cancellationToken);
            return;
        }

        transaction.Commit();
    }

    private static async Task RollbackTransactionAsync(IDbTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            return;
        }

        transaction.Rollback();
    }

    public async Task CreateSavepointAsync(
        IDbTransaction transaction,
        string savepointName,
        CancellationToken cancellationToken = default)
    {
        var dbTransaction = ResolveSavepointTransaction(transaction);
        ValidateSavepointName(savepointName);

        await dbTransaction.SaveAsync(savepointName, cancellationToken);
    }

    public async Task RollbackToSavepointAsync(
        IDbTransaction transaction,
        string savepointName,
        CancellationToken cancellationToken = default)
    {
        var dbTransaction = ResolveSavepointTransaction(transaction);
        ValidateSavepointName(savepointName);

        await dbTransaction.RollbackAsync(savepointName, cancellationToken);
    }

    public async Task ReleaseSavepointAsync(
        IDbTransaction transaction,
        string savepointName,
        CancellationToken cancellationToken = default)
    {
        ValidateSavepointName(savepointName);
        var dbTransaction = ResolveSavepointTransaction(transaction);

        await dbTransaction.ReleaseAsync(savepointName, cancellationToken);
    }

    private static DbTransaction ResolveSavepointTransaction(IDbTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction is not DbTransaction dbTransaction)
        {
            throw new NotSupportedException("Savepoints require a DbTransaction implementation.");
        }

        if (!dbTransaction.SupportsSavepoints)
        {
            throw new NotSupportedException("The current database provider does not support savepoints.");
        }

        return dbTransaction;
    }

    private static void ValidateSavepointName(string savepointName)
    {
        if (string.IsNullOrWhiteSpace(savepointName))
        {
            throw new ArgumentException("Savepoint name cannot be null or empty.", nameof(savepointName));
        }
    }
    #endregion Transaction Methods

    #region Basic Methods
    public async Task<T> Read<T>(object Id, IDbTransaction? transaction = null) where T : class
    {
        var (dbConnection, shouldDispose) = await ResolveExecutionConnectionAsync(transaction);

        try
        {
            return await dbConnection.GetAsync<T>(Id, transaction);
        }
        finally
        {
            if (shouldDispose)
            {
                dbConnection.Dispose();
            }
        }
    }

    public async Task<bool> Update<T>(T Obj, IDbTransaction? transaction = null) where T : class
    {
        var (dbConnection, shouldDispose) = await ResolveExecutionConnectionAsync(transaction);

        try
        {
            return await dbConnection.UpdateAsync<T>(Obj, transaction);
        }
        finally
        {
            if (shouldDispose)
            {
                dbConnection.Dispose();
            }
        }
    }

    public async Task<object> Insert<T>(T Obj, IDbTransaction? transaction = null) where T : class
    {
        var (dbConnection, shouldDispose) = await ResolveExecutionConnectionAsync(transaction);

        try
        {
            return await dbConnection.InsertAsync(Obj, transaction);
        }
        finally
        {
            if (shouldDispose)
            {
                dbConnection.Dispose();
            }
        }
    }

    public async Task<bool> Delete<T>(T Obj, IDbTransaction? transaction = null) where T : class
    {
        var (dbConnection, shouldDispose) = await ResolveExecutionConnectionAsync(transaction);

        try
        {
            return await dbConnection.DeleteAsync(Obj, transaction);
        }
        finally
        {
            if (shouldDispose)
            {
                dbConnection.Dispose();
            }
        }
    }
    #endregion Basic Methods
    #endregion Base Methods
}
