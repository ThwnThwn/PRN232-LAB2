using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers;

/// <summary>
/// Base class for every API controller.
/// Controllers must NOT contain business logic: they only call the Service layer and
/// wrap the result in the consistent { success, message, data, errors } envelope.
/// Exceptions are translated into HTTP responses by <c>GlobalExceptionHandlingMiddleware</c>.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // [LAB2-01] ĐÂY LÀ CHỖ RETURN RESPONSE nhưng chưa chuyển object thành JSON/XML.
    // StatusCode(code, object) tạo ObjectResult. Sau khi action return, MVC chọn output formatter
    // theo Accept rồi mới serialize object vào response body. Ok(object) cũng theo cơ chế này.
    // Vì vậy không cần if (Accept == "xml") trong từng action, và không JsonSerializer.Serialize
    // trước khi return: hãy giữ dữ liệu là object để formatter xử lý đúng kiểu.
    // ApiResponse<T>.Ok là hàm tạo envelope của bài, khác ControllerBase.Ok tạo HTTP 200 result.
    protected IActionResult Success<T>(T data, string message = "Request processed successfully", int statusCode = StatusCodes.Status200OK)
    {
        if (data is PagedResult<object> paged)
        {
            // List API giữ data là mảng, pagination ở cùng cấp success/message/data/errors.
            // Envelope là cấu trúc dữ liệu; JSON/XML là cách biểu diễn cùng cấu trúc đó.
            return StatusCode(statusCode, ApiResponse<IReadOnlyList<object>>.Ok(paged.Items, paged.Pagination, message));
        }

        return StatusCode(statusCode, ApiResponse<T>.Ok(data, message));
    }

    protected IActionResult Failure(int statusCode, string message, IReadOnlyList<string>? errors = null)
        => StatusCode(statusCode, ApiResponse<object>.Fail(message, errors));

    // [LAB2-04/08] Named route tạo Location trỏ đến resource vừa thêm, thay vì nối URL bằng tay.
    // Ví dụ routeName = "GetStudentById", id = 51, version = 1 -> /api/v1/students/51.
    // CreatedAtRoute trả HTTP 201 cùng body envelope; body vẫn được content negotiation xử lý.
    protected IActionResult CreatedAtRouteResponse<T>(string routeName, object routeValues, T data, string message)
    {
        var values = new RouteValueDictionary(routeValues)
        {
            ["version"] = RouteData.Values["version"]
        };
        return CreatedAtRoute(routeName, values, ApiResponse<T>.Ok(data, message));
    }
}
