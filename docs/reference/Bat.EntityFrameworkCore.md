# Bat.EntityFrameworkCore

`src/DataAccess/Bat.EntityFrameworkCore` · namespace `Bat.EntityFrameworkCore` · EF Core 10 + SqlServer · depends on Bat.Core.
Global usings include `System.Reflection` (so `PropertyInfo` is ambiguous with `Bat.Core.PropertyInfo` — fully qualify).

## BatDbContext (abstract, : DbContext, IBatDbContext)
Override of `SaveChanges()` / `SaveChangesAsync()` runs, in order:
1. `ApplyPersianYK()` — for **every tracked entry** (any state), every public read/write `string` property:
   `ToPersianCharacters()` (Arabic ي/ك → Persian, **Trim**, whitespace-only → `""`).
2. `ApplyEnglishNumber()` — same entries/properties: Persian/Arabic digits → ASCII.
   (String property lists are cached per entity type.)
3. `BasePropertiesInitializer()` for entries implementing `IBaseProperties`:
   - Added: `InsertDate*`, `InsertTime`, `ModifyDate*`, `ModifyTime` = `DateTime.Now`; `*Sh` = `DateTime.Now.ToPersianDate()`.
   - Modified: `ModifyDate*`/`ModifyTime` = `DateTime.Now`; `ModifyDateSh` = `DateTime.Now.ToPersianDate()`.
   - All values come from the **server's local clock** (`DateTime.Now`); on Kubernetes nodes running in UTC the `*Sh`
     strings are UTC-based, not Iran time. Set the container time zone (`TZ=Asia/Tehran`) if Iran time is required.
   - Deleted + `ISoftDeleteProperty`: `IsDeleted = true`, state → Modified (soft delete).
   One timestamp is used for the whole SaveChanges call.

`BatSaveChanges(Async)()` → `SaveChangeResult { IsSuccess, Result, ResultType, Message, Exception, ValidationErrors }`, mapping
exceptions to `SaveChangeResultType` (`DuplicateIndexKeyException` detected from "cannot insert duplicate key", validation,
concurrency, update, unknown) with localized messages from `Strings.resx`.
`BatSaveChangesWithValidation(Async)()` first runs DataAnnotations validation on Added/Modified entities (`ValidateContext()`).
`PartialUpdate(entity, props…)` detaches then marks the given properties modified.

`IBatUnitOfWork` / `IBatUnitOfWork<TContext>` are interfaces only (implemented by consumers).

## Repository
`EFGenericRepo<TEntity>(DbContext)` (`TEntity : class, IBaseEntity`; does **not** implement `IEFGenericRepo<T>` itself —
consumers register/derive). Public field `_dbSet`.
- Commands: `Add(Async)`, `AddRange(Async)`, `Update`, `UpdateRange`, `PartialUpdate(entity, names | expressions)`,
  `UpdateRangeAsync(setters)` (`ExecuteUpdateAsync`), `Delete`, `DeleteUnAttached`, `DeleteRange(entities | predicate)` (`ExecuteDeleteAsync`).
- Queries take `QueryFilter<T> { AsNoTracking = true, Conditions, IncludeProperties, ThenIncludeProperties, OrderBy, PagingParameter, CancellationToken }`
  or `QueryFilterWithSelector<T, TResult>` (+ `Selector`): `FindAsync`, `AnyAsync`, `CountAsync`, `LongCountAsync`,
  `FirstOrDefaultAsync`, `GetAsync`, `GetPagingAsync`, `ExecuteQueryAsync(sql, ct, params)` (`FromSqlRaw`).
- `RepositoryExtensions` add LINQ-style helpers directly on `EFGenericRepo<T>` (`Where`, `Select`, `Include`, `ToListAsync`, paging…).
- `IRepositoryFactory` / `RepositoryFactory` resolve `IEFGenericRepo<T>` from `IServiceProvider` and cast to `EFGenericRepo<T>`.

## Extensions
- `DbContextExtensions`: `ExecuteProcedure<TResult>(sql, params)` (keyless `BatProcedureDbContext<TResult>` with the same
  connection string), `ExecuteCommandAsync`, `ExecuteQuery(ListAsync)`, `ContainsEntity<T>`, `GetChangedEntity(state?)`,
  `GetAddedEntity`, `GetUpdatedEntity`, `GetDeletedEntity`, `GetAddOrUpdateEntity`, `BasePropertiesInitializer`,
  `ValidateContext` (first error per entity type), `SaveAuditLog<TAudit>(userId)` (adds one `IAuditLogProperties` row per
  Added/Modified/Deleted entry; Modified uses `GetDatabaseValues()` = one synchronous query per entry), `GetConnectionString`, `GetSqlConnection`.
- `DbSetExtensions`: `GetDbContext`, `PartialUpdate`, `SerializeDbSetToJson` (hand-written JSON of value-type props, no escaping), `ExecuteQuery*`.
- `OrderByExtensions`: `IQueryable<T>.OrderBy(string)` accepts `"Name"`, `"Name desc, Id"`, nested `"Address.City"`
  (case-insensitive); `OrderByDescending(string propertyName)` (single exact property).
- `PagingExtensions.ToPagingListDetailsAsync(query, paging[, orderBy])` — `CountAsync` + `Skip/Take`.
- `PublicExtensions`: `ChangeIsActiveStatus`, `ChangeIsDeletedStatus`, `IsActiveFilter`, `ToSaveChangeResult(Message)`.

## Pitfalls
- The SaveChanges pipeline touches every string property of every tracked entity — keep contexts short-lived and use
  `AsNoTracking` for reads (the default in `QueryFilter`).
- `ApplyPersianYK` trims strings and turns whitespace-only strings into `""` — intentional, existing behavior.
