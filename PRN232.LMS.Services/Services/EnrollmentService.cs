using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;
using PRN232.LMS.Services.Exceptions;
using PRN232.LMS.Services.Infrastructure;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.BusinessModels;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Services;

/// <summary>
/// Enrollment business logic. Enrollment creation runs through the
/// <see cref="EnrollmentBusinessModel"/> (Business Model) so business rules
/// are evaluated in the Service layer, never in the repository or controller.
/// </summary>
public class EnrollmentService : IEnrollmentService
{
    private static readonly string[] SortFields = { "enrollmentId", "studentId", "courseId", "enrollDate", "status" };
    private static readonly string[] ExpandTokens = { "student", "course" };
    private static readonly string[] FieldNames = { "enrollmentId", "studentId", "studentName", "courseId", "courseName", "enrollDate", "status" };

    private readonly IRepository<Enrollment> _enrollments;
    private readonly IRepository<Student> _students;
    private readonly IRepository<Course> _courses;
    private readonly IRepository<Semester> _semesters;
    private readonly IValidator<CreateEnrollmentRequest> _createValidator;
    private readonly IValidator<UpdateEnrollmentRequest> _updateValidator;

    public EnrollmentService(
        IRepository<Enrollment> enrollments,
        IRepository<Student> students,
        IRepository<Course> courses,
        IRepository<Semester> semesters,
        IValidator<CreateEnrollmentRequest> createValidator,
        IValidator<UpdateEnrollmentRequest> updateValidator)
    {
        _enrollments = enrollments;
        _students = students;
        _courses = courses;
        _semesters = semesters;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<object>> GetEnrollmentsAsync(EnrollmentQueryRequest query, CancellationToken ct = default)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, FieldNames);

        var q = _enrollments.Query();

        // Keyword search runs across the related student/course names and the status.
        q = QueryableHelpers.ApplyKeyword(q, opts.Search,
            e => e.Student != null ? e.Student.FullName : null,
            e => e.Course != null ? e.Course.CourseName : null,
            e => e.Status);

        if (query.StudentId.HasValue)
        {
            q = q.Where(e => e.StudentId == query.StudentId.Value);
        }

        if (query.CourseId.HasValue)
        {
            q = q.Where(e => e.CourseId == query.CourseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            q = q.Where(e => e.Status.ToLower() == status);
        }

        q = opts.SortTerms.Count == 0
            ? q.OrderBy(e => e.EnrollmentId)
            : QueryableHelpers.ApplySorting(q, opts.SortTerms);

        var totalItems = await q.CountAsync(ct);
        var page = await q.Skip((opts.Page - 1) * opts.Size).Take(opts.Size).ToListAsync(ct);

        var reference = await LoadReferenceDataAsync(page, ct);

        var items = page.Select(e => ToResponse(
            e,
            reference,
            expandStudent: opts.HasExpansion("student"),
            expandCourse: opts.HasExpansion("course"))).ToList();

        return PagedResultFactory.Create(items, opts, totalItems);
    }

    public async Task<EnrollmentResponse> GetEnrollmentByIdAsync(int id, CancellationToken ct = default)
    {
        var enrollment = await _enrollments.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Enrollment with id {id} was not found.");

        var reference = await LoadReferenceDataAsync(new[] { enrollment }, ct);
        return ToResponse(enrollment, reference, expandStudent: true, expandCourse: true);
    }

    public async Task<EnrollmentResponse> CreateEnrollmentAsync(CreateEnrollmentRequest request, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        // --- Business rules evaluated on the Business Model (Service layer) ---
        var business = new EnrollmentBusinessModel
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            EnrollDate = request.EnrollDate ?? DateTime.Now,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim()
        };

        var student = await _students.GetByIdAsync(business.StudentId, ct: ct)
            ?? throw new BadRequestException($"Student with id {business.StudentId} does not exist.");

        var course = await _courses.GetByIdAsync(business.CourseId, ct: ct)
            ?? throw new BadRequestException($"Course with id {business.CourseId} does not exist.");

        business.StudentName = student.FullName;
        business.CourseName = course.CourseName;

        var duplicateActive = await _enrollments.Query().AnyAsync(
            e => e.StudentId == business.StudentId
                 && e.CourseId == business.CourseId
                 && e.Status == "Active", ct);
        if (duplicateActive)
        {
            throw new BadRequestException(
                $"{student.FullName} already has an active enrollment in course '{course.CourseName}'.");
        }

        var entity = new Enrollment
        {
            StudentId = business.StudentId,
            CourseId = business.CourseId,
            EnrollDate = business.EnrollDate,
            Status = business.Status
        };

        entity = await _enrollments.AddAsync(entity, ct);

        var reference = await LoadReferenceDataAsync(new[] { entity }, ct);
        return ToResponse(entity, reference, expandStudent: true, expandCourse: true);
    }

    public async Task<EnrollmentResponse> UpdateEnrollmentAsync(int id, UpdateEnrollmentRequest request, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var entity = await _enrollments.GetByIdAsync(id, tracking: true, ct: ct)
            ?? throw new NotFoundException($"Enrollment with id {id} was not found.");

        entity.Status = request.Status.Trim();
        if (request.EnrollDate.HasValue)
        {
            entity.EnrollDate = request.EnrollDate.Value;
        }

        await _enrollments.UpdateAsync(entity, ct);

        var reference = await LoadReferenceDataAsync(new[] { entity }, ct);
        return ToResponse(entity, reference, expandStudent: true, expandCourse: true);
    }

    /// <summary>API v2 list — adds the semester of the course as flat fields.</summary>
    public async Task<PagedResult<object>> GetEnrollmentsV2Async(EnrollmentQueryRequest query, CancellationToken ct = default)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, FieldNames.Concat(new[] { "semesterId", "semesterName" }));
        var v1 = await GetEnrollmentsAsync(new EnrollmentQueryRequest
        {
            Search = query.Search,
            Sort = query.Sort,
            Page = query.Page,
            Size = query.Size,
            Expand = query.Expand,
            StudentId = query.StudentId,
            CourseId = query.CourseId,
            Status = query.Status
        }, ct);

        var items = v1.Items.OfType<EnrollmentResponse>().ToList();
        var semesterInfo = await LoadSemesterInfoAsync(items.Select(e => e.CourseId).Distinct().ToList(), ct);

        var v2Items = items.Select(e =>
        {
            var info = semesterInfo.GetValueOrDefault(e.CourseId);
            return new EnrollmentResponseV2
            {
                EnrollmentId = e.EnrollmentId,
                StudentId = e.StudentId,
                StudentName = e.StudentName,
                CourseId = e.CourseId,
                CourseName = e.CourseName,
                EnrollDate = e.EnrollDate,
                Status = e.Status,
                Student = e.Student,
                Course = e.Course,
                SemesterId = info?.SemesterId ?? 0,
                SemesterName = info?.SemesterName ?? string.Empty
            };
        }).Cast<object>().ToList();

        return PagedResultFactory.Create(v2Items, opts, v1.Pagination.TotalItems);
    }

    /// <summary>API v2 detail.</summary>
    public async Task<EnrollmentResponseV2> GetEnrollmentByIdV2Async(int id, CancellationToken ct = default)
    {
        var enrollment = await GetEnrollmentByIdAsync(id, ct);
        var semesterInfo = await LoadSemesterInfoAsync(new[] { enrollment.CourseId }, ct);
        var info = semesterInfo.GetValueOrDefault(enrollment.CourseId);

        return new EnrollmentResponseV2
        {
            EnrollmentId = enrollment.EnrollmentId,
            StudentId = enrollment.StudentId,
            StudentName = enrollment.StudentName,
            CourseId = enrollment.CourseId,
            CourseName = enrollment.CourseName,
            EnrollDate = enrollment.EnrollDate,
            Status = enrollment.Status,
            Student = enrollment.Student,
            Course = enrollment.Course,
            SemesterId = info?.SemesterId ?? 0,
            SemesterName = info?.SemesterName ?? string.Empty
        };
    }

    /// <summary>Loads the semester (id + name) of each given course — used by the v2 contract.</summary>
    private async Task<Dictionary<int, SemesterLookup>> LoadSemesterInfoAsync(
        IReadOnlyCollection<int> courseIds,
        CancellationToken ct)
    {
        var courses = await _courses.Query()
            .Where(c => courseIds.Contains(c.CourseId))
            .ToListAsync(ct);

        var semesterIds = courses.Select(c => c.SemesterId).Distinct().ToList();
        var semesterNames = await _semesters.Query()
            .Where(s => semesterIds.Contains(s.SemesterId))
            .ToDictionaryAsync(s => s.SemesterId, s => s.SemesterName, ct);

        return courses.ToDictionary(
            c => c.CourseId,
            c => new SemesterLookup(c.SemesterId, semesterNames.GetValueOrDefault(c.SemesterId) ?? string.Empty));
    }

    private sealed record SemesterLookup(int SemesterId, string SemesterName);

    public async Task DeleteEnrollmentAsync(int id, CancellationToken ct = default)
    {
        var entity = await _enrollments.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Enrollment with id {id} was not found.");

        await _enrollments.DeleteAsync(entity, ct);
    }

    // ---------------------------------------------------------------

    /// <summary>Students, courses and semesters referenced by the given enrollments (single batch load, no N+1).</summary>
    private async Task<ReferenceData> LoadReferenceDataAsync(IEnumerable<Enrollment> enrollments, CancellationToken ct)
    {
        var list = enrollments.ToList();
        var studentIds = list.Select(e => e.StudentId).Distinct().ToList();
        var courseIds = list.Select(e => e.CourseId).Distinct().ToList();

        var students = await _students.Query()
            .Where(s => studentIds.Contains(s.StudentId))
            .ToDictionaryAsync(s => s.StudentId, ct);

        var courses = await _courses.Query()
            .Where(c => courseIds.Contains(c.CourseId))
            .ToDictionaryAsync(c => c.CourseId, ct);

        var semesterIds = courses.Values.Select(c => c.SemesterId).Distinct().ToList();
        var semesters = await _semesters.Query()
            .Where(s => semesterIds.Contains(s.SemesterId))
            .ToDictionaryAsync(s => s.SemesterId, ct);

        return new ReferenceData(students, courses, semesters);
    }

    private static EnrollmentResponse ToResponse(
        Enrollment e,
        ReferenceData reference,
        bool expandStudent,
        bool expandCourse)
    {
        reference.Students.TryGetValue(e.StudentId, out var student);
        reference.Courses.TryGetValue(e.CourseId, out var course);
        var semesterName = course != null && reference.Semesters.TryGetValue(course.SemesterId, out var semester)
            ? semester.SemesterName
            : string.Empty;

        return new EnrollmentResponse
        {
            EnrollmentId = e.EnrollmentId,
            StudentId = e.StudentId,
            StudentName = student?.FullName ?? string.Empty,
            CourseId = e.CourseId,
            CourseName = course?.CourseName ?? string.Empty,
            EnrollDate = e.EnrollDate,
            Status = e.Status,
            Student = expandStudent && student is not null
                ? new StudentBriefResponse
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    Email = student.Email,
                    DateOfBirth = student.DateOfBirth
                }
                : null,
            Course = expandCourse && course is not null
                ? new CourseBriefResponse
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    SemesterId = course.SemesterId,
                    SemesterName = semesterName
                }
                : null
        };
    }

    private sealed record ReferenceData(
        IReadOnlyDictionary<int, Student> Students,
        IReadOnlyDictionary<int, Course> Courses,
        IReadOnlyDictionary<int, Semester> Semesters);
}
