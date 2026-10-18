using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Security;
using PRN232.LMS.Services.Services;
using PRN232.LMS.Services.Validation.Validators;

namespace PRN232.LMS.Services;

/// <summary>Registers the Service layer (business logic, validation, security) into DI.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Business services
        services.AddScoped<ISemesterService, SemesterService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();

        // Lab 2 — authentication & security
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // [LAB2-03] DI: khi StudentService cần IValidator<CreateStudentRequest>, container
        // tạo CreateStudentRequestValidator và đưa qua constructor. AddScoped dùng cùng
        // scope request. Đăng ký không đồng nghĩa đã chạy validate; service phải gọi validator.
        services.AddScoped<IValidator<CreateStudentRequest>, CreateStudentRequestValidator>();
        services.AddScoped<IValidator<UpdateStudentRequest>, UpdateStudentRequestValidator>();
        services.AddScoped<IValidator<CreateEnrollmentRequest>, CreateEnrollmentRequestValidator>();
        services.AddScoped<IValidator<UpdateEnrollmentRequest>, UpdateEnrollmentRequestValidator>();

        return services;
    }
}

