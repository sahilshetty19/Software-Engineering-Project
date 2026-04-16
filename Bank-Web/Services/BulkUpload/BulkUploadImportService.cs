using Bank.Web.Data;
using Bank.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bank.Web.Services.BulkUpload;

public sealed class BulkUploadImportService
{
    private readonly BankDbContext _context;
    private readonly BulkUploadPackageReader _packageReader;
    private readonly BulkUploadRowValidator _rowValidator;

    public BulkUploadImportService(
        BankDbContext context,
        BulkUploadPackageReader packageReader,
        BulkUploadRowValidator rowValidator)
    {
        _context = context;
        _packageReader = packageReader;
        _rowValidator = rowValidator;
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

        if (rowResults.Count == 0)
            return (false, "Batch rows were not prepared. Read the batch before importing.");

        var rowResultsByKey = rowResults.ToDictionary(
            x => BuildRowKey(x.RowNumber, x.RowRef),
            x => x);

        var linkedRowCount = 0;

        foreach (var validatedRow in validationResult.Rows.Where(x => x.IsValid))
        {
            var parsed = validatedRow.ParsedRow;
            var normalizedRowRef = NormalizeRowRef(parsed.RowRef);
            var rowKey = BuildRowKey(parsed.RowNumber, normalizedRowRef);

            if (!rowResultsByKey.TryGetValue(rowKey, out var rowResult))
            {
                rowResult = rowResults.FirstOrDefault(x => x.RowNumber == parsed.RowNumber)
                    ?? rowResults.FirstOrDefault(x =>
                        string.Equals(NormalizeRowRef(x.RowRef), normalizedRowRef, StringComparison.OrdinalIgnoreCase));
            }

            if (rowResult == null)
            {
                rowResult = new BulkUploadRowResult
                {
                    BulkUploadBatchId = bulkUploadBatchId,
                    RowNumber = parsed.RowNumber,
                    RowRef = normalizedRowRef,
                    Status = BulkUploadRowStatus.Pending,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                _context.BulkUploadRowResults.Add(rowResult);
                rowResults.Add(rowResult);
                rowResultsByKey[rowKey] = rowResult;
            }
            else
            {
                rowResult.RowRef = normalizedRowRef;
            }

            if (rowResult.KycUploadId != null)
            {
                rowResult.Status = BulkUploadRowStatus.Success;
                rowResult.ErrorMessage = "Imported and queued for automation.";
                linkedRowCount += 1;
                continue;
            }

            var firstName = (parsed.FirstName ?? "").Trim();
            var middleName = (parsed.MiddleName ?? "").Trim();
            var lastName = (parsed.LastName ?? "").Trim();
            var ppsn = (parsed.PPSN ?? "").Trim();
            var dob = validatedRow.DateOfBirth!.Value;

            var requestRef = await Bank.Web.Services.KycRequestRefGenerator.GenerateAsync(_context);
            var identityHash = Sha256Hex($"{firstName}|{middleName}|{lastName}|{dob:yyyy-MM-dd}|{ppsn}");

            var frontBytes = await File.ReadAllBytesAsync(validatedRow.PscFrontFilePath!);
            var backBytes = await File.ReadAllBytesAsync(validatedRow.PscBackFilePath!);

            var frontContentType = GetContentType(validatedRow.PscFrontFilePath!);
            var backContentType = GetContentType(validatedRow.PscBackFilePath!);

            var upload = new KycUploadDetails
            {
                RequestRef = requestRef,
                Source = UploadSource.ExcelUpload,
                Status = KycWorkflowStatus.Draft,
                OcrStatus = OcrStatus.Pending,
                ValidationStatus = ValidationStatus.Pass,
                DedupeStatus = DedupeStatus.NotChecked,
                IdentityHash = identityHash,
                AutomationStatus = AutomationRunStatus.Queued,
                RetryAttemptCount = 0,
                MaxRetryAttempts = 5,
                NextRetryAtUtc = null,
                LastAutomationStartedAtUtc = null,
                LastAutomationCompletedAtUtc = null,
                AutomationLockedUntilUtc = null,
                LastFailedStep = null,
                LastAutomationError = null,

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
            rowResult.ErrorMessage = "Imported and queued for automation.";
            linkedRowCount += 1;

            await _context.SaveChangesAsync();
        }

        await _context.SaveChangesAsync();

        var expectedLinkedRows = validationResult.ValidRows;

        if (expectedLinkedRows > 0 && linkedRowCount < expectedLinkedRows)
        {
            batch.TotalRows = rowResults.Count;
            batch.SuccessRows = linkedRowCount;
            batch.FailedRows = rowResults.Count(x => x.Status == BulkUploadRowStatus.Failed);
            batch.Status = BulkUploadBatchStatus.Failed;
            batch.FailureReason = $"Only {linkedRowCount} of {expectedLinkedRows} valid rows were linked to KYC records.";
            batch.CompletedAtUtc = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return (false, batch.FailureReason);
        }

        batch.SuccessRows = rowResults.Count(x => x.KycUploadId != null);
        batch.FailedRows = rowResults.Count(x => x.Status == BulkUploadRowStatus.Failed);
        batch.TotalRows = rowResults.Count;
        batch.Status = batch.FailedRows > 0
            ? BulkUploadBatchStatus.PartiallyCompleted
            : BulkUploadBatchStatus.Completed;
        batch.FailureReason = null;
        batch.CompletedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Bulk import completed successfully and records were queued for automation.");
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

    private static string NormalizeRowRef(string? rowRef)
    {
        return (rowRef ?? "").Trim();
    }

    private static string BuildRowKey(int rowNumber, string? rowRef)
    {
        return $"{rowNumber:D6}|{NormalizeRowRef(rowRef).ToUpperInvariant()}";
    }
}
