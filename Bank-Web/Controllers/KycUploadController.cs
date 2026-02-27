using Bank.Web.Data;
using Bank.Web.Models;
using Bank.Web.Services;
using Bank.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bank.Web.Controllers;

public class KycUploadController : Controller
{
    private readonly BankDbContext _context;
    private readonly CkycApiClient _ckyc;

    public KycUploadController(BankDbContext context, CkycApiClient ckyc)
    {
        _context = context;
        _ckyc = ckyc;
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

        if (vm.Documents == null || vm.Documents.Count == 0)
        {
            ModelState.AddModelError("", "Please upload at least one document.");
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
            CkycNumber = null
        };

        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "application/pdf"
        };

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".pdf"
        };

        const long maxFileBytes = 10 * 1024 * 1024;

        foreach (var file in vm.Documents)
        {
            if (file == null || file.Length == 0) continue;

            if (file.Length > maxFileBytes)
            {
                ModelState.AddModelError("", $"File '{file.FileName}' is too large (max 10MB).");
                await LoadDropdowns();
                return View(vm);
            }

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("", $"File '{file.FileName}' must be JPG, PNG, or PDF.");
                await LoadDropdowns();
                return View(vm);
            }

            var contentType = file.ContentType ?? "";
            if (!allowedContentTypes.Contains(contentType))
            {
                ModelState.AddModelError("", $"File '{file.FileName}' must be JPG, PNG, or PDF.");
                await LoadDropdowns();
                return View(vm);
            }

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var fileHash = Sha256Hex(bytes);

            upload.Images.Add(new KycUploadImage
            {
                DocumentType = DocumentType.Other,
                FileName = Path.GetFileName(file.FileName),
                ContentType = contentType,
                FileSizeBytes = file.Length,
                FileHashSha256 = fileHash,
                ImageBytes = bytes,
                OcrText = "",
                OcrConfidence = 0
            });
        }

        if (upload.Images.Count == 0)
        {
            ModelState.AddModelError("", "No valid documents were uploaded. Please upload JPG/PNG/PDF files.");
            await LoadDropdowns();
            return View(vm);
        }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchCkyc(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .Include(x => x.County)
            .Include(x => x.City)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        var (ok, message, found, ckycNumber) = await _ckyc.SearchAsync(
            upload.FirstName,
            upload.LastName,
            upload.DateOfBirth,
            upload.PPSN);

        if (!ok)
        {
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadFromCkyc(Guid id)
    {
        var upload = await _context.KycUploadDetails.FirstOrDefaultAsync(x => x.KycUploadId == id);
        if (upload == null) return NotFound();

        if (upload.SearchExecuted != true || upload.SearchFound != true || string.IsNullOrWhiteSpace(upload.CkycNumber))
        {
            TempData["Error"] = "Download is only available after Search FOUND a CKYC record.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _ = await _ckyc.DownloadProfileJsonAsync(upload.CkycNumber);

        upload.CkycDownloadedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Info"] = "CKYC record downloaded successfully. You can now push it to internal bank records.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitToCkyc(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .Include(x => x.County)
            .Include(x => x.City)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        if (upload.SearchExecuted != true || upload.SearchFound != false)
        {
            TempData["Error"] = "You can only upload to CKYC after running Search and getting NOT FOUND.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var zipBytes = ZipBuilder.BuildKycZip(upload);

        var requestRef = upload.RequestRef;
        var bankCode = "BANKDEMO";

        var (ok, message, submissionRef) = await _ckyc.UploadZipAsync(
            zipBytes,
            zipFileName: $"{requestRef}.zip",
            bankCode: bankCode,
            requestRef: requestRef);

        if (!ok)
        {
            TempData["Error"] = "CKYC Upload failed: " + message;
            return RedirectToAction(nameof(Details), new { id });
        }

        upload.SubmittedAtUtc = DateTimeOffset.UtcNow;
        upload.Status = KycWorkflowStatus.Sent;
        await _context.SaveChangesAsync();

        TempData["Info"] = $"Sent to CKYC. Tracking: {submissionRef}";
        return RedirectToAction(nameof(Details), new { id });
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

        var (status, message, ckycNumber) = await _ckyc.GetStatusAsync(upload.RequestRef);

        TempData["Info"] = $"CKYC status: {status} - {message}";

        if (status.Equals("Success", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ckycNumber))
        {
            upload.CkycNumber = ckycNumber;
            upload.CompletedAtUtc = DateTimeOffset.UtcNow;
            upload.Status = KycWorkflowStatus.Completed;
            await _context.SaveChangesAsync();
            TempData["Info"] = $"CKYC Success. CKYC Number: {ckycNumber}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PushToInternal(Guid id)
    {
        var upload = await _context.KycUploadDetails
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.KycUploadId == id);

        if (upload == null) return NotFound();

        if (string.IsNullOrWhiteSpace(upload.CkycNumber))
        {
            TempData["Error"] = "No CKYC Number available to push.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (upload.SearchFound == true && upload.CkycDownloadedAtUtc == null)
        {
            TempData["Error"] = "Please download the CKYC record first, then push to bank records.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (upload.SearchFound == false && upload.Status != KycWorkflowStatus.Completed)
        {
            TempData["Error"] = "Please wait for CKYC Status = Success before pushing to bank records.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Info"] = "Pushed to internal bank records successfully (placeholder).";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadDropdowns()
    {
        ViewBag.Counties = await _context.Counties
            .Where(c => c.IsActive)
            .OrderBy(c => c.CountyName)
            .ToListAsync();

        ViewBag.Cities = await _context.Cities
            .Where(c => c.IsActive)
            .OrderBy(c => c.CityName)
            .ToListAsync();
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