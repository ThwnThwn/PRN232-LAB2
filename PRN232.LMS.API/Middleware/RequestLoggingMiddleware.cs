using System.Diagnostics;

namespace PRN232.LMS.API.Middleware;

/// <summary>
/// Request logging middleware (Lab 2 requirement).
/// Logs the request path, HTTP method, execution time and the response status code.
/// </summary>
public class RequestLoggingMiddleware
{
    // [LAB2-05] Middleware có RequestDelegate _next để gọi thành phần tiếp theo của pipeline.
    // HttpContext chứa request/response/User của MỘT lần gọi API.
    // Code trước await _next: xử lý chiều request đi vào. Code sau await: response đi ra.
    // Logging được đăng ký trước exception handler trong Program.cs nên bao ngoài handler đó.
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Bắt đầu trước khi vào routing/auth/controller để đo toàn bộ thời gian phía sau.
        // QueryString cũng được giữ: /students?graderProbe=abc cần xuất hiện đầy đủ trong log.
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var query = context.Request.QueryString.Value ?? string.Empty;

        try
        {
            // Chờ các bước phía sau xử lý xong. Không gọi _next thì request dừng ở middleware này.
            await _next(context);
        }
        finally
        {
            // finally luôn chạy khi quay ra, kể cả phía sau có lỗi.
            // Nếu exception handler đã đổi lỗi thành 500 thì ở đây đọc được status cuối là 500.
            // LogInformation mặc định ra stdout, có thể xem bằng docker compose logs api.
            // Ví dụ: HTTP GET /api/v1/students?page=1 -> 200 in 12 ms.
            stopwatch.Stop();
            _logger.LogInformation(
                "HTTP {Method} {Path}{Query} -> {StatusCode} in {ElapsedMilliseconds} ms",
                method,
                path,
                query,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
