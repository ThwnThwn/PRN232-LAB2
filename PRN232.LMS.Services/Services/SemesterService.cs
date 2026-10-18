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
/// Semester business logic: list (search/sort/page/fields/expand), get-by-id, create, update, delete.
/// No data-access code here — everything goes through the repository.
/// </summary>
public class SemesterService : ISemesterService
{
    private static readonly string[] SortFields = { "semesterId", "semesterName", "startDate", "endDate" };
    private static readonly string[] ExpandTokens = { "courses" };
    private static readonly string[] FieldNames = { "semesterId", "semesterName", "startDate", "endDate" };

    private readonly IRepository<Semester> _semesters;
    private readonly IRepository<Course> _courses;

    public SemesterService(IRepository<Semester> semesters, IRepository<Course> courses)
    {
        _semesters = semesters;
        _courses = courses;
    }

    public async Task<PagedResult<object>> GetSemestersAsync(SemesterQueryRequest query, CancellationToken ct = default)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, FieldNames);

        var q = _semesters.Query();
        q = QueryableHelpers.ApplyKeyword(q, opts.Search, s => s.SemesterName);

        q = opts.SortTerms.Count == 0
            ? q.OrderBy(s => s.SemesterId)
            : QueryableHelpers.ApplySorting(q, opts.SortTerms);

        var totalItems = await q.CountAsync(ct);
        var page = await q.Skip((opts.Page - 1) * opts.Size).Take(opts.Size).ToListAsync(ct);

        // Optional expansion: load courses of the page of semesters.
        IReadOnlyDictionary<int, List<Course>> coursesBySemester = new Dictionary<int, List<Course>>();
        if (opts.HasExpansion("courses"))
        {
            var ids = page.Select(s => s.SemesterId).ToList();
            var courses = await _courses.Query()
                .Where(c => ids.Contains(c.SemesterId))
                .ToListAsync(ct);
            coursesBySemester = courses.GroupBy(c => c.SemesterId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        var items = page.Select(s => ToResponse(s, coursesBySemester.GetValueOrDefault(s.SemesterId))).ToList();

        return PagedResultFactory.Create(items, opts, totalItems);
    }

    public async Task<SemesterResponse> GetSemesterByIdAsync(int id, CancellationToken ct = default)
    {
        var semester = await _semesters.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Semester with id {id} was not found.");

        var courses = await _courses.Query().Where(c => c.SemesterId == id).ToListAsync(ct);
        return ToResponse(semester, courses);
    }

    public async Task<SemesterResponse> CreateSemesterAsync(CreateSemesterRequest request, CancellationToken ct = default)
    {
        if (request.EndDate <= request.StartDate)
        {
            throw new BadRequestException("EndDate must be after StartDate.");
        }

        var duplicate = await _semesters.Query()
            .AnyAsync(s => s.SemesterName == request.SemesterName, ct);
        if (duplicate)
        {
            throw new BadRequestException($"A semester named '{request.SemesterName}' already exists.");
        }

        var entity = new Semester
        {
            SemesterName = request.SemesterName.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        entity = await _semesters.AddAsync(entity, ct);
        return ToResponse(entity, null);
    }

    public async Task<SemesterResponse> UpdateSemesterAsync(int id, UpdateSemesterRequest request, CancellationToken ct = default)
    {
        if (request.EndDate <= request.StartDate)
        {
            throw new BadRequestException("EndDate must be after StartDate.");
        }

        var entity = await _semesters.GetByIdAsync(id, tracking: true, ct: ct)
            ?? throw new NotFoundException($"Semester with id {id} was not found.");

        var duplicate = await _semesters.Query()
            .AnyAsync(s => s.SemesterName == request.SemesterName && s.SemesterId != id, ct);
        if (duplicate)
        {
            throw new BadRequestException($"A semester named '{request.SemesterName}' already exists.");
        }

        entity.SemesterName = request.SemesterName.Trim();
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;

        await _semesters.UpdateAsync(entity, ct);
        return ToResponse(entity, null);
    }

    /// <summary>API v2 list — same filters as v1, plus the number of courses per semester.</summary>
    public async Task<PagedResult<object>> GetSemestersV2Async(SemesterQueryRequest query, CancellationToken ct = default)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, FieldNames.Concat(new[] { "coursesCount" }));
        var v1 = await GetSemestersAsync(new SemesterQueryRequest
        {
            Search = query.Search,
            Sort = query.Sort,
            Page = query.Page,
            Size = query.Size,
            Expand = query.Expand
        }, ct);

        var items = v1.Items.OfType<SemesterResponse>().ToList();
        var ids = items.Select(s => s.SemesterId).ToList();

        var counts = await _courses.Query()
            .Where(c => ids.Contains(c.SemesterId))
            .GroupBy(c => c.SemesterId)
            .Select(g => new { SemesterId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SemesterId, x => x.Count, ct);

        var v2Items = items.Select(s => new SemesterResponseV2
        {
            SemesterId = s.SemesterId,
            SemesterName = s.SemesterName,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Courses = s.Courses,
            CoursesCount = counts.GetValueOrDefault(s.SemesterId)
        }).Cast<object>().ToList();

        return PagedResultFactory.Create(v2Items, opts, v1.Pagination.TotalItems);
    }

    /// <summary>API v2 detail.</summary>
    public async Task<SemesterResponseV2> GetSemesterByIdV2Async(int id, CancellationToken ct = default)
    {
        var semester = await GetSemesterByIdAsync(id, ct);
        var count = await _courses.Query().CountAsync(c => c.SemesterId == id, ct);

        return new SemesterResponseV2
        {
            SemesterId = semester.SemesterId,
            SemesterName = semester.SemesterName,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            Courses = semester.Courses,
            CoursesCount = count
        };
    }

    public async Task DeleteSemesterAsync(int id, CancellationToken ct = default)
    {
        var entity = await _semesters.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Semester with id {id} was not found.");

        await _semesters.DeleteAsync(entity, ct);
    }

    private static SemesterResponse ToResponse(Semester s, IReadOnlyList<Course>? courses)
        => new()
        {
            SemesterId = s.SemesterId,
            SemesterName = s.SemesterName,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Courses = courses is null
                ? null
                : courses.Select(c => new CourseBriefResponse
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    SemesterId = c.SemesterId,
                    SemesterName = s.SemesterName
                }).ToList()
        };
}
