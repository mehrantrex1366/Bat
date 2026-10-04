# Bat.AspNetCore

`src/AspNetCore/Bat.AspNetCore` · namespace `Bat.AspNetCore` · `Microsoft.NET.Sdk.Web` · depends on Bat.Core.
Contains an empty `Program.Main` (the Web SDK requires an entry point); it is a class library in practice.

## Authentication (JWT)
- `JwtSettings { SecretKey, Encryptionkey, Issuer, Audience, NotBeforeMinutes, ExpirationMinutes (default 30), RefreshTokenExpirationMinutes }`.
- `services.AddBatJwtConfiguration(JwtSettings[, JwtBearerEvents])` — JwtBearer with signed (HMAC-SHA256) **and encrypted**
  (`A128KW` + `A128CBC-HS256`) tokens, `ClockSkew = 0`, issuer/audience validated only when set, `RequireHttpsMetadata = false`,
  `SaveToken = true`. Overload with `Action<JwtBearerOptions>`. `app.UseBatJwtConfiguration()` = `UseAuthentication()+UseAuthorization()`.
- `IJwtService` / `JwtService`: `CreateToken(SecurityTokenDescriptor | string userData | List<Claim>, JwtSettings)` → `JwtToken
  { Token, TokenType="Bearer", RefreshToken (32 random chars, CSPRNG), ExpireTime (ValidTo + 3:30) }`;
  `GetClaimsPrincipal(token, settings, validateLifetime)` (returns null unless the token is encrypted with A128KW),
  `ReadToken`, `ReadJwtToken`, `GetTokenExpireTime` (Iran time). Uses one shared `JwtSecurityTokenHandler`.
- If `SecretKey`/`Encryptionkey` are empty, **hard-coded fallback keys** are used (see known issues). `Encryptionkey` must be 16 chars.

## Middlewares
| Class | Behavior |
|---|---|
| `BatEnableRequestBufferingMiddleware` | `Request.EnableBuffering()` (needed by `ReadRequestBody`). |
| `BatExceptionHandlingMiddleware` | Catches pipeline exceptions, logs (with request body if buffered), returns `{isSuccessful:false, resultCode, message}` (Persian): `DomainException`/`ServiceException` → 400, others → 500. Does nothing if the response already started. |
| `BatJwtParserMiddleware` | If an `Authorization` header exists, validates it with `IJwtService` + `IOptions<JwtSettings>` and sets `HttpContext.User`. Invalid/expired token → 401 JSON (`resultCode` 1001 expired / 1002 other) and the pipeline stops. No token but an already-authenticated user (e.g. cookie) → 401 "Token Not Sent". Exceptions thrown later in the pipeline are **not** handled here. |

Register with `app.UseMiddleware<T>()`. Typical order: buffering → exception handling → JWT parser → routing/auth.

## Authorization
- `[AuthorizationFilter]` (`IAuthorizationFilter`): authenticated user required (else 403); actions with `[AllowAnonymous]`
  (method-level) skip the check; otherwise `IUserActionProvider.GetUserActions(userId)` (synchronous!) must contain the
  controller/action (case-insensitive) — or the pair given by `[AuthEqualTo("Controller","Action")]` — else 401.
  User id = `ClaimTypes.NameIdentifier` (missing → 401). Attribute lookups are cached per action method.
- `IUserActionProvider` (Bat.Core) is implemented by the consuming app and should cache per user.

## Filters
- `BatModelValidationAttribute` — invalid `ModelState` → 400 with `Response<object>` containing joined errors.
- `BatExceptionFilter` (`IExceptionFilter`, MVC) — logs with `FileLoger` and redirects to `CustomSettings:ErrorUrl` or `/Error/Details?code=`.
- `BatExceptionLogingFilter` — logs only.
- Swagger: `BatSwaggerAuthenticateFilter` (adds `Token` header param for `*Authenticate*` attributes), `BatSwaggerAuthorizeFilter`
  (Bearer security + 401/403 for `[Authorize]`), `BatSwaggerExcludeFilter` (schema props with `[SwaggerExclude]`),
  `BatSwaggerExcludeOperationFilter` (document filter), `BatSwaggerFileFilter` (`FileUploadModel` params).

## Extensions
- `AddBatSwagger(SwaggerSetting | Action<SwaggerGenOptions> | OpenApiInfo, name)`, `UseBatSwaggerConfiguration(...)` (route prefix `help`).
- `AddBatCors(policyName, CorsPolicy | domains/headers/methods lists)`, `UseBatCors(policyName | lists)`; null list = allow any.
- `AddBatAuthentication(...)` (cookie auth), `AddBatHttpContextAccessor()`.
- `HttpRequest.ReadRequestBody()` (needs buffering; returns `""` otherwise), `FillWithHttpRequest(obj, request)` (form for POST,
  query otherwise; converts to the property type, skips invalid values).
- `ISession.Set/Get(<T>)` (JSON), `ModelStateDictionary.GetModelError()` (`|`-joined), enum → `SelectListItem` lists.
- `Controller.RenderViewToString(Async)`, `IViewRenderService` / `ViewRenderService` (render Razor view to string).
- `IFormFile.ToByteArray/ToBase64/Save(fullPath)`, `byte[].SaveFile(fullPath)`.

## Files
`HttpFileOperation`: `GetPath(...)` (builds `wwwroot/<root>/<yyyy>/<M>/<d>/<id>/<name>_<ticks>.<ext>` with Persian date parts and creates the folder),
`Save(byte[] | IFormFile, fullPath)`, `SaveWithPath`, `Delete`, `SaveLargeFile(LargeFileSaveModel)` (streams multipart sections
to disk; use with `[DisableModelBinding]`). `MultipartRequestHelper` for boundaries. Paths are OS-independent.

## Tag helpers (Razor)
`custom-input(-for)`, `custom-select(-for)`, `custom-textarea(-for)`, `custom-checkbox(-for)`, and by convention
`custom-button`, `single-uploader`, `multi-uploader` (Dropzone markup). Common attributes from `FormGroupModel`: `wrapper-class`,
`label-visibility`, `label-class`, `Class` (default `form-control`), `Readonly`; extra attributes via `input-*` / `select-*` prefixes.
