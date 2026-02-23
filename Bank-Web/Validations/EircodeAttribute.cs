using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Bank.Web.Validation;

// Eircode format check (general pattern): A65 F4E2, D02 X285, etc.
public sealed class EircodeAttribute : ValidationAttribute
{
    private static readonly Regex Rx = new(@"^[A-Z0-9]{3}\s?[A-Z0-9]{4}$", RegexOptions.IgnoreCase);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var s = (value as string)?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(s))
            return new ValidationResult("Eircode is required.");

        s = s.Replace(" ", "");

        if (!Rx.IsMatch(s))
            return new ValidationResult("Eircode format should be like D02X285 (7 characters).");

        return ValidationResult.Success;
    }
}