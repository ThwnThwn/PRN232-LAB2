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
/// Subject business logic. A subject is a stand-alone catalog record
/// (no relationships in Lab 1), so it has no expansion tokens.
/// </summary>
public class SubjectService : ISubjectService
{
    private static readonly string[] SortFields = { "subjectId", "subjectCode", "subjectName", "credit" };
    private static readonly string[] ExpandTokens = Array.Empty<string>();
    private static readonly string[] FieldNames = { "subjectId", "subjectCode", "subjectName", "credit" };

    private readonly IRepository<Subject> _subjects;

    public SubjectService(IRepository<Subject> subjects)
    {
        _subjects = subjects;
    }

    public async Task<PagedResult<object>> GetSubjectsAsync(SubjectQueryRequest query, CancellationToken ct = default)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, FieldNames);

        var q = _subjects.Query();
        q = QueryableHelpers.ApplyKeyword(q, opts.Search, s => s.SubjectCode, s => s.SubjectName);

        if (query.Credit.HasValue)
        {
            q = q.Where(s => s.Credit == query.Credit.Value);
        }

        q = opts.SortTerms.Count == 0
            ? q.OrderBy(s => s.SubjectId)
            : QueryableHelpers.ApplySorting(q, opts.SortTerms);

        var totalItems = await q.CountAsync(ct);
        var page = await q.Skip((opts.Page - 1) * opts.Size).Take(opts.Size).ToListAsync(ct);

        var items = page.Select(ToResponse).ToList();
        return PagedResultFactory.Create(items, opts, totalItems);
    }

    public async Task<SubjectResponse> GetSubjectByIdAsync(int id, CancellationToken ct = default)
    {
        var subject = await _subjects.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Subject with id {id} was not found.");

        return ToResponse(subject);
    }

    public async Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct = default)
    {
        await EnsureCodeUniqueAsync(request.SubjectCode, null, ct);

        var entity = new Subject
        {
            SubjectCode = request.SubjectCode.Trim().ToUpperInvariant(),
            SubjectName = request.SubjectName.Trim(),
            Credit = request.Credit
        };

        entity = await _subjects.AddAsync(entity, ct);
        return ToResponse(entity);
    }

    public async Task<SubjectResponse> UpdateSubjectAsync(int id, UpdateSubjectRequest request, CancellationToken ct = default)
    {
        var entity = await _subjects.GetByIdAsync(id, tracking: true, ct: ct)
            ?? throw new NotFoundException($"Subject with id {id} was not found.");

        await EnsureCodeUniqueAsync(request.SubjectCode, id, ct);

        entity.SubjectCode = request.SubjectCode.Trim().ToUpperInvariant();
        entity.SubjectName = request.SubjectName.Trim();
        entity.Credit = request.Credit;

        await _subjects.UpdateAsync(entity, ct);
        return ToResponse(entity);
    }

    /// <summary>API v2 list — adds a human-friendly display name.</summary>
    public async Task<PagedResult<object>> GetSubjectsV2Async(SubjectQueryRequest query, CancellationToken ct = default)
    {
        var opts = QueryOptions.Parse(
            query.Search, query.Sort, query.Page, query.Size, query.Fields, query.Expand,
            SortFields, ExpandTokens, FieldNames.Concat(new[] { "displayName" }));
        var v1 = await GetSubjectsAsync(new SubjectQueryRequest
        {
            Search = query.Search,
            Sort = query.Sort,
            Page = query.Page,
            Size = query.Size,
            Expand = query.Expand,
            Credit = query.Credit
        }, ct);

        var items = v1.Items.OfType<SubjectResponse>()
            .Select(s => new SubjectResponseV2
            {
                SubjectId = s.SubjectId,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                Credit = s.Credit,
                DisplayName = $"{s.SubjectCode} - {s.SubjectName}"
            })
            .Cast<object>()
            .ToList();

        return PagedResultFactory.Create(items, opts, v1.Pagination.TotalItems);
    }

    /// <summary>API v2 detail.</summary>
    public async Task<SubjectResponseV2> GetSubjectByIdV2Async(int id, CancellationToken ct = default)
    {
        var subject = await GetSubjectByIdAsync(id, ct);
        return new SubjectResponseV2
        {
            SubjectId = subject.SubjectId,
            SubjectCode = subject.SubjectCode,
            SubjectName = subject.SubjectName,
            Credit = subject.Credit,
            DisplayName = $"{subject.SubjectCode} - {subject.SubjectName}"
        };
    }

    public async Task DeleteSubjectAsync(int id, CancellationToken ct = default)
    {
        var entity = await _subjects.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException($"Subject with id {id} was not found.");

        await _subjects.DeleteAsync(entity, ct);
    }

    private async Task EnsureCodeUniqueAsync(string code, int? excludeId, CancellationToken ct)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var duplicate = await _subjects.Query()
            .AnyAsync(s => s.SubjectCode == normalized && (!excludeId.HasValue || s.SubjectId != excludeId.Value), ct);
        if (duplicate)
        {
            throw new BadRequestException($"A subject with code '{code}' already exists.");
        }
    }

    private static SubjectResponse ToResponse(Subject s)
        => new()
        {
            SubjectId = s.SubjectId,
            SubjectCode = s.SubjectCode,
            SubjectName = s.SubjectName,
            Credit = s.Credit
        };
}
