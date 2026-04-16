using ClosedXML.Excel;
using System.IO.Compression;

namespace Bank.Web.Services.BulkUpload;

public sealed class BulkUploadPackageReader
{
    private static readonly string[] RequiredHeaders =
    {
        "RowRef",
        "FirstName",
        "MiddleName",
        "LastName",
        "DateOfBirth",
        "PPSN",
        "Email",
        "Phone",
        "AddressLine1",
        "County",
        "City",
        "Eircode",
        "IsPEP",
        "RiskRating",
        "PscFrontFileName",
        "PscBackFileName"
    };

    public Task<BulkUploadPackageReadResult> ReadAsync(string zipFilePath)
    {
        var result = new BulkUploadPackageReadResult();

        if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
        {
            result.Success = false;
            result.ErrorMessage = "ZIP file not found.";
            return Task.FromResult(result);
        }

        var extractRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "BulkUploads",
            "Extracted",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(extractRoot);

        ZipFile.ExtractToDirectory(zipFilePath, extractRoot);

        var dataFolder = Path.Combine(extractRoot, "data");
        var frontFolder = Path.Combine(extractRoot, "psc-front");
        var backFolder = Path.Combine(extractRoot, "psc-back");

        if (!Directory.Exists(dataFolder))
        {
            result.Success = false;
            result.ErrorMessage = "Missing 'data' folder in ZIP.";
            return Task.FromResult(result);
        }

        if (!Directory.Exists(frontFolder))
        {
            result.Success = false;
            result.ErrorMessage = "Missing 'psc-front' folder in ZIP.";
            return Task.FromResult(result);
        }

        if (!Directory.Exists(backFolder))
        {
            result.Success = false;
            result.ErrorMessage = "Missing 'psc-back' folder in ZIP.";
            return Task.FromResult(result);
        }

        var excelFilePath = Directory
            .GetFiles(dataFolder, "*.xlsx", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(excelFilePath))
        {
            result.Success = false;
            result.ErrorMessage = "No .xlsx file found inside 'data' folder.";
            return Task.FromResult(result);
        }

        using var workbook = new XLWorkbook(excelFilePath);
        var worksheet = workbook.Worksheets.FirstOrDefault(x => x.Name == "KYC_UPLOAD");

        if (worksheet == null)
        {
            result.Success = false;
            result.ErrorMessage = "Worksheet 'KYC_UPLOAD' not found.";
            return Task.FromResult(result);
        }

        var headerMap = BuildHeaderMap(worksheet);
        var missingHeaders = RequiredHeaders.Where(x => !headerMap.ContainsKey(x)).ToList();

        if (missingHeaders.Count > 0)
        {
            result.Success = false;
            result.ErrorMessage = "Missing required columns: " + string.Join(", ", missingHeaders);
            return Task.FromResult(result);
        }

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);

            if (IsRowEmpty(row, headerMap))
                continue;

            result.Rows.Add(new BulkUploadParsedRow
            {
                RowNumber = rowNumber,
                RowRef = GetCellValue(row, headerMap, "RowRef"),
                FirstName = GetCellValue(row, headerMap, "FirstName"),
                MiddleName = GetCellValue(row, headerMap, "MiddleName"),
                LastName = GetCellValue(row, headerMap, "LastName"),
                DateOfBirth = GetCellValue(row, headerMap, "DateOfBirth"),
                PPSN = GetCellValue(row, headerMap, "PPSN"),
                Email = GetCellValue(row, headerMap, "Email"),
                Phone = GetCellValue(row, headerMap, "Phone"),
                AddressLine1 = GetCellValue(row, headerMap, "AddressLine1"),
                County = GetCellValue(row, headerMap, "County"),
                City = GetCellValue(row, headerMap, "City"),
                Eircode = GetCellValue(row, headerMap, "Eircode"),
                IsPEP = GetCellValue(row, headerMap, "IsPEP"),
                RiskRating = GetCellValue(row, headerMap, "RiskRating"),
                PscFrontFileName = GetCellValue(row, headerMap, "PscFrontFileName"),
                PscBackFileName = GetCellValue(row, headerMap, "PscBackFileName")
            });
        }

        result.Success = true;
        result.ExtractedRootPath = extractRoot;
        result.ExcelFilePath = excelFilePath;
        result.PscFrontFolderPath = frontFolder;
        result.PscBackFolderPath = backFolder;

        return Task.FromResult(result);
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
    {
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = worksheet.Row(1);
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int col = 1; col <= lastColumn; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(header) && !headerMap.ContainsKey(header))
            {
                headerMap[header] = col;
            }
        }

        return headerMap;
    }

    private static string GetCellValue(IXLRow row, Dictionary<string, int> headerMap, string headerName)
    {
        if (!headerMap.TryGetValue(headerName, out var columnNumber))
            return "";

        return row.Cell(columnNumber).GetString().Trim();
    }

    private static bool IsRowEmpty(IXLRow row, Dictionary<string, int> headerMap)
    {
        foreach (var header in RequiredHeaders)
        {
            var value = GetCellValue(row, headerMap, header);
            if (!string.IsNullOrWhiteSpace(value))
                return false;
        }

        return true;
    }
}