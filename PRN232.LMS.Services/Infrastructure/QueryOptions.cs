using PRN232.LMS.Services.Exceptions;

namespace PRN232.LMS.Services.Infrastructure;

/// <summary>
/// Parsed and validated values coming from a list-API query (search / sort / page / size / fields / expand).
/// </summary>
public sealed class QueryOptions
{
    public string? Search { get; init; }

    public IReadOnlyList<SortTerm> SortTerms { get; init; } = Array.Empty<SortTerm>();

    public int Page { get; init; } = 1;

    public int Size { get; init; } = 10;

    /// <summary>Requested field names (empty = return the whole response model).</summary>
    public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();

    /// <summary>Requested expansion tokens (empty = no expansion).</summary>
    public IReadOnlySet<string> Expand { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasExpansion(string token) => Expand.Contains(token);

    public bool HasFields => Fields.Count > 0;

    public static QueryOptions Parse(
        string? search,
        string? sort,
        int page,
        int size,
        string? fields,
        string? expand,
        IEnumerable<string> allowedSortFields,
        IEnumerable<string> allowedExpandTokens,
        IEnumerable<string> allowedFieldNames)
    {
        if (page < 1 || page > 1000000)
        {
            throw new BadRequestException("'page' must be between 1 and 1000000.");
        }

        if (size < 1 || size > 100)
        {
            throw new BadRequestException("'size' must be between 1 and 100.");
        }

        var sortTerms = ParseSort(sort, allowedSortFields);
        var fieldList = ParseFields(fields, allowedFieldNames);
        var expandSet = ParseExpand(expand, allowedExpandTokens);

        return new QueryOptions
        {
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            SortTerms = sortTerms,
            Page = page,
            Size = size,
            Fields = fieldList,
            Expand = expandSet
        };
    }

    private static IReadOnlyList<SortTerm> ParseSort(string? sort, IEnumerable<string> allowed)
    {
        var allowedSet = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
        var result = new List<SortTerm>();

        if (string.IsNullOrWhiteSpace(sort))
        {
            return result;
        }

        foreach (var raw in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var token = raw;
            var descending = false;
            if (token.StartsWith('-'))
            {
                descending = true;
                token = token[1..];
            }
            else if (token.StartsWith('+'))
            {
                token = token[1..];
            }

            if (token.Length == 0)
            {
                continue;
            }

            if (!allowedSet.Contains(token))
            {
                throw new BadRequestException(
                    $"Sort field '{token}' is not supported. Allowed fields: {string.Join(", ", allowedSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}.");
            }

            result.Add(new SortTerm(token, descending));
        }

        return result;
    }

    private static IReadOnlyList<string> ParseFields(string? fields, IEnumerable<string> allowed)
    {
        var allowedSet = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(fields))
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        foreach (var raw in fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!allowedSet.Contains(raw))
            {
                throw new BadRequestException(
                    $"Field '{raw}' is not supported. Allowed fields: {string.Join(", ", allowedSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}.");
            }

            if (!result.Contains(raw, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(raw);
            }
        }

        return result;
    }

    private static IReadOnlySet<string> ParseExpand(string? expand, IEnumerable<string> allowed)
    {
        var allowedSet = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(expand))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in expand.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!allowedSet.Contains(raw))
            {
                throw new BadRequestException(
                    $"Expand token '{raw}' is not supported. Allowed tokens: {string.Join(", ", allowedSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}.");
            }

            result.Add(raw);
        }

        return result;
    }
}
