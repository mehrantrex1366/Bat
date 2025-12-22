namespace Bat.Tools;

public static class ExcelExtension
{
    public static byte[] ToExcel<T>(this List<T> data) where T : class
    {
        ExcelPackage.License.SetNonCommercialPersonal("Mehran");
        using var package = new ExcelPackage();
        var workSheet = package.Workbook.Worksheets.Add("Data");
        var reportFields = data.Count > 0
            ? [.. data.First().GetType().GetProperties()]
            : typeof(T).GetProperties().ToList();

        var row = 1;
        var cell = 1;
        foreach (var field in reportFields)
        {
            workSheet.Cells[row, cell].Value = field.Name;
            workSheet.Cells[row, cell].Style.Font.Size = 16;
            workSheet.Cells[row, cell].Style.Font.Bold = true;
            workSheet.Cells[row, cell].AutoFitColumns(15, 50);
            cell++;
        }

        if (data.Count != 0)
        {
            row = 2;
            foreach (var record in data)
            {
                #region Fill Data
                cell = 1;
                foreach (var field in reportFields)
                {
                    var value = field.GetValue(record);
                    var typeName = field.PropertyType.Name;

                    if (value == null)
                    {
                        workSheet.Cells[row, cell].Value = string.Empty;
                    }
                    else
                    {
                        if (typeName.Contains("Enumerable"))
                        {
                            foreach (var item in value as IEnumerable<object>)
                            {
                                workSheet.Cells[row, cell].Value += item.SerializeToJson() + Environment.NewLine;
                                row++;
                            }
                            row--;
                        }
                        else
                        {
                            workSheet.Cells[row, cell].Value = value.ToString();
                        }
                    }
                    cell++;
                }
                row++;
                #endregion
            }
        }

        workSheet.Protection.IsProtected = false;
        workSheet.Protection.AllowSelectLockedCells = false;
        using var fileStream = new MemoryStream();
        package.SaveAs(fileStream);
        return fileStream.ToArray();
    }

    public static byte[] ToExcel<T>(this List<T> data, string sheetName) where T : class
    {
        ExcelPackage.License.SetNonCommercialPersonal("Mehran");
        using var package = new ExcelPackage();
        var workSheet = package.Workbook.Worksheets.Add(sheetName);
        var reportFields = data.Count > 0
            ? [.. data.First().GetType().GetProperties()]
            : typeof(T).GetProperties().ToList();

        var row = 1;
        var cell = 1;
        foreach (var field in reportFields)
        {
            workSheet.Cells[row, cell].Value = field.Name;
            workSheet.Cells[row, cell].Style.Font.Size = 16;
            workSheet.Cells[row, cell].Style.Font.Bold = true;
            workSheet.Cells[row, cell].AutoFitColumns(15, 50);
            cell++;
        }

        if (data.Count != 0)
        {
            row = 2;
            foreach (var record in data)
            {
                #region Fill Data
                cell = 1;
                foreach (var field in reportFields)
                {
                    var value = field.GetValue(record);
                    var typeName = field.PropertyType.Name;

                    if (value == null)
                    {
                        workSheet.Cells[row, cell].Value = string.Empty;
                    }
                    else
                    {
                        if (typeName.Contains("Enumerable"))
                        {
                            foreach (var item in value as IEnumerable<object>)
                            {
                                workSheet.Cells[row, cell].Value += item.SerializeToJson() + Environment.NewLine;
                                row++;
                            }
                            row--;
                        }
                        else
                        {
                            workSheet.Cells[row, cell].Value = value.ToString();
                        }
                    }
                    cell++;
                }
                row++;
                #endregion
            }
        }

        workSheet.Protection.IsProtected = false;
        workSheet.Protection.AllowSelectLockedCells = false;
        using var fileStream = new MemoryStream();
        package.SaveAs(fileStream);
        return fileStream.ToArray();
    }

    public static byte[] ToExcel<T>(this List<T> data, string sheetName,
        bool withAnonymousObject, bool withCollectionsObject) where T : class
    {
        ExcelPackage.License.SetNonCommercialPersonal("Mehran");
        using var package = new ExcelPackage();
        var workSheet = package.Workbook.Worksheets.Add(sheetName);
        var reportFields = data.Count > 0
            ? [.. data.First().GetType().GetProperties()]
            : typeof(T).GetProperties().ToList();

        var row = 1;
        var cell = 1;
        foreach (var field in reportFields)
        {
            workSheet.Cells[row, cell].Value = field.Name;
            workSheet.Cells[row, cell].Style.Font.Size = 16;
            workSheet.Cells[row, cell].Style.Font.Bold = true;
            workSheet.Cells[row, cell].AutoFitColumns(15, 50);
            cell++;
        }

        if (data.Count != 0)
        {
            row = 2;
            foreach (var record in data)
            {
                #region Fill Data
                cell = 1;
                foreach (var field in reportFields)
                {
                    var value = field.GetValue(record);
                    var typeName = field.PropertyType.Name;

                    if (value == null)
                    {
                        workSheet.Cells[row, cell].Value = string.Empty;
                    }
                    else
                    {
                        if (typeName.Contains("Anonymous"))
                        {
                            workSheet.Cells[row, cell].Value = withAnonymousObject == false
                                ? string.Empty
                                : value.SerializeToJson();
                        }
                        else if (typeName.Contains("Enumerable"))
                        {
                            if (withCollectionsObject == false)
                            {
                                workSheet.Cells[row, cell].Value = string.Empty;
                            }
                            else
                            {
                                foreach (var item in value as IEnumerable<object>)
                                {
                                    workSheet.Cells[row, cell].Value += item.SerializeToJson() + Environment.NewLine;
                                    row++;
                                }
                                row--;
                            }
                        }
                        else
                        {
                            workSheet.Cells[row, cell].Value = value.ToString();
                        }
                    }
                    cell++;
                }
                row++;
                #endregion
            }
        }

        workSheet.Protection.IsProtected = false;
        workSheet.Protection.AllowSelectLockedCells = false;
        using var fileStream = new MemoryStream();
        package.SaveAs(fileStream);
        return fileStream.ToArray();
    }

    public static byte[] ToExcel<T>(this List<T> data, string sheetName,
        bool withAnonymousObject = true, bool withCollectionsObject = true, List<string> excludeProperties = null) where T : class
    {
        ExcelPackage.License.SetNonCommercialPersonal("Mehran");
        using var package = new ExcelPackage();
        var workSheet = package.Workbook.Worksheets.Add(sheetName);
        var reportFields = data.Count > 0 ? [.. data.First().GetType().GetProperties()] : typeof(T).GetProperties().ToList();
        if (excludeProperties.Count != 0) reportFields = reportFields.Except(reportFields.Where(x => excludeProperties.Contains(x.Name)).ToList()).ToList();

        var row = 1;
        var cell = 1;
        foreach (var field in reportFields)
        {
            workSheet.Cells[row, cell].Value = field.Name;
            workSheet.Cells[row, cell].Style.Font.Size = 16;
            workSheet.Cells[row, cell].Style.Font.Bold = true;
            workSheet.Cells[row, cell].AutoFitColumns(15, 50);
            cell++;
        }

        if (data.Count != 0)
        {
            row = 2;
            foreach (var record in data)
            {
                #region Fill Data
                cell = 1;
                foreach (var field in reportFields)
                {
                    var value = field.GetValue(record);
                    var typeName = field.PropertyType.Name;

                    if (value == null)
                    {
                        workSheet.Cells[row, cell].Value = string.Empty;
                    }
                    else
                    {
                        if (typeName.Contains("Anonymous"))
                        {
                            workSheet.Cells[row, cell].Value = withAnonymousObject == false
                                ? string.Empty
                                : value.SerializeToJson();
                        }
                        else if (typeName.Contains("Enumerable"))
                        {
                            if (withCollectionsObject == false)
                            {
                                workSheet.Cells[row, cell].Value = string.Empty;
                            }
                            else
                            {
                                foreach (var item in value as IEnumerable<object>)
                                {
                                    workSheet.Cells[row, cell].Value += item.SerializeToJson() + Environment.NewLine;
                                    row++;
                                }
                                row--;
                            }
                        }
                        else
                        {
                            workSheet.Cells[row, cell].Value = value.ToString();
                        }
                    }
                    cell++;
                }
                row++;
                #endregion
            }
        }

        workSheet.Protection.IsProtected = false;
        workSheet.Protection.AllowSelectLockedCells = false;
        using var fileStream = new MemoryStream();
        package.SaveAs(fileStream);
        return fileStream.ToArray();
    }
}