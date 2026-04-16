using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public enum BulkUploadBatchStatus
{
    Uploaded = 0,
    Processing = 1,
    Completed = 2,
    PartiallyCompleted = 3,
    Failed = 4
}

public sealed class BulkUploadBatch
{
    [Key]
    public Guid BulkUploadBatchId { get; set; } = Guid.NewGuid();

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = "";

    [Required, MaxLength(255)]
    public string StoredFileName { get; set; } = "";

    [MaxLength(255)]
    public string? UploadedBy { get; set; }

    public BulkUploadBatchStatus Status { get; set; } = BulkUploadBatchStatus.Uploaded;

    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public List<BulkUploadRowResult> RowResults { get; set; } = new();
}