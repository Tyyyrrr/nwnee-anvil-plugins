using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MySqlConnector;
using NLog;

namespace MySQLClient
{
    internal sealed class MySQLQueryResult : ISqlQueryResult
    {
        public static ISqlQueryResult FromInt(int result) => new NonQueryResult() { Rows = result };

        private readonly struct NonQueryResult : ISqlQueryResult
        {
            public int Rows { get; init; }
            public readonly bool HasData => false;
            public readonly IEnumerator<ISqlRowData> GetEnumerator() => throw new NotSupportedException("Non-query results can't be enumerated.");
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public readonly void Dispose() { }
        }

        public int Rows => -1;
        public bool HasData => true;
        public bool IsDisposed { get; private set; }

        private sealed class MySQLRowData : ISqlRowData
        {
            private static readonly Logger _log = LogManager.GetCurrentClassLogger();
            readonly object[] _columns;

            public T? Get<T>(int column)
            {
                if (column < 0 || column > _columns.Length)
                    _log.Error($"Column index out of range: {column}/{_columns.Length}");
                else if (_columns[column] is not T)
                    _log.Error($"Invalid cast at column index [{column}]: {_columns[column].GetType().FullName} - {typeof(T).FullName}");
                else
                    return (T)_columns[column];

                return default;
            }

            public bool TryGet<T>(int column, [NotNullWhen(true)] out T? value)
            {
                if (column < 0 || column > _columns.Length)
                    _log.Error($"Column index out of range: {column}/{_columns.Length}");
                else if (_columns[column] is not T)
                    _log.Error($"Invalid cast at column index [{column}]: {_columns[column].GetType().FullName} - {typeof(T).FullName}");
                else
                {
                    value = (T)_columns[column];
                    return true;
                }
                value = default;
                return false;
            }

            public MySQLRowData(object[] columns) { _columns = columns; }
        }

        private sealed class MySQLQueryResultEnumerator : IEnumerator<ISqlRowData>
        {
            private MySqlDataReader _reader;
            private static readonly Logger _log = LogManager.GetCurrentClassLogger();
            public MySQLQueryResultEnumerator(MySqlDataReader reader)
            {
                _reader = reader;
                _columns = new object[_reader.FieldCount];
                Current = new MySQLRowData(_columns);
            }
            private readonly object[] _columns;
            public ISqlRowData Current { get; private set; }
            object IEnumerator.Current => Current;
            public void Dispose()
            {
                _reader.Close();
                _reader.Dispose();
            }
            public bool MoveNext()
            {
                try { if (_reader.Read()) return _reader.FieldCount == _reader.GetValues(_columns); }
                catch (MySqlException ex) { _log.Error($"Failed to read SQL query result! - {ex.Message}"); }
                return false;
            }
            public void Reset() => throw new NotSupportedException();
        }

        private MySqlCommand _command;
        private MySQLQueryResultEnumerator _enumerator;
        public MySQLQueryResult(MySqlDataReader reader, MySqlCommand command)
        {
            _enumerator = new MySQLQueryResultEnumerator(reader);
            _command = command;
        }
        IEnumerator<ISqlRowData> IEnumerable<ISqlRowData>.GetEnumerator() => _enumerator;
        IEnumerator IEnumerable.GetEnumerator() => _enumerator;
        public void Dispose()
        {
            _enumerator.Dispose();
            _command.Dispose();
            IsDisposed = true;
        }
    }
}