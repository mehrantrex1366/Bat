# Migrating a service from Bat 10.0.x to Bat 11

Bat 11.0.0 targets **.NET 11** (`net11.0`) only. A service that references Bat 11 must target `net11.0` too.
Update **all** Bat packages to the same version at once (see [build-test-release.md](build-test-release.md#after-releasing)).

## 1. Project
```xml
<TargetFramework>net11.0</TargetFramework>
```
- Install the .NET 11 SDK; update `global.json` if the service pins an SDK.
- Docker images: `mcr.microsoft.com/dotnet/aspnet:11.0` (runtime) and `mcr.microsoft.com/dotnet/sdk:11.0` (build).
- Windows servers (payment gateway): install the ASP.NET Core 11 Hosting Bundle before deploying.
- Move the service's own Microsoft packages (EF Core, ASP.NET Core, Microsoft.Extensions.*) to `11.0.x` as well: Bat 11 depends on
  `>= 11.0.0`, so mixing 10.0.x packages produces `NU1605` (package downgrade) errors.

## 2. Breaking changes that can need code changes

### Swashbuckle 11 / Microsoft.OpenApi 3 (Bat.AspNetCore)
Bat.AspNetCore now depends on Swashbuckle.AspNetCore 11, which brings Microsoft.OpenApi 3. Bat's own filters were migrated;
**your own** `IOperationFilter` / `ISchemaFilter` / `IDocumentFilter` classes may need small changes (namespace `Microsoft.OpenApi`,
`IOpenApiSchema`, `JsonSchemaType`, references created with `new OpenApiSecuritySchemeReference(id, document)`).
See the Swashbuckle 11 release notes.

If you use `BatSwaggerAuthorizeFilter`, register the security scheme it references:
```csharp
services.AddBatSwagger(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    o.AddSecurityDefinition(BatSwaggerAuthorizeFilter.BearerSchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    o.OperationFilter<BatSwaggerAuthorizeFilter>();
});
```

### EF Core 11 (Bat.EntityFrameworkCore, Bat.EntityFrameworkCore.Tools)
Read Microsoft's "Breaking changes in EF Core 11" page and run your own data-access tests.
`EFCore.BulkExtensions.SqlServer` has no EF Core 11 build yet: Bat 11 keeps 10.0.1. Test bulk insert/update/delete/read
(`EFBulkGenericRepo`) against a real SQL Server before releasing (see [known-issues.md](known-issues.md#dependencies)).
Migrations: regenerate the model snapshot (`dotnet ef migrations add <Name>`) after upgrading the `dotnet-ef` tool to 11.

### Behavior fixes you may notice
- `BatSwaggerExcludeFilter` now really removes `[SwaggerExclude]` properties from the schemas (it removed nothing before).
- `BatSwaggerAuthorizeFilter` no longer makes `swagger.json` fail for `[Authorize]` actions.

## 3. New in Bat 11 (optional — nothing is enabled unless you call it)
All of these are **additions**: Microsoft ApiCompat reports no breaking change against 10.0.14 for any package, and no existing
method changed behavior, so a service compiles and runs as before until it opts in.

| Need | Use |
|---|---|
| Two-level cache (memory + Redis) | `services.AddBatHybridCacheWithRedis("svc:hc:")` → inject `IHybridCacheProvider` |
| Cache hit/miss metrics | OpenTelemetry `.AddMeter(BatCacheMetrics.MeterName)`; `services.AddBatCacheStatistics()` for `dotnet.cache.*` |
| Kubernetes probes | `services.AddBatHealthChecks().AddBatRedisHealthCheck()` + `app.MapBatHealthChecks()` |
| Fail fast on bad settings | `.ValidateBatJwtSettings()`, `.ValidateBatRedisSettings()` on `AddOptions<...>()` |
| Response compression with zstd | `services.AddBatResponseCompression()` + `app.UseBatResponseCompression()` |
| Async validation (DB checks) | `[BatAsyncValidation]` on MVC actions; `obj.ValidateDataAnnotationsAsync()` elsewhere |
| Compress payloads | `bytes.CompressZstd()`, `text.CompressZstdToBase64()` |
| Stream large exports | `items.SerializeToJsonLinesAsync(stream)` / `stream.DeSerializeJsonLinesAsync<T>()` |
| Encrypt **new** sensitive data | `AuthenticatedEncryption.Encrypt(text, key, associatedData)` (key from secret store) |
| Key exchange with a partner/service | `X25519KeyAgreement.GenerateKeyPair()` / `DeriveSharedKeyBase64(...)` |
| Run an external tool | `ProcessTools.RunAsync("tool", ["arg"], TimeSpan.FromSeconds(30))` |

Also: `IHybridCacheProvider.GetOrSetAsync(key, factory, expiration[, ct])` now compiles (was ambiguous); options overload with flags + tags.

## 4. Unchanged (no action)
- Encryption output (`Encryption`, `AesEncryption`, EF encryptor converters, `HashGenerator`) — byte-for-byte identical, pinned by tests.
- JSON defaults of `SerializationExtension`, Redis payloads, JWT format (signed + encrypted, same fallback keys).
- Every public type and member of Bat 10.0.14 still exists with the same signature.

## 5. Checklist
- [ ] `net11.0` + .NET 11 SDK / runtime images / Hosting Bundle.
- [ ] All Bat packages on 11.0.x; Microsoft packages on 11.0.x.
- [ ] Own Swagger filters compile; `swagger.json` opens; Bearer scheme registered if `BatSwaggerAuthorizeFilter` is used.
- [ ] EF Core: tests pass, new migration snapshot generated, bulk operations verified on SQL Server.
- [ ] Smoke test: authentication (JWT), Redis cache/lock, RabbitMQ publish/consume.
