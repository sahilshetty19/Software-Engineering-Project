using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Validation;

public sealed class MinimumAgeAttribute : ValidationAttribute
{
    private readonly int _minAge;

    public MinimumAgeAttribute(int minAge) => _minAge = minAge;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime dob)
            return new ValidationResult("Date of birth is required.");

        var today = DateTime.Today;
        if (dob.Date > today)
            return new ValidationResult("Date of birth cannot be in the future.");

        var age = today.Year - dob.Year;
        if (dob.Date > today.AddYears(-age)) age--;

        if (age < _minAge)
            return new ValidationResult($"Customer must be at least {_minAge} years old.");

        return ValidationResult.Success;
    }
}