# Bat.EntityFrameworkCore.Tools

`src/DataAccess/Bat.EntityFrameworkCore.Tools` · namespace `Bat.EntityFrameworkCore.Tools` · depends on Bat.Core,
Bat.EntityFrameworkCore, EFCore.BulkExtensions.SqlServer.

- `EFBulkGenericRepo<TEntity>(DbContext) : IEFBulkGenericRepo<TEntity>` (transient): `BulkInsertAsync`, `BulkUpdateAsync`,
  `BulkInsertOrUpdateAsync`, `BulkDeleteAsync`, `BulkReadAsync` (all take `IList<T>`, optional `BulkConfig`).
- `RepositoryFactory` / `BulkRepositoryFactory` (`IDisposable`, `IAsyncDisposable`) resolve repos from DI. Dispose is a no-op
  (10.0.0 recursed infinitely → `StackOverflowException`).
- `RepositoryExtension`: `IServiceProvider.GetRepository<T>()`, `GetBulkRepository<T>()`.
- `ModelBuilderExtension`: `OverrideMaxLength(type, len)`, `OverrideDeleteBehavior(behavior = Restrict)`,
  `OverrideSqlDefaultValue(type, sql)`, `OverrideGuidToSequentialGuid()`, `RegisterAllEntities<TBase>(assemblies)`,
  `RegisterEntityTypeConfiguration(assemblies)`.
- Value converters: `StringEncryptorConverter`, `IntEncryptorConverter`, `GuidEncryptorConverter` (store values encrypted
  with `AesEncryption` default key — **never change that output**), `IntToStringConverter`, `ObjectToJsonConverter` (`object` ↔ JSON, reads as `JsonElement`).
- `GroupByExtension`: `GroupByMany`, `GroupByWithRollup`, `SubTotal`, `GrandTotal` (in-memory LINQ helpers).
- `DbContextFactory.GetInstance<TDbContext>([connectionString])` — returns **one process-wide context per (type, connection string)**.
  DbContext is not thread-safe; do not use this from concurrent code (prefer DI / `IDbContextFactory`). The parameterless
  overload uses `_connectionString`, which is never set.
