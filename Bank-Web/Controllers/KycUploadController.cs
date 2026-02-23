using Bank.Web.Data;
using Bank.Web.Models;
using Bank.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bank.Web.Controllers;

public class KycUploadController : Controller
{
    private readonly BankDbContext _context;

    public KycUploadController(BankDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Counties = await _context.Counties
            .Where(c => c.IsActive)
            .OrderBy(c => c.CountyName)
            .ToListAsync();

        ViewBag.Cities = await _context.Cities
            .Where(c => c.IsActive)
            .OrderBy(c => c.CityName)
            .ToListAsync();

        return View(new KycUploadCreateVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KycUploadCreateVm vm)
    {
        async Task LoadDropdowns()
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

        // Convert UI DateTime -> DateOnly for database
        var dob = DateOnly.FromDateTime(vm.DateOfBirth);

        // Unique request reference
        var requestRef = $"REQ-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        if (requestRef.Length > 60) requestRef = requestRef[..60];

        // Identity hash (demo dedupe key)
        var identityHash = Sha256Hex($"{vm.FirstName}|{vm.LastName}|{dob:yyyy-MM-dd}|{vm.PPSN}");

        var upload = new KycUploadDetails
        {
            RequestRef = requestRef,
            Source = UploadSource.ManualForm,

            Status = KycWorkflowStatus.Draft,
            OcrStatus = OcrStatus.Pending,
            ValidationStatus = ValidationStatus.Pass,
            DedupeStatus = DedupeStatus.NotChecked,

            IdentityHash = identityHash,

            FirstName = vm.FirstName.Trim(),
            LastName = vm.LastName.Trim(),
            DateOfBirth = dob,

            PPSN = vm.PPSN.Trim(),
            Email = vm.Email.Trim(),
            Phone = vm.Phone.Trim(),
            AddressLine1 = vm.AddressLine1.Trim(),

            CountyId = vm.CountyId,
            CityId = vm.CityId,

            Eircode = vm.Eircode.Trim(),
            Country = "Ireland",

            // Defaults for fields not in UI yet
            Nationality = "Ireland",
            Gender = "NA",
            Occupation = "NA",
            EmployerName = "NA",
            SourceOfFunds = "NA",
            IsPEP = vm.IsPEP,
            RiskRating = vm.RiskRating
        };

        // ✅ Server-side file validation
        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "application/pdf"
        };

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".pdf"
        };

        const long maxFileBytes = 10 * 1024 * 1024; // 10MB (demo rule)

        foreach (var file in vm.Documents)
        {
            if (file == null || file.Length == 0) continue;

            // File size validation
            if (file.Length > maxFileBytes)
            {
                ModelState.AddModelError("", $"File '{file.FileName}' is too large (max 10MB).");
                await LoadDropdowns();
                return View(vm);
            }

            // Extension validation (quick sanity check)
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("", $"File '{file.FileName}' must be JPG, PNG, or PDF.");
                await LoadDropdowns();
                return View(vm);
            }

            // Content-Type validation (primary)
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

            // Optional: validate the actual bytes for images (light check)
            // (Skipping deep validation for demo)

            var fileHash = Sha256Hex(bytes);

            upload.Images.Add(new KycUploadImage
            {
                DocumentType = DocumentType.Other, // improve later (per-file doc type)
                FileName = Path.GetFileName(file.FileName),
                ContentType = contentType,
                FileSizeBytes = file.Length,
                FileHashSha256 = fileHash,
                ImageBytes = bytes,
                OcrText = "",
                OcrConfidence = 0
            });
        }

        // Ensure at least 1 valid file made it through validation
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