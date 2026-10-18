using System.ComponentModel.DataAnnotations;

namespace PRN232.LMS.Services.Models.RequestModels;

/// <summary>
/// Base query parameters shared by every list API
/// (search / sort / paging / field-selection / expansion).
/// </summary>
public abstract class QueryParameters
{
    // [LAB2-02/03] Model binding ánh xạ tên query không phân biệt hoa/thường vào property.
    // ?page=2&size=5 -> Page=2, Size=5. Không gửi -> dùng giá trị mặc định ở property.
    // ?page=abc -> lỗi BINDING vì không đổi được sang int.
    // ?page=0 -> binding thành công (int=0), nhưng lỗi VALIDATION vì vi phạm [Range].
    // Cả hai trả 400 khi controller có [ApiController]. Controller truyền model xuống service;
    // service/QueryOptions mới diễn giải sort, fields, expand và kiểm tra tên được phép.
    /// <summary>Keyword used to filter data (e.g. ?search=nguyen).</summary>
    public string? Search { get; set; }

    /// <summary>Comma-separated sort fields; prefix a field with '-' for descending (e.g. ?sort=fullName,-dateOfBirth).</summary>
    public string? Sort { get; set; }

    /// <summary>Page number, starting at 1 (e.g. ?page=2).</summary>
    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    /// <summary>Page size (e.g. ?size=10).</summary>
    [Range(1, 100)]
    public int Size { get; set; } = 10;

    /// <summary>Comma-separated list of fields to return only (e.g. ?fields=studentId,fullName,email).</summary>
    public string? Fields { get; set; }

    /// <summary>Comma-separated related resources to include (e.g. ?expand=student,course).</summary>
    public string? Expand { get; set; }
}
