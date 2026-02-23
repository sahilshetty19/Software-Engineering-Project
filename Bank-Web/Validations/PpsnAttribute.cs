using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Bank.Web.Validation;

// Demo-level PPSN validation: structure only (not official checksum)
// Format examples: 1234567AB, 1234567A, 1234567TA
public sealed class PpsnAttribute : ValidationAttribute
{
    private static readonly Regex Rx = new(@"^\d{7}[A-Z]{1,2}$", RegexOptions.IgnoreCase);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var s = (value as string)?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(s))
            return new ValidationResult("PPS number is required.");

        // Remove spaces (some users type spaces)
        s = s.Replace(" ", "");

        if (!Rx.IsMatch(s))
            return new ValidationResult("PPS number format should be like 1234567A or 1234567AB.");

        return ValidationResult.Success;
    }
}