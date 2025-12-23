namespace MySQLClient
{
  /// <summary>A state machine which uses underlying string builder to programatically prepare queries to MySQL database.</summary>
  /// <remarks>It's strongly recommended to use the builder instead of passing raw queries to <see cref="MySQLService.ExecuteQuery(string)"/>, since the builder performs all necessary validation of the input.</remarks>
  public interface ISqlQueryBuilder
  {
    /// <summary> Starts the query with "SELECT ({<see cref="columns"/>}) FROM {<see cref="table"/>}"</summary>
    /// <param name="table">Name of the table in database</param>
    /// <param name="columns">Valid syntax: "column1, column2, ..." (whitespaces are ignored) </param>
    /// <remarks>Valid only for an empty <see cref="ISqlQueryBuilder"/></remarks>
    public ISqlQueryBuilder Select(string table, string columns);

    /// <summary> Starts the query with "SELECT COUNT (*) FROM {<see cref="table"/>}"</summary>
    /// <inheritdoc cref="Select"/>
    public ISqlQueryBuilder SelectCount(string table);

    /// <summary>Builds the "INSERT INTO {<see cref="table"/>} ({<see cref="columns"/>}) VALUES ({<see cref="values"/>}[0], {<see cref="values"/>}[1], [...])" query</summary>
    /// <inheritdoc cref="Select"/>
    public ISqlQueryBuilder InsertInto(string table, string columns, params object[] values);

    /// <summary>Builds the "UPDATE {<see cref="table"/>} SET {<see cref="columns"/>}[0] = {<see cref="values"/>}[0], {<see cref="columns"/>}[1] = {<see cref="values"/>}[1], [...] " query</summary>
    /// <inheritdoc cref="Select"/>
    public ISqlQueryBuilder Update(string table, string columns, params object[] values);

    /// <summary>Builds the "INSERT INTO table (columns) VALUES (values[0], values[1], ...) ON DUPLICATE KEY UPDATE column[0] = values[0], column[1] = values[1], ..." query</summary>
    /// <inheritdoc cref="Select"/>
    public ISqlQueryBuilder InsertOrUpdate(string table, string columns, params object[] values);

    /// <summary>Starts the query with "DELETE FROM {<see cref="table"/>}"</summary>
    /// <inheritdoc cref="Select"/>
    public ISqlQueryBuilder DeleteFrom(string table);

    /// <summary>Appends " WHERE {<see cref="column"/>} {<see cref="condition"/>} {<see cref="value"/>}" to the builder. For info about operators see <see cref="SqlCondition"/> <paramref name="condition"/></summary>
    /// <param name="column">A single string with a database column name</param>
    /// <param name="value">A value to compare</param>
    /// <param name="condition">Comparison operator: Equal (default), Greater, Less or NotEqual</param>
    /// <remarks>Valid only after: <list type="bullet"><item><see cref="Select"/></item><item><see cref="SelectCount"/></item><item><see cref="DeleteFrom"/></item></list></remarks>
    public ISqlQueryBuilder Where(string column, object value, SqlCondition condition = default);

    /// <summary>Appends " WHERE NOT {<see cref="column"/>} {<see cref="condition"/>} {<see cref="value"/>}" to the builder. For info about operators see <see cref="SqlCondition"/> <paramref name="condition"/></summary>
    /// <inheritdoc cref="Where"/>
    public ISqlQueryBuilder WhereNot(string column, object value, SqlCondition condition = default);

    /// <summary>Appends " AND {<see cref="column"/>} {<see cref="condition"/>} {<see cref="value"/>}" to the builder. For info about operators see <see cref="SqlCondition"/> <paramref name="condition"/></summary>
    /// <remarks>Valid only after: <list type="bullet"><item><see cref="Where"/></item><item><see cref="WhereNot"/></item><item><see cref="And"/></item><item><see cref="AndNot"/></item><item><see cref="Or"/></item><item><see cref="OrNot"/></item></list></remarks>
    /// <inheritdoc cref="Where"/>
    public ISqlQueryBuilder And(string column, object value, SqlCondition condition = default);

    /// <summary>Appends " AND NOT {<see cref="column"/>} {<see cref="condition"/>} {<see cref="value"/>}" to the builder. For info about operators see <see cref="SqlCondition"/> <paramref name="condition"/></summary>
    /// <inheritdoc cref="And"/>
    public ISqlQueryBuilder AndNot(string column, object value, SqlCondition condition = default);

    /// <summary>Appends " OR {<see cref="column"/>} {<see cref="condition"/>} {<see cref="value"/>}" to the builder. For info about operators see <see cref="SqlCondition"/> <paramref name="condition"/></summary>
    /// <inheritdoc cref="And"/>
    public ISqlQueryBuilder Or(string column, object value, SqlCondition condition = default);

    /// <summary>Appends " OR NOT {<see cref="column"/>} {<see cref="condition"/>} {<see cref="value"/>}" to the builder. For info about operators see <see cref="SqlCondition"/> <paramref name="condition"/></summary>
    /// <inheritdoc cref="And"/>
    public ISqlQueryBuilder OrNot(string column, object value, SqlCondition condition = default);

    /// <summary> Appends " LIMIT {<see cref="limit"/>}" to the end of the query.</summary>
    /// <param name="limit">Any positive integer</param>
    /// <remarks>Valid only at the end of: <list type="bullet"><item><see cref="Select"/></item><item><see cref="SelectCount"/></item><item><see cref="DeleteFrom"/></item></list> and as a last statement after Where/And/Or (Not) conditions</remarks>
    public ISqlQueryBuilder Limit(int limit);

    /// <returns>Constructed MySQL query string for caching and reusing with <see cref="MySQLService.ExecuteQuery(string)"/></returns>
    public string Build();

    /// <summary>Clears the underlying <see cref="System.Text.StringBuilder"/> and resets the state of <see cref="ISqlQueryBuilder"/> to default (empty)</summary>
    public void Reset();
  }
}