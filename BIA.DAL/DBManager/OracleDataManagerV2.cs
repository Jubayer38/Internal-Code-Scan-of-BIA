using BIA.Entity.Collections;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace BIA.DAL.DBManager;

public class OracleDataManagerV2
{
    private readonly string connectionString;

    public OracleDataManagerV2()
    {
        connectionString = SettingsValues.GetConnectionString();
    }

    public OracleDataManagerV2(string connectionString)
    {
        this.connectionString = connectionString;
    }

    /// <summary>
    /// Executes a stored procedure with expecting a return value.
    /// </summary>
    public async Task<long> CallInsertProcedure(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = (OracleTransaction)transaction;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        using var outputParameter = new OracleParameter("po_PKValue", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputParameter);

        try
        {
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Ignore rollback failures when connection is lost
            }
            throw;
        }

        OracleDecimal oracleResult = (OracleDecimal)outputParameter.Value;
        return Convert.ToInt64(oracleResult.Value);
    }

    public async Task<string?> CallInsertProcedureForRefer(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;
        command.Transaction = (OracleTransaction)transaction;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        using var outputParameter = new OracleParameter("PO_PKVALUE", OracleDbType.Varchar2, 50)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(outputParameter);

        try
        {
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Ignore rollback failures
            }
            throw;
        }

        return outputParameter.Value == DBNull.Value ? null : outputParameter.Value.ToString();
    }

    public async Task<int> CallInsertProcedureV2(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = (OracleTransaction)transaction;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        using var outputParameter = new OracleParameter("po_cursor", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputParameter);

        try
        {
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Ignore rollback failures
            }
            throw;
        }

        OracleDecimal oracleResult = (OracleDecimal)outputParameter.Value;
        return Convert.ToInt32(oracleResult.Value);
    }

    public async Task<int> CallInsertProcedureV3(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = (OracleTransaction)transaction;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        using var outputParameter = new OracleParameter("po_return", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputParameter);

        try
        {
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Ignore rollback failures
            }
            throw;
        }

        OracleDecimal oracleResult = (OracleDecimal)outputParameter.Value;
        return Convert.ToInt32(oracleResult.Value);
    }

    /// <summary>
    /// Executes a stored procedure with expecting a return value.
    /// </summary>
    public async Task<bool> CallUpdateProcedure(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = (OracleTransaction)transaction;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        using var outputParameter = new OracleParameter("row_affected", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputParameter);

        try
        {
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Ignore rollback failures
            }
            throw;
        }

        OracleDecimal oracleResult = (OracleDecimal)outputParameter.Value;
        int result = Convert.ToInt32(oracleResult.Value);

        return result > 0;
    }

    /// <summary>
    /// Executes a stored procedure and returns an output parameter value.
    /// </summary>
    public async Task<object> CallSelectDataWithObjectReturn(string storedProcedureName, string outputParamName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = (OracleTransaction)transaction;

        command.Parameters.AddRange(parameters);

        using var outputParameter = new OracleParameter(outputParamName, OracleDbType.Decimal)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputParameter);

        try
        {
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            return command.Parameters[outputParamName].Value;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Ignore rollback failures
            }
            throw;
        }
    }

    /// <summary>
    /// Executes a stored procedure and returns a DataTable.
    /// </summary>
    public async Task<DataTable> SelectProcedure(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        using var adapter = new OracleDataAdapter(command);
        var dataTable = new DataTable();

        adapter.Fill(dataTable);
        return dataTable;
    }

    public async Task<DataTable> SelectProcedureV2(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        using var outputParameter = new OracleParameter("po_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputParameter);

        using var adapter = new OracleDataAdapter(command);
        var dataTable = new DataTable();

        adapter.Fill(dataTable);
        return dataTable;
    }

    public async Task ExecuteProcedure(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<(int result, string? message)> ExecuteProcedureWithOutput(string storedProcedureName, params OracleParameter[] parameters)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        if (parameters?.Length > 0)
            command.Parameters.AddRange(parameters);

        using var resultParam = new OracleParameter("PO_RESULT", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };

        using var messageParam = new OracleParameter("PO_MESSAGE", OracleDbType.Varchar2, 4000)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(resultParam);
        command.Parameters.Add(messageParam);

        await command.ExecuteNonQueryAsync();

        int result = 0;
        if (resultParam.Value != DBNull.Value && resultParam.Value != null)
        {
            result = Convert.ToInt32(resultParam.Value.ToString());
        }

        string? message = messageParam.Value == DBNull.Value ? null : messageParam.Value?.ToString();

        return (result, message);
    }
}
