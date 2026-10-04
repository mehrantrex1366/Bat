# Bat.Tools

`src/Core/Bat.Tools` · namespace `Bat.Tools` · depends on Bat.Core, EPPlus 8 (non-commercial license set once in a static ctor).

`ExcelExtension.ToExcel<T>(this List<T> data[, sheetName[, withAnonymousObject, withCollectionsObject[, excludeProperties]]])`
→ `byte[]` (xlsx). Columns = public properties of the first item's runtime type (or `T`), header row bold size 16.
Collection properties are written one JSON item per row (rows are advanced), anonymous-type properties as JSON.

`PublicExtension` is empty. Note: `ExpressionExtensions` (`And`/`Or`) lives in namespace `Bat.Tools` but in the **Bat.Core** assembly.
