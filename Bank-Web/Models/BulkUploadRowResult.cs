using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public enum BulkUploadRowStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2
}

public sealed class BulkUploadRowResult
{
    [Key]
    public Guid BulkUploadRowResultId { get; set; } = Guid.NewGuid();

    public Guid BulkUploadBatchId { get; set; }
    public BulkUploadBatch? BulkUploadBatch { get; set; }

    public int RowNumber { get; set; }

    [Required, MaxLength(100)]
    public string RowRef { get; set; } = "";

    public Guid? KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    public BulkUploadRowStatus Status { get; set; } = BulkUploadRowStatus.Pending;

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}