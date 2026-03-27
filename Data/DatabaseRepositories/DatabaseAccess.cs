using AdoNetCore.AseClient;
using Dapper;
using Dapper.Contrib.Extensions;
using Domain.Enums;
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

public class DatabaseAccess
{
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

    public async Task<T?> ProcedureFirstOrDefaultAsync<T>(string sQuery, object parameter, DataBaseConnectionModel connection)
    {
        return await ExecQueryFirstOrDefault<T>(sQuery, parameter, connection, CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<T>?> Procedure<T>(string sQuery, object parameter, DataBaseConnectionModel connection)
    {
        return await ExecQuery<T>(sQuery, parameter, connection, CommandType.StoredProcedure);
    }

    public IAsyncEnumerable<T> ProcedureStream<T>(string sQuery, object? parameter, DataBaseConnectionModel connection, CancellationToken cancellationToken = default)
    {
        return ExecQueryStream<T>(sQuery, parameter, connection, CommandType.StoredProcedure, cancellationToken);
    }

    public async Task<IEnumerable<IDictionary<string, object?>>?> ProcedureMultipleTables(string sQuery, object parameter, DataBaseConnectionModel connection)
    {
        List<Dictionary<string, object?>> objReturn = [];

        using var dbConnection = GetDatabaseConnection(connection);

        try
        {
            await OpenConnectionAsync(dbConnection);

            using var result = await dbConnection.QueryMultipleAsync(sQuery, parameter, commandType: CommandType.StoredProcedure);

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

        return objReturn;
    }


    public async Task<T?> QueryFirstOrDefault<T>(string sQuery, object? parameter, DataBaseConnectionModel connection)
    {
        return await ExecQueryFirstOrDefault<T>(sQuery, parameter, connection);
    }

    public async Task<IEnumerable<T>?> Query<T>(string sQuery, object? parameter, DataBaseConnectionModel connection)
    {
        return await ExecQuery<T>(sQuery, parameter, connection);
    }

    public IAsyncEnumerable<T> QueryStream<T>(string sQuery, object? parameter, DataBaseConnectionModel connection, CancellationToken cancellationToken = default)
    {
        return ExecQueryStream<T>(sQuery, parameter, connection, cancellationToken: cancellationToken);
    }

    #region BaseMethods
    private async Task<IEnumerable<T>?> ExecQuery<T>(string sQuery, object? parameter, DataBaseConnectionModel connection, CommandType? type = null)
    {
        using var dbConnection = GetDatabaseConnection(connection);

        try
        {
            await OpenConnectionAsync(dbConnection);

            return await dbConnection.QueryAsync<T>(sQuery, parameter, commandType: type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }

    }

    private async IAsyncEnumerable<T> ExecQueryStream<T>(
        string sQuery,
        object? parameter,
        DataBaseConnectionModel connection,
        CommandType? type = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var dbConnection = GetDatabaseConnection(connection);

        await OpenConnectionAsync(dbConnection, cancellationToken);

        var command = new CommandDefinition(sQuery, parameter, commandType: type, cancellationToken: cancellationToken);
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

    private async Task<T?> ExecQueryFirstOrDefault<T>(string sQuery, object? parameter, DataBaseConnectionModel connection, CommandType? type = null)
    {
        using var dbConnection = GetDatabaseConnection(connection);

        try
        {
            await OpenConnectionAsync(dbConnection);

            return await dbConnection.QueryFirstOrDefaultAsync<T>(sQuery, parameter, commandType: type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return default;
        }

    }

    public async Task<T> Read<T>(object Id, DataBaseConnectionModel connection) where T : class
    {
        using var dbConnection = GetDatabaseConnection(connection);
        await OpenConnectionAsync(dbConnection);

        return await dbConnection.GetAsync<T>(Id);
    }

    public async Task<bool> Update<T>(T Obj, DataBaseConnectionModel connection) where T : class
    {
        using var dbConnection = GetDatabaseConnection(connection);
        await OpenConnectionAsync(dbConnection);

        return await dbConnection.UpdateAsync<T>(Obj);
    }

    public async Task<object> Insert<T>(T Obj, DataBaseConnectionModel connection) where T : class
    {
        using var dbConnection = GetDatabaseConnection(connection);
        await OpenConnectionAsync(dbConnection);

        return await dbConnection.InsertAsync(Obj);

    }

    public async Task<bool> Delete<T>(T Obj, DataBaseConnectionModel connection) where T : class
    {
        using var dbConnection = GetDatabaseConnection(connection);
        await OpenConnectionAsync(dbConnection);

        return await dbConnection.DeleteAsync(Obj);
    }
    #endregion BaseMethods
}
