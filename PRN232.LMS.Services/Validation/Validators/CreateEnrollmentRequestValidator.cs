using FluentValidation;
using PRN232.LMS.Services.Models.RequestModels;

namespace PRN232.LMS.Services.Validation.Validators;

/// <summary>
/// FluentValidation rules for CreateEnrollmentRequest.
/// Includes a custom rule for the enrollment status set.
/// </summary>
public class CreateEnrollmentRequestValidator : AbstractValidator<CreateEnrollmentRequest>
{
    private static readonly string[] AllowedStatuses = { "Active", "Completed", "Dropped", "Pending" };

    public CreateEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a positive integer.");

        RuleFor(x => x.CourseId)
            .GreaterThan(0).WithMessage("CourseId must be a positive integer.");

        RuleFor(x => x.EnrollDate)
            .Must(date => !date.HasValue || (date.Value >= new DateTime(1900, 1, 1) && date.Value <= DateTime.UtcNow))
            .When(x => x.EnrollDate.HasValue)
            .WithMessage("EnrollDate must be on or after 1900-01-01 and cannot be in the future.");

        // --- Custom rule: status must belong to the allowed set ---
        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) ||
                            AllowedStatuses.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");
    }
}
