using System.Linq.Expressions;
using System.Reflection;
using PRN232.LMS.Services.Exceptions;

namespace PRN232.LMS.Services.Infrastructure;

/// <summary>
/// Small query building helpers used by the Service layer:
/// keyword (search) filtering, dynamic multi-field sorting and field projection.
/// </summary>
public static class QueryableHelpers
{
    /// <summary>
    /// Filters the query so that every whitespace/comma-separated term of <paramref name="keyword"/>
    /// appears (case-insensitively) in at least one of the given text fields.
    /// </summary>
    public static IQueryable<T> ApplyKeyword<T>(
        IQueryable<T> query,
        string? keyword,
        params Expression<Func<T, string?>>[] textFields)
    {
        if (string.IsNullOrWhiteSpace(keyword) || textFields.Length == 0)
        {
            return query;
        }

        var terms = keyword.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var term in terms)
        {
            query = query.Where(BuildAnyContainsPredicate(term, textFields));
        }

        return query;
    }

    /// <summary>Applies an ordered list of sort terms to the query (OrderBy + ThenBy chain).</summary>
    public static IQueryable<T> ApplySorting<T>(IQueryable<T> query, IReadOnlyList<SortTerm> sortTerms)
    {
        var ordered = false;
        foreach (var term in sortTerms)
        {
            query = ordered
                ? OrderByDynamic((IOrderedQueryable<T>)query, term.Field, term.Descending, thenBy: true)
                : OrderByDynamic(query, term.Field, term.Descending, thenBy: false);
            ordered = true;
        }

        return query;
    }

    /// <summary>
    /// Projects each DTO instance into a dictionary holding only the requested field names.
    /// The properties are read from the runtime type of the items, so field selection also
    /// works when the list is typed as <see cref="object"/> (v1/v2 response mappers).
    /// </summary>
    public static List<Dictionary<string, object?>> ProjectFields<T>(IEnumerable<T> items, IReadOnlyList<string> fields)
    {
        var itemList = items as IList<T> ?? items.ToList();
        var sampleType = itemList.FirstOrDefault()?.GetType() ?? typeof(T);

        var properties = sampleType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => fields.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var result = new List<Dictionary<string, object?>>();
        foreach (var item in itemList)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in properties)
            {
                dict[ToCamelCase(property.Name)] = property.GetValue(item);
            }

            result.Add(dict);
        }

        return result;
    }

    private static string ToCamelCase(string name)
        => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    // ---------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------

    private static Expression<Func<T, bool>> BuildAnyContainsPredicate<T>(
        string term,
        params Expression<Func<T, string?>>[] textFields)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? body = null;
        var lowerTerm = term.ToLowerInvariant();

        foreach (var field in textFields)
        {
            var fieldAccess = ReplaceParameter(field.Body, field.Parameters[0], parameter);
            if (fieldAccess.Type != typeof(string))
            {
                fieldAccess = Expression.Convert(fieldAccess, typeof(string));
            }

            // Case-insensitive Contains (translates to LOWER(...) LIKE ... on SQL Server,
            // and behaves consistently on the in-memory provider too).
            var lowerField = Expression.Call(
                fieldAccess,
                typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
            var contains = Expression.Call(
                lowerField,
                typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!,
                Expression.Constant(lowerTerm));

            body = body == null ? contains : Expression.OrElse(body, contains);
        }

        body ??= Expression.Constant(false);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression from, ParameterExpression to)
        => new ParameterReplaceVisitor(from, to).Visit(expression)!;

    private static IQueryable<T> OrderByDynamic<T>(IQueryable<T> source, string propertyName, bool descending, bool thenBy)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null)
        {
            throw new BadRequestException($"Sort field '{propertyName}' is not supported.");
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var access = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(access, parameter);

        var methodName = (thenBy ? "ThenBy" : "OrderBy") + (descending ? "Descending" : string.Empty);
        var method = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2);

        var genericMethod = method.MakeGenericMethod(typeof(T), property.PropertyType);
        return (IQueryable<T>)genericMethod.Invoke(null, new object?[] { source, lambda })!;
    }

    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
