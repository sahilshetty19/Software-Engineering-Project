using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public enum EmployeeRole : short
{
    Staff = 0,
    Supervisor = 1,
    Admin = 2
}

public sealed class BankEmployee
{
    [Key]
    public Guid BankEmployeeId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(30)]
    public string EmployeeCode { get; set; } = ""; // Unique employee ID

    [Required]
    [MaxLength(120)]
    public string FullName { get; set; } = "";

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = "";

    public EmployeeRole Role { get; set; } = EmployeeRole.Staff;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAtUtc { get; set; }
}