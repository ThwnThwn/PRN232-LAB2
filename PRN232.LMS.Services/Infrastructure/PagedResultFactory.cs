using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Infrastructure;

/// <summary>Builds <see cref="PagedResult{T}"/> payloads consistently across list APIs.</summary>
public static class PagedResultFactory
{
    public static PagedResult<object> Create<TSource>(
        List<TSource> typedItems,
        QueryOptions opts,
        int totalItems)
    {
        IReadOnlyList<object> payload = opts.HasFields
            ? QueryableHelpers.ProjectFields(typedItems, opts.Fields).Cast<object>().ToList()
            : typedItems.Cast<object>().ToList();

        return new PagedResult<object>
        {
            Items = payload,
            Pagination = new PaginationMetadata
            {
                Page = opts.Page,
                PageSize = opts.Size,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)opts.Size)
            }
        };
    }
}
