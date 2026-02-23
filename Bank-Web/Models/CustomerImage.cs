using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class CustomerImage
{
    [Key] public Guid CustomerImageId { get; set; } = Guid.NewGuid();

    public Guid BankCustomerId { get; set; }
    public BankCustomerDetails? BankCustomer { get; set; }

    public DocumentType DocumentType { get; set; } = DocumentType.Other;

    [Required, MaxLength(255)] public string FileName { get; set; } = "";
    [Required, MaxLength(100)] public string ContentType { get; set; } = "";

    public long FileSizeBytes { get; set; }

    [Required, MaxLength(64)]
    public string FileHashSha256 { get; set; } = "";

    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

    public DateTimeOffset StoredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}