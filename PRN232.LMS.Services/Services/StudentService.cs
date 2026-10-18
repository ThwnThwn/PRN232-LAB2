using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;
using PRN232.LMS.Services.Exceptions;
using PRN232.LMS.Services.Infrastructure;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Services;

/// <summary>
/// Student business logic. Controllers stay thin; all rules live here.
/// V2 adds studentCode / phoneNumber / age to the response contract.
/// </summary>
public class StudentService : IStudentService
{
    private static readonly string[] SortFields = { "studentId", "fullName", "email", "dateOfBirth", "studentCode" };
    private static readonly string[] ExpandTokens = { "enrollments" };
    private static readonly string[] FieldNamesV1 = { "studentId", "fullName", "email", "dateOfBirth", "enrollmentsCount" };
    private static readonly string[] FieldNamesV2 = { "studentId", "fullName", "email", "dateOfBirth", "enrollmentsCount", "studentCode", "phoneNumber", "age" };

    private readonly IRepository<Student> _students;
    private readonly IRepository<Enrollment> _enrollments;
    private readonly IRepository<Course> _courses;
    private readonly IValidator<CreateStudentRequest> _createValidator;
    private readonly IValidator<UpdateStudentRequest> _updateValidator;

    public StudentService(
        IRepository<Student> students,
        IRepository<Enrollment> enrollments,
        IRepository<Course> courses,
        IValidator<CreateStudentRequest> createValidator,
        IValidator<UpdateStudentRequest> updateValidator)
    {
        _students = students;
        _enrollments = enrollments;
        _courses = courses;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public Task<PagedResult<object>> GetStudentsAsync(StudentQueryRequest query, CancellationToken ct = default)
        // [LAB2-08] V1 dùng danh sách field và mapper V1, giữ hợp đồng trả về từ Lab 1.
        => QueryStudentsAsync(query, FieldNamesV1, (s, count, enrollments) => MapV1(s, count, enrollments), ct);

    /// <summary>API v2 list: same query features, richer response contract.</summary>
    public Task<PagedResult<object>> GetStudentsV2Async(StudentQueryRequest query, CancellationToken ct = default)
        // [LAB2-08] V2 tái sử dụng query nhưng đổi mapper/field cho phép; không nhân đôi database.
        // Chính MapV2 bổ sung dữ liệu; [ApiVersion] chỉ chọn action, không tự thêm trường response.
        => QueryStudentsAsync(query, FieldNamesV2, (s, count, enrollments) => MapV2(s, count, enrollments), ct);

    private async Task<PagedResult<object>> QueryStudentsAsync(
        StudentQueryRequest query,
        string[] fieldNames,
        Func<Student, int, IReadOnlyList<EnrollmentBriefResponse>?, object> mapper,
        CancellationToken ct)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, fieldNames);

        var q = _students.Query();
        q = QueryableHelpers.ApplyKeyword(q, opts.Search, s => s.FullName, s => s.Email, s => s.StudentCode);

        if (!string.IsNullOrWhiteSpace(query.StudentCode))
        {
            var code = query.StudentCode.Trim().ToUpperInvariant();
            q = q.Where(s => s.StudentCode == code);
        }

        q = opts.SortTerms.Count == 0
            ? q.OrderBy(s => s.StudentId)
            : QueryableHelpers.ApplySorting(q, opts.SortTerms);

        var totalItems = await q.CountAsync(ct);
        var page = await q.Skip((opts.Page - 1) * opts.Size).Take(opts.Size).ToListAsync(ct);

        var ids = page.Select(s => s.StudentId).ToList();

        // Enrollment counts for the page (single grouped query — no N+1).
        var counts = await _enrollments.Query()
            .Where(e => ids.Contains(e.StudentId))
            .GroupBy(e => e.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, ct);

        // Optional expansion: full enrollment lists for the page.
        IReadOnlyDictionary<int, List<EnrollmentBriefResponse>> expanded = new Dictionary<int, List<EnrollmentBriefResponse>>();
        if (opts.HasExpansion("enrollments"))
        {
            var studentNames = page.ToDictionary(s => s.StudentId, s => s.FullName);
            expanded = await LoadEnrollmentsGroupedByStudentAsync(ids, studentNames, ct);
        }

        var items = page.Select(s => mapper(
            s,
            counts.GetValueOrDefault(s.StudentId),
            expanded.GetValueOrDefault(s.StudentId))).ToList();

        return PagedResultFactory.Create(items, opts, totalItems);
    }

    public async Task<StudentResponse> GetStudentByIdAsync(int id, CancellationToken ct = default)
    {
        var student = await _students.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Student with id {id} was not found.");

        var enrollments = await LoadEnrollmentsOfStudentAsync(id, student.FullName, ct);
        return (StudentResponse)MapV1(student, enrollments.Count, enrollments);
    }

    /// <summary>API v2 detail: adds studentCode, phoneNumber and age.</summary>
    public async Task<StudentResponseV2> GetStudentByIdV2Async(int id, CancellationToken ct = default)
    {
        var student = await _students.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Student with id {id} was not found.");

        var enrollments = await LoadEnrollmentsOfStudentAsync(id, student.FullName, ct);
        return (StudentResponseV2)MapV2(student, enrollments.Count, enrollments);
    }

    public async Task<StudentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken ct = default)
    {
        // [LAB2-03] Lớp 1: data annotations đã được MVC kiểm tra trước action HTTP.
        // Lớp 2: gọi FluentValidation tường minh tại đây; rule sai ném ValidationException.
        // Lớp 3: các kiểm tra nghiệp vụ như email/mã SV trùng cần đọc dữ liệu qua repository.
        // Chỉ khi qua đủ các bước mới tạo entity và lưu; validation không nằm trong controller.
        await _createValidator.ValidateAndThrowAsync(request, ct);

        await EnsureEmailUniqueAsync(request.Email, null, ct);

        // Auto-generate a StudentCode if the caller did not supply one.
        var studentCode = string.IsNullOrWhiteSpace(request.StudentCode)
            ? await GenerateUniqueStudentCodeAsync(ct)
            : request.StudentCode.Trim().ToUpperInvariant();

        await EnsureStudentCodeUniqueAsync(studentCode, null, ct);

        var entity = new Student
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            DateOfBirth = request.DateOfBirth,
            StudentCode = studentCode,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim()
        };

        entity = await _students.AddAsync(entity, ct);
        return (StudentResponse)MapV1(entity, 0, null);
    }

    public async Task<StudentResponse> UpdateStudentAsync(int id, UpdateStudentRequest request, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var entity = await _students.GetByIdAsync(id, tracking: true, ct: ct)
            ?? throw new NotFoundException($"Student with id {id} was not found.");

        await EnsureEmailUniqueAsync(request.Email, id, ct);

        entity.FullName = request.FullName.Trim();
        entity.Email = request.Email.Trim();
        entity.DateOfBirth = request.DateOfBirth;
        // Only update StudentCode if caller provided one
        if (!string.IsNullOrWhiteSpace(request.StudentCode))
        {
            var newCode = request.StudentCode.Trim().ToUpperInvariant();
            await EnsureStudentCodeUniqueAsync(newCode, id, ct);
            entity.StudentCode = newCode;
        }
        entity.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        await _students.UpdateAsync(entity, ct);

        var count = await _enrollments.Query().CountAsync(e => e.StudentId == id, ct);
        return (StudentResponse)MapV1(entity, count, null);
    }

    public async Task DeleteStudentAsync(int id, CancellationToken ct = default)
    {
        var entity = await _students.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Student with id {id} was not found.");

        await _students.DeleteAsync(entity, ct);
    }

    // ---------------------------------------------------------------

    // [LAB2-03] [EmailAddress] chỉ kiểm tra định dạng, còn hàm này kiểm tra trùng email.
    // excludeId khi update giúp không xem chính sinh viên đang sửa là một bản ghi trùng.
    private async Task EnsureEmailUniqueAsync(string email, int? excludeId, CancellationToken ct)
    {
        var normalized = email.Trim();
        var duplicate = await _students.Query()
            .AnyAsync(s => s.Email == normalized && (!excludeId.HasValue || s.StudentId != excludeId.Value), ct);
        if (duplicate)
        {
            throw new BadRequestException($"A student with email '{email}' already exists.");
        }
    }

    private async Task EnsureStudentCodeUniqueAsync(string studentCode, int? excludeId, CancellationToken ct)
    {
        var normalized = studentCode.Trim().ToUpperInvariant();
        var duplicate = await _students.Query()
            .AnyAsync(s => s.StudentCode == normalized && (!excludeId.HasValue || s.StudentId != excludeId.Value), ct);
        if (duplicate)
        {
            throw new BadRequestException($"A student with code '{studentCode}' already exists.");
        }
    }

    /// <summary>Generates a unique student code in the form SE{6-digit-number}.</summary>
    private async Task<string> GenerateUniqueStudentCodeAsync(CancellationToken ct)
    {
        var rng = new Random();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = $"SE{rng.Next(100000, 999999)}";
            var exists = await _students.Query().AnyAsync(s => s.StudentCode == code, ct);
            if (!exists) return code;
        }
        // Fallback: use timestamp
        return $"SE{DateTime.UtcNow:ffffff}";
    }

    private async Task<Dictionary<int, List<EnrollmentBriefResponse>>> LoadEnrollmentsGroupedByStudentAsync(
        IReadOnlyCollection<int> studentIds,
        IReadOnlyDictionary<int, string> studentNames,
        CancellationToken ct)
    {
        var enrollments = await _enrollments.Query()
            .Where(e => studentIds.Contains(e.StudentId))
            .ToListAsync(ct);

        var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
        var courseNames = await _courses.Query()
            .Where(c => courseIds.Contains(c.CourseId))
            .ToDictionaryAsync(c => c.CourseId, c => c.CourseName, ct);

        return enrollments
            .GroupBy(e => e.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => ToEnrollmentBrief(
                    e,
                    studentNames.GetValueOrDefault(g.Key) ?? string.Empty,
                    courseNames.GetValueOrDefault(e.CourseId) ?? string.Empty)).ToList());
    }

    private async Task<List<EnrollmentBriefResponse>> LoadEnrollmentsOfStudentAsync(int studentId, string studentName, CancellationToken ct)
    {
        var enrollments = await _enrollments.Query()
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);

        var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
        var courseNames = await _courses.Query()
            .Where(c => courseIds.Contains(c.CourseId))
            .ToDictionaryAsync(c => c.CourseId, c => c.CourseName, ct);

        return enrollments
            .OrderByDescending(e => e.EnrollDate)
            .Select(e => ToEnrollmentBrief(
                e,
                studentName,
                courseNames.GetValueOrDefault(e.CourseId) ?? string.Empty))
            .ToList();
    }

    private static EnrollmentBriefResponse ToEnrollmentBrief(Enrollment e, string studentName, string courseName)
        => new()
        {
            EnrollmentId = e.EnrollmentId,
            StudentId = e.StudentId,
            StudentName = studentName,
            CourseId = e.CourseId,
            CourseName = courseName,
            EnrollDate = e.EnrollDate,
            Status = e.Status
        };

    private static StudentResponse MapV1(
        Student s,
        int enrollmentsCount,
        IReadOnlyList<EnrollmentBriefResponse>? enrollments)
        => new()
        {
            StudentId = s.StudentId,
            FullName = s.FullName,
            Email = s.Email,
            DateOfBirth = s.DateOfBirth,
            EnrollmentsCount = enrollmentsCount,
            Enrollments = enrollments
        };

    /// <summary>V2 mapper — adds the Lab 2 columns (studentCode, phoneNumber) and a computed age.</summary>
    // [LAB2-08] Entity vẫn là Student chung cho cả hai version. Response model là hợp đồng client.
    // Có thể thêm Age vào V2 mà không làm V1 xuất hiện field mới ngoài dự kiến.
    private static StudentResponseV2 MapV2(
        Student s,
        int enrollmentsCount,
        IReadOnlyList<EnrollmentBriefResponse>? enrollments)
        => new()
        {
            StudentId = s.StudentId,
            FullName = s.FullName,
            Email = s.Email,
            DateOfBirth = s.DateOfBirth,
            EnrollmentsCount = enrollmentsCount,
            Enrollments = enrollments,
            StudentCode = s.StudentCode,
            PhoneNumber = s.PhoneNumber,
            Age = CalculateAge(s.DateOfBirth)
        };

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }

        return age < 0 ? 0 : age;
    }
}
