using Bank.Web.Data;
using Bank.Web.Models;
using Bank.Web.Services.BulkUpload;
using Bank.Web.Services.Automation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace Bank_Web.Controllers.Api
{
    [ApiController]
    [Route("api/kyc/bulk-upload")]
    public class BulkUploadApiController : ControllerBase
    {
        private readonly BankDbContext _context;
        private readonly BulkUploadPackageReader _packageReader;
        private readonly BulkUploadRowValidator _rowValidator;
        private readonly BulkUploadImportService _importService;
        private readonly KycAutomationService _automationService;

        public BulkUploadApiController(BankDbContext context, BulkUploadPackageReader packageReader, BulkUploadRowValidator rowValidator, BulkUploadImportService importService, KycAutomationService automationService)
        {
            _context = context;
            _packageReader = packageReader;
            _rowValidator = rowValidator;
            _importService = importService;
            _automationService = automationService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadBulkZip([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("ZIP file is required.");

            var ext = Path.GetExtension(file.FileName);
            if (!string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only ZIP files are allowed.");

            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "BulkUploads");
            Directory.CreateDirectory(uploadsRoot);

            var storedFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
            var storedFilePath = Path.Combine(uploadsRoot, storedFileName);

            await using (var stream = new FileStream(storedFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var batch = new BulkUploadBatch
            {
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                UploadedBy = "system",
                Status = BulkUploadBatchStatus.Uploaded,
                TotalRows = 0,
                SuccessRows = 0,
                FailedRows = 0,
                FailureReason = null,
                UploadedAtUtc = DateTimeOffset.UtcNow
            };

            _context.BulkUploadBatches.Add(batch);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                batchId = batch.BulkUploadBatchId,
                message = "Bulk upload accepted successfully."
            });
        }

        [HttpGet("{batchId}")]
        public async Task<IActionResult> GetBatchSummary(string batchId)
        {
            if (!Guid.TryParse(batchId, out var guidBatchId))
                return BadRequest("Invalid batch id.");

            var batch = await _context.BulkUploadBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BulkUploadBatchId == guidBatchId);

            if (batch == null)
                return NotFound("Batch not found.");

            return Ok(new
            {
                batch.BulkUploadBatchId,
                batch.OriginalFileName,
                batch.StoredFileName,
                batch.Status,
                batch.TotalRows,
                batch.SuccessRows,
                batch.FailedRows,
                batch.FailureReason,
                batch.UploadedAtUtc,
                batch.CompletedAtUtc
            });
        }

        [HttpGet("{batchId}/rows")]
        public async Task<IActionResult> GetBatchRows(string batchId)
        {
            if (!Guid.TryParse(batchId, out var guidBatchId))
                return BadRequest("Invalid batch id.");

            var rows = await _context.BulkUploadRowResults
                .AsNoTracking()
                .Where(x => x.BulkUploadBatchId == guidBatchId)
                .OrderBy(x => x.RowNumber)
                .GroupJoin(
                    _context.KycUploadDetails.AsNoTracking(),
                    row => row.KycUploadId,
                    kyc => kyc.KycUploadId,
                    (row, kycs) => new { row, kycs }
                )
                .SelectMany(
                    x => x.kycs.DefaultIfEmpty(),
                    (x, kyc) => new
                    {
                        x.row.BulkUploadRowResultId,
                        x.row.RowNumber,
                        x.row.RowRef,
                        x.row.KycUploadId,
                        RequestRef = kyc != null ? kyc.RequestRef : null,
                        Status = MapBulkRowStatus(x.row, kyc),
                        ImportStatus = x.row.Status.ToString(),
                        x.row.ErrorMessage,
                        x.row.CreatedAtUtc,
                        AutomationStatus = kyc != null ? MapAutomationStatus(kyc.AutomationStatus) : null,
                        RetryAttemptCount = kyc != null ? kyc.RetryAttemptCount : 0,
                        MaxRetryAttempts = kyc != null ? kyc.MaxRetryAttempts : 0,
                        NextRetryAtUtc = kyc != null ? kyc.NextRetryAtUtc : null,
                        LastAutomationError = kyc != null ? kyc.LastAutomationError : null,
                        LastFailedStep = kyc != null ? kyc.LastFailedStep : null
                    })
                .ToListAsync();

            return Ok(rows);
        }

        private static string MapAutomationStatus(AutomationRunStatus status)
        {
            return status switch
            {
                AutomationRunStatus.Queued => "Queued",
                AutomationRunStatus.Running => "Running",
                AutomationRunStatus.WaitingRetry => "Waiting Retry",
                AutomationRunStatus.Completed => "Completed",
                AutomationRunStatus.TerminalFailed => "Terminal Failed",
                _ => status.ToString()
            };
        }

        private static string MapBulkRowStatus(BulkUploadRowResult row, KycUploadDetails? kyc)
        {
            if (kyc != null)
            {
                if (kyc.AutomationStatus == AutomationRunStatus.TerminalFailed || kyc.Status == KycWorkflowStatus.Failed)
                    return "Failed";

                if (kyc.AutomationStatus == AutomationRunStatus.Completed ||
                    kyc.Status == KycWorkflowStatus.Completed ||
                    kyc.Status == KycWorkflowStatus.KycDone)
                    return "Completed";

                if (kyc.AutomationStatus == AutomationRunStatus.Running)
                    return "Processing";

                if (kyc.AutomationStatus == AutomationRunStatus.WaitingRetry)
                    return "Waiting Retry";

                if (kyc.AutomationStatus == AutomationRunStatus.Queued)
                    return "Pending";

                return "Processing";
            }

            return row.Status switch
            {
                BulkUploadRowStatus.Success => "Imported",
                BulkUploadRowStatus.Failed => "Failed",
                _ => "Pending"
            };
        }
        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("KYC_UPLOAD");

            var headers = new[]
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

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            var sampleRows = new[]
            {
        new object[]
        {
            "REC0001", "Sahil", "Ravindra", "Shetty", "2000-03-13", "1234567AB",
            "sahil@test.com", "0899482618", "Main Street", "Westmeath", "Moate",
            "N37PE84", "false", "Low", "REC0001.jpg", "REC0001.jpg"
        },
        new object[]
        {
            "REC0002", "John", "", "Doe", "1999-01-01", "2345678BC",
            "john@test.com", "0890000001", "Dublin Street", "Westmeath", "Moate",
            "N37AA01", "true", "Medium", "REC0002.jpg", "REC0002.jpg"
        }
    };

            for (int row = 0; row < sampleRows.Length; row++)
            {
                for (int col = 0; col < sampleRows[row].Length; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value = sampleRows[row][col]?.ToString();
                }
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "kyc_bulk_upload_template.xlsx");
        }
        [HttpPost("{batchId}/read")]
        public async Task<IActionResult> ReadBatchPackage(string batchId)
        {
            if (!Guid.TryParse(batchId, out var guidBatchId))
                return BadRequest("Invalid batch id.");

            var batch = await _context.BulkUploadBatches
                .FirstOrDefaultAsync(x => x.BulkUploadBatchId == guidBatchId);

            if (batch == null)
                return NotFound("Batch not found.");

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

                return BadRequest(readResult.ErrorMessage);
            }

            var validationResult = await _rowValidator.ValidateAsync(
                readResult.Rows,
                readResult.PscFrontFolderPath,
                readResult.PscBackFolderPath);

            batch.TotalRows = validationResult.TotalRows;
            batch.SuccessRows = validationResult.ValidRows;
            batch.FailedRows = validationResult.InvalidRows;
            batch.Status = validationResult.InvalidRows > 0
                ? BulkUploadBatchStatus.PartiallyCompleted
                : BulkUploadBatchStatus.Processing;
            batch.FailureReason = null;

            var existingRows = await _context.BulkUploadRowResults
                .Where(x => x.BulkUploadBatchId == batch.BulkUploadBatchId)
                .ToListAsync();

            if (existingRows.Count > 0)
            {
                _context.BulkUploadRowResults.RemoveRange(existingRows);
            }

            foreach (var row in validationResult.Rows)
            {
                _context.BulkUploadRowResults.Add(new BulkUploadRowResult
                {
                    BulkUploadBatchId = batch.BulkUploadBatchId,
                    RowNumber = row.ParsedRow.RowNumber,
                    RowRef = row.ParsedRow.RowRef,
                    KycUploadId = null,
                    Status = row.IsValid ? BulkUploadRowStatus.Pending : BulkUploadRowStatus.Failed,
                    ErrorMessage = row.ErrorMessage,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                batchId = batch.BulkUploadBatchId,
                totalRows = validationResult.TotalRows,
                validRows = validationResult.ValidRows,
                invalidRows = validationResult.InvalidRows,
                preview = validationResult.Rows.Take(10).Select(x => new
                {
                    x.ParsedRow.RowNumber,
                    x.ParsedRow.RowRef,
                    x.ParsedRow.FirstName,
                    x.ParsedRow.LastName,
                    x.IsValid,
                    x.ErrorMessage
                })
            });
        }
        [HttpPost("{batchId}/import")]
        public async Task<IActionResult> ImportBatch(string batchId)
        {
            if (!Guid.TryParse(batchId, out var guidBatchId))
                return BadRequest("Invalid batch id.");

            var result = await _importService.ImportBatchAsync(guidBatchId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetBatches()
        {
            var batches = await _context.BulkUploadBatches
                .AsNoTracking()
                .OrderByDescending(x => x.UploadedAtUtc)
                .Select(x => new
                {
                    x.BulkUploadBatchId,
                    x.OriginalFileName,
                    x.StoredFileName,
                    x.Status,
                    x.TotalRows,
                    x.SuccessRows,
                    x.FailedRows,
                    x.FailureReason,
                    x.UploadedAtUtc,
                    x.CompletedAtUtc
                })
                .ToListAsync();

            return Ok(batches);
        }
        [HttpPost("{batchId}/run-automation")]
        public async Task<IActionResult> RunAutomationForBatch(string batchId)
        {
            if (!Guid.TryParse(batchId, out var guidBatchId))
                return BadRequest("Invalid batch id.");

            var batch = await _context.BulkUploadBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BulkUploadBatchId == guidBatchId);

            if (batch == null)
                return NotFound("Batch not found.");

            var rowResults = await _context.BulkUploadRowResults
                .AsNoTracking()
                .Where(x => x.BulkUploadBatchId == guidBatchId && x.KycUploadId != null)
                .OrderBy(x => x.RowNumber)
                .ToListAsync();

            if (rowResults.Count == 0)
                return BadRequest("No imported KYC records found for this batch.");

            var results = new List<object>();

            foreach (var row in rowResults)
            {
                var result = await _automationService.ProcessRecordAsync(row.KycUploadId!.Value);

                results.Add(new
                {
                    row.RowNumber,
                    row.RowRef,
                    row.KycUploadId,
                    result.Success,
                    result.IsComplete,
                    result.Message
                });
            }

            return Ok(new
            {
                success = true,
                batchId = guidBatchId,
                totalRecords = results.Count,
                results
            });
        }
    }
}
