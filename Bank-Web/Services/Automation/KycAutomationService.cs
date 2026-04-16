using Bank.Web.Data;
using Bank.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bank.Web.Services.Automation;

public sealed class KycAutomationResult
{
    public bool Success { get; set; }
    public bool IsComplete { get; set; }
    public string Message { get; set; } = "";
}

public sealed class KycAutomationService
{
    private readonly BankDbContext _context;
    private readonly CkycApiClient _ckyc;
    private readonly AesGcmCryptoService _aes;
    private readonly PscOcrService _pscOcr;
    private readonly CkycStatusClient _ckycStatus;
    private readonly SftpZipUploader _sftp;

    public KycAutomationService(
        BankDbContext context,
        CkycApiClient ckyc,
        AesGcmCryptoService aes,
        PscOcrService pscOcr,
        CkycStatusClient ckycStatus,
        SftpZipUploader sftp)
    {
        _context = context;
        _ckyc = ckyc;
        _aes = aes;
        _pscOcr = pscOcr;
        _ckycStatus = ckycStatus;
        _sftp = sftp;
    }

    public async Task<KycAutomationResult> ProcessRecordAsync(Guid kycUploadId)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.KycUploadId == kycUploadId);

        if (upload == null)
        {
            return new KycAutomationResult
            {
                Success = false,
                IsComplete = false,
                Message = "KYC record not found."
            };
        }

        if (upload.Status == KycWorkflowStatus.KycDone)
        {
            return new KycAutomationResult
            {
                Success = true,
                IsComplete = true,
                Message = "Record already completed."
            };
        }

        if (!await EnsurePscFrontValidated(upload))
            return Fail("PSC Front validation failed.");

        if (!await EnsurePscBackValidated(upload))
            return Fail("PSC Back validation failed.");

        if (!await EnsureDedupeChecked(upload))
            return Fail("Dedupe check failed.");

        if (upload.DedupePassed == true)
        {
            return new KycAutomationResult
            {
                Success = true,
                IsComplete = true,
                Message = "Dedupe PASS. Customer already exists in bank records."
            };
        }

        if (!await EnsureSearchExecuted(upload))
            return Fail("CKYC search failed.");

        if (upload.SearchFound == true)
        {
            if (!await EnsureDownloadCompleted(upload))
                return Fail("CKYC download failed.");

            if (!await EnsurePushedToInternal(upload))
                return Fail("Push to internal failed.");

            return new KycAutomationResult
            {
                Success = true,
                IsComplete = true,
                Message = "Found in CKYC, downloaded, and pushed to internal successfully."
            };
        }

        if (!await EnsureZipGenerated(upload))
            return Fail("ZIP generation failed.");

        if (upload.Status != KycWorkflowStatus.Sent &&
            upload.Status != KycWorkflowStatus.Completed &&
            upload.Status != KycWorkflowStatus.KycDone)
        {
            if (!await EnsureZipSent(upload))
                return Fail("SFTP upload failed.");
        }

        if (upload.Status == KycWorkflowStatus.Sent)
        {
            var statusResult = await CheckStatusOnce(upload);
            if (!statusResult.Success)
                return statusResult;
        }

        if (upload.Status == KycWorkflowStatus.Completed &&
            !string.IsNullOrWhiteSpace(upload.CkycNumber))
        {
            if (!await EnsurePushedToInternal(upload))
                return Fail("Push to internal failed after CKYC completion.");

            return new KycAutomationResult
            {
                Success = true,
                IsComplete = true,
                Message = "CKYC completed and pushed to internal successfully."
            };
        }

        return new KycAutomationResult
        {
            Success = true,
            IsComplete = false,
            Message = "Record processed up to current automation step. Awaiting next retry/check."
        };
    }

    private async Task<bool> EnsurePscFrontValidated(KycUploadDetails upload)
    {
        var front = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCFront);
        if (front == null)
            return false;

        if (front.IsImageValidated)
            return true;

        var log = await StartStep(upload.KycUploadId, "Validate PSC Front");

        var ocr = _pscOcr.ReadText(front.ImageBytes);
        front.OcrText = ocr.Text ?? "";
        front.OcrConfidence = ocr.Confidence;

        var (forenameRaw, surnameRaw) = ExtractPscNameParts(front.OcrText);

        var expectedForename = NormalizeForMatch($"{upload.FirstName} {upload.MiddleName}".Trim());
        var expectedSurname = NormalizeForMatch($"{upload.LastName}".Trim());

        var extractedForename = NormalizeForMatch(forenameRaw);
        var extractedSurname = NormalizeForMatch(surnameRaw);

        var ok = extractedForename.Contains(expectedForename) &&
                 extractedSurname.Contains(expectedSurname);

        front.IsImageValidated = ok;
        front.ImageValidatedAtUtc = DateTimeOffset.UtcNow;

        var expectedFull = $"{upload.FirstName} {upload.MiddleName} {upload.LastName}"
            .Replace("  ", " ").Trim();

        var extractedFull = $"{forenameRaw} {surnameRaw}"
            .Replace("  ", " ").Trim();

        front.ImageValidationMessage = ok
            ? $"PSC Front validated. Name matched: {expectedFull}"
            : $"PSC Front validation failed. Expected name: {expectedFull}. OCR name extracted: '{extractedFull}'. Full OCR: '{front.OcrText}'";

        if (!ok)
        {
            upload.Status = KycWorkflowStatus.Failed;
            upload.FailureReason = front.ImageValidationMessage;
            await _context.SaveChangesAsync();
            await FinishStep(log, KycWorkflowStepStatus.Failed, front.ImageValidationMessage, front.ImageValidationMessage);
            return false;
        }

        await _context.SaveChangesAsync();
        await FinishStep(log, KycWorkflowStepStatus.Success, front.ImageValidationMessage);
        return true;
    }

    private async Task<bool> EnsurePscBackValidated(KycUploadDetails upload)
    {
        var back = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCBack);
        if (back == null)
            return false;

        if (back.IsImageValidated)
            return true;

        var log = await StartStep(upload.KycUploadId, "Validate PSC Back");

        var ocr = _pscOcr.ReadText(back.ImageBytes);
        back.OcrText = ocr.Text ?? "";
        back.OcrConfidence = ocr.Confidence;

        var expected = NormalizeForMatch(upload.PPSN);
        var extracted = NormalizeForMatch(back.OcrText);

        var ok = extracted.Contains(expected);

        back.IsImageValidated = ok;
        back.ImageValidatedAtUtc = DateTimeOffset.UtcNow;
        back.ImageValidationMessage = ok
            ? $"PSC Back validated. PPSN matched: {upload.PPSN.Trim()}"
            : $"PSC Back validation failed. Expected PPSN: {upload.PPSN.Trim()}. Full OCR: '{back.OcrText}'";

        if (!ok)
        {
            upload.Status = KycWorkflowStatus.Failed;
            upload.FailureReason = back.ImageValidationMessage;
            await _context.SaveChangesAsync();
            await FinishStep(log, KycWorkflowStepStatus.Failed, back.ImageValidationMessage, back.ImageValidationMessage);
            return false;
        }

        await _context.SaveChangesAsync();
        await FinishStep(log, KycWorkflowStepStatus.Success, back.ImageValidationMessage);
        return true;
    }

    private async Task<bool> EnsureDedupeChecked(KycUploadDetails upload)
    {
        if (upload.DedupeExecuted)
            return true;

        var log = await StartStep(upload.KycUploadId, "Check Dedupe");

        var fn = (upload.FirstName ?? "").Trim().ToUpperInvariant();
        var mn = (upload.MiddleName ?? "").Trim().ToUpperInvariant();
        var ln = (upload.LastName ?? "").Trim().ToUpperInvariant();
        var ppsn = (upload.PPSN ?? "").Trim().ToUpperInvariant();

        var exists = await _context.BankCustomerDetails
            .AsNoTracking()
            .AnyAsync(c =>
                c.DateOfBirth == upload.DateOfBirth &&
                c.FirstName.ToUpper() == fn &&
                c.MiddleName.ToUpper() == mn &&
                c.LastName.ToUpper() == ln &&
                c.PPSN.ToUpper() == ppsn
            );

        upload.DedupeExecuted = true;
        upload.DedupeCheckedAtUtc = DateTimeOffset.UtcNow;
        upload.DedupePassed = exists;

        if (exists)
        {
            upload.Status = KycWorkflowStatus.KycDone;
            upload.DedupeMessage = "Dedupe PASS: customer already exists in bank records. No CKYC needed.";
        }
        else
        {
            upload.DedupeMessage = "Dedupe FAIL: customer not found in bank records. Proceed to CKYC Search.";
        }

        await _context.SaveChangesAsync();
        await FinishStep(log, KycWorkflowStepStatus.Success, upload.DedupeMessage);
        return true;
    }

    private async Task<bool> EnsureSearchExecuted(KycUploadDetails upload)
    {
        if (upload.SearchExecuted)
            return true;

        var log = await StartStep(upload.KycUploadId, "Search CKYC");

        var requestObj = new
        {
            firstName = upload.FirstName,
            middleName = upload.MiddleName,
            lastName = upload.LastName,
            dateOfBirth = upload.DateOfBirth.ToString("yyyy-MM-dd"),
            ppsn = upload.PPSN
        };

        var requestBytes = JsonSerializer.SerializeToUtf8Bytes(requestObj);
        var encryptedRequestBytes = _aes.Encrypt(requestBytes);
        var requestHash = Sha256Hex(requestBytes);

        var (ok, message, found, ckycNumber) = await _ckyc.SearchAsync(
            upload.FirstName,
            upload.MiddleName,
            upload.LastName,
            upload.DateOfBirth,
            upload.PPSN);

        var responseObj = new
        {
            requestRef = upload.RequestRef,
            kycUploadId = upload.KycUploadId,
            timestampUtc = DateTimeOffset.UtcNow,
            ok,
            message,
            found,
            ckycNumber
        };

        var responseBytes = JsonSerializer.SerializeToUtf8Bytes(responseObj);
        var encryptedResponseBytes = _aes.Encrypt(responseBytes);
        var responseHash = Sha256Hex(responseBytes);

        var encryptedRow = new SearchResponseEncrypted
        {
            KycUploadId = upload.KycUploadId,
            RequestHashSha256 = requestHash,
            ResponseHashSha256 = responseHash,
            EncryptedRequestBytes = encryptedRequestBytes,
            EncryptedResponseBytes = encryptedResponseBytes,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _context.SearchResponseEncrypted.Add(encryptedRow);

        var decryptedPayload = new
        {
            request = JsonSerializer.Deserialize<object>(Encoding.UTF8.GetString(requestBytes)),
            response = JsonSerializer.Deserialize<object>(Encoding.UTF8.GetString(responseBytes))
        };

        _context.SearchResponseDecrypted.Add(new SearchResponseDecrypted
        {
            KycUploadId = upload.KycUploadId,
            SearchResponseEncryptedId = encryptedRow.SearchResponseEncryptedId,
            DecryptedJson = JsonSerializer.Serialize(decryptedPayload),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        if (!ok)
        {
            upload.Status = KycWorkflowStatus.Failed;
            upload.FailureReason = message;
            await _context.SaveChangesAsync();
            await FinishStep(log, KycWorkflowStepStatus.Failed, message, message);
            return false;
        }

        upload.SearchExecuted = true;
        upload.SearchFound = found;
        upload.CkycDownloadedAtUtc = null;

        if (found && !string.IsNullOrWhiteSpace(ckycNumber))
        {
            upload.CkycNumber = ckycNumber;
        }
        else
        {
            upload.CkycNumber = null;
        }

        await _context.SaveChangesAsync();

        var successMessage = found && !string.IsNullOrWhiteSpace(ckycNumber)
            ? $"CKYC record found. CKYC Number: {ckycNumber}"
            : "No CKYC record found. Proceed to ZIP/SFTP flow.";

        await FinishStep(log, KycWorkflowStepStatus.Success, successMessage);
        return true;
    }

    private async Task<bool> EnsureDownloadCompleted(KycUploadDetails upload)
    {
        if (upload.CkycDownloadedAtUtc != null)
            return true;

        if (upload.SearchExecuted != true || upload.SearchFound != true || string.IsNullOrWhiteSpace(upload.CkycNumber))
            return false;

        var log = await StartStep(upload.KycUploadId, "Download CKYC");

        var json = await _ckyc.DownloadProfileJsonAsync(upload.CkycNumber);
        json ??= "";

        var plainBytes = Encoding.UTF8.GetBytes(json);
        var respHash = Sha256Hex(plainBytes);
        var encBytes = _aes.Encrypt(plainBytes);

        _context.DownloadResponseEncrypted.Add(new DownloadResponseEncrypted
        {
            KycUploadId = upload.KycUploadId,
            CkycNumber = upload.CkycNumber.Trim(),
            ResponseHashSha256 = respHash,
            EncryptedResponseBytes = encBytes,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        _context.DownloadResponseDecrypted.Add(new DownloadResponseDecrypted
        {
            KycUploadId = upload.KycUploadId,
            CkycNumber = upload.CkycNumber.Trim(),
            ResponseHashSha256 = respHash,
            ResponseJson = json,
            PayloadJson = json,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        upload.CkycDownloadedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        await FinishStep(log, KycWorkflowStepStatus.Success, "CKYC record downloaded and stored.");
        return true;
    }

    private async Task<bool> EnsureZipGenerated(KycUploadDetails upload)
    {
        var existingZip = await _context.ZipFileUploadDetails
            .FirstOrDefaultAsync(z => z.KycUploadId == upload.KycUploadId);

        if (existingZip != null && existingZip.ZipBytes != null && existingZip.ZipBytes.Length > 0)
            return true;

        var log = await StartStep(upload.KycUploadId, "Generate ZIP");

        var zipBytes = ZipBuilder.BuildKycZip(upload);
        if (zipBytes == null || zipBytes.Length == 0)
        {
            upload.Status = KycWorkflowStatus.Failed;
            upload.FailureReason = "ZIP generation failed.";
            await _context.SaveChangesAsync();
            await FinishStep(log, KycWorkflowStepStatus.Failed, "ZIP generation failed.", "ZIP generation failed.");
            return false;
        }

        var zipFileName = $"{upload.RequestRef}.zip";
        var zipHash = Sha256Hex(zipBytes);

        if (existingZip == null)
        {
            _context.ZipFileUploadDetails.Add(new ZipFileUploadDetails
            {
                ZipUploadId = Guid.NewGuid(),
                KycUploadId = upload.KycUploadId,
                ZipFileName = zipFileName,
                ZipHashSha256 = zipHash,
                ZipSizeBytes = zipBytes.LongLength,
                ZipBytes = zipBytes,
                SftpRemotePath = null,
                UploadStatus = 0,
                UploadedAtUtc = DateTime.UtcNow,
                FailureReason = null
            });
        }
        else
        {
            existingZip.ZipFileName = zipFileName;
            existingZip.ZipHashSha256 = zipHash;
            existingZip.ZipSizeBytes = zipBytes.LongLength;
            existingZip.ZipBytes = zipBytes;
            existingZip.SftpRemotePath = null;
            existingZip.UploadStatus = 0;
            existingZip.UploadedAtUtc = DateTime.UtcNow;
            existingZip.FailureReason = null;
        }

        upload.Status = KycWorkflowStatus.Zipped;

        await _context.SaveChangesAsync();
        await FinishStep(log, KycWorkflowStepStatus.Success, "ZIP generated successfully.");
        return true;
    }

    private async Task<bool> EnsureZipSent(KycUploadDetails upload)
    {
        var zipRow = await _context.ZipFileUploadDetails
            .FirstOrDefaultAsync(z => z.KycUploadId == upload.KycUploadId);

        if (zipRow == null || zipRow.ZipBytes == null || zipRow.ZipBytes.Length == 0)
            return false;

        if (zipRow.UploadStatus == 1 && upload.Status == KycWorkflowStatus.Sent)
            return true;

        var log = await StartStep(upload.KycUploadId, "Send ZIP to SFTP");

        var (ok, remotePath, error) = await _sftp.UploadBytesAsync(zipRow.ZipBytes, zipRow.ZipFileName);

        var responseObj = new
        {
            requestRef = upload.RequestRef,
            zipFileName = zipRow.ZipFileName,
            success = ok,
            remotePath,
            error,
            receivedAtUtc = DateTimeOffset.UtcNow
        };

        var responseJson = JsonSerializer.Serialize(responseObj);
        var responseHash = Sha256Hex(responseJson);

        _context.KycUpdationResponses.Add(new KycUpdationResponse
        {
            KycUploadId = upload.KycUploadId,
            RequestRef = upload.RequestRef,
            ResponseType = "SFTP_UPLOAD",
            FileName = "sftp-upload-response.json",
            ContentType = "application/json",
            ResponseJson = responseJson,
            ResponseHashSha256 = responseHash,
            CkycNumber = upload.CkycNumber,
            UpdateStatus = ok ? UpdateStatus.Success : UpdateStatus.Failed,
            RejectionReason = ok ? null : error,
            ReceivedAtUtc = DateTimeOffset.UtcNow
        });

        if (ok)
        {
            zipRow.SftpRemotePath = remotePath;
            zipRow.UploadStatus = 1;
            zipRow.FailureReason = null;
            zipRow.UploadedAtUtc = DateTime.UtcNow;

            upload.SubmittedAtUtc = DateTimeOffset.UtcNow;
            upload.Status = KycWorkflowStatus.Sent;

            await _context.SaveChangesAsync();
            await FinishStep(log, KycWorkflowStepStatus.Success, $"ZIP uploaded to SFTP: {remotePath}");
            return true;
        }

        zipRow.UploadStatus = 2;
        zipRow.FailureReason = error;
        zipRow.UploadedAtUtc = DateTime.UtcNow;

        upload.SubmittedAtUtc = DateTimeOffset.UtcNow;
        upload.Status = KycWorkflowStatus.Failed;
        upload.FailureReason = error;

        await _context.SaveChangesAsync();
        await FinishStep(log, KycWorkflowStepStatus.Failed, $"SFTP upload failed: {error}", error);
        return false;
    }

    private async Task<KycAutomationResult> CheckStatusOnce(KycUploadDetails upload)
    {
        var log = await StartStep(upload.KycUploadId, "Check CKYC Status");

        var st = await _ckycStatus.GetInboundStatus(upload.RequestRef);

        if (st == null)
        {
            var notFoundJson = JsonSerializer.Serialize(new
            {
                requestRef = upload.RequestRef,
                status = "NotFound",
                statusMessage = "RequestRef not received by CKYC server yet.",
                receivedAtUtc = DateTimeOffset.UtcNow
            });

            _context.KycUpdationResponses.Add(new KycUpdationResponse
            {
                KycUploadId = upload.KycUploadId,
                RequestRef = upload.RequestRef,
                ResponseType = "CKYC_STATUS",
                FileName = "ckyc-status-response.json",
                ContentType = "application/json",
                ResponseJson = notFoundJson,
                ResponseHashSha256 = Sha256Hex(notFoundJson),
                CkycNumber = upload.CkycNumber,
                UpdateStatus = UpdateStatus.Failed,
                RejectionReason = "RequestRef not received by CKYC server yet.",
                ReceivedAtUtc = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
            await FinishStep(log, KycWorkflowStepStatus.Skipped, "CKYC status not found yet.");

            return new KycAutomationResult
            {
                Success = true,
                IsComplete = false,
                Message = "CKYC status not found yet."
            };
        }

        var responseObj = new
        {
            requestRef = upload.RequestRef,
            status = st.Status,
            statusMessage = st.StatusMessage,
            ckycNumber = st.CkycNumber,
            failureReason = st.FailureReason,
            receivedAtUtc = DateTimeOffset.UtcNow
        };

        var responseJson = JsonSerializer.Serialize(responseObj);

        var statusText = (st.Status ?? "").Trim();

        _context.KycUpdationResponses.Add(new KycUpdationResponse
        {
            KycUploadId = upload.KycUploadId,
            RequestRef = upload.RequestRef,
            ResponseType = "CKYC_STATUS",
            FileName = "ckyc-status-response.json",
            ContentType = "application/json",
            ResponseJson = responseJson,
            ResponseHashSha256 = Sha256Hex(responseJson),
            CkycNumber = string.IsNullOrWhiteSpace(st.CkycNumber) ? upload.CkycNumber : st.CkycNumber.Trim(),
            UpdateStatus = statusText.Equals("Success", StringComparison.OrdinalIgnoreCase)
                ? UpdateStatus.Success
                : UpdateStatus.Failed,
            RejectionReason = st.FailureReason,
            ReceivedAtUtc = DateTimeOffset.UtcNow
        });

        if (statusText.Equals("Success", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(st.CkycNumber))
        {
            upload.CkycNumber = st.CkycNumber.Trim();
            upload.CompletedAtUtc = DateTimeOffset.UtcNow;
            upload.Status = KycWorkflowStatus.Completed;

            await _context.SaveChangesAsync();
            await FinishStep(log, KycWorkflowStepStatus.Success, $"CKYC status success. CKYC Number: {upload.CkycNumber}");

            return new KycAutomationResult
            {
                Success = true,
                IsComplete = false,
                Message = "CKYC status success."
            };
        }

        if (statusText.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            upload.Status = KycWorkflowStatus.Failed;
            upload.FailureReason = st.FailureReason ?? st.StatusMessage ?? "Unknown CKYC failure";

            await _context.SaveChangesAsync();
            await FinishStep(log, KycWorkflowStepStatus.Failed, $"CKYC status failed: {upload.FailureReason}", upload.FailureReason);

            return new KycAutomationResult
            {
                Success = false,
                IsComplete = false,
                Message = $"CKYC status failed: {upload.FailureReason}"
            };
        }

        await _context.SaveChangesAsync();
        await FinishStep(log, KycWorkflowStepStatus.Skipped, $"CKYC status: {statusText}");

        return new KycAutomationResult
        {
            Success = true,
            IsComplete = false,
            Message = $"CKYC status: {statusText}"
        };
    }

    private async Task<bool> EnsurePushedToInternal(KycUploadDetails upload)
    {
        if (upload.Status == KycWorkflowStatus.KycDone)
            return true;

        var log = await StartStep(upload.KycUploadId, "Push To Internal");

        var hasDownload = upload.CkycDownloadedAtUtc != null;

        string ckycNumber = "";
        string firstName = "";
        string middleName = "";
        string lastName = "";
        string ppsn = "";
        DateOnly dob = upload.DateOfBirth;

        if (hasDownload)
        {
            var dec = await _context.DownloadResponseDecrypted
                .AsNoTracking()
                .Where(x => x.KycUploadId == upload.KycUploadId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (dec == null || string.IsNullOrWhiteSpace(dec.ResponseJson))
            {
                await FinishStep(log, KycWorkflowStepStatus.Failed, "No decrypted download response found.", "No decrypted download response found.");
                return false;
            }

            using var doc = JsonDocument.Parse(dec.ResponseJson);
            var root = doc.RootElement;

            string GetString(string name)
                => root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                    ? (p.GetString() ?? "")
                    : "";

            ckycNumber = GetString("ckycNumber").Trim();
            firstName = GetString("firstName").Trim();
            middleName = GetString("middleName").Trim();
            lastName = GetString("lastName").Trim();
            ppsn = GetString("ppsn").Trim();

            var dobStr = GetString("dateOfBirth").Trim();
            if (DateOnly.TryParse(dobStr, out var parsedDob))
                dob = parsedDob;

            if (string.IsNullOrWhiteSpace(ckycNumber))
            {
                await FinishStep(log, KycWorkflowStepStatus.Failed, "Downloaded CKYC payload does not contain ckycNumber.", "Downloaded CKYC payload does not contain ckycNumber.");
                return false;
            }

            upload.CkycNumber = ckycNumber;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(upload.CkycNumber))
            {
                await FinishStep(log, KycWorkflowStepStatus.Failed, "CKYC number not available yet.", "CKYC number not available yet.");
                return false;
            }

            ckycNumber = upload.CkycNumber.Trim();
            firstName = upload.FirstName?.Trim() ?? "";
            middleName = upload.MiddleName?.Trim() ?? "";
            lastName = upload.LastName?.Trim() ?? "";
            ppsn = upload.PPSN?.Trim() ?? "";
            dob = upload.DateOfBirth;
        }

        var existingCustomer = await _context.BankCustomerDetails
            .FirstOrDefaultAsync(c => c.CkycNumber == ckycNumber);

        BankCustomerDetails cust;

        if (existingCustomer != null)
        {
            cust = existingCustomer;
            cust.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            cust = new BankCustomerDetails
            {
                BankCustomerId = Guid.NewGuid(),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                IsActive = true
            };
            _context.BankCustomerDetails.Add(cust);
        }

        cust.KycUploadId = upload.KycUploadId;
        cust.CkycNumber = ckycNumber;

        cust.FirstName = string.IsNullOrWhiteSpace(firstName) ? upload.FirstName : firstName;
        cust.MiddleName = string.IsNullOrWhiteSpace(middleName) ? upload.MiddleName : middleName;
        cust.LastName = string.IsNullOrWhiteSpace(lastName) ? upload.LastName : lastName;
        cust.DateOfBirth = dob;

        cust.PPSN = string.IsNullOrWhiteSpace(ppsn) ? upload.PPSN : ppsn;

        cust.Email = upload.Email;
        cust.Phone = upload.Phone;
        cust.AddressLine1 = upload.AddressLine1;
        cust.CountyId = upload.CountyId;
        cust.CityId = upload.CityId;
        cust.Eircode = upload.Eircode;
        cust.Country = upload.Country;

        cust.Nationality = upload.Nationality;
        cust.Gender = upload.Gender;
        cust.Occupation = upload.Occupation;
        cust.EmployerName = upload.EmployerName;
        cust.SourceOfFunds = upload.SourceOfFunds;
        cust.IsPEP = upload.IsPEP;
        cust.RiskRating = upload.RiskRating;

        var existingHashes = await _context.CustomerImages
            .AsNoTracking()
            .Where(i => i.BankCustomerId == cust.BankCustomerId)
            .Select(i => i.FileHashSha256)
            .ToListAsync();

        var hashSet = new HashSet<string>(existingHashes, StringComparer.OrdinalIgnoreCase);

        foreach (var img in upload.Images)
        {
            if (img.ImageBytes == null || img.ImageBytes.Length == 0) continue;
            if (string.IsNullOrWhiteSpace(img.FileHashSha256)) continue;
            if (hashSet.Contains(img.FileHashSha256)) continue;

            _context.CustomerImages.Add(new CustomerImage
            {
                CustomerImageId = Guid.NewGuid(),
                BankCustomerId = cust.BankCustomerId,
                DocumentType = img.DocumentType,
                FileName = img.FileName,
                ContentType = string.IsNullOrWhiteSpace(img.ContentType) ? "application/octet-stream" : img.ContentType,
                FileSizeBytes = img.FileSizeBytes,
                FileHashSha256 = img.FileHashSha256,
                ImageBytes = img.ImageBytes,
                StoredAtUtc = DateTimeOffset.UtcNow
            });

            hashSet.Add(img.FileHashSha256);
        }

        upload.Status = KycWorkflowStatus.KycDone;

        await _context.SaveChangesAsync();
        await FinishStep(log, KycWorkflowStepStatus.Success, $"Pushed to bank records successfully. Customer CKYC: {ckycNumber}");
        return true;
    }

    private async Task<KycWorkflowExecutionLog> StartStep(Guid kycUploadId, string stepName)
    {
        var log = new KycWorkflowExecutionLog
        {
            KycUploadId = kycUploadId,
            StepName = stepName,
            Status = KycWorkflowStepStatus.Started,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        _context.KycWorkflowExecutionLogs.Add(log);
        await _context.SaveChangesAsync();

        return log;
    }

    private async Task FinishStep(
        KycWorkflowExecutionLog log,
        KycWorkflowStepStatus status,
        string? message,
        string? errorDetails = null)
    {
        log.Status = status;
        log.Message = message;
        log.ErrorDetails = errorDetails;
        log.CompletedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static (string Forename, string Surname) ExtractPscNameParts(string ocrText)
    {
        var t = ocrText ?? "";
        var cleaned = new string(t.Select(ch => (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) || ch == '|') ? ch : ' ').ToArray());

        string afterLabel(string label)
        {
            var idx = cleaned.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return "";
            var start = idx + label.Length;
            if (start >= cleaned.Length) return "";
            var rest = cleaned[start..];

            var stopWords = new[]
            {
                "|", "TUSAINM", "SLOINNE", "FORENAME", "SURNAME", "DATA", "DATE", "CARTE", "PUBLIC", "SERVICES"
            };

            var cutPos = rest.Length;

            foreach (var w in stopWords)
            {
                var p = rest.IndexOf(w, StringComparison.OrdinalIgnoreCase);
                if (p >= 0) cutPos = Math.Min(cutPos, p);
            }

            rest = rest[..cutPos];
            return string.Join(' ', rest.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        var surname = afterLabel("SURNAME");
        var forename = afterLabel("FORENAME");

        return (forename, surname);
    }

    private static string NormalizeForMatch(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ') sb.Append(ch);
            else sb.Append(' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Trim()
            .ToUpperInvariant();
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

    private static KycAutomationResult Fail(string message)
    {
        return new KycAutomationResult
        {
            Success = false,
            IsComplete = false,
            Message = message
        };
    }
}