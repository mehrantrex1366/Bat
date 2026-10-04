# Bat.Http

`src/AspNetCore/Bat.Http` · namespace `Bat.Http` · depends on Bat.Core.

## HttpRequestTools (static)
Convenience wrappers for outgoing HTTP calls. Every operation has two result shapes:
- `Task<T>` — body deserialized with `DeSerializeJson<T>()` (Bat JSON conventions; empty body → `default`);
- `Task<(HttpStatusCode httpStatusCode, string response)>` — status + raw body.

Methods (each in many overloads): `GetAsync`, `PostAsync`, `PostFormAsync` (multipart text fields), `PostFormFileAsync`
(multipart file field named `file`), `PutAsync`, `PutFormAsync`, `DeleteAsync`, plus `IsAjaxRequest(HttpRequest)`.

Overload families:
| Shape | Client used |
|---|---|
| `(string url, …)` | shared internal client |
| `(…, bool byPassServerSertificate, int timeOutSecond, …)` | shared client; the "bypass" client accepts any TLS certificate |
| `(HttpClient httpClient, string url, …)` | the caller's client (recommended with `IHttpClientFactory`) |
| `(HttpClient httpClient, …, int timeOutSecond, …)` | the caller's client with a per-request timeout |

Inputs: query parameters as `Dictionary<string,string>` or as an object + `Type` (public properties, values `ToString()`'d,
URL-escaped); headers as `Dictionary<string,string>` (`TryAddWithoutValidation`); bodies as object (serialized) or
JSON string; `resultEncoding` is actually the **request** content encoding (default UTF-8).

### Implementation notes (since 10.0.1)
- Two process-wide `HttpClient`s on `SocketsHttpHandler` (normal and certificate-bypass), `PooledConnectionLifetime = 2 min`
  (DNS changes are picked up), **cookies disabled** (no state shared between calls), `Timeout = Infinite`.
- Timeouts are per request (`SendAsync` helper: linked `CancellationTokenSource.CancelAfter`). Default for the shared clients is 100 s
  (the old `HttpClient` default). A timeout throws `TaskCanceledException` with an inner `TimeoutException` (same as `HttpClient`).
  On caller-supplied clients the client's own `Timeout` still applies in addition.
- Never set `DefaultRequestHeaders`, `BaseAddress` or `Timeout` on the shared clients; set headers on the `HttpRequestMessage`.
- Requests and responses are disposed; responses are fully buffered before returning.
- `PostFormFileAsync(url, …, byPassServerCertificate = true)` and `PutFormAsync(…, byPassServerSertificate = true)` default
  to **skipping certificate validation** (historical default, kept for compatibility — pass `false`).

## ClientInfo / HttpExtensions
- `HttpContext.GetIP()` (RemoteIpAddress), `GetIPBehindCloud()` (`CF-Connecting-IP`, then `True-Client-IP`, then remote IP),
  `GetDeviceLog()` → `DeviceLog { IsMobile, IP, Os (≤25), Device (≤50), Application (≤50) }`.
- `ClientInfo.GetRequestDetails(HttpContext)` → `RequestDetails { OsName, OsVersion, BrowserName, BrowserVersion,
  Manufacture, Model, IP, IsMobile }` parsed from `User-Agent` with simple `Contains` rules; returns `null` only on unexpected errors.
  User agents without `(...)` (probes, curl) return an empty model instead of throwing.
- `ClientInfo.GetIP(IHttpContextAccessor)` — falls back to the machine's DNS addresses when the accessor is null.

## Pitfalls
- Polly / Microsoft.Extensions.Http.Polly are package references but not used by the code (no retries are performed).
- `Microsoft.AspNetCore.Http.Abstractions 2.3.x` is referenced for `HttpContext`; ASP.NET Core apps use the shared framework version.
