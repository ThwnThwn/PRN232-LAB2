using FluentValidation;
using PRN232.LMS.Services.Models.RequestModels;

namespace PRN232.LMS.Services.Validation.Validators;

/// <summary>FluentValidation rules for UpdateStudentRequest — same rules as create.</summary>
public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required.")
            .MaximumLength(100).WithMessage("FullName must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.StudentCode)
            .Must(StudentRules.BeFptuStudentCode)
            .When(x => !string.IsNullOrWhiteSpace(x.StudentCode))
            .WithMessage("StudentCode must follow the FPTU format, e.g. SE19886 or CE18793.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth is required.")
            .GreaterThanOrEqualTo(new DateTime(1900, 1, 1)).WithMessage("DateOfBirth must be on or after 1900-01-01.")
            .Must(date => date.Date < DateTime.UtcNow.Date).WithMessage("DateOfBirth must be in the past.");

        RuleFor(x => x.PhoneNumber)
            .Must(StudentRules.BeVietnamesePhoneNumber)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("PhoneNumber must be a valid Vietnamese phone number (e.g. 0987654321).");
    }
}
