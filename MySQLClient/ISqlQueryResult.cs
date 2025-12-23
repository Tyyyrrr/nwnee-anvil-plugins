using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MySQLClient
{
  /// <summary>
  /// There are two types of result: "query" and "non-query".
  /// To check the type use <see cref="HasData"/> property. (true for query, false for non-query)
  /// </summary>
  /// <remarks><list type="bullet">
  /// <item>"Query" result can be enumerated ONCE with foreach, it also MUST be disposed when you're done with processing the data.</item>
  /// <item>"Non-Query" does not have to be disposed. Enumerating it will throw an exception.
  /// All it has is <see cref="Rows"/> property, either -1 indicating error, or non-negative integer having rows affected (or count for SELECT COUNT)</item>
  /// </list>
  /// </remarks>
  public interface ISqlQueryResult : IEnumerable<ISqlRowData>, IDisposable
  {
    /// <summary>
    /// A "non-query" result with <see cref="Rows"/> == -1
    /// </summary>
    public static readonly ISqlQueryResult NoResult = MySQLQueryResult.FromInt(-1);

    /// <summary>
    /// A "non-query" result with <see cref="Rows"/> == 0
    /// </summary>
    public static readonly ISqlQueryResult Zero = MySQLQueryResult.FromInt(0);

    /// <summary>
    /// Number of rows affected by the query. -1 if HasData or an error occurred
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// Indicates if there are any rows with data to iterate.
    /// </summary>
    public bool HasData { get; }
  }

  public interface ISqlRowData
  {
    /// <typeparam name="T">Type of value in the column</typeparam>
    /// <param name="column">Index of the column</param>
    /// <returns>A value of the column at specified index, null if index is out of range or value is not a type of <see cref="T"/></returns>
    public T? Get<T>(int column);

    /// <inheritdoc cref="Get"/>
    /// <param name="value">A value of the column at specified index, null if index is out of range or value is not a type of <see cref="T"/></param>
    /// <returns>True if the index is in range and value is a type of <see cref="T"/></returns>
    public bool TryGet<T>(int column, [NotNullWhen(true)] out T? value);
  }
}