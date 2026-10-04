# Bat.Core

`src/Core/Bat.Core` · namespace `Bat.Core` · no Bat dependencies · **every other package depends on it**.

Global usings (`GlobalUsing.cs`): `System`, `System.Linq`, `System.Text.Json`, `System.ComponentModel`,
`System.Collections.Generic`, `System.Text.Json.Serialization`, `System.Text.RegularExpressions`,
`System.ComponentModel.DataAnnotations`, `System.ComponentModel.DataAnnotations.Schema`.
⚠ `Bat.Core.PropertyInfo` (a DTO) clashes with `System.Reflection.PropertyInfo` — fully qualify the reflection one.

## Folder map

| Folder | Contents |
|---|---|
| `Serialization/` | `SerializationExtension` (all JSON), `BatStringToBoolConverter`, `BatNumberToStringConverter`, `DateTimeConverterForSerializer` |
| `Extensions/` | String, number, enum, reflection, object-copy, logic, validator, mobile, Persian chars/digits, DateTime, claims, expression, menu extensions |
| `DateTime/` | `PersianDateTime`, `PersianDateTimeFormat`, `PersianDateTimeMode`, `TimeFormat`, `TimeSpanExtension` |
| `Security/` | `Encryption` (Rijndael), `AesEncryption`, `AesAlgorithm`, `RijndaelAlgorithm`, internal `SymmetricCrypto`, `HashGenerator`, `RsaEncryptor`, enums |
| `Regex/` | `RegexPattern` (public const patterns), internal `BatRegex` (shared compiled instances) |
| `ValidationAttribute/` | DataAnnotations attributes: `Email`, `Mobile`, `Ip`, `Url`, `PersianDate`, `NationalCode`, `BiggerThanZero`, `Int/Long/Byte/Float/Double` |
| `DataModel/` | Entity & DI marker interfaces, `MenuModel`, `PropertyInfo` DTO |
| `Pagination/` | `PagingParameter`, `PagingDetails`, `PagingList<T>`, `PagingListDetails<T>`, `PagingExtention` |
| `Pattern/` | `Response`, `Response<T>`, `IResponse`, `IBaseResponse` |
| `Authorization/` | `ICurrentUserPrincipal`/`CurrentUserPrincipal`, `IUserActionProvider`, `UserAction` |
| `Cache/` | `IMemoryCacheProvider` (implemented in Bat.Cache) |
| `File/` | `FileOperation` (save/delete/exists/CreateDirectory/extension checks), `FileConvertor`, `FileType`, `SizeType` |
| `Log/` | `FileLoger` (file logging), `ExceptionBusiness`, `ExceptionDetails` |
| `Math/` | `Randomizer` |
| `Exception/` | `ApiException`, `DomainException`, `ServiceException` (used by Bat.AspNetCore exception middleware) |
| `Attribute/` | `SwaggerExcludeAttribute` (used by Bat.AspNetCore Swagger filters) |

## Serialization

`SerializationExtension` (static, extension methods):

| Member | Notes |
|---|---|
| `DefaultOptions` | Shared, **read-only** options. Use when you call `JsonSerializer` directly. Mutating throws `InvalidOperationException`. |
| `GetSharedOption(int? depth)` | Shared read-only options with `MaxDepth` (cached per depth). |
| `GetDefaultOption(int? depth)` | Returns a **new mutable** instance each call (kept for compatibility). Don't call it per request. |
| `SerializeToJson(obj[, depth \| options])` | `null` → `""`. |
| `SerializeToJsonUtf8Bytes(obj)` | UTF-8 bytes, `null` → empty array. Same bytes as `UTF8(SerializeToJson(obj))`. |
| `DeSerializeJson<T>(string[, depth \| options])` | null/whitespace → `default`. |
| `DeSerializeJson<T>(byte[])` | UTF-8 input; null/empty/whitespace → `default`. |
| `DeSerializeJson(string)` | → `JsonElement`. `DeSerializeJsonToJsonElement(string, depth \| options)`. |
| `DeSerializeJsonToDynamic(string[, depth \| options])` | Returns `JsonElement` boxed as `dynamic`. |
| `AddJsonArrayRoot(string)` | Wraps in `[...]`. |

Default options (the contract — do not change): `IncludeFields`, `AllowTrailingCommas`, `PropertyNameCaseInsensitive`,
`ReferenceHandler.IgnoreCycles`, `JsonNamingPolicy.CamelCase`, `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`,
`JsonNumberHandling.AllowReadingFromString`, converters `BatStringToBoolConverter` (bool from `true`/`false`/`"true"`)
and `BatNumberToStringConverter` (string properties accept JSON numbers; formatted with invariant culture).

Why shared options matter: an options instance owns System.Text.Json's per-type metadata cache. A new instance per
call (the 10.0.0 behavior) rebuilt metadata and emitted new IL accessors on every call — ~40× slower and ~30× more
allocations in a micro-benchmark, plus steady growth of JIT/loader memory in services.

## Persian / Iran helpers

- `PersianDateTime`: wraps a `DateTime`; `Year/Month/Day/...`, `DayName`, `MonthName`, `AddDays/Months/Years`,
  `FirstDayOfMonth/Week/Year`, operators, `ToString()` (= `"yyyy/MM/dd HH:mm:ss"`), `ToString(PersianDateTimeFormat)`,
  `ToString(string format)` (custom tokens `yyyy MM M dd d HH hh mm ss tt dddd MMMM`), `ToInt()` (`yyyyMMdd`),
  `Parse(DateTime)`, `Parse("1403/01/15")`, `Parse(date, "12:30")`, `GetRelativeTime(DateTime)` ("۵ دقیقه قبل" style),
  `GetMonthName(1..12)`, `GetDaysInMonth`, `IsLeapYear`.
  - `PersianDateTime.Now` = **Iran time** computed as `UtcNow + 03:30` (Iran has no DST since 2022). Not affected by the server time zone.
  - `PersianDateTimeFormat.Date` → `"1403/01/15"`; `DateTime` (default for `ToString()`) adds `" HH:mm:ss"`.
- `DateTimeExtensions` (C# 14 `extension` blocks) on `DateTime`, `DateTime?` and `string` (Persian date → `DateTime`):
  `ToPersianDateTime`, `ToPersianDate` (date **and** time), `ToTime` (`HH:mm`), `ToFullTime`, `ToDateOnly`, `ToTimeOnly`,
  `IsFuture/IsPast`, `"1403/01/15".ToDateTime(hour, minute[, second])`. Uses the given value; no time-zone conversion.
- `PersianCharactersExtensions.ToPersianCharacters()` — Arabic `ي/ك` → Persian `ی/ک` and **Trim**; null/whitespace → `""`.
  `ToPersianCharacters2()` — more variants + ZWNJ/NBSP → space. Both also exist for `DbCommand` (text + string parameters).
- `EnglishNumberExtensions.ToEnglishNumber()` — Persian/Arabic digits → ASCII (returns the same instance if none);
  overload with `NumericCultureType` converts between Persian/Arabic/English; `ToEnglishNumber2`; `DbCommand` overloads.
- `NumberExtensions`: `ToText()` (number to Persian words), `GetNumberString`, `ToNormalNumber` (e.g. "۱۲ میلیون ریال"),
  `ToNumeric` (`N0`), `ToCurrency` (`C0`, **current culture**), `To3DigitSplited`, `ToPlainNumber`, `ToSizeUnit`.
- `MobileNumberExtensions`: `IsMobileNumber`, `IsMciMobileNumber`, `IsIrancellMobileNumber` (pattern `RegexPattern.IranCellMobileNumber`; old const name `IrancellMobileNumber` is obsolete), `ToMobileNumberPattern`
  (`98…`), `ToMobileNumber` (string→long / long normalized to `98…`), `ToStandardMobileNumber` (strips leading `98`).
- `ValidatorExtensions`: `IsIp`, `IsIp2`, `IsUrl`, `IsIban`, `IsEmail`, `IsTime`, `IsPersianDate` (`1ddd/m/d`, `-` or `/`,
  optional time; not anchored), `IsPicture` (jpg, jpeg, png, gif, bmp, heic; case-insensitive), `IsDateTime`, `IsNationalCode`/`IsNationalCode2` (same algorithm),
  `IsBankCardNumber` (`dddd-dddd-dddd-dddd`), `IsBankAccountNumber`, `IsBankSheba` (length only), `IsCarPlate`,
  `IsComplexPassword(config = default)`.

All regex-based checks use the shared instances in `BatRegex` built from `RegexPattern` constants.

## Other extensions
- `StringExtensions`: `ToInt/ToLong/ToDecimal` (TryParse, 0 on failure), `ToBinary`, `ToBase64String`, `ToBytes`,
  `ToBytesFromBase64`, `ToPersianAlphaNumeric` (actually keeps only `[A-Za-z0-9]`), `ToCustomMask(MaskOption)`,
  `ToCustomSubstring`, `Fill` (`string.Format`), `SplitTo<T>`, `RemoveHtml`, `RemoveScript`, `ReplaceAll`.
- `ObjectExtensions`: `CopyFrom` / `UpdateWith` (reflection property copy by name, cached per type, errors swallowed),
  `GetProperty/SetProperty` by name, `GetInstance`.
- `ReflectionExtensions`: `IsInheritFrom`, `GetMemberInfo/GetNameProperty/GetDisplayProperty` (from expressions),
  `GetAttribute`, `GetDescription`, `GetDisplayName`, `GetClassFields` (→ `Bat.Core.PropertyInfo` list).
- `EnumExtensions`: `GetDisplayName`, `GetDescription`, `GetEnumElements<T>()`, `FilterEnumWithAttributeOf<TEnum, TAttr>()`.
- `LogicExtensions`: `IsNull/IsNotNull` (string = null/whitespace; `Guid?` = null or empty; `Guid.IsNotNull` = not empty),
  `CanBeCastTo<TEnum>(string)`, `ForEach` for `IEnumerable`/`List` and `IAsyncEnumerable` (**returns `Task` — await it**).
- `ExpressionExtensions` (namespace `Bat.Tools`!): `And`, `Or` (`OrElse`) to combine `Expression<Func<T,bool>>` predicates for EF.
- `ClaimsExtensions` on `ClaimsPrincipal`: `GetUserId()` (Guid), `GetUserId_Str()` (int!), `GetEmail`, `GetUsername`,
  `GetFullName`, `GetPicture`, `GetCustomField<T>()` (JSON claim `CustomField`). Throws if the claim is missing (except `GetCustomField`).
- `MenuExtensions.GetAllMenu()` flattens `MenuModel` + `ChildMenus` (one level).
- `TimeSpanExtension`: `ToTime(int/long seconds)` → `HH:mm:ss`, `ToTimeFormat`, `ToInteger`, `ToShort`, `ToHHMM`, `ToHHMMSS`.

## Security (see [known-issues](../known-issues.md))
- `Encryption.Encrypt/Decrypt` (Rijndael) and `AesEncryption.Encrypt/Decrypt` produce **identical** output (both AES-CBC/PKCS7).
  Overloads take key, salt, IV, `HashAlgorithmsTypes` (MD5/SHA1/SHA256 for key derivation) and `EncryptKeySize` (128/256; 512/1024 throw).
  Defaults: hard-coded pass phrase, salt and IV; `PasswordDeriveBytes` with **1 iteration**. Decrypt replaces `' '` with `'+'` (URL-decoding artefact).
- `AesAlgorithm` / `RijndaelAlgorithm`: the raw primitives; delegate to internal `SymmetricCrypto`, which caches derived keys per
  (pass phrase, salt, hash, iterations, key size). Output is pinned by tests.
- `HashGenerator.Hash(key[, salt])` = Base64(SHA256(UTF-16(salt + key))), default salt hard-coded; `VerifyHash`, `IsCorrectHash` (length heuristics).
- `RsaEncryptor.Sign(pemPath, password, data)` / `Verify(pemPath, data, signature)` — SHA256/PKCS1; reads the key file on every call.

## Pagination and responses
- `PagingParameter { PageNumber = 1, PageSize = 10 }`. `PagingList<T> : List<T>` + `PagingDetails`.
- `ToPagingListDetails(IQueryable|IEnumerable, PagingParameter)`: count + Skip/Take (PageSize ≤ 0 → 10, PageNumber ≤ 0 → 1).
  `ToPagingListModel(...)`: wraps an already-paged list (no Skip/Take). EF async versions live in Bat.EntityFrameworkCore.
- `Response` / `Response<T>`: `Success(...)`, `Error(...)`, ctor overloads; `ResultCode` default 200; `ExecutionTime` is `DateTime.Now` at read time.

## Files and logging
- `FileLoger.Info/CriticalInfo/Message/Error/CriticalError(…, path = "")`: appends to `<path or AppBase/Log>/<Kind>-yyyy-MM-dd.txt`
  (Persian date, one file per day per kind), synchronous, process-wide locks. Prefer `ILogger` in services.
- `FileOperation`: `Save`, `Delete`, `Exist`, `CreateDirectory` (creates all segments, cross-platform), `CheckExtension(FileType, name)`
  (case-insensitive; old name `CheckExtention` is an obsolete alias; images include `.heic`), `GetFileType(name)` (case-sensitive), `GetFileInfo`.
- `FileConvertor`: Base64 helpers, `ToStandardSize`. Two overloads are unimplemented stubs (`ToStandardSize(size,in,out)` returns `""`, `ToNewSize` returns `1`).

## Randomness
`Randomizer.GetRandomString(len[, alphabet])` uses `RandomNumberGenerator` (safe for tokens; used for JWT refresh tokens).
`GetRandomInteger(len)` (digits 1–9, `len ≤ 9`), `GetUniqueKey`, `GetUniqueKey2` (Base64 of random bytes).

## DI and entity marker interfaces
See [architecture.md](../architecture.md). Entity property interfaces:
`IInsertDateProperty {InsertDateMi}`, `IInsertDateProperties {InsertDateMi, InsertDateSh}`, `IInsertDateOnlyProperty {InsertDate}`,
`IInsertTimeOnlyProperty {InsertTime}`, the `IModify*` equivalents, `ISoftDeleteProperty {IsDeleted}`, `IIsActiveProperty {IsActive}`,
`IAuditLogProperties`, `IEventLogProperties` — all derive from `IBaseProperties`.

## Pitfalls
- `ToCurrency`/`ToNumeric` use the current culture by design (UI formatting).
- `DateTimeConverterForSerializer` uses culture-dependent `DateTime.Parse/ToString`; it is not part of the default options.
- `MenuModel.ChildMenus` deserializes `Menus` JSON lazily and caches it until `Menus` changes.
