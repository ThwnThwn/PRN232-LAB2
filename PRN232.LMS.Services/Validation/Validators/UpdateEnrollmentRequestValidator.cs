using FluentValidation;
using PRN232.LMS.Services.Models.RequestModels;

namespace PRN232.LMS.Services.Validation.Validators;

/// <summary>FluentValidation rules for UpdateEnrollmentRequest.</summary>
public class UpdateEnrollmentRequestValidator : AbstractValidator<UpdateEnrollmentRequest>
{
    private static readonly string[] AllowedStatuses = { "Active", "Completed", "Dropped", "Pending" };

    public UpdateEnrollmentRequestValidator()
    {
        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => AllowedStatuses.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");

        RuleFor(x => x.EnrollDate)
            .Must(date => !date.HasValue || (date.Value >= new DateTime(1900, 1, 1) && date.Value <= DateTime.UtcNow))
            .WithMessage("EnrollDate must be on or after 1900-01-01 and cannot be in the future.");
    }
}
