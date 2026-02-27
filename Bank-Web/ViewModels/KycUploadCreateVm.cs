using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Bank.Web.Models;
using Bank.Web.Validation;

namespace Bank.Web.ViewModels;

public class KycUploadCreateVm
{
    [Display(Name = "First Name")]
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z '\-]{0,99}$", ErrorMessage = "First name contains invalid characters.")]
    public string FirstName { get; set; } = "";

    [Display(Name = "Middle Name")]
    [Required(ErrorMessage = "Middle name is required.")]
    [MaxLength(100)]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z '\-]{0,99}$", ErrorMessage = "Middle name contains invalid characters.")]
    public string MiddleName { get; set; } = "";

    [Display(Name = "Last Name")]
    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z '\-]{0,99}$", ErrorMessage = "Last name contains invalid characters.")]
    public string LastName { get; set; } = "";

    [Display(Name = "Date of Birth")]
    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [MinimumAge(18)]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-18);

    [Display(Name = "PPS Number")]
    [Ppsn]
    [MaxLength(20)]
    public string PPSN { get; set; } = "";

    [Display(Name = "Email Address")]
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(200)]
    public string Email { get; set; } = "";

    [Display(Name = "Phone Number")]
    [Required(ErrorMessage = "Phone number is required.")]
    [MaxLength(30)]
    [RegularExpression(@"^\+?\d[\d\s\-]{6,}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = "";

    [Display(Name = "Address Line 1")]
    [Required(ErrorMessage = "Address is required.")]
    [MaxLength(200)]
    public string AddressLine1 { get; set; } = "";

    [Display(Name = "County")]
    [Required(ErrorMessage = "County is required.")]
    public Guid CountyId { get; set; }

    [Display(Name = "City")]
    [Required(ErrorMessage = "City is required.")]
    public Guid CityId { get; set; }

    [Display(Name = "Eircode")]
    [Eircode]
    [MaxLength(10)]
    public string Eircode { get; set; } = "";

    [Display(Name = "Politically Exposed Person (PEP)")]
    public bool IsPEP { get; set; }

    [Display(Name = "Risk Rating")]
    [Required]
    public RiskRating RiskRating { get; set; } = RiskRating.Low;

    [Display(Name = "Upload Documents")]
    [Required(ErrorMessage = "At least one document is required.")]
    public List<IFormFile> Documents { get; set; } = new();
}