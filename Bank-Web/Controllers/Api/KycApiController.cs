using Microsoft.AspNetCore.Mvc;
using Bank.Web.Data;
using Bank.Web.Models;
using Bank.Web.Services;
using Bank.Web.Services.Automation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bank_Web.Controllers.Api
{
    [ApiController]
    [Route("api/kyc")]
    public class KycApiController : ControllerBase
    {
        private readonly BankDbContext _context;
        private readonly CkycApiClient _ckyc;
        private readonly AesGcmCryptoService _aes;
        private readonly PscOcrService _pscOcr;
        private readonly CkycStatusClient _ckycStatus;
        private readonly KycAutomationService _automationService;

        public KycApiController(BankDbContext context, CkycApiClient ckyc, AesGcmCryptoService aes, PscOcrService pscOcr, CkycStatusClient ckycStatus, KycAutomationService automationService)
        {
            _context = context;
            _ckyc = ckyc;
            _aes = aes;
            _pscOcr = pscOcr;
            _ckycStatus = ckycStatus;
            _automationService = automationService;
        }

        public class CreateKycRequest
        {
            public string? FirstName { get; set; }
            public string? MiddleName { get; set; }
            public string? LastName { get; set; }
            public string? DateOfBirth { get; set; }
            public string? PpsNumber { get; set; }
            public string? EmailAddress { get; set; }
            public string? PhoneNumber { get; set; }

            // keep both old and new names so current React payload does not break
            public string? AddressLine1 { get; set; }
            public string? Address { get; set; }

            public string? CountyName { get; set; }
            public string? County { get; set; }

            public string? CityName { get; set; }
            public string? City { get; set; }

            public string? Eircode { get; set; }
            public bool IsPEP { get; set; }
            public string? RiskRating { get; set; }

            public IFormFile? PscFront { get; set; }
            public IFormFile? PscBack { get; set; }
        }
        public class WorkflowLogItem
        {
            public string StepName { get; set; } = "";
            public string Status { get; set; } = "";
            public string? Message { get; set; }
            public string? ErrorDetails { get; set; }
            public DateTimeOffset StartedAtUtc { get; set; }
            public DateTimeOffset? CompletedAtUtc { get; set; }
        }

        public class SavedKycRecord
        {
            public string Id { get; set; } = "";
            public string KycId { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public string PPSNumber { get; set; } = "";
            public string County { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreatedDate { get; set; }

            public string FirstName { get; set; } = "";
            public string MiddleName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string DateOfBirth { get; set; } = "";
            public string EmailAddress { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public string Address { get; set; } = "";
            public string City { get; set; } = "";
            public string Eircode { get; set; } = "";

            public string PscFrontFileName { get; set; } = "";
            public string PscBackFileName { get; set; } = "";
            public string PscFrontImageUrl { get; set; } = "";
            public string PscBackImageUrl { get; set; } = "";

            public string[] SearchResponses { get; set; } = Array.Empty<string>();
            public string[] DownloadResponses { get; set; } = Array.Empty<string>();
            public string[] ProcessingStatusFiles { get; set; } = Array.Empty<string>();
            public bool? SearchFound { get; set; }
            public string[] ZipFiles { get; set; } = Array.Empty<string>();
            public List<WorkflowLogItem> WorkflowLogs { get; set; } = new();
        }

        

        private static string MapUiStatus(KycWorkflowStatus status)
        {
            return status switch
            {
                KycWorkflowStatus.Draft => "Pending",
                KycWorkflowStatus.Sent => "Processing",
                KycWorkflowStatus.Zipped => "Processing",
                KycWorkflowStatus.Completed => "Completed",
                KycWorkflowStatus.KycDone => "Completed",
                KycWorkflowStatus.Failed => "Failed",
                _ => status.ToString()
            };
        }

        private async Task<List<SavedKycRecord>> GetDbRecordsAsync()
        {
            var uploads = await _context.KycUploadDetails
                .Include(x => x.Images)
                .Include(x => x.County)
                .Include(x => x.City)
                .AsSplitQuery()
                .ToListAsync();

            return uploads.Select(upload =>
            {
                var front = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCFront);
                var back = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCBack);

                return new SavedKycRecord
                {
                    Id = upload.KycUploadId.ToString(),
                    KycId = upload.RequestRef,
                    CustomerName = $"{upload.FirstName} {upload.LastName}".Trim(),
                    PPSNumber = upload.PPSN,
                    County = upload.County?.CountyName ?? "",
                    Status = MapUiStatus(upload.Status),
                    CreatedDate = upload.CreatedAtUtc.UtcDateTime,

                    FirstName = upload.FirstName,
                    MiddleName = upload.MiddleName,
                    LastName = upload.LastName,
                    DateOfBirth = upload.DateOfBirth.ToString("yyyy-MM-dd"),
                    EmailAddress = upload.Email,
                    PhoneNumber = upload.Phone,
                    Address = upload.AddressLine1,
                    City = upload.City?.CityName ?? "",
                    Eircode = upload.Eircode,

                    PscFrontFileName = front?.FileName ?? "",
                    PscBackFileName = back?.FileName ?? "",
                    PscFrontImageUrl = front != null ? $"/KycUpload/Document/{front.KycUploadImageId}" : "",
                    PscBackImageUrl = back != null ? $"/KycUpload/Document/{back.KycUploadImageId}" : "",

                    SearchResponses = Array.Empty<string>(),
                    DownloadResponses = Array.Empty<string>(),
                    ProcessingStatusFiles = Array.Empty<string>(),
                    SearchFound = upload.SearchFound,
                    ZipFiles = Array.Empty<string>(),
                };
            }).ToList();
        }

        private async Task<List<SavedKycRecord>> GetAllRecordsAsync()
        {
            return await GetDbRecordsAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] string? county,
            [FromQuery] DateTime? createdDate,
            [FromQuery] string? transactionStatus)
        {
            var records = (await GetAllRecordsAsync()).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                records = records.Where(x =>
                    x.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(county))
            {
                records = records.Where(x =>
                    x.County.Equals(county, StringComparison.OrdinalIgnoreCase));
            }

            if (createdDate.HasValue)
            {
                records = records.Where(x => x.CreatedDate.Date == createdDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(transactionStatus))
            {
                if (transactionStatus.Equals("Success", StringComparison.OrdinalIgnoreCase))
                    records = records.Take(2);
                else if (transactionStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                    records = records.Take(1);
                else if (transactionStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                    records = records.Take(1);
            }

            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .Include(x => x.Images)
                .Include(x => x.County)
                .Include(x => x.City)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            var front = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCFront);
            var back = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCBack);

            var searchResponses = new List<string>();
            var downloadResponses = new List<string>();

            var processingStatusFiles = await _context.KycUpdationResponses
                .AsNoTracking()
                .Where(x => x.KycUploadId == guidId)
                .OrderByDescending(x => x.ReceivedAtUtc)
                .Select(x => x.FileName)
                .Distinct()
                .ToListAsync();

            if (await _context.SearchResponseEncrypted.AnyAsync(x => x.KycUploadId == guidId))
                searchResponses.Add($"{upload.RequestRef}_SEARCH_ENC.dat");

            if (await _context.SearchResponseDecrypted.AnyAsync(x => x.KycUploadId == guidId))
                searchResponses.Add($"{upload.RequestRef}_SEARCH_DEC.json");

            if (upload.SearchFound == true)
            {
                if (await _context.DownloadResponseEncrypted.AnyAsync(x => x.KycUploadId == guidId))
                    downloadResponses.Add("download-response.enc");

                if (await _context.DownloadResponseDecrypted.AnyAsync(x => x.KycUploadId == guidId))
                    downloadResponses.Add("download-response.json");
            }

            var zipFiles = await _context.ZipFileUploadDetails
                .AsNoTracking()
                .Where(x => x.KycUploadId == guidId)
                .OrderByDescending(x => x.UploadedAtUtc)
                .Select(x => x.ZipFileName)
                .Take(1)
                .ToListAsync();
            var workflowLogs = await _context.KycWorkflowExecutionLogs
                .AsNoTracking()
                .Where(x => x.KycUploadId == guidId)
                .OrderByDescending(x => x.StartedAtUtc)
                .Select(x => new WorkflowLogItem
                {
                    StepName = x.StepName,
                    Status = x.Status.ToString(),
                    Message = x.Message,
                    ErrorDetails = x.ErrorDetails,
                    StartedAtUtc = x.StartedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc
                })
                .ToListAsync();

            return Ok(new SavedKycRecord
            {
                Id = upload.KycUploadId.ToString(),
                KycId = upload.RequestRef,
                CustomerName = $"{upload.FirstName} {upload.LastName}".Trim(),
                PPSNumber = upload.PPSN,
                County = upload.County?.CountyName ?? "",
                Status = MapUiStatus(upload.Status),
                CreatedDate = upload.CreatedAtUtc.UtcDateTime,

                FirstName = upload.FirstName,
                MiddleName = upload.MiddleName,
                LastName = upload.LastName,
                DateOfBirth = upload.DateOfBirth.ToString("yyyy-MM-dd"),
                EmailAddress = upload.Email,
                PhoneNumber = upload.Phone,
                Address = upload.AddressLine1,
                City = upload.City?.CityName ?? "",
                Eircode = upload.Eircode,

                PscFrontFileName = front?.FileName ?? "",
                PscBackFileName = back?.FileName ?? "",
                PscFrontImageUrl = front != null ? $"/KycUpload/Document/{front.KycUploadImageId}" : "",
                PscBackImageUrl = back != null ? $"/KycUpload/Document/{back.KycUploadImageId}" : "",

                SearchResponses = searchResponses.ToArray(),
                DownloadResponses = downloadResponses.ToArray(),
                ProcessingStatusFiles = processingStatusFiles.ToArray(),
                SearchFound = upload.SearchFound,
                ZipFiles = zipFiles.ToArray(),
                WorkflowLogs = workflowLogs
            });
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var records = await GetAllRecordsAsync();

            var dashboard = new
            {
                summary = new
                {
                    total = records.Count,
                    pending = records.Count(x => x.Status == "Pending"),
                    processing = records.Count(x => x.Status == "Processing"),
                    completed = records.Count(x => x.Status == "Completed"),
                    failed = records.Count(x => x.Status == "Failed")
                },
                statusDistribution = new[]
                {
                    new { label = "Pending", count = records.Count(x => x.Status == "Pending") },
                    new { label = "Processing", count = records.Count(x => x.Status == "Processing") },
                    new { label = "Completed", count = records.Count(x => x.Status == "Completed") },
                    new { label = "Failed", count = records.Count(x => x.Status == "Failed") }
                },
                uploadTrend = records
                    .GroupBy(x => x.CreatedDate.ToString("yyyy-MM-dd"))
                    .OrderBy(x => x.Key)
                    .Select(x => new { label = x.Key, count = x.Count() }),

                countyDistribution = records
                    .GroupBy(x => x.County)
                    .Select(x => new { label = x.Key, count = x.Count() }),

                transactionStats = new[]
                {
                    new { label = "Success", count = 2 },
                    new { label = "Failed", count = 1 },
                    new { label = "Pending", count = 1 }
                },

                recentUploads = records
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(4),

                alerts = new[]
                {
                    "2 records pending for more than 2 days",
                    "1 failed CKYC transaction found"
                }
            };

            return Ok(dashboard);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateKycRequest request)
        {
            if (request.PscFront == null || request.PscFront.Length == 0 ||
                request.PscBack == null || request.PscBack.Length == 0)
            {
                return BadRequest("PSC Front and PSC Back are required.");
            }

            if (!DateOnly.TryParse(request.DateOfBirth, out var dob))
            {
                return BadRequest("Invalid Date of Birth.");
            }

            var countyName = (request.CountyName ?? request.County ?? "").Trim();
            var cityName = (request.CityName ?? request.City ?? "").Trim();
            var addressLine1 = (request.AddressLine1 ?? request.Address ?? "").Trim();

            var county = await _context.Counties
                .FirstOrDefaultAsync(x => x.CountyName.ToLower() == countyName.ToLower());

            if (county == null)
            {
                return BadRequest("Invalid County.");
            }

            var city = await _context.Cities
                .FirstOrDefaultAsync(x => x.CityName.ToLower() == cityName.ToLower() && x.CountyId == county.CountyId);

            if (city == null)
            {
                return BadRequest("Invalid City.");
            }

            var firstName = (request.FirstName ?? "").Trim();
            var middleName = (request.MiddleName ?? "").Trim();
            var lastName = (request.LastName ?? "").Trim();
            var ppsn = (request.PpsNumber ?? "").Trim();

            var todayPrefix = $"KYC-{DateTime.UtcNow:yyyyMMdd}-";

            var todayCount = await _context.KycUploadDetails
                .CountAsync(x => x.RequestRef.StartsWith(todayPrefix));

            var requestRef = $"{todayPrefix}{(todayCount + 1):D6}";

            var identityHash = Sha256Hex($"{firstName}|{middleName}|{lastName}|{dob:yyyy-MM-dd}|{ppsn}");

            var risk = Enum.TryParse<RiskRating>(request.RiskRating, true, out var parsedRisk)
                ? parsedRisk
                : RiskRating.Low;

            var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/png", "application/pdf"
            };

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".pdf"
            };

            const long maxFileBytes = 10 * 1024 * 1024;

            async Task<(bool ok, string err, byte[] bytes, string contentType, string originalFileName, long size)> ReadAndValidate(IFormFile file)
            {
                if (file == null || file.Length == 0)
                    return (false, "File is required.", Array.Empty<byte>(), "", "", 0);

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

            var front = await ReadAndValidate(request.PscFront);
            if (!front.ok) return BadRequest("PSC Front: " + front.err);

            var back = await ReadAndValidate(request.PscBack);
            if (!back.ok) return BadRequest("PSC Back: " + back.err);

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
                Email = (request.EmailAddress ?? "").Trim(),
                Phone = (request.PhoneNumber ?? "").Trim(),
                AddressLine1 = addressLine1,

                CountyId = county.CountyId,
                CityId = city.CityId,

                Eircode = (request.Eircode ?? "").Trim(),
                Country = "Ireland",

                Nationality = "Ireland",
                Gender = "NA",
                Occupation = "NA",
                EmployerName = "NA",
                SourceOfFunds = "NA",
                IsPEP = request.IsPEP,
                RiskRating = risk,

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
                FileName = front.originalFileName,
                ContentType = front.contentType,
                FileSizeBytes = front.size,
                FileHashSha256 = Sha256Hex(front.bytes),
                ImageBytes = front.bytes,
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
                FileName = back.originalFileName,
                ContentType = back.contentType,
                FileSizeBytes = back.size,
                FileHashSha256 = Sha256Hex(back.bytes),
                ImageBytes = back.bytes,
                OcrText = "",
                OcrConfidence = 0,
                ExtractedDocNumber = "",
                IsImageValidated = false,
                ImageValidationMessage = null,
                ImageValidatedAtUtc = null
            });

            _context.KycUploadDetails.Add(upload);
            await _context.SaveChangesAsync();

            var automationResult = await _automationService.ProcessRecordAsync(upload.KycUploadId);

            return Ok(new
            {
                success = true,
                id = upload.KycUploadId.ToString(),
                message = automationResult.Success
                    ? $"KYC record created successfully. Automation: {automationResult.Message}"
                    : $"KYC record created successfully, but automation stopped: {automationResult.Message}",
                automation = new
                {
                    automationResult.Success,
                    automationResult.IsComplete,
                    automationResult.Message
                }
            });
        }

        [HttpPost("{id}/search-ckyc")]
        public async Task<IActionResult> SearchCkycFromApi(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            var frontOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCFront && x.IsImageValidated);
            var backOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCBack && x.IsImageValidated);

            if (!frontOk || !backOk)
            {
                return BadRequest("Validate PSC Front and PSC Back first.");
            }

            if (upload.DedupeExecuted != true || upload.DedupePassed != false)
            {
                return BadRequest("CKYC Search is only allowed after Dedupe FAIL.");
            }

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
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = false,
                    message
                });
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

            return Ok(new
            {
                success = true,
                found,
                ckycNumber,
                message = found && !string.IsNullOrWhiteSpace(ckycNumber)
                    ? $"CKYC record found. CKYC Number: {ckycNumber}"
                    : "No CKYC record found. You can upload this record to CKYC."
            });
        }

        [HttpPost("{id}/download-ckyc")]
        public async Task<IActionResult> DownloadFromCkycFromApi(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            if (upload.SearchExecuted != true || upload.SearchFound != true || string.IsNullOrWhiteSpace(upload.CkycNumber))
            {
                return BadRequest("Download is only available after Search FOUND a CKYC record.");
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

            return Ok(new
            {
                success = true,
                message = "CKYC record downloaded and stored in encrypted and decrypted formats."
            });
        }

        [HttpPost("{id}/validate-psc-front")]
        public async Task<IActionResult> ValidatePscFrontFromApi(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            var front = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCFront);
            if (front == null)
                return NotFound("PSC Front not found.");

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

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = ok,
                message = front.ImageValidationMessage
            });
        }

        [HttpPost("{id}/validate-psc-back")]
        public async Task<IActionResult> ValidatePscBackFromApi(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            var back = upload.Images.FirstOrDefault(x => x.DocumentType == DocumentType.PSCBack);
            if (back == null)
                return NotFound("PSC Back not found.");

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

            return Ok(new
            {
                success = ok,
                message = back.ImageValidationMessage
            });
        }

        [HttpPost("{id}/check-dedupe")]
        public async Task<IActionResult> CheckDedupeFromApi(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            var frontOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCFront && x.IsImageValidated);
            var backOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCBack && x.IsImageValidated);

            if (!frontOk || !backOk)
            {
                return BadRequest("Please validate PSC Front and PSC Back before dedupe check.");
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

            return Ok(new
            {
                success = true,
                dedupePassed = exists,
                message = upload.DedupeMessage
            });
        }

        [HttpPost("{id}/generate-zip")]
        public async Task<IActionResult> GenerateZipFromApi(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            var frontOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCFront && x.IsImageValidated);
            var backOk = upload.Images.Any(x => x.DocumentType == DocumentType.PSCBack && x.IsImageValidated);

            if (!frontOk || !backOk)
                return BadRequest("Validate PSC Front and PSC Back before generating ZIP.");

            var zipBytes = ZipBuilder.BuildKycZip(upload);
            if (zipBytes == null || zipBytes.Length == 0)
                return BadRequest("ZIP generation failed.");

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

            return Ok(new
            {
                success = true,
                message = "ZIP generated successfully."
            });
        }

        [HttpPost("{id}/send-zip")]
        public async Task<IActionResult> SendZipToSftpFromApi(string id, [FromServices] SftpZipUploader sftp)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            var zipRow = await _context.ZipFileUploadDetails
                .FirstOrDefaultAsync(z => z.KycUploadId == guidId);

            if (zipRow == null)
                return BadRequest("ZIP is not generated yet. Generate ZIP first.");

            if (zipRow.ZipBytes == null || zipRow.ZipBytes.Length == 0)
                return BadRequest("ZIP bytes are empty. Generate ZIP again.");

            var (ok, remotePath, error) = await sftp.UploadBytesAsync(zipRow.ZipBytes, zipRow.ZipFileName);

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
            }
            else
            {
                zipRow.UploadStatus = 2;
                zipRow.FailureReason = error;
                zipRow.UploadedAtUtc = DateTime.UtcNow;

                upload.SubmittedAtUtc = DateTimeOffset.UtcNow;
                upload.Status = KycWorkflowStatus.Failed;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = ok,
                message = ok
                    ? $"ZIP uploaded to SFTP: {remotePath}"
                    : $"SFTP upload failed: {error}"
            });
        }

        [HttpPost("{id}/check-ckyc-status")]
        public async Task<IActionResult> CheckCkycStatusFromApi(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

            if (upload.Status != KycWorkflowStatus.Sent)
                return BadRequest("You can only check status after uploading to CKYC.");

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

                return Ok(new
                {
                    success = false,
                    message = "CKYC status: Not found yet."
                });
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
            var responseHash = Sha256Hex(responseJson);

            var statusText = (st.Status ?? "").Trim();

            _context.KycUpdationResponses.Add(new KycUpdationResponse
            {
                KycUploadId = upload.KycUploadId,
                RequestRef = upload.RequestRef,
                ResponseType = "CKYC_STATUS",
                FileName = "ckyc-status-response.json",
                ContentType = "application/json",
                ResponseJson = responseJson,
                ResponseHashSha256 = responseHash,
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
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"CKYC status: {statusText}",
                ckycNumber = st.CkycNumber
            });
        }

        [HttpPost("{id}/push-to-internal")]
        public async Task<IActionResult> PushToInternalFromApi(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var upload = await _context.KycUploadDetails
                .Include(x => x.Images)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.KycUploadId == guidId);

            if (upload == null)
                return NotFound("Record not found.");

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
                    .Where(x => x.KycUploadId == guidId)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefaultAsync();

                if (dec == null || string.IsNullOrWhiteSpace(dec.ResponseJson))
                    return BadRequest("No decrypted download response found. Download CKYC first.");

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
                    return BadRequest("Downloaded CKYC payload does not contain ckycNumber.");

                upload.CkycNumber = ckycNumber;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(upload.CkycNumber))
                    return BadRequest("CKYC number not available yet. Check CKYC Status first.");

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

            return Ok(new
            {
                success = true,
                message = $"Pushed to bank records successfully. Customer CKYC: {ckycNumber}"
            });
        }

        [HttpPost("{id}/run-automation")]
        public async Task<IActionResult> RunAutomation(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return BadRequest("Invalid record id.");

            var result = await _automationService.ProcessRecordAsync(guidId);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    isComplete = result.IsComplete,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                isComplete = result.IsComplete,
                message = result.Message
            });
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(string id, [FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File name is required.");

            if (Guid.TryParse(id, out var guidId))
            {
                if (fileName.EndsWith("_SEARCH_ENC.dat", StringComparison.OrdinalIgnoreCase))
                {
                    var row = await _context.SearchResponseEncrypted
                        .AsNoTracking()
                        .Where(x => x.KycUploadId == guidId)
                        .OrderByDescending(x => x.CreatedAtUtc)
                        .FirstOrDefaultAsync();

                    if (row == null) return NotFound("Search encrypted file not found.");

                    return File(row.EncryptedResponseBytes, "application/octet-stream", fileName);
                }

                if (fileName.EndsWith("_SEARCH_DEC.json", StringComparison.OrdinalIgnoreCase))
                {
                    var row = await _context.SearchResponseDecrypted
                        .AsNoTracking()
                        .Where(x => x.KycUploadId == guidId)
                        .OrderByDescending(x => x.CreatedAtUtc)
                        .FirstOrDefaultAsync();

                    if (row == null) return NotFound("Search decrypted file not found.");

                    var bytes = Encoding.UTF8.GetBytes(row.DecryptedJson ?? "{}");
                    return File(bytes, "application/json", fileName);
                }

                if (fileName.Equals("download-response.enc", StringComparison.OrdinalIgnoreCase))
                {
                    var row = await _context.DownloadResponseEncrypted
                        .AsNoTracking()
                        .Where(x => x.KycUploadId == guidId)
                        .OrderByDescending(x => x.CreatedAtUtc)
                        .FirstOrDefaultAsync();

                    if (row == null) return NotFound("Encrypted download response not found.");

                    return File(row.EncryptedResponseBytes, "application/octet-stream", fileName);
                }

                if (fileName.Equals("download-response.json", StringComparison.OrdinalIgnoreCase))
                {
                    var row = await _context.DownloadResponseDecrypted
                        .AsNoTracking()
                        .Where(x => x.KycUploadId == guidId)
                        .OrderByDescending(x => x.CreatedAtUtc)
                        .FirstOrDefaultAsync();

                    if (row == null) return NotFound("Decrypted download response not found.");

                    var json = row.ResponseJson ?? row.PayloadJson ?? "{}";
                    var bytes = Encoding.UTF8.GetBytes(json);
                    return File(bytes, "application/json", fileName);
                }

                var updationRow = await _context.KycUpdationResponses
                    .AsNoTracking()
                    .Where(x => x.KycUploadId == guidId && x.FileName == fileName)
                    .OrderByDescending(x => x.ReceivedAtUtc)
                    .FirstOrDefaultAsync();

                if (updationRow != null)
                {
                    var json = updationRow.ResponseJson ?? "{}";
                    var bytes = Encoding.UTF8.GetBytes(json);
                    var contentType = string.IsNullOrWhiteSpace(updationRow.ContentType)
                        ? "application/json"
                        : updationRow.ContentType;

                    return File(bytes, contentType, fileName);
                }
                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var row = await _context.ZipFileUploadDetails
                        .AsNoTracking()
                        .Where(x => x.KycUploadId == guidId)
                        .OrderByDescending(x => x.UploadedAtUtc)
                        .FirstOrDefaultAsync();

                    if (row == null || row.ZipBytes == null || row.ZipBytes.Length == 0)
                        return NotFound("ZIP file not found.");

                    return File(row.ZipBytes, "application/zip", row.ZipFileName);
                }

                return NotFound("Unsupported file.");
            }
            return BadRequest("Invalid record id.");


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
    }
}