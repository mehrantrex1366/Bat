# Bat.SqlClrAssembly

`src/Sql/Bat.SqlAssembly/Bat.SqlClrAssembly.sqlproj` — SQL Server CLR assembly (SSDT project, .NET Framework).
Builds only in Visual Studio on Windows with the SQL Server Data Tools workload; excluded from `Bat.NoSql.slnf`.

Contains its **own copies** of `PersianDateTime`, `MaskMode`, `MaskOption` (under `External Resource/`) — changes to Bat.Core
are not reflected here automatically (e.g. the `PersianDateTime` DST and month-name fixes of 10.0.1 are not applied here).

Functions:
- Scalar: `ToDateTimeShamsi`, `ToDateTimeShamsiFromString`, `ToDateTimeMiladi`, `AddTime`, `ConvertStringToHex`,
  `ConvertHexToString`, `IsNullOrEmpty`, `RemoveHtml`, `ReplaceAll`, `ToBinary`, `CustomSubstring`, `CustomMask`,
  `GetPlainNumber`, `Get3DigitSplitNumber`, `NormalizeNumber`, `ToText`, `ToTextWithLevel`.
- Table-valued: `SplitText` (with `GetSplitedItems` fill-row method).

Deploying a new version requires redeploying the assembly to SQL Server (CLR enabled, appropriate permission set).
