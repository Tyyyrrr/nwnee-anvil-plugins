using System;
using System.Linq;
using Anvil.Services;
using MySqlConnector;
using NLog;

namespace MySQLClient
{
  [ServiceBinding(typeof(MySQLService))]
  public class MySQLService
  {
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private MySqlConnection _connection;
    private MySQLQueryBuilder _queryBuilder;

    /// <summary>NOTE: Each call to this getter will call <see cref="ISqlQueryBuilder.Reset"/> on the service query builder, restoring default (empty) state.</summary>
    /// <remarks>If you want to keep the current query - either store the result of <see cref="ISqlQueryBuilder.Build"/> or a reference to <see cref="ISqlQueryBuilder"/> itself in a variable and use the variable instead of the getter</remarks>
    public ISqlQueryBuilder QueryBuilder { get { _queryBuilder.Reset(); return _queryBuilder; } }

    public MySQLService(EasyConfig.ConfigurationService easyCfg)
    {
      _log.Info("Establishing SQL connection...");

      var config = easyCfg.GetConfig<MySQLClientConfig>();

      config ??= new();

      var builder = new MySqlConnectionStringBuilder
      {
        Server = config.Server,
        Port = config.Port,
        UserID = config.UserID,
        Password = config.Password,
        Database = config.Database,
        CharacterSet = config.CharacterSet,
        SslMode = config.SSL ? MySqlSslMode.VerifyFull : MySqlSslMode.None
      };

      _connection = (MySqlConnection)MySqlConnectorFactory.Instance.CreateConnection();

      _connection.ConnectionString = builder.ConnectionString;

      _queryBuilder = new();

      try
      {
        _connection.Open();
      }
      catch (MySqlException ex)
      {
        _log.Error($"Failed to open SQL connection! - {ex.Message}");
        return;
      }

      _log.Info("SQL Connection open!");

      _queryBuilder = new();
    }


    MySQLQueryResult? _lastDisposableResult = null;

    /// <summary>
    /// Directly executes the command
    /// </summary>
    /// <param name="query">Command to run on database</param>
    public ISqlQueryResult ExecuteQuery(string query)
    {
      if (_lastDisposableResult != null)
      {
        if (!_lastDisposableResult.IsDisposed)
        {
          _log.Warn("Last query result is not disposed. Always dispose of ISqlQueryResult if it HasData!");
          _lastDisposableResult.Dispose();
        }
        _lastDisposableResult = null;
      }

      var command = _connection.CreateCommand();

      command.CommandText = query;

      MySqlDataReader reader;
      try
      {
        reader = command.ExecuteReader();
      }
      catch (MySqlException e)
      {
        _log.Error($"Failed to execute MySqlDataReader - {e.Message}\nQuery: {command.CommandText}");
        command.Dispose();
        return ISqlQueryResult.NoResult;
      }

      if (reader.IsClosed)
      {
        _log.Warn("MySqlDataReader is closed!");
        command.Dispose();
        return ISqlQueryResult.NoResult;
      }

      if (reader.HasRows)
      {
        if (reader.FieldCount < 1)
        {
          reader.Close();
          reader.Dispose();
          command.Dispose();
          return ISqlQueryResult.NoResult;
        }

        if (query.StartsWith("SELECT COUNT"))
        {
          using var result = new MySQLQueryResult(reader, command);
          
          if (!result.HasData || !result.First().TryGet<long>(0, out var value))
            return ISqlQueryResult.NoResult;

          return MySQLQueryResult.FromInt(Convert.ToInt32(value));
        }
        _lastDisposableResult = new MySQLQueryResult(reader, command);
        return _lastDisposableResult;
      }

      int rows = reader.RecordsAffected;

      reader.Close();
      reader.Dispose();
      command.Dispose();

      return rows > 0 ? MySQLQueryResult.FromInt(rows) : ISqlQueryResult.Zero;
    }

    /// <summary>
    /// Executes a query from the builder.
    /// </summary>
    public ISqlQueryResult ExecuteQuery()
    {
      if (!_queryBuilder.IsValid)
      {
        _log.Warn("Can't execute query. Builder is in invalid state.");
        return ISqlQueryResult.NoResult;
      }

      if (_queryBuilder.IsEmpty)
      {
        _log.Warn("Can't execute empty query.");
        return ISqlQueryResult.NoResult;
      }

      return ExecuteQuery(_queryBuilder.Build());
    }
  }
}