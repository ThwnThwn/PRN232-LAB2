namespace PRN232.LMS.Services.Infrastructure;

/// <summary>One sorting term parsed from the "sort" query parameter, e.g. "fullName" or "-dateOfBirth".</summary>
public sealed record SortTerm(string Field, bool Descending);
