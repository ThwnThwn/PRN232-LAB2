using System.Text.Json.Serialization;

namespace PRN232.LMS.Services.Models.ResponseModels;

/// <summary>
/// Consistent API response envelope used by every endpoint:
/// { success, message, data, errors }
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public T? Data { get; set; }

    public IReadOnlyList<string>? Errors { get; set; }

    // Collection endpoints expose pagination beside data, as required by Lab 1.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMetadata? Pagination { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Request processed successfully")
        => new() { Success = true, Message = message, Data = data, Errors = null };

    public static ApiResponse<T> Ok(T data, PaginationMetadata pagination, string message = "Request processed successfully")
        => new() { Success = true, Message = message, Data = data, Errors = null, Pagination = pagination };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null)
        => new() { Success = false, Message = message, Data = default, Errors = errors };
}
