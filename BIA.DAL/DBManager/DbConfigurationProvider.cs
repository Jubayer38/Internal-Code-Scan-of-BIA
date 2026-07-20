using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;

namespace BIA.DAL.DBManager;

public class DbConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;
    private readonly string _environmentName;

    public DbConfigurationSource(string connectionString, string environmentName)
    {
        _connectionString = connectionString;
        _environmentName = environmentName;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DbConfigurationProvider(_connectionString, _environmentName);
    }
}

public class DbConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;
    private readonly string _environmentName;
    private readonly object _lock = new object();

    public DbConfigurationProvider(string connectionString, string environmentName)
    {
        _connectionString = connectionString;
        _environmentName = environmentName;
    }

    public override void Load()
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var connection = new OracleConnection(_connectionString);
            using var command = connection.CreateCommand();
            
            // Query configuration keys and values from the database, overriding common values with environment-specific ones
            command.CommandText = @"
                SELECT CONFIG_KEY, CONFIG_VALUE 
                FROM BIA_SETTINGS 
                WHERE IS_ACTIVE = 1 
                  AND (ENVIRONMENT = :env OR ENVIRONMENT = 'Common')
                ORDER BY CASE WHEN ENVIRONMENT = 'Common' THEN 0 ELSE 1 END ASC";
                
            command.CommandType = CommandType.Text;
            command.BindByName = true;
            
            command.Parameters.Add(new OracleParameter("env", OracleDbType.Varchar2) { Value = _environmentName });

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string key = reader.GetString(0);
                string value = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                if (!string.IsNullOrWhiteSpace(key))
                {
                    data[key] = value;
                }
            }
        }
        catch (Exception ex)
        {
            // Fail-safe: log configuration load failures to standard error, but do not crash startup
            Console.Error.WriteLine($"Warning: Failed to load configuration from Oracle database for environment '{_environmentName}'. Error: {ex.Message}");
        }

        Data = data;
    }

    public override bool TryGet(string key, out string? value)
    {
        // 1. Try to find the value in the local cache
        if (base.TryGet(key, out value) && !string.IsNullOrEmpty(value))
        {
            return true;
        }

        // 2. Thread-safe lock to check and query database if missing (on-demand lazy load)
        lock (_lock)
        {
            // Double check in case another concurrent thread populated it
            if (base.TryGet(key, out value) && !string.IsNullOrEmpty(value))
            {
                return true;
            }

            try
            {
                string? dbValue = QueryValueFromDb(key);
                if (!string.IsNullOrEmpty(dbValue))
                {
                    // Thread-safe copy-on-write update to the Data dictionary
                    var newData = new Dictionary<string, string?>(Data, StringComparer.OrdinalIgnoreCase);
                    newData[key] = dbValue;
                    Data = newData;

                    value = dbValue;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Failed to fetch key '{key}' from database dynamically. Error: {ex.Message}");
            }
        }

        return base.TryGet(key, out value);
    }

    private string? QueryValueFromDb(string key)
    {
        using var connection = new OracleConnection(_connectionString);
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT CONFIG_VALUE 
            FROM BIA_SETTINGS 
            WHERE IS_ACTIVE = 1 
              AND (ENVIRONMENT = :env OR ENVIRONMENT = 'Common')
              AND CONFIG_KEY = :key
            ORDER BY CASE WHEN ENVIRONMENT = 'Common' THEN 0 ELSE 1 END ASC";

        command.CommandType = CommandType.Text;
        command.BindByName = true;

        command.Parameters.Add(new OracleParameter("env", OracleDbType.Varchar2) { Value = _environmentName });
        command.Parameters.Add(new OracleParameter("key", OracleDbType.Varchar2) { Value = key });

        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
        }
        return null;
    }
}
