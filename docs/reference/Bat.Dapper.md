# Bat.Dapper

`src/DataAccess/Bat.Dapper` · namespace `Bat.Dapper` · depends on Bat.Core, Dapper, Microsoft.Data.SqlClient.

- `DapperExtension` (on `SqlConnection`): `ExecuteQuerySingleAsync<T>`, `ExecuteQueryAsync<T>`, `ExecuteQueryCommandAsync` (→ affected > 0),
  `ExecuteProcedure<T>` / `ExecuteProcedureAsync<T>` (first row), `ExecuteProcedureList(Async)<T>`, `ExecuteProcedureCommandAsync`,
  `ExecuteQueryMultipleAsync` (`GridReader`), `ExecuteQueryWithNavigation<T1,T2,TResult>` (multi-mapping, **unbuffered**, `splitOn = "Id"`).
  All accept `parameters`, `transaction`, `commandTimOut`.
- `ParameterExtension.ToTableValuedParameter(List<T> | T, typeName[, columnNames])` → Dapper TVP from public read/write
  properties (excluding `[NotMapped]`, `[ForeignKey]`, collections). Nullable properties → underlying column type, nulls → `DBNull`.
  Enums become `int` columns. Column order = property declaration order; it must match the SQL table type. Metadata is cached per type.
- `TableValueParameter(DataTable)` — custom TVP using `DataTable.TableName` as the SQL type name.
- `DapperGenericRepo<T>(connectionString) : IDapperGenericRepo<T>` — holds one `SqlConnection` (Dapper opens/closes it per call;
  not thread-safe — one instance per scope). `GetPaging` reads the **whole** result and pages in memory.
