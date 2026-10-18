namespace PRN232.LMS.Services.Validation.Validators;

/// <summary>Shared custom validation helpers used by the FluentValidation validators.</summary>
internal static class StudentRules
{
    private static readonly string[] ValidCodePrefixes = { "SE", "CE", "IA", "AI", "MC", "GD" };

    internal static bool BeFptuStudentCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var value = code.Trim().ToUpperInvariant();
        return value.Length is 7 or 8
               && ValidCodePrefixes.Contains(value[..2])
               && value[2..].All(c => c is >= '0' and <= '9');
    }

    internal static bool BeVietnamesePhoneNumber(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length is 10 or 11 && digits.StartsWith('0');
    }
}
