using Bank.Web.Data;
using Bank.Web.Models;
using Bank.Web.Services.Automation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bank.Web.Services.BulkUpload;

public sealed class BulkUploadImportService
{
    private readonly BankDbContext _context;
    private readonly BulkUploadPackageReader _packageReader;
    private readonly BulkUploadRowValidator _rowValidator;
    private readonly KycAutomationService _automationService;

    public BulkUploadImportService(
        BankDbContext context,
        BulkUploadPackageReader packageReader,
        BulkUploadRowValidator rowValidator,
        KycAutomationService automationService)
    {
        _context = context;
        _packageReader = packageReader;
        _rowValidator = rowValidator;
        _automationService = automationService;
    }

    public async Task<(bool Success, string Message)> ImportBatchAsync(Guid bulkUploadBatchId)
    {
        var batch = await _context.BulkUploadBatches
            .FirstOrDefaultAsync(x => x.BulkUploadBatchId == bulkUploadBatchId);

        if (batch == null)
            return (false, "Batch not found.");

        var zipFilePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "BulkUploads",
            batch.StoredFileName);

        var readResult = await _packageReader.ReadAsync(zipFilePath);
        if (!readResult.Success)
        {
            batch.Status = BulkUploadBatchStatus.Failed;
            batch.FailureReason = readResult.ErrorMessage;
            await _context.SaveChangesAsync();
            return (false, readResult.ErrorMessage ?? "Failed to read package.");
        }

        var validationResult = await _rowValidator.ValidateAsync(
            readResult.Rows,
            readResult.PscFrontFolderPath,
            readResult.PscBackFolderPath);

        var rowResults = await _context.BulkUploadRowResults
            .Where(x => x.BulkUploadBatchId == bulkUploadBatchId)
            .ToListAsync();

        foreach (var validatedRow in validationResult.Rows.Where(x => x.IsValid))
        {
            var parsed = validatedRow.ParsedRow;

            var rowResult = rowResults.FirstOrDefault(x =>
                x.RowNumber == parsed.RowNumber &&
                x.RowRef == parsed.RowRef);

            if (rowResult == null)
                continue;

            if (rowResult.KycUploadId != null)
                continue;

            var firstName = (parsed.FirstName ?? "").Trim();
            var middleName = (parsed.MiddleName ?? "").Trim();
            var lastName = (parsed.LastName ?? "").Trim();
            var ppsn = (parsed.PPSN ?? "").Trim();
            var dob = validatedRow.DateOfBirth!.Value;

            var todayPrefix = $"KYC-{DateTime.UtcNow:yyyyMMdd}-";
            var todayCount = await _context.KycUploadDetails
                .CountAsync(x => x.RequestRef.StartsWith(todayPrefix));

            var requestRef = $"{todayPrefix}{(todayCount + 1):D6}";
            var identityHash = Sha256Hex($"{firstName}|{middleName}|{lastName}|{dob:yyyy-MM-dd}|{ppsn}");

            var frontBytes = await File.ReadAllBytesAsync(validatedRow.PscFrontFilePath!);
            var backBytes = await File.ReadAllBytesAsync(validatedRow.PscBackFilePath!);

            var frontContentType = GetContentType(validatedRow.PscFrontFilePath!);
            var backContentType = GetContentType(validatedRow.PscBackFilePath!);

            var upload = new KycUploadDetails
            {
                RequestRef = requestRef,
                Source = UploadSource.ManualForm,
                Status = KycWorkflowStatus.Draft,
                OcrStatus = OcrStatus.Pending,
                ValidationStatus = ValidationStatus.Pass,
                DedupeStatus = DedupeStatus.NotChecked,
                IdentityHash = identityHash,

                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                DateOfBirth = dob,

                PPSN = ppsn,
                Email = (parsed.Email ?? "").Trim(),
                Phone = (parsed.Phone ?? "").Trim(),
                AddressLine1 = (parsed.AddressLine1 ?? "").Trim(),

                CountyId = validatedRow.CountyId!.Value,
                CityId = validatedRow.CityId!.Value,

                Eircode = (parsed.Eircode ?? "").Trim(),
                Country = "Ireland",

                Nationality = "Ireland",
                Gender = "NA",
                Occupation = "NA",
                EmployerName = "NA",
                SourceOfFunds = "NA",
                IsPEP = validatedRow.IsPEP!.Value,
                RiskRating = validatedRow.RiskRating!.Value,

                SearchExecuted = false,
                SearchFound = null,
                CkycDownloadedAtUtc = null,
                CkycNumber = null,

                DedupeExecuted = false,
                DedupePassed = null,
                DedupeCheckedAtUtc = null,
                DedupeMessage = null
            };

            upload.Images.Add(new KycUploadImage
            {
                DocumentType = DocumentType.PSCFront,
                FileName = Path.GetFileName(validatedRow.PscFrontFilePath!),
                ContentType = frontContentType,
                FileSizeBytes = frontBytes.LongLength,
                FileHashSha256 = Sha256Hex(frontBytes),
                ImageBytes = frontBytes,
                OcrText = "",
                OcrConfidence = 0,
                ExtractedDocNumber = "",
                IsImageValidated = false,
                ImageValidationMessage = null,
                ImageValidatedAtUtc = null
            });

            upload.Images.Add(new KycUploadImage
            {
                DocumentType = DocumentType.PSCBack,
                FileName = Path.GetFileName(validatedRow.PscBackFilePath!),
                ContentType = backContentType,
                FileSizeBytes = backBytes.LongLength,
                FileHashSha256 = Sha256Hex(backBytes),
                ImageBytes = backBytes,
                OcrText = "",
                OcrConfidence = 0,
                ExtractedDocNumber = "",
                IsImageValidated = false,
                ImageValidationMessage = null,
                ImageValidatedAtUtc = null
            });

            _context.KycUploadDetails.Add(upload);
            await _context.SaveChangesAsync();

            rowResult.KycUploadId = upload.KycUploadId;
            rowResult.Status = BulkUploadRowStatus.Success;
            rowResult.ErrorMessage = null;

            await _context.SaveChangesAsync();

            var automationResult = await _automationService.ProcessRecordAsync(upload.KycUploadId);

            rowResult.ErrorMessage = automationResult.Success
                ? automationResult.IsComplete
                    ? $"Imported and automation completed. {automationResult.Message}"
                    : $"Imported and automation started. {automationResult.Message}"
                : $"Imported but automation stopped. {automationResult.Message}";

            await _context.SaveChangesAsync();
        }

        await _context.SaveChangesAsync();

        batch.SuccessRows = rowResults.Count(x => x.Status == BulkUploadRowStatus.Success);
        batch.FailedRows = rowResults.Count(x => x.Status == BulkUploadRowStatus.Failed);
        batch.TotalRows = rowResults.Count;
        batch.Status = batch.FailedRows > 0
            ? BulkUploadBatchStatus.PartiallyCompleted
            : BulkUploadBatchStatus.Completed;
        batch.CompletedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Bulk import completed successfully and automation was triggered for imported records.");
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    private static string Sha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Sha256Hex(bytes);
    }

    private static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}