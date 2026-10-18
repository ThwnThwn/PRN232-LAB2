using FluentValidation;
using PRN232.LMS.Services.Exceptions;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Middleware;

/// <summary>
/// Global exception handling middleware (Lab 2 requirement).
/// Catches unhandled exceptions, logs them and returns the consistent
/// { success, message, data, errors } envelope — never leaking internal details.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    // [LAB2-05] Middleware này bao các bước phía sau bằng try/catch.
    // Ví dụ service ném NotFoundException -> lan ra action -> _next ném lại tại đây -> trả 404.
    // Nhờ xử lý chung, controller không phải lặp try/catch ở từng action.
    // Chỉ bắt exception phát sinh phía SAU vị trí đăng ký, không bắt lỗi startup trước app.Run.
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            // Exception nghiệp vụ đã được định nghĩa thông điệp an toàn: dùng đúng HTTP status.
            await WriteEnvelopeAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            await WriteEnvelopeAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (BadRequestException ex)
        {
            await WriteEnvelopeAsync(context, StatusCodes.Status400BadRequest, ex.Message,
                ex.Errors.Count > 0 ? ex.Errors : null);
        }
        catch (ValidationException ex)
        {
            // [LAB2-03] Đây là lỗi FluentValidation do ValidateAndThrowAsync trong service ném.
            // Khác lỗi data annotations/ModelState: lỗi đó đã được MVC tự trả 400 trước action.
            // FluentValidation failures -> 400 with per-field messages.
            await WriteEnvelopeAsync(context, StatusCodes.Status400BadRequest, "Request validation failed.",
                ex.Errors.Select(e => e.ErrorMessage).ToList());
        }
        catch (Exception ex)
        {
            // Với lỗi không dự kiến, giữ chi tiết trong log server để chẩn đoán.
            // Client chỉ nhận "Internal server error", không nhận kiểu lỗi hay đường dẫn code.
            // Log the detail server-side but never expose it to the client.
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteEnvelopeAsync(context, StatusCodes.Status500InternalServerError,
                "Internal server error");
        }
    }

    private static async Task WriteEnvelopeAsync(
        HttpContext context,
        int statusCode,
        string message,
        IReadOnlyList<string>? errors = null)
    {
        if (context.Response.HasStarted)
        {
            // Header/body đã gửi thì không thể thay thế an toàn bằng một envelope/status mới.
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // [LAB2-01/05] Ghi JSON trực tiếp, không return ObjectResult và không qua MVC formatter.
        // Vì vậy response lỗi của middleware này luôn JSON dù request có Accept: application/xml.
        // Envelope vẫn cùng success/message/data/errors; định dạng và cấu trúc là hai khái niệm khác.
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message, errors));
    }
}
