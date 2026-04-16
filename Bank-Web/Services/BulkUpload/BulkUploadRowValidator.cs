using Bank.Web.Data;
using Bank.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Bank.Web.Services.BulkUpload;

public sealed class BulkUploadRowValidator
{
    private readonly BankDbContext _context;

    public BulkUploadRowValidator(BankDbContext context)
    {
        _context = context;
    }

    public async Task<BulkUploadValidationResult> ValidateAsync(
        List<BulkUploadParsedRow> parsedRows,
        string pscFrontFolderPath,
        string pscBackFolderPath)
    {
        var result = new BulkUploadValidationResult();

        var counties = await _context.Counties
            .AsNoTracking()
            .ToListAsync();

        var cities = await _context.Cities
            .AsNoTracking()
            .ToListAsync();

        var rowRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in parsedRows)
        {
            var validated = new BulkUploadValidatedRow
            {
                ParsedRow = row,
                IsValid = false
            };

            var errors = new List<string>();

            void Require(string value, string fieldName)
            {
                if (string.IsNullOrWhiteSpace(value))
                    errors.Add($"{fieldName} is required.");
            }

            Require(row.RowRef, "RowRef");
            Require(row.FirstName, "FirstName");
            Require(row.LastName, "LastName");
            Require(row.DateOfBirth, "DateOfBirth");
            Require(row.PPSN, "PPSN");
            Require(row.Email, "Email");
            Require(row.Phone, "Phone");
            Require(row.AddressLine1, "AddressLine1");
            Require(row.County, "County");
            Require(row.City, "City");
            Require(row.Eircode, "Eircode");
            Require(row.IsPEP, "IsPEP");
            Require(row.RiskRating, "RiskRating");
            Require(row.PscFrontFileName, "PscFrontFileName");
            Require(row.PscBackFileName, "PscBackFileName");

            if (!string.IsNullOrWhiteSpace(row.RowRef))
            {
                if (!rowRefs.Add(row.RowRef))
                    errors.Add("Duplicate RowRef found in uploaded Excel.");
            }

            if (!DateOnly.TryParse(row.DateOfBirth, out var dob))
            {
                errors.Add("DateOfBirth must be a valid date in yyyy-MM-dd format.");
            }
            else
            {
                validated.DateOfBirth = dob;
            }

            if (!bool.TryParse(row.IsPEP, out var isPep))
            {
                errors.Add("IsPEP must be either true or false.");
            }
            else
            {
                validated.IsPEP = isPep;
            }

            if (!Enum.TryParse<RiskRating>(row.RiskRating, true, out var riskRating))
            {
                errors.Add("RiskRating must be Low, Medium, or High.");
            }
            else
            {
                validated.RiskRating = riskRating;
            }

            var county = counties.FirstOrDefault(x =>
                string.Equals(x.CountyName, row.County, StringComparison.OrdinalIgnoreCase));

            if (county == null)
            {
                errors.Add("County not found in master data.");
            }
            else
            {
                validated.CountyId = county.CountyId;

                var city = cities.FirstOrDefault(x =>
                    x.CountyId == county.CountyId &&
                    string.Equals(x.CityName, row.City, StringComparison.OrdinalIgnoreCase));

                if (city == null)
                {
                    errors.Add("City not found for selected County.");
                }
                else
                {
                    validated.CityId = city.CityId;
                }
            }

            var frontPath = Path.Combine(pscFrontFolderPath, row.PscFrontFileName ?? "");
            if (!File.Exists(frontPath))
            {
                errors.Add($"PSC Front file not found: {row.PscFrontFileName}");
            }
            else
            {
                validated.PscFrontFilePath = frontPath;
            }

            var backPath = Path.Combine(pscBackFolderPath, row.PscBackFileName ?? "");
            if (!File.Exists(backPath))
            {
                errors.Add($"PSC Back file not found: {row.PscBackFileName}");
            }
            else
            {
                validated.PscBackFilePath = backPath;
            }

            validated.IsValid = errors.Count == 0;
            validated.ErrorMessage = errors.Count == 0 ? null : string.Join(" ", errors);

            result.Rows.Add(validated);
        }

        return result;
    }
}