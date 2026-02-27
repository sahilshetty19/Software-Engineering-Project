using System.ComponentModel.DataAnnotations;

namespace CKYC.Server.DTOs;

public class CkycSearchRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    public string MiddleName { get; set; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required, MaxLength(20)]
    public string PPSN { get; set; } = "";
}