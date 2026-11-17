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
- `ClientInfo.GetRequestDetails(HttpContext)` → `RequestDetails { OsName, OsVersion, BrowserName, BrowserVersion, EngineName, EngineVersion,
  DeviceType, Manufacture, Model, IP, IsMobile, IsTablet, IsBot }` parsed from `User-Agent` + client hints; returns `null` only for a null context.
- `ClientInfo.ParseUserAgent(userAgent[, headers])` — same parsing without an `HttpContext` (IP is `null`); never returns `null`.
- `GetRequestOs`, `GetRequestBrowser`, `GetRequestDeviceModel` use the same parser (old signatures). `GetRequestOsVersion` /
  `GetRequestBrowserVersion` (read the version after a token) and `GetRequestDeviceManufacture(os)` are unchanged legacy helpers.

### User-Agent parsing (since 10.0.14)
Implementation: `HttpRequestTools/UserAgentParser.cs` (internal). Ordered token rules, ordinal `IndexOf` + span slicing; no Regex,
no exceptions, ~1.7 µs and ~270 B per call. Only the first 1024 chars of the User-Agent are scanned; names/versions are clipped.
Rule order matters (specific tokens first: `Edg/`, `OPR/`, `SamsungBrowser/` before `Chrome/`; `Windows Phone` before `Android`;
`iPhone` before `Mac OS X`); add new rules next to their family and a test case in `tests/Bat.Regression.Tests/ClientInfoTests.cs`.

| Field | Values |
|---|---|
| `OsName` | `Windows 95`…`Windows 11`, `Windows NT`, `Windows Phone`, `Windows Mobile`, `Mac OS`, `IOS` (iPhone/iPad/iPod), `watchOS`, `tvOS`, `Android`, `HarmonyOS`, `Fire OS`, `Chrome OS`, `Linux`, `Ubuntu`/`Fedora`/`Debian`/…, `FreeBSD`/`OpenBSD`/`NetBSD`, `Solaris`, `Black Berry`, `Symbian`, `KaiOS`, `Tizen`, `webOS`, `Sailfish OS`, `Firefox OS`, `Chromecast`, `Roku OS`, `Xbox`, `PlayStation`, `Nintendo`, `Postman`, `Unknown` |
| `OsVersion` | `14`, `17.4.1`, `10.15.7`, … or empty. Windows desktop versions are in the name (empty version). |
| `BrowserName` | `Chrome`, `Edge`, `Firefox`, `Safari`, `Opera`, `Opera Mini`, `Internet Explorer`, `SamsungBrowser`, `Yandex`, `UCBrowser`, `Brave`, `Chrome WebView`, `iOS WebView`, `Android Browser`, in-app (`Instagram`, `Facebook`, …), bots (`Googlebot`, …), tools (`curl`, `Postman`, …), `Unknown` (version `0.0`) |
| `EngineName` | `Blink`, `WebKit`, `Gecko`, `Trident`, `EdgeHTML`, `Presto`, `Goanna`, `KHTML`, `Unknown` |
| `DeviceType` | `Desktop`, `Mobile`, `Tablet`, `TV`, `Console`, `Wearable`, `Bot`, `Unknown` |
| `Manufacture` / `Model` | `Apple`/`iPhone`, `Samsung`/`SM-S911B`, `Xiaomi`/`2201123C`, `Huawei`/`ANA-AN00`, … ; `Unknown`/empty when not sent (desktops, Chrome's reduced `Android 10; K`) |

Client hints (`Sec-CH-UA*`, Chromium browsers, HTTPS only) refine the result in `GetRequestDetails(HttpContext)` /
`ParseUserAgent(ua, headers)`: Windows 11 (`Sec-CH-UA-Platform-Version` ≥ 13), real macOS/Android versions, `Sec-CH-UA-Model`,
full browser version (`Sec-CH-UA-Full-Version-List`), Brave (`Sec-CH-UA` brand). The low-entropy hints are sent by default; for the
others the app must send `Accept-CH`, e.g. `app.Use((ctx, next) => { ctx.Response.Headers["Accept-CH"] = ClientInfo.AcceptClientHints; return next(); });`
(browsers then send them from the next request on).

Limits (not fixable from the server): iPadOS 13+ Safari sends a Mac User-Agent (reported as `Mac OS` / `Desktop`); Safari and
Firefox freeze macOS at 10.15; without client hints Windows 11 is `Windows 10` and Chrome on Android hides the model (`K`) and
reports Android 10; Brave, Arc and Vivaldi (desktop) look like Chrome unless client hints are present.
- `ClientInfo.GetIP(IHttpContextAccessor)` — falls back to the machine's DNS addresses when the accessor is null.

## Pitfalls
- Polly / Microsoft.Extensions.Http.Polly are package references but not used by the code (no retries are performed).
- `Microsoft.AspNetCore.Http.Abstractions 2.3.x` is referenced for `HttpContext`; ASP.NET Core apps use the shared framework version.
