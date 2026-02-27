using CKYC.Server.Data;
using CKYC.Server.DTOs;
using CKYC.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CKYC.Server.Controllers;

[ApiController]
[Route("api/ckyc")]
public class CkycController : ControllerBase
{
    private readonly CkycDbContext _db;

    public CkycController(CkycDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<ActionResult<CkycSearchResponse>> Search([FromBody] CkycSearchRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var firstName = (req.FirstName ?? "").Trim();
        var middleName = (req.MiddleName ?? "").Trim();
        var lastName = (req.LastName ?? "").Trim();
        var ppsn = (req.PPSN ?? "").Trim();

        if (firstName.Length == 0 || middleName.Length == 0 || lastName.Length == 0 || ppsn.Length == 0)
            return BadRequest(new { message = "FirstName, MiddleName, LastName, DateOfBirth and PPSN are required." });

        var fn = firstName.ToUpperInvariant();
        var mn = middleName.ToUpperInvariant();
        var ln = lastName.ToUpperInvariant();
        var pn = ppsn.ToUpperInvariant();

        var ckycNumber = await _db.CkycProfiles
            .AsNoTracking()
            .Where(p =>
                p.DateOfBirth == req.DateOfBirth &&
                p.FirstName.ToUpper() == fn &&
                p.MiddleName.ToUpper() == mn &&
                p.LastName.ToUpper() == ln &&
                p.PPSN.ToUpper() == pn
            )
            .Select(p => p.CkycNumber)
            .FirstOrDefaultAsync();

        if (ckycNumber == null)
        {
            return Ok(new CkycSearchResponse
            {
                Found = false,
                Message = "No match found.",
                CkycNumber = null
            });
        }

        return Ok(new CkycSearchResponse
        {
            Found = true,
            Message = "Match found.",
            CkycNumber = ckycNumber
        });
    }

    [HttpGet("profiles/{ckycNumber}")]
    public async Task<ActionResult<CkycProfileDto>> DownloadProfile(string ckycNumber)
    {
        ckycNumber = (ckycNumber ?? "").Trim();
        if (ckycNumber.Length == 0)
            return BadRequest(new { message = "CKYC number is required." });

        var profile = await _db.CkycProfiles
            .AsNoTracking()
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.CkycNumber == ckycNumber);

        if (profile == null)
            return NotFound(new { message = "CKYC profile not found." });

        var dto = new CkycProfileDto
        {
            CkycProfileId = profile.CkycProfileId,
            CkycNumber = profile.CkycNumber,

            FirstName = profile.FirstName,
            MiddleName = profile.MiddleName,
            LastName = profile.LastName,
            DateOfBirth = profile.DateOfBirth,
            PPSN = profile.PPSN,

            Nationality = profile.Nationality,
            Gender = profile.Gender,

            Email = profile.Email,
            Phone = profile.Phone,

            AddressLine1 = profile.AddressLine1,
            City = profile.City,
            County = profile.County,
            Eircode = profile.Eircode,
            Country = profile.Country,

            Occupation = profile.Occupation,
            EmployerName = profile.EmployerName,
            SourceOfFunds = profile.SourceOfFunds,

            IsPEP = profile.IsPEP,
            RiskRating = (short)profile.RiskRating,

            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,

            Documents = profile.Documents.Select(d => new CkycDocumentDto
            {
                CkycDocumentId = d.CkycDocumentId,
                DocumentType = (short)d.DocumentType,

                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSizeBytes = d.FileSizeBytes,
                FileHashSha256 = d.FileHashSha256,

                OcrText = d.OcrText,
                OcrConfidence = d.OcrConfidence,

                ExtractedDocNumber = d.ExtractedDocNumber,
                ExtractedExpiryDate = d.ExtractedExpiryDate,

                UploadedAtUtc = d.UploadedAtUtc
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost("uploads/zip")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<CkycZipUploadReceiptDto>> UploadZip([FromForm] CkycZipUploadRequestDto req)
    {
        if (req.ZipFile == null || req.ZipFile.Length == 0)
            return BadRequest(new { message = "zipFile is required." });

        if (!Path.GetExtension(req.ZipFile.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .zip files are allowed." });

        var bankCode = (req.BankCode ?? "").Trim();
        var requestRef = (req.RequestRef ?? "").Trim();

        if (bankCode.Length == 0) return BadRequest(new { message = "bankCode is required." });
        if (requestRef.Length == 0) return BadRequest(new { message = "requestRef is required." });

        var exists = await _db.InboundSubmissions.AnyAsync(s => s.RequestRef == requestRef);
        if (exists)
            return Conflict(new { message = $"requestRef '{requestRef}' already exists." });

        byte[] zipBytes;
        await using (var ms = new MemoryStream())
        {
            await req.ZipFile.CopyToAsync(ms);
            zipBytes = ms.ToArray();
        }

        var zipHash = Sha256Hex(zipBytes);

        var submission = new InboundSubmission
        {
            InboundSubmissionId = Guid.NewGuid(),
            BankCode = bankCode,
            RequestRef = requestRef,
            ReceivedAtUtc = DateTime.UtcNow,
            Status = SubmissionStatus.Received,
            StatusMessage = "ZIP received and queued for processing."
        };

        var pkg = new InboundPackage
        {
            InboundPackageId = Guid.NewGuid(),
            InboundSubmissionId = submission.InboundSubmissionId,
            FileName = Path.GetFileName(req.ZipFile.FileName),
            ContentType = req.ZipFile.ContentType ?? "application/zip",
            FileSizeBytes = zipBytes.LongLength,
            FileHashSha256 = zipHash,
            ZipBytes = zipBytes,
            UploadedAtUtc = DateTime.UtcNow
        };

        submission.Packages.Add(pkg);

        _db.InboundSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        return Ok(new CkycZipUploadReceiptDto
        {
            Received = true,
            Message = "ZIP received successfully.",
            SubmissionRef = requestRef
        });
    }

    [HttpGet("uploads/{requestRef}/status")]
    public async Task<ActionResult<CkycUploadStatusDto>> UploadStatus(string requestRef)
    {
        requestRef = (requestRef ?? "").Trim();
        if (requestRef.Length == 0)
            return BadRequest(new { message = "requestRef is required." });

        var submission = await _db.InboundSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RequestRef == requestRef);

        if (submission == null)
            return NotFound(new { message = "Submission not found." });

        string msg;
        if (submission.Status == SubmissionStatus.Failed && !string.IsNullOrWhiteSpace(submission.FailureReason))
        {
            msg = submission.FailureReason!;
        }
        else if (!string.IsNullOrWhiteSpace(submission.StatusMessage))
        {
            msg = submission.StatusMessage!;
        }
        else if (!string.IsNullOrWhiteSpace(submission.FailureReason))
        {
            msg = submission.FailureReason!;
        }
        else
        {
            msg = "";
        }

        return Ok(new CkycUploadStatusDto
        {
            RequestRef = submission.RequestRef,
            Status = submission.Status.ToString(),
            Message = msg,
            CkycNumber = submission.CkycNumber
        });
    }

    [HttpGet("documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid documentId)
    {
        var doc = await _db.CkycDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.CkycDocumentId == documentId);

        if (doc == null)
            return NotFound(new { message = "Document not found." });

        return File(doc.FileBytes, doc.ContentType, doc.FileName);
    }

    private static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}