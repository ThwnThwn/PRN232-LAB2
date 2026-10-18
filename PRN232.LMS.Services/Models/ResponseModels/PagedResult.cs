namespace PRN232.LMS.Services.Models.ResponseModels;

/// <summary>
/// Pagination metadata returned by every list API:
/// { "pagination": { "page", "pageSize", "totalItems", "totalPages" } }
/// </summary>
public class PaginationMetadata
{
    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }
}

/// <summary>
/// Wrapper around the page of items + its pagination metadata.
/// Serialized as: { "items": [...], "pagination": {...} }
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public PaginationMetadata Pagination { get; init; } = new();
}
