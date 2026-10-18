using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using PRN232.LMS.API.Formatters;
using PRN232.LMS.API.Middleware;
using PRN232.LMS.Repositories;
using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;
using PRN232.LMS.Services;
using PRN232.LMS.Services.Data;
using PRN232.LMS.Services.Models.ResponseModels;
using PRN232.LMS.Services.Options;
using PRN232.LMS.Services.Security;

var builder = WebApplication.CreateBuilder(args);

// BẢN ĐỒ ĐỌC CODE LAB 2 - tìm các nhãn [LAB2-01] đến [LAB2-08] trong solution.
// [LAB2-01] Content Negotiation: object response được ghi thành JSON hay XML.
// [LAB2-02] Model Binding: request -> tham số C# (StudentsController, RequestModels).
// [LAB2-03] Data Validation: kiểm tra input (RequestModels, Validators, StudentService).
// [LAB2-04] Advanced Routing: URL/method -> action, route con, route có tên.
// [LAB2-05] Middleware: các bước bao quanh controller, ghi log và xử lý lỗi.
// [LAB2-06] Authentication & Authorization: xác minh danh tính và kiểm tra quyền.
// [LAB2-07] JWT Security: tạo/kiểm tra chữ ký, claims, hạn dùng, refresh token.
// [LAB2-08] API Versioning: chọn hợp đồng API v1 hoặc v2 theo URL.
//
// Phân biệt hai giai đoạn trong file này:
// builder.Services.Add... đăng ký dịch vụ/cấu hình trước khi app được tạo.
// app.Use.../Map... ở phía dưới thiết lập cách xử lý mỗi HTTP request.

// ------------------------------------------------------------------
// Controllers + content negotiation (Lab 2)
// ------------------------------------------------------------------
builder.Services.AddControllers(options =>
{
    // [LAB2-01] Client đề nghị định dạng NHẬN bằng Accept: application/xml hoặc application/json.
    // Controller return Success(data) vẫn trả object C#; MVC thực thi ObjectResult sau đó
    // mới chọn output formatter phù hợp Accept và các content type được endpoint cho phép.
    // Ví dụ GET /api/v1/students + Accept: application/xml -> formatter XML ghi body XML.
    // Content-Type của REQUEST mô tả body gửi lên, ví dụ JSON khi POST; nó không thay Accept.
    // Khi không yêu cầu kiểu cụ thể, response object thông thường của bài dùng JSON mặc định.

    // [LAB2-01] Không tìm được formatter cho Accept (ví dụ application/pdf) -> HTTP 406.
    // Nếu bỏ cờ này, framework có thể dùng định dạng mặc định thay vì từ chối request.
    options.ReturnHttpNotAcceptable = true;

    // The API never returns raw strings: drop the default text/plain formatter.
    // Otherwise Swashbuckle advertises "text/plain" as the first response content type and
    // Swagger UI sends "Accept: text/plain", which no formatter can write -> 406.
    options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.StringOutputFormatter>();

    // [LAB2-01] Đăng ký bộ GHI XML cho response MVC, gồm cả data dạng dictionary do fields.
    // Xem Prn232XmlOutputFormatter.WriteResponseBodyAsync: đây là nơi tạo body XML thực tế.
    // Những response ghi trực tiếp bằng WriteAsJsonAsync (401/403, exception middleware)
    // dùng JSON cố định và không đi qua cơ chế chọn output formatter của MVC.
    options.OutputFormatters.Add(new Prn232XmlOutputFormatter());

    // [LAB2-01] ProducesAttribute giới hạn/khai báo kiểu RESPONSE của các action: JSON và XML.
    // Attribute này không tự serialize; formatter mới là thành phần ghi nội dung ra mạng.
    // Swagger dựa vào khai báo này để hiển thị lựa chọn media type khi thử endpoint.
    options.Filters.Add(new ProducesAttribute("application/json", "application/xml"));
})
// [LAB2-01] Thêm formatter XML tích hợp; custom output formatter ở trên xử lý object/dictionary.
.AddXmlSerializerFormatters()
// [LAB2-02/03] JSON body chứa property không có trong request model -> lỗi binding/400.
// Ví dụ CreateStudentRequest không có credit: gửi { ..., "credit": -99 } bị từ chối.
.AddJsonOptions(options => options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)
.ConfigureApiBehaviorOptions(options =>
{
    // [LAB2-03] [ApiController] chạy binding + data annotations trước khi action thực thi.
    // ModelState chứa cả lỗi chuyển kiểu (page=abc, JSON sai) lẫn lỗi attribute (EmailAddress...).
    // Khi không hợp lệ, framework gọi factory này và trả 400, không đi vào action/service.
    // Ta thay response lỗi mặc định bằng envelope { success, message, data, errors }.
    // FluentValidation trong service đi theo đường khác: ném ValidationException -> middleware.
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                $"{entry.Key}: {(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage)}"))
            .ToList();

        return new BadRequestObjectResult(ApiResponse<object>.Fail(
            errors.Count > 0 ? "Request validation failed." : "Invalid request.",
            errors.Count > 0 ? errors : null));
    };
});

// [LAB2-08] Đăng ký hệ thống versioning thực sự, không chỉ đặt chữ "v1" trong tên thư mục.
// GET /api/v1/students chọn controller [ApiVersion(1.0)] ở Controllers/V1.
// GET /api/v2/students chọn controller [ApiVersion(2.0)] ở Controllers/V2.
// Hai action có thể dùng chung database/service nhưng trả response model khác nhau.
builder.Services.AddApiVersioning(options =>
{
    // Version mặc định áp dụng khi thiếu thông tin version trên route có thể khớp,
    // ví dụ alias /api/auth/login. Nó không tự tạo alias /api/students cho resource.
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    // Báo các version được hỗ trợ qua response header của cơ chế versioning.
    options.ReportApiVersions = true;
    // Đọc version từ segment URL v{version:apiVersion}, không từ body hay JWT.
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    // Explorer nhóm mô tả endpoint thành v1/v2 để tạo hai tài liệu Swagger.
    options.GroupNameFormat = "'v'VVV";
    // Swagger hiển thị /api/v1/students thay vì /api/v{version}/students.
    options.SubstituteApiVersionInUrl = true;
});

// ------------------------------------------------------------------
// JWT configuration (secret comes from environment variables in Docker)
// ------------------------------------------------------------------
// [LAB2-07] JwtOptions lấy cùng bộ cấu hình mà JwtTokenGenerator dùng để phát hành token.
// Docker truyền Jwt__Key; ASP.NET Core đổi dấu __ thành dấu : trong configuration.
// JWT_SECRET trong Compose -> Jwt__Key trong container -> Configuration["Jwt:Key"].
// Secret ký chỉ ở server; client được nhận token, không được nhận secret này.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured with at least 32 characters. " +
        "In Docker it is supplied by the JWT_SECRET environment variable (.env -> docker-compose.yml).");
}

// ------------------------------------------------------------------
// Data access (Repository layer) + business logic (Service layer)
// ------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

// Background job: removes expired refresh tokens periodically (interval = Jwt:CleanupIntervalMinutes).
builder.Services.AddHostedService<PRN232.LMS.API.BackgroundServices.RefreshTokenCleanupService>();

// ------------------------------------------------------------------
// Authentication & Authorization (JWT Bearer)
// ------------------------------------------------------------------
// [LAB2-06] Authentication trả lời "request này thuộc người dùng nào?".
// Bearer handler đọc Authorization: Bearer <accessToken>; token hợp lệ tạo HttpContext.User.
// Đăng nhập nằm trong AuthService; handler này kiểm tra access token ở các request tiếp theo.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // [LAB2-07] Đọc được payload chưa chứng minh token đáng tin: phải kiểm tra đủ các điều sau.
        // Issuer: token do đúng hệ thống phát hành. Audience: token dành cho đúng API/client này.
        // SigningKey: kiểm tra chữ ký HMAC để phát hiện token bị giả mạo/sửa nội dung.
        // Lifetime: kiểm tra nbf/exp. ClockSkew cho lệch đồng hồ tối đa 30 giây ở biên thời gian.
        // ValidAlgorithms: chỉ chấp nhận HS256 theo cách JwtTokenGenerator đang ký.
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration[$"{JwtOptions.SectionName}:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration[$"{JwtOptions.SectionName}:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ValidAlgorithms = new[] { Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Keep the response format consistent for 401/403 as well.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                // [LAB2-06] 401: thiếu token hoặc token không xác thực được.
                // HandleResponse cho biết ta tự ghi response, thay hành vi challenge mặc định.
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                    "Authentication is required. Provide a valid Bearer access token."));
            },
            OnForbidden = async context =>
            {
                // [LAB2-06] 403: đã xác thực nhưng thiếu quyền, ví dụ Student gọi GET users.
                // Cả hai event này ghi JSON trực tiếp; Accept: application/xml không đổi body này.
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                    "You do not have permission to access this resource."));
            }
        };
    });

// [LAB2-06] Đăng ký bộ kiểm tra quyền theo metadata [Authorize]/[Authorize(Roles = "Admin")].
// Role dùng từ claims của User đã được authentication tạo, không lấy từ body client gửi lên.
builder.Services.AddAuthorization();

// ------------------------------------------------------------------
// Swagger / OpenAPI — two documents (v1, v2) with a JWT "Authorize" button
// ------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // [LAB2-02] Swagger hiển thị tên tham số bằng camelCase: SemesterId -> semesterId,
    // Search -> search, Page -> page. Property C# vẫn giữ PascalCase theo quy ước C#.
    // Query binding không phân biệt hoa/thường nên các tên hiển thị này dùng được khi Try it out.
    options.DescribeAllParametersInCamelCase();
    options.OperationFilter<PRN232.LMS.API.OpenApi.AuthorizationOperationFilter>();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PRN232.LMS API",
        Version = "v1",
        Description = "RESTful API for a Learning Management System (LMS).\n\n" +
                      "Every resource endpoint requires a JWT: call POST /api/v1/auth/login first " +
                      "(admin / Admin@123 or student / Student@123) and paste the access token into Authorize. " +
                      "Students can read resources; only Admin can create, update, delete or list users.\n\n" +
                      "List endpoints support: ?search=..., ?sort=field,-field, ?page=&size=, ?fields=a,b,c and ?expand=related."
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "PRN232.LMS API",
        Version = "v2",
        Description = "API version 2 — same query features as v1 with a richer response contract " +
                      "(students: + studentCode/phoneNumber/age, courses: + enrollmentsCount, " +
                      "semesters: + coursesCount, subjects: + displayName, enrollments: + semesterId/semesterName)."
    });

    // [LAB2-08] Document v1 chỉ chứa mô tả nhóm v1, tương tự cho v2.
    // Swagger mô tả route; việc chọn version khi request chạy do hệ thống versioning ở trên.
    options.DocInclusionPredicate((documentName, apiDescription) => apiDescription.GroupName == documentName);

    // [LAB2-06/07] Cấu hình nút Authorize: Swagger gắn Authorization: Bearer <token> khi gọi thử.
    // Nút này không tự bảo vệ API; server vẫn phải có authentication/authorization và attributes.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token returned by /api/auth/login (no need to type 'Bearer ')."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---- Create + seed the database at startup (with a short retry for Docker races) ----
await DatabaseBootstrapper.InitializeAsync(app.Services, app.Logger);

// [LAB2-05] Request API đi theo thứ tự đăng ký:
// Logging -> Exception handler -> Routing -> Authentication -> Authorization -> MVC endpoint.
// Trong MVC: binding + validation -> controller -> service/repository -> formatter ghi response.
// Khi hoàn tất, code sau await _next(context) chạy ngược ra các middleware phía ngoài.
// Logging đứng ngoài exception handler để ghi mã cuối cùng, ví dụ 500 đã được xử lý.
// Một middleware có thể dừng sớm: thiếu quyền -> 401/403, controller không được gọi.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Swagger is enabled in every environment so it can be demoed from Docker as well.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PRN232.LMS API v1");
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "PRN232.LMS API v2");
});

// [LAB2-04] Routing chọn endpoint dựa trên HTTP method, URL, constraints và version metadata.
// [LAB2-06] Authentication phải chạy trước Authorization để có User/claims khi kiểm tra quyền.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Ánh xạ các action có attribute routing vào hệ thống endpoint.
app.MapControllers();

// Anonymous health endpoint (handy for Docker health checks).
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.Run();

/// <summary>
/// Creates the database, seeds the Lab 1 data (5 semesters / 50 students / 10 subjects / 20 courses /
/// 500 enrollments) and the Lab 2 accounts (admin / Admin@123, student / Student@123).
/// </summary>
public static partial class DatabaseBootstrapper
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await DbSeeder.EnsureSeededAsync(db);

                var users = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await UserSeeder.EnsureSeededAsync(users, passwordHasher);

                logger.LogInformation("Database is ready and seeded.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database not reachable yet (attempt {Attempt}/{Max}). Retrying in 3 seconds...", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not initialize the database after {Max} attempts.", maxAttempts);
                throw;
            }
        }
    }
}
