# Conventions and rules

## Compatibility rules
- **Public API is frozen** within a major version: no removals, signature or return-type changes.
  Add overloads/new members. A rename is allowed only if the old name stays as an `[Obsolete("Use X.")]` alias that forwards to the new one
  (for settings classes the old property must still bind from configuration).
- **Data compatibility:** encryption output, hashing output, JSON shape, Redis value format, audit-log JSON format and
  file/folder naming of uploads must not change. If a bug forces a change, record it under "Behavior changes" in `CHANGELOG.md`.
- Existing misspelled public names are kept on purpose (`FileLoger`, `PagingExtention`, `Extentions` folders,
  `byPassServerSertificate`, `commandTimOut`, `ToCurrectSize`, ...). Renamed in 10.0.1 with obsolete aliases: `CheckExtention` → `CheckExtension`,
  `RegexPattern.IrancellMobileNumber` → `IranCellMobileNumber`, `RedisSslSettings.Host` → `SslHost`.

## Performance rules
These rules exist because Bat runs on the hot path of many services. Violations of the first three were the main
cause of CPU/allocation/loader-heap growth found in production (see `CHANGELOG.md` 10.0.1).

| Don't | Do |
|---|---|
| `new JsonSerializerOptions { ... }` per call (or `SerializationExtension.GetDefaultOption()` per call) | `SerializationExtension.DefaultOptions`, `GetSharedOption(depth)`, or `x.SerializeToJson()` / `json.DeSerializeJson<T>()` |
| Serialize to `string` and then `Encoding.UTF8.GetBytes(...)` | `x.SerializeToJsonUtf8Bytes()` / `bytes.DeSerializeJson<T>()` |
| `new HttpClient()` / `new HttpClientHandler()` per request | `IHttpClientFactory` in apps; inside Bat use the shared clients in `HttpRequestTools` |
| Set `HttpClient.Timeout` on a client that is already in use | Per-request `CancellationTokenSource.CancelAfter` (see `HttpRequestTools.SendAsync`) |
| `new Regex(pattern)` per call | `BatRegex.X` (Bat.Core, internal) or `private static readonly Regex` |
| `type.GetProperties()` / `GetCustomAttributes()` per call/per entity | `ConcurrentDictionary<Type, ...>` cache (see `ObjectExtensions`, `BatDbContext`, `AuthorizationFilter`) |
| `new Random()` per call | `Random.Shared`; `RandomNumberGenerator` for anything security related (tokens) |
| Re-enumerating an `IEnumerable` (`Count()` + `ToList()` + ...) | Materialize once |
| Exceptions for control flow on hot paths (e.g. parsing user agents) | Guard with `IndexOf`/`TryParse` |
| Re-deriving crypto keys per value | Cache derived keys (see `SymmetricCrypto`) |
| Buffering whole uploads in `MemoryStream` | Stream to the destination |
| Opening a RabbitMQ connection/channel per publish | One connection + channel, reused (see `RabbitProducer`) |

## Correctness / safety rules
- No `async void` (except event handlers). Return `Task`.
- Prefer async APIs end-to-end; do not add `.Result` / `.Wait()` / `GetAwaiter().GetResult()` in new code.
- Dispose `IDisposable` (`HttpRequestMessage`, `HttpResponseMessage`, crypto objects, streams, `RSA`).
- `Dispose`/`DisposeAsync`: do the cleanup **before** setting the `_disposed` flag if the cleanup method checks it.
- Middlewares: after writing a response, `return` (don't call `_next`). Check `Response.HasStarted` before setting status/headers.
  Don't wrap `_next(context)` in a catch that rewrites the response unless that is the middleware's purpose.
- Logging: use message templates (`_logger.LogError(ex, "Url: {Url}", url)`), not interpolated strings.
- Use `CultureInfo.InvariantCulture` for machine-readable number/date text.
- File paths: `Path.Combine`, `Path.DirectorySeparatorChar`; never hard-coded `\\`.
- Shared clients/connections must not carry per-caller state (cookies, default headers, base address).
- When an interface (e.g. `IRedisCacheProvider`) may be mocked by consumers, keep a fallback path that only uses its methods.

## Style
- C# latest, `ImplicitUsings` on, nullable reference types **off** (do not enable per file).
- File-scoped namespaces; one flat namespace per package.
- Check `GlobalUsing.cs` of the project before adding `using`s. In Bat.Core, `Bat.Core.PropertyInfo` exists, so
  `System.Reflection.PropertyInfo` must be fully qualified where both are visible.
- Private fields and private static fields `_camelCase` (no `s_` prefix); constants `PascalCase`.
- Keep methods short and overloads consistent with siblings (the libraries use many overloads with the same shape).
- Add a short comment when a line exists to fix a bug or avoid a performance trap.
