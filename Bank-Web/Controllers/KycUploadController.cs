using Bank.Web.Data;
using Bank.Web.Models;
using Bank.Web.Services;
using Bank.Web.ViewModels;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bank.Web.Controllers;

public class KycUploadController : Controller
{
    private readonly BankDbContext _context;
    private readonly CkycApiClient _ckyc;
    private readonly PscOcrService _pscOcr;
    private readonly AesGcmCryptoService _aes;
    private readonly CkycStatusClient _ckycStatus;

    public KycUploadController(BankDbContext context, CkycApiClient ckyc, PscOcrService pscOcr, AesGcmCryptoService aes, CkycStatusClient ckycStatus)
    {
        _context = context;
        _ckyc = ckyc;
        _pscOcr = pscOcr;
        _aes = aes;
        _ckycStatus = ckycStatus;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new KycUploadCreateVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KycUploadCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(vm);
        }

        if (vm.PscFront == null || vm.PscFront.Length == 0 || vm.PscBack == null || vm.PscBack.Length == 0)
        {
            ModelState.AddModelError("", "PSC Front and PSC Back are required.");
            await LoadDropdowns();
            return View(vm);
        }

        var dob = DateOnly.FromDateTime(vm.DateOfBirth);

        var requestRef = $"REQ-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        if (requestRef.Length > 60) requestRef = requestRef[..60];

        var firstName = (vm.FirstName ?? "").Trim();
        var middleName = (vm.MiddleName ?? "").Trim();
        var lastName = (vm.LastName ?? "").Trim();
        var ppsn = (vm.PPSN ?? "").Trim();

        var identityHash = Sha256Hex($"{firstName}|{middleName}|{lastName}|{dob:yyyy-MM-dd}|{ppsn}");

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
            Email = (vm.Email ?? "").Trim(),
            Phone = (vm.Phone ?? "").Trim(),
            AddressLine1 = (vm.AddressLine1 ?? "").Trim(),

            CountyId = vm.CountyId,
            CityId = vm.CityId,

            Eircode = (vm.Eircode ?? "").Trim(),
            Country = "Ireland",

            Nationality = "Ireland",
            Gender = "NA",
            Occupation = "NA",
            EmployerName = "NA",
            SourceOfFunds = "NA",
            IsPEP = vm.IsPEP,
            RiskRating = vm.RiskRating,

            SearchExecuted = false,
            SearchFound = null,
            CkycDownloadedAtUtc = null,
            CkycNumber = null,

            DedupeExecuted = false,
            DedupePassed = null,
            DedupeCheckedAtUtc = null,
            DedupeMessage = null
        };

        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "application/pdf" };
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".pdf" };
        const long maxFileBytes = 10 * 1024 * 1024;

        async Task<(bool ok, string err, byte[] bytes, string contentType, string fileName, long size)> ReadAndValidate(IFormFile file)
        {
            if (file == null || file.Length == 0) return (false, "File is required.", Array.Empty<byte>(), "", "", 0);

            if (file.Length > maxFileBytes)
                return (false, $"File '{file.FileName}' is too large (max 10MB).", Array.Empty<byte>(), "", "", 0);

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !allowedExtensions.Contains(ext))
                return (false, $"File '{file.FileName}' must be JPG, PNG, or PDF.", Array.Empty<byte>(), "", "", 0);

            var contentType = file.ContentType ?? "";
            if (!allowedContentTypes.Contains(contentType))
                return (false, $"File '{file.FileName}' must be JPG, PNG, or PDF.", Array.Empty<byte>(), "", "", 0);

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            return (true, "", bytes, contentType, Path.GetFileName(file.FileName), file.Length);
        }

        var front = await ReadAndValidate(vm.PscFront);
        if (!front.ok)
        {
            ModelState.AddModelError("", "PSC Front: " + front.err);
            await LoadDropdowns();
            return View(vm);
        }

        var back = await ReadAndValidate(vm.PscBack);
        if (!back.ok)
        {
            ModelState.AddModelError("", "PSC Back: " + back.err);
            await LoadDropdowns();
            return View(vm);
        }

        upload.Images.Add(new KycUploadImage
        {
            DocumentType = DocumentType.PSCFront,
            FileName = front.fileName,
            ContentType = front.contentType,
            FileSizeBytes = front.size,
            FileHashSha256 = Sha256Hex(front.bytes),
            ImageBytes = front.bytes,
            OcrText = "",
            OcrConfidence = 0,
            IsImageValidated = false,
            ImageValidationMessage = null,
            ImageValidatedAtUtc = null
        });

        upload.Images.Add(new KycUploadImage
        {
            DocumentType = DocumentType.PSCBack,
            FileName = back.fileName,
            ContentType = back.contentType,
            FileSizeBytes = back.size,
            FileHashSha256 = Sha256Hex(back.bytes),
            ImageBytes = back.bytes,
            OcrText = "",
            OcrConfidence = 0,
            IsImageValidated = false,
            ImageValidationMessage = null,
            ImageValidatedAtUtc = null
        });

        _context.KycUploadDetails.Add(upload);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = upload.KycUploadId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .Include(x => x.County)
            .Include(x => x.City)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();
        return View(upload);
    }

    [HttpGet]
    public async Task<IActionResult> Document(Guid id)
    {
        var doc = await _context.KycUploadImages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KycUploadImageId == id);

        if (doc == null) return NotFound();

        var contentType = string.IsNullOrWhiteSpace(doc.ContentType) ? "application/octet-stream" : doc.ContentType;
        return File(doc.ImageBytes, contentType, doc.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidatePscFront(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        var front = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCFront);
        if (front == null)
        {
            TempData["Error"] = "PSC Front not found.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ocr = _pscOcr.ReadText(front.ImageBytes);
        front.OcrText = ocr.Text ?? "";
        front.OcrConfidence = ocr.Confidence;

        var (forenameRaw, surnameRaw) = ExtractPscNameParts(front.OcrText);

        var expectedForename = NormalizeForMatch($"{upload.FirstName} {upload.MiddleName}".Trim());
        var expectedSurname = NormalizeForMatch($"{upload.LastName}".Trim());

        var extractedForename = NormalizeForMatch(forenameRaw);
        var extractedSurname = NormalizeForMatch(surnameRaw);

        var ok = extractedForename.Contains(expectedForename) && extractedSurname.Contains(expectedSurname);

        front.IsImageValidated = ok;
        front.ImageValidatedAtUtc = DateTimeOffset.UtcNow;

        var expectedFull = $"{upload.FirstName} {upload.MiddleName} {upload.LastName}".Replace("  ", " ").Trim();
        var extractedFull = $"{forenameRaw} {surnameRaw}".Replace("  ", " ").Trim();

        front.ImageValidationMessage = ok
            ? $"PSC Front validated. Name matched: {expectedFull}"
            : $"PSC Front validation failed. Expected name: {expectedFull}. OCR name extracted: '{extractedFull}'. Full OCR: '{front.OcrText}'";

        await _context.SaveChangesAsync();

        TempData[ok ? "Info" : "Error"] = front.ImageValidationMessage;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidatePscBack(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        var back = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCBack);
        if (back == null)
        {
            TempData["Error"] = "PSC Back not found.";
            return RedirectToAction(nameof(Details), new { id });
        }

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

        await _context.SaveChangesAsync();

        TempData[ok ? "Info" : "Error"] = back.ImageValidationMessage;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckDedupe(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        var frontOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCFront && x.IsImageValidated);
        var backOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCBack && x.IsImageValidated);

        if (!frontOk || !backOk)
        {
            TempData["Error"] = "Please validate PSC Front and PSC Back before dedupe check.";
            return RedirectToAction(nameof(Details), new { id });
        }

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
        TempData["Info"] = upload.DedupeMessage;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchCkyc(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        var frontOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCFront && x.IsImageValidated);
        var backOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCBack && x.IsImageValidated);

        if (!frontOk || !backOk)
        {
            TempData["Error"] = "Validate PSC Front and PSC Back first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (upload.DedupeExecuted != true || upload.DedupePassed != false)
        {
            TempData["Error"] = "CKYC Search is only allowed after Dedupe FAIL.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var requestObj = new
        {
            firstName = upload.FirstName,
            middleName = upload.MiddleName,
            lastName = upload.LastName,
            dateOfBirth = upload.DateOfBirth.ToString("yyyy-MM-dd"),
            ppsn = upload.PPSN
        };

        var requestBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(requestObj);
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

        var responseBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(responseObj);
        var encryptedResponseBytes = _aes.Encrypt(responseBytes);
        var responseHash = Sha256Hex(responseBytes);

        _context.SearchResponseEncrypted.Add(new SearchResponseEncrypted
        {
            KycUploadId = upload.KycUploadId,
            RequestHashSha256 = requestHash,
            ResponseHashSha256 = responseHash,
            EncryptedRequestBytes = encryptedRequestBytes,
            EncryptedResponseBytes = encryptedResponseBytes,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        if (!ok)
        {
            await _context.SaveChangesAsync();
            TempData["Error"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }

        upload.SearchExecuted = true;
        upload.SearchFound = found;
        upload.CkycDownloadedAtUtc = null;

        if (found && !string.IsNullOrWhiteSpace(ckycNumber))
        {
            upload.CkycNumber = ckycNumber;
            TempData["Info"] = $"CKYC record found. CKYC Number: {ckycNumber}";
        }
        else
        {
            upload.CkycNumber = null;
            TempData["Info"] = "No CKYC record found. You can upload this record to CKYC.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet]
    public async Task<IActionResult> ViewLatestSearchDecrypted(Guid id)
    {
        var row = await _context.SearchResponseEncrypted
            .AsNoTracking()
            .Where(x => x.KycUploadId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (row == null)
            return Content("No encrypted search response found for this upload.");

        var reqPlain = _aes.Decrypt(row.EncryptedRequestBytes);
        var respPlain = _aes.Decrypt(row.EncryptedResponseBytes);

        var reqText = Encoding.UTF8.GetString(reqPlain);
        var respText = Encoding.UTF8.GetString(respPlain);

        return Content(
            $"--- Decrypted Request ---\n{reqText}\n\n--- Decrypted Response ---\n{respText}",
            "text/plain"
        );
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadFromCkyc(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        if (upload.SearchExecuted != true || upload.SearchFound != true || string.IsNullOrWhiteSpace(upload.CkycNumber))
        {
            TempData["Error"] = "Download is only available after Search FOUND a CKYC record.";
            return RedirectToAction(nameof(Details), new { id });
        }

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

        upload.CkycDownloadedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Info"] = "CKYC record downloaded and stored as ENCRYPTED response. Use Decrypt button to view/save decrypted.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DecryptLatestDownloadResponse(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        if (string.IsNullOrWhiteSpace(upload.CkycNumber))
        {
            TempData["Error"] = "No CKYC Number found. Download must be completed first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var enc = await _context.DownloadResponseEncrypted
            .AsNoTracking()
            .Where(x => x.KycUploadId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (enc == null)
        {
            TempData["Error"] = "No encrypted download response found for this upload.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!string.Equals(enc.CkycNumber?.Trim(), upload.CkycNumber.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Mismatch: encrypted CKYC number does not match current upload CKYC number.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (enc.EncryptedResponseBytes == null || enc.EncryptedResponseBytes.Length == 0)
        {
            TempData["Error"] = "Encrypted response bytes are empty.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var alreadyDecrypted = await _context.DownloadResponseDecrypted
            .AsNoTracking()
            .AnyAsync(x => x.KycUploadId == id && x.ResponseHashSha256 == enc.ResponseHashSha256);

        if (alreadyDecrypted)
        {
            TempData["Info"] = "Latest download response is already decrypted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        string decryptedJson;
        try
        {
            var plainBytes = _aes.Decrypt(enc.EncryptedResponseBytes);
            decryptedJson = Encoding.UTF8.GetString(plainBytes);

            var recomputedHash = Sha256Hex(plainBytes);
            if (!string.Equals(recomputedHash, enc.ResponseHashSha256, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Hash validation failed after decrypt (integrity check failed).";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Decrypt failed: " + ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.DownloadResponseDecrypted.Add(new DownloadResponseDecrypted
        {
            KycUploadId = id,
            CkycNumber = upload.CkycNumber.Trim(),
            ResponseHashSha256 = enc.ResponseHashSha256,
            ResponseJson = decryptedJson,
            PayloadJson = decryptedJson,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Info"] = "Download response decrypted and saved successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateZip(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        var frontOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCFront && x.IsImageValidated);
        var backOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCBack && x.IsImageValidated);

        if (!frontOk || !backOk)
        {
            TempData["Error"] = "Validate PSC Front and PSC Back before generating ZIP.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var zipBytes = ZipBuilder.BuildKycZip(upload);
        if (zipBytes == null || zipBytes.Length == 0)
        {
            TempData["Error"] = "ZIP generation failed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var zipFileName = $"{upload.RequestRef}.zip";
        var zipHash = Sha256Hex(zipBytes);

        var existing = await _context.ZipFileUploadDetails
            .FirstOrDefaultAsync(z => z.KycUploadId == upload.KycUploadId);

        if (existing == null)
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
            existing.ZipFileName = zipFileName;
            existing.ZipHashSha256 = zipHash;
            existing.ZipSizeBytes = zipBytes.LongLength;
            existing.ZipBytes = zipBytes;
            existing.SftpRemotePath = null;
            existing.UploadStatus = 0;
            existing.UploadedAtUtc = DateTime.UtcNow;
            existing.FailureReason = null;
        }

        upload.Status = KycWorkflowStatus.Zipped;

        await _context.SaveChangesAsync();

        return File(zipBytes, "application/zip", zipFileName);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendZipToSftp(Guid id, [FromServices] Bank.Web.Services.SftpZipUploader sftp)
    {
        var upload = await _context.KycUploadDetails
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        var zipRow = await _context.ZipFileUploadDetails
            .FirstOrDefaultAsync(z => z.KycUploadId == id);

        if (zipRow == null)
        {
            TempData["Error"] = "ZIP is not generated yet. Generate ZIP first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (zipRow.ZipBytes == null || zipRow.ZipBytes.Length == 0)
        {
            TempData["Error"] = "ZIP bytes are empty. Generate ZIP again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var (ok, remotePath, error) = await sftp.UploadBytesAsync(zipRow.ZipBytes, zipRow.ZipFileName);

        if (ok)
        {
            zipRow.SftpRemotePath = remotePath;
            zipRow.UploadStatus = 1;
            zipRow.FailureReason = null;
            zipRow.UploadedAtUtc = DateTime.UtcNow;


            upload.SubmittedAtUtc = DateTimeOffset.UtcNow;
            upload.Status = KycWorkflowStatus.Sent;
            await _context.SaveChangesAsync();

            TempData["Info"] = $"ZIP uploaded to SFTP: {remotePath}";
            return RedirectToAction(nameof(Details), new { id });
        }
        else
        {
            zipRow.UploadStatus = 2;
            zipRow.FailureReason = error;
            zipRow.UploadedAtUtc = DateTime.UtcNow;

            upload.SubmittedAtUtc = DateTimeOffset.UtcNow;
            upload.Status = KycWorkflowStatus.Failed;
            await _context.SaveChangesAsync();

            TempData["Error"] = "SFTP upload failed: " + error;
            return RedirectToAction(nameof(Details), new { id });
        }

    }



    

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckCkycStatus(Guid id)
    {
        var upload = await _context.KycUploadDetails.FirstOrDefaultAsync(x => x.KycUploadId == id);
        if (upload == null) return NotFound();

        
        if (upload.Status != KycWorkflowStatus.Sent)
        {
            TempData["Error"] = "You can only check status after uploading to CKYC.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var st = await _ckycStatus.GetInboundStatus(upload.RequestRef);

        if (st == null)
        {
            TempData["Info"] = "CKYC status: Not found (RequestRef not received by CKYC server yet).";
            return RedirectToAction(nameof(Details), new { id });
        }

        var status = (st.Status ?? "").Trim();

        if (status.Equals("Success", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(st.CkycNumber))
        {
            upload.CkycNumber = st.CkycNumber.Trim();
            upload.CompletedAtUtc = DateTimeOffset.UtcNow;
            upload.Status = KycWorkflowStatus.Completed;

            await _context.SaveChangesAsync();
            TempData["Info"] = $"CKYC Success. CKYC Number: {upload.CkycNumber}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Failed
        if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = $"CKYC status: Failed - {(st.FailureReason ?? st.StatusMessage ?? "Unknown error")}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Still processing / received
        TempData["Info"] = $"CKYC status: {status} - {(st.StatusMessage ?? "")}".Trim();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PushToInternal(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

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
                .Where(x => x.KycUploadId == id)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (dec == null || string.IsNullOrWhiteSpace(dec.ResponseJson))
            {
                TempData["Error"] = "No decrypted download response found. Click 'Decrypt Download' first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var doc = JsonDocument.Parse(dec.ResponseJson);
            var root = doc.RootElement;

            string GetString(string name)
                => root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? (p.GetString() ?? "") : "";

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
                TempData["Error"] = "Decrypted download JSON does not contain 'ckycNumber'. Cannot push.";
                return RedirectToAction(nameof(Details), new { id });
            }

            upload.CkycNumber = ckycNumber;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(upload.CkycNumber))
            {
                TempData["Error"] = "CKYC number not available yet. Please click 'Check CKYC Status' first, then push to bank records.";
                return RedirectToAction(nameof(Details), new { id });
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

        TempData["Info"] = $"Pushed to bank records successfully. Customer CKYC: {ckycNumber}";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadDropdowns()
    {
        ViewBag.Counties = await _context.Counties.Where(c => c.IsActive).OrderBy(c => c.CountyName).ToListAsync();
        ViewBag.Cities = await _context.Cities.Where(c => c.IsActive).OrderBy(c => c.CityName).ToListAsync();
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
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim().ToUpperInvariant();
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
