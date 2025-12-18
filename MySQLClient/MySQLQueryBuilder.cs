using System.Text;
using MySqlConnector;
using NLog;

namespace MySQLClient
{
    internal sealed class MySQLQueryBuilder : ISqlQueryBuilder
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        StringBuilder _builder;

        public MySQLQueryBuilder()
        {
            _builder = new StringBuilder(128);
        }

        private State _state = State.Empty;
        private enum State
        {
            Empty,
            Select,
            SelectCount,
            InsertInto,
            Update,
            InsertOrUpdate,
            DeleteFrom,
            Where,
            WhereNot,
            And,
            Or,
            Not,
            Limit,
            Invalid
        }
        public bool IsValid => _state != State.Invalid;
        public bool IsEmpty => _state == State.Empty;
        public string Build()
        {
            if (!IsValid || IsEmpty) return string.Empty;
            return _builder.ToString();
        }
        public void Reset() => _state = State.Empty;
        
        private void SetInvalid(State rejectedInput)
        {
            _log.Error($"Can't make transition from {_state} to {rejectedInput}");
            _state = State.Invalid;
        }

        public ISqlQueryBuilder Select(string table, string columns)
        {
            if (_state != State.Empty)
            {
                SetInvalid(State.Select);
                return this;
            }

            _builder.Clear().Append("SELECT ").Append(columns.Trim()).Append(" FROM ").Append(table);

            _state = State.Select;
            return this;
        }

        public ISqlQueryBuilder SelectCount(string table)
        {
            if (_state != State.Empty)
            {
                SetInvalid(State.SelectCount);
                return this;
            }

            _builder.Clear().Append("SELECT COUNT(*) FROM ").Append(table);
            
            _state = State.SelectCount;
            return this;
        }



        public ISqlQueryBuilder InsertInto(string table, string columns, params object[] values)
        {
            if (_state != State.Empty)
            {
                SetInvalid(State.InsertInto);
                return this;
            }

            var columnsArray = columns.Split(',');

            if (columnsArray.Length != values.Length)
            {
                _log.Error("Columns and values must have the same length.");
                
                _state = State.Invalid;
                return this;
            }
            CoerceValues(values);

            _builder.Clear().Append("INSERT INTO ").Append(table)
                .Append(" (").Append(columns.Trim()).Append(") ")
                .Append("VALUES (").Append(string.Join(", ", values)).Append(")");
                
            _state = State.InsertInto;
            return this;
        }

        public ISqlQueryBuilder Update(string table, string columns, params object[] values)
        {
            if (_state != State.Empty)
            {
                SetInvalid(State.Update);
                return this;
            }
            var columnsArray = columns.Split(',');

            if (columnsArray.Length != values.Length)
            {
                _log.Error("Columns and values must have the same length.");
                
                _state = State.Invalid;
                return this;
            }
            CoerceValues(values);

            _builder.Clear().Append("UPDATE ").Append(table).Append(" SET ");
            for (int i = 0; i < columnsArray.Length; i++)
            {
                if (i > 0) _builder.Append(", ");
                _builder.Append(columnsArray[i].Trim()).Append(" = ").Append(values[i]);
            }

            _state = State.Update;
            return this;
        }

        public ISqlQueryBuilder InsertOrUpdate(string table, string columns, params object[] values)
        {
            if (_state != State.Empty)
            {
                SetInvalid(State.InsertOrUpdate);
                return this;
            }

            var columnsArray = columns.Split(',');

            if (columnsArray.Length != values.Length)
            {
                _log.Error("Columns and values must have the same length.");
                _state = State.Invalid;
                return this;
            }
            CoerceValues(values);

            _builder.Clear().Append("INSERT INTO ").Append(table)
                .Append(" (").Append(columns.Trim()).Append(") ")
                .Append("VALUES (").Append(string.Join(", ", values)).Append(") ")
                .Append("ON DUPLICATE KEY UPDATE ");

            for (int i = 0; i < columnsArray.Length; i++)
            {
                if (i > 0) _builder.Append(", ");
                _builder.Append(columnsArray[i].Trim()).Append(" = ").Append(values[i]);
            }

            _state = State.InsertOrUpdate;
            return this;
        }

        private static void CoerceValues(object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is bool b)
                    values[i] = b ? "\'1\'" : "\'0\'";

                else
                    values[i] = $"\'{MySqlHelper.EscapeString(values[i].ToString() ?? "NULL")}\'";
            }
        }


        public ISqlQueryBuilder DeleteFrom(string table)
        {
            if (_state != State.Empty)
            {
                SetInvalid(State.DeleteFrom);
                return this;
            }

            _builder.Clear().Append("DELETE FROM ").Append(table);

            _state = State.DeleteFrom;
            return this;
        }



        private static readonly string[] _conditionalOperators = new string[]
        {
            " = ",    // SqlCondition.Equal = 0
            " != ",   // SqlCondition.NotEqual = 1
            " >= ",   // SqlCondition.Greater = 2
            " <= ",   // SqlCondition.Less = 3
        };

        public ISqlQueryBuilder Where(string column, object value, SqlCondition condition = default)
        {
            if (!(_state == State.Select || _state == State.SelectCount || _state == State.Update || _state == State.DeleteFrom))
            {
                SetInvalid(State.Where);
                return this;
            }

            _builder.Append(" WHERE ").Append(column).Append(_conditionalOperators[(int)condition]).Append('\'').Append(value).Append('\'');

            _state = State.Where;
            return this;
        }
        public ISqlQueryBuilder WhereNot(string column, object value, SqlCondition condition = default)
        {
            if (!(_state == State.Select || _state == State.SelectCount || _state == State.Update || _state == State.DeleteFrom))
            {
                SetInvalid(State.WhereNot);
                return this;
            }

            _builder.Append(" WHERE NOT ").Append(column).Append(_conditionalOperators[(int)condition]).Append('\'').Append(value).Append('\'');

            _state = State.WhereNot;
            return this;
        }


        public ISqlQueryBuilder And(string column, object value, SqlCondition condition = default)
        {
            if (_state < State.Where || _state >= State.Limit)
            {
                SetInvalid(State.And);
                return this;
            }

            _builder.Append(" AND ").Append(column).Append(_conditionalOperators[(int)condition]).Append('\'').Append(value).Append('\'');

            _state = State.And;
            return this;
        }

        public ISqlQueryBuilder AndNot(string column, object value, SqlCondition condition = default)
        {
            if (_state < State.Where || _state >= State.Limit)
            {
                SetInvalid(State.And);
                return this;
            }

            _builder.Append(" AND NOT ").Append(column).Append(_conditionalOperators[(int)condition]).Append('\'').Append(value).Append('\'');

            _state = State.And;
            return this;
        }

        public ISqlQueryBuilder Or(string column, object value, SqlCondition condition = default)
        {
            if (_state < State.Where || _state >= State.Limit)
            {
                SetInvalid(State.Or);
                return this;
            }

            _builder.Append(" OR ").Append(column).Append(_conditionalOperators[(int)condition]).Append('\'').Append(value).Append('\'');

            _state = State.Or;
            return this;
        }
        public ISqlQueryBuilder OrNot(string column, object value, SqlCondition condition = default)
        {
            if (_state < State.Where || _state >= State.Limit)
            {
                SetInvalid(State.Or);
                return this;
            }

            _builder.Append(" OR NOT ").Append(column).Append(_conditionalOperators[(int)condition]).Append('\'').Append(value).Append('\'');

            _state = State.Or;
            return this;
        }

        public ISqlQueryBuilder Limit(int limit)
        {

            if (_state == State.Empty || _state == State.Limit)
            {
                SetInvalid(State.Limit);
                return this;
            }

            _builder.Append(" LIMIT ").Append(limit);

            _state = State.Limit;
            return this;
        }
        
        
    }
}