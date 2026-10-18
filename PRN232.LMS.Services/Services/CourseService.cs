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
/// Course business logic.
/// A course belongs to one semester and contains many enrollments.
/// </summary>
public class CourseService : ICourseService
{
    private static readonly string[] SortFields = { "courseId", "courseName", "semesterId" };
    private static readonly string[] ExpandTokens = { "semester", "enrollments" };
    private static readonly string[] FieldNames = { "courseId", "courseName", "semesterId", "semesterName" };

    private readonly IRepository<Course> _courses;
    private readonly IRepository<Semester> _semesters;
    private readonly IRepository<Enrollment> _enrollments;
    private readonly IRepository<Student> _students;

    public CourseService(
        IRepository<Course> courses,
        IRepository<Semester> semesters,
        IRepository<Enrollment> enrollments,
        IRepository<Student> students)
    {
        _courses = courses;
        _semesters = semesters;
        _enrollments = enrollments;
        _students = students;
    }

    public async Task<PagedResult<object>> GetCoursesAsync(CourseQueryRequest query, CancellationToken ct = default)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, FieldNames);

        var q = _courses.Query();
        q = QueryableHelpers.ApplyKeyword(q, opts.Search, c => c.CourseName);

        if (query.SemesterId.HasValue)
        {
            q = q.Where(c => c.SemesterId == query.SemesterId.Value);
        }

        q = opts.SortTerms.Count == 0
            ? q.OrderBy(c => c.CourseId)
            : QueryableHelpers.ApplySorting(q, opts.SortTerms);

        var totalItems = await q.CountAsync(ct);
        var page = await q.Skip((opts.Page - 1) * opts.Size).Take(opts.Size).ToListAsync(ct);

        var semesterIds = page.Select(c => c.SemesterId).Distinct().ToList();
        var semesters = await _semesters.Query()
            .Where(s => semesterIds.Contains(s.SemesterId))
            .ToDictionaryAsync(s => s.SemesterId, ct);

        // Optional expansion: enrollments of the page of courses.
        IReadOnlyDictionary<int, List<EnrollmentBriefResponse>> expanded = new Dictionary<int, List<EnrollmentBriefResponse>>();
        if (opts.HasExpansion("enrollments"))
        {
            var courseNames = page.ToDictionary(c => c.CourseId, c => c.CourseName);
            expanded = await LoadEnrollmentsGroupedByCourseAsync(page.Select(c => c.CourseId).ToList(), courseNames, ct);
        }

        var items = page.Select(c => ToResponse(
            c,
            semesters.GetValueOrDefault(c.SemesterId),
            opts.HasExpansion("semester") ? semesters.GetValueOrDefault(c.SemesterId) : null,
            expanded.GetValueOrDefault(c.CourseId))).ToList();

        return PagedResultFactory.Create(items, opts, totalItems);
    }

    public async Task<CourseResponse> GetCourseByIdAsync(int id, CancellationToken ct = default)
    {
        var course = await _courses.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Course with id {id} was not found.");

        var semester = await _semesters.GetByIdAsync(course.SemesterId, ct: ct);
        var enrollments = await LoadEnrollmentsOfCourseAsync(id, course.CourseName, ct);

        return ToResponse(course, semester, semester, enrollments);
    }

    public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        if (!await _semesters.ExistsAsync(request.SemesterId, ct))
        {
            throw new BadRequestException($"Semester with id {request.SemesterId} does not exist.");
        }

        var entity = new Course
        {
            CourseName = request.CourseName.Trim(),
            SemesterId = request.SemesterId
        };

        entity = await _courses.AddAsync(entity, ct);
        var semester = await _semesters.GetByIdAsync(entity.SemesterId, ct: ct);
        return ToResponse(entity, semester, null, null);
    }

    public async Task<CourseResponse> UpdateCourseAsync(int id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var entity = await _courses.GetByIdAsync(id, tracking: true, ct: ct)
            ?? throw new NotFoundException($"Course with id {id} was not found.");

        if (!await _semesters.ExistsAsync(request.SemesterId, ct))
        {
            throw new BadRequestException($"Semester with id {request.SemesterId} does not exist.");
        }

        entity.CourseName = request.CourseName.Trim();
        entity.SemesterId = request.SemesterId;

        await _courses.UpdateAsync(entity, ct);

        var semester = await _semesters.GetByIdAsync(entity.SemesterId, ct: ct);
        return ToResponse(entity, semester, null, null);
    }

    /// <summary>API v2 list — same filters as v1, plus the number of enrollments per course.</summary>
    public async Task<PagedResult<object>> GetCoursesV2Async(CourseQueryRequest query, CancellationToken ct = default)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, FieldNames.Concat(new[] { "enrollmentsCount" }));
        var v1 = await GetCoursesAsync(new CourseQueryRequest
        {
            Search = query.Search,
            Sort = query.Sort,
            Page = query.Page,
            Size = query.Size,
            Expand = query.Expand,
            SemesterId = query.SemesterId
        }, ct);

        var items = v1.Items.OfType<CourseResponse>().ToList();
        var ids = items.Select(c => c.CourseId).ToList();

        var counts = await _enrollments.Query()
            .Where(e => ids.Contains(e.CourseId))
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, ct);

        var v2Items = items.Select(c => new CourseResponseV2
        {
            CourseId = c.CourseId,
            CourseName = c.CourseName,
            SemesterId = c.SemesterId,
            SemesterName = c.SemesterName,
            Semester = c.Semester,
            Enrollments = c.Enrollments,
            EnrollmentsCount = counts.GetValueOrDefault(c.CourseId)
        }).Cast<object>().ToList();

        return PagedResultFactory.Create(v2Items, opts, v1.Pagination.TotalItems);
    }

    /// <summary>API v2 detail.</summary>
    public async Task<CourseResponseV2> GetCourseByIdV2Async(int id, CancellationToken ct = default)
    {
        var course = await GetCourseByIdAsync(id, ct);
        var count = await _enrollments.Query().CountAsync(e => e.CourseId == id, ct);

        return new CourseResponseV2
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            SemesterId = course.SemesterId,
            SemesterName = course.SemesterName,
            Semester = course.Semester,
            Enrollments = course.Enrollments,
            EnrollmentsCount = count
        };
    }

    /// <summary>Nested resource (Lab 2): students enrolled in a course, with the usual query features.</summary>
    public async Task<PagedResult<object>> GetCourseStudentsAsync(
        int courseId,
        StudentQueryRequest query,
        CancellationToken ct = default)
    {
        if (!await _courses.ExistsAsync(courseId, ct))
        {
            throw new NotFoundException($"Course with id {courseId} was not found.");
        }

        var allowedSortFields = new[] { "studentId", "fullName", "email", "dateOfBirth", "studentCode" };
        var allowedFieldNames = new[] { "studentId", "fullName", "email", "dateOfBirth", "enrollmentsCount" };

        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            allowedSortFields, Array.Empty<string>(), allowedFieldNames);

        var q = _students.Query().Where(s => s.Enrollments.Any(e => e.CourseId == courseId));
        q = QueryableHelpers.ApplyKeyword(q, opts.Search, s => s.FullName, s => s.Email, s => s.StudentCode);

        q = opts.SortTerms.Count == 0
            ? q.OrderBy(s => s.StudentId)
            : QueryableHelpers.ApplySorting(q, opts.SortTerms);

        var totalItems = await q.CountAsync(ct);
        var page = await q.Skip((opts.Page - 1) * opts.Size).Take(opts.Size).ToListAsync(ct);

        var ids = page.Select(s => s.StudentId).ToList();
        var counts = await _enrollments.Query()
            .Where(e => ids.Contains(e.StudentId))
            .GroupBy(e => e.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, ct);

        var items = page.Select(s => new StudentResponse
        {
            StudentId = s.StudentId,
            FullName = s.FullName,
            Email = s.Email,
            DateOfBirth = s.DateOfBirth,
            EnrollmentsCount = counts.GetValueOrDefault(s.StudentId)
        }).ToList();

        return PagedResultFactory.Create(items, opts, totalItems);
    }

    public async Task DeleteCourseAsync(int id, CancellationToken ct = default)
    {
        var entity = await _courses.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Course with id {id} was not found.");

        await _courses.DeleteAsync(entity, ct);
    }

    // ---------------------------------------------------------------

    private async Task<Dictionary<int, List<EnrollmentBriefResponse>>> LoadEnrollmentsGroupedByCourseAsync(
        IReadOnlyCollection<int> courseIds,
        IReadOnlyDictionary<int, string> courseNames,
        CancellationToken ct)
    {
        var enrollments = await _enrollments.Query()
            .Where(e => courseIds.Contains(e.CourseId))
            .ToListAsync(ct);

        var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();
        var studentNames = await _students.Query()
            .Where(s => studentIds.Contains(s.StudentId))
            .ToDictionaryAsync(s => s.StudentId, s => s.FullName, ct);

        return enrollments
            .GroupBy(e => e.CourseId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => ToEnrollmentBrief(
                    e,
                    courseNames.GetValueOrDefault(g.Key) ?? string.Empty,
                    studentNames.GetValueOrDefault(e.StudentId) ?? string.Empty)).ToList());
    }

    private async Task<List<EnrollmentBriefResponse>> LoadEnrollmentsOfCourseAsync(int courseId, string courseName, CancellationToken ct)
    {
        var enrollments = await _enrollments.Query()
            .Where(e => e.CourseId == courseId)
            .ToListAsync(ct);

        var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();
        var studentNames = await _students.Query()
            .Where(s => studentIds.Contains(s.StudentId))
            .ToDictionaryAsync(s => s.StudentId, s => s.FullName, ct);

        return enrollments
            .OrderByDescending(e => e.EnrollDate)
            .Select(e => ToEnrollmentBrief(
                e,
                courseName,
                studentNames.GetValueOrDefault(e.StudentId) ?? string.Empty))
            .ToList();
    }

    private static EnrollmentBriefResponse ToEnrollmentBrief(Enrollment e, string courseName, string studentName)
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

    private static CourseResponse ToResponse(
        Course c,
        Semester? semester,
        Semester? nestedSemester,
        IReadOnlyList<EnrollmentBriefResponse>? enrollments)
        => new()
        {
            CourseId = c.CourseId,
            CourseName = c.CourseName,
            SemesterId = c.SemesterId,
            SemesterName = semester?.SemesterName ?? string.Empty,
            Semester = nestedSemester is null
                ? null
                : new SemesterBriefResponse
                {
                    SemesterId = nestedSemester.SemesterId,
                    SemesterName = nestedSemester.SemesterName,
                    StartDate = nestedSemester.StartDate,
                    EndDate = nestedSemester.EndDate
                },
            Enrollments = enrollments
        };
}
