using Microsoft.Extensions.DependencyInjection;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>Registers the Repository layer services into DI.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRepository<Semester>, SemesterRepository>();
        services.AddScoped<IRepository<Course>, CourseRepository>();
        services.AddScoped<IRepository<Subject>, SubjectRepository>();
        services.AddScoped<IRepository<Student>, StudentRepository>();
        services.AddScoped<IRepository<Enrollment>, EnrollmentRepository>();

        // Lab 2 — authentication tables.
        services.AddScoped<IRepository<User>, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRepository<RefreshToken>>(sp => sp.GetRequiredService<IRefreshTokenRepository>());
        return services;
    }
}
